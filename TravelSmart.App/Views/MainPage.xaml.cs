using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TravelSmart.App.Services;
using TravelSmart.App.Models;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Plugin.Maui.Audio;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace TravelSmart.App.Views;

public partial class MainPage : ContentPage
{
    private readonly DataService _dataService;
    private List<PoiModel> _pois = new();

    private HashSet<string> _playedAudioPois = new();

    private IDispatcherTimer _timer;
    private Dictionary<Pin, PoiModel> _pinPoiMap = new();

    private const string ApiBaseUrl = "http://10.0.2.2:5088/api";

    private Location _currentSelectedLocation;
    private string _currentPoiTts = "";

    private CancellationTokenSource _ttsCancellationTokenSource;
    private IAudioPlayer _audioPlayer;

    private Queue<PoiModel> _audioQueue = new();
    private bool _isAudioPlaying = false;

    public MainPage() { InitializeComponent(); _dataService = new DataService(); }

    private async Task ReloadMapData()
    {
        await _dataService.SyncFromServerAsync();
        _pois = await _dataService.GetPOIsAsync();
        FilterMapPins("");
    }

    private void FilterMapPins(string keyword)
    {
        MyMap.Pins.Clear(); _pinPoiMap.Clear();
        var filtered = string.IsNullOrWhiteSpace(keyword) ? _pois : _pois.Where(p => p.Name.ToLower().Contains(keyword.ToLower())).ToList();
        foreach (var poi in filtered)
        {
            var pin = new Pin { Label = poi.Name, Address = poi.Description, Type = PinType.Place, Location = new Location(poi.Latitude, poi.Longitude) };
            pin.MarkerClicked += OnPinClicked; _pinPoiMap[pin] = poi; MyMap.Pins.Add(pin);
        }
    }

    private void OnSearchButtonPressed(object sender, EventArgs e) { FilterMapPins(SearchPoi.Text); }
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) { if (string.IsNullOrWhiteSpace(e.NewTextValue)) FilterMapPins(""); }
    private async void OnNotificationClicked(object sender, EventArgs e) { BadgeNoti.IsVisible = false; await Navigation.PushAsync(new NotificationsPage()); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(10.7605, 106.7025), Distance.FromKilometers(1)));
        await SyncWithServer();
        await ReloadMapData();
        await StartTrackingGPS();
    }

    public async Task SyncWithServer()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("authToken");
            if (string.IsNullOrEmpty(token))
            {
                await SecureStorage.Default.SetAsync("role", "Guest");
                return;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{ApiBaseUrl}/Auth/sync");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<SyncDataDto>();
                if (data != null)
                {
                    await SecureStorage.Default.SetAsync("role", data.roleId == 1 ? "Admin" : (data.roleId == 2 ? "Merchant" : "User"));
                    BadgeNoti.IsVisible = data.unreadCount > 0;
                    LblUnreadCount.Text = data.unreadCount > 9 ? "9+" : data.unreadCount.ToString();
                }
            }
        }
        catch { }
    }
    public class SyncDataDto { public int roleId { get; set; } public int unreadCount { get; set; } }

    private async Task OpenBottomSheetForPoi(Pin pin, PoiModel poi)
    {
        _currentSelectedLocation = pin.Location;
        LblPoiName.Text = poi.Name;
        LblPoiAddress.Text = "Đang tải thông tin chi tiết...";
        _currentPoiTts = poi.TtsContent;

        // 🔥 MÓC LINK ẢNH TỪ SERVER ĐẬP VÀO ĐÂY
        if (!string.IsNullOrEmpty(poi.ImageUrl))
        {
            // Fix lỗi máy ảo Android không chịu hiểu chữ localhost
            ImgPoi.Source = poi.ImageUrl.Replace("localhost", "10.0.2.2");
        }
        else
        {
            // Trả về ảnh mặc định nếu quán chưa có ảnh
            ImgPoi.Source = "https://images.unsplash.com/photo-1514933651103-005eec06c04b?q=80&w=800&auto=format&fit=crop";
        }

        ContainerMenu.Children.Clear(); ContainerReviews.Children.Clear(); EntryReview.Text = "";
        FrameWriteReview.IsVisible = await SecureStorage.Default.GetAsync("role") == "User";
        SheetOverlay.IsVisible = true; SheetOverlay.InputTransparent = false;

        await BottomSheet.TranslateTo(0, 0, 300, Easing.CubicOut);
        await FetchPoiDetails(poi.Id, poi.Description);
        SaveToHistory(poi.Name, poi.Description);
    }

    private void SaveToHistory(string name, string address)
    {
        var historyStr = Preferences.Default.Get("AppHistory", "[]");
        var list = JsonSerializer.Deserialize<List<HistoryPage.HistoryItem>>(historyStr) ?? new();
        if (list.Count == 0 || list[0].PoiName != name)
        {
            list.Insert(0, new HistoryPage.HistoryItem { PoiName = name, Address = address, Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm") });
            Preferences.Default.Set("AppHistory", JsonSerializer.Serialize(list));
        }
    }

    private async Task StartTrackingGPS()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted) return;

        _timer = Application.Current.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.Tick += async (s, e) =>
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(2));
                var location = await Geolocation.GetLocationAsync(request);
                if (location != null) await CheckGeofence(location);
            }
            catch { }
        };
        _timer.Start();
    }

    private async Task CheckGeofence(Location userLoc)
    {
        foreach (var poi in _pois)
        {
            var poiLoc = new Location(poi.Latitude, poi.Longitude);
            var distanceKm = Location.CalculateDistance(userLoc, poiLoc, DistanceUnits.Kilometers);

            if (distanceKm <= 0.05)
            {
                if (!_playedAudioPois.Contains(poi.Id))
                {
                    _playedAudioPois.Add(poi.Id);
                    var pin = _pinPoiMap.Keys.FirstOrDefault(p => p.Location.Latitude == poi.Latitude && p.Location.Longitude == poi.Longitude);
                    if (pin != null)
                    {
                        await OpenBottomSheetForPoi(pin, poi);
                        QueueAudioAndPlay(poi);
                    }
                }
            }
            else if (distanceKm > 0.1)
            {
                _playedAudioPois.Remove(poi.Id);
            }
        }
    }

    private void StopSpeech()
    {
        try
        {
            if (_ttsCancellationTokenSource != null && !_ttsCancellationTokenSource.IsCancellationRequested)
            {
                _ttsCancellationTokenSource.Cancel();
            }
            if (_audioPlayer != null && _audioPlayer.IsPlaying)
            {
                _audioPlayer.Stop();
                _audioPlayer.Dispose();
                _audioPlayer = null;
            }
            _audioQueue.Clear();
            _isAudioPlaying = false;
        }
        catch { }
    }

    private void QueueAudioAndPlay(PoiModel poi)
    {
        _audioQueue.Enqueue(poi);
        if (!_isAudioPlaying) { _ = PlayNextAudioInQueue(); }
    }

    private async Task PlayNextAudioInQueue()
    {
        if (_audioQueue.Count == 0)
        {
            _isAudioPlaying = false;
            return;
        }

        _isAudioPlaying = true;
        var poi = _audioQueue.Dequeue();

        if (!string.IsNullOrEmpty(poi.AudioUrl))
        {
            try
            {
                var localFilePath = Path.Combine(FileSystem.CacheDirectory, $"{poi.Id}.mp3");

                if (!File.Exists(localFilePath) && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    using var httpClient = new HttpClient();
                    var audioBytes = await httpClient.GetByteArrayAsync(poi.AudioUrl.Replace("localhost", "10.0.2.2"));

                    await File.WriteAllBytesAsync(localFilePath, audioBytes);
                }

                if (File.Exists(localFilePath))
                {
                    var stream = File.OpenRead(localFilePath);
                    _audioPlayer = AudioManager.Current.CreatePlayer(stream);
                    _audioPlayer.PlaybackEnded += (s, e) =>
                    {
                        stream.Dispose();
                        _ = PlayNextAudioInQueue();
                    };
                    _audioPlayer.Play();
                    return;
                }
            }
            catch { }
        }

        await SmartSpeak(poi.TtsContent);
        _ = PlayNextAudioInQueue();
    }

    private async Task SmartSpeak(string text, string forceLangCode = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        _ttsCancellationTokenSource = new CancellationTokenSource();
        string targetLang = forceLangCode ?? Preferences.Default.Get("DefaultLang", "vi");
        string textToSpeak = text;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet && targetLang != "vi")
        {
            textToSpeak = "Ứng dụng ngoại tuyến. " + text;
            targetLang = "vi";
        }
        else if (targetLang != "vi")
        {
            try
            {
                using var client = new HttpClient();
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";
                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                textToSpeak = doc.RootElement[0][0][0].GetString() ?? text;
            }
            catch { }
        }

        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var locale = locales.FirstOrDefault(l => l.Language.StartsWith(targetLang, StringComparison.OrdinalIgnoreCase));

        try
        {
            await TextToSpeech.Default.SpeakAsync(textToSpeak, new SpeechOptions { Locale = locale, Pitch = 1.0f, Volume = 1.0f }, _ttsCancellationTokenSource.Token);
        }
        catch { }
    }

    private void OnLangClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && !string.IsNullOrEmpty(_currentPoiTts))
        {
            StopSpeech();
            _ = SmartSpeak(_currentPoiTts, btn.CommandParameter.ToString());
        }
    }

    private async void OnPinClicked(object sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;
        if (sender is Pin pin && _pinPoiMap.TryGetValue(pin, out PoiModel poi))
        {
            StopSpeech();
            await OpenBottomSheetForPoi(pin, poi);
            QueueAudioAndPlay(poi);
        }
    }

    private async void CloseBottomSheet(object sender, EventArgs e)
    {
        await BottomSheet.TranslateTo(0, 700, 300, Easing.CubicIn);
        SheetOverlay.InputTransparent = true; SheetOverlay.IsVisible = false;

        StopSpeech();
    }

    private async Task FetchPoiDetails(string poiId, string description)
    {
        try
        {
            LblPoiAddress.Text = description; using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            var details = await client.GetFromJsonAsync<PoiDetailDto>($"{ApiBaseUrl}/Pois/{poiId}");
            if (details != null)
            {
                if (details.menu != null && details.menu.Count > 0)
                {
                    foreach (var item in details.menu)
                    {
                        ContainerMenu.Children.Add(new HorizontalStackLayout
                        {
                            Children = {
                            new Label { Text = item.itemName, FontAttributes = FontAttributes.Bold, WidthRequest = 200, TextColor = Colors.Black },
                            new Label { Text = $"{item.price:N0} đ", TextColor = Colors.Green, FontAttributes = FontAttributes.Bold }
                        }
                        });
                    }
                }
                else ContainerMenu.Children.Add(new Label { Text = "Chưa có thực đơn.", FontAttributes = FontAttributes.Italic, TextColor = Colors.Gray });

                if (details.reviews != null && details.reviews.Count > 0)
                {
                    foreach (var rev in details.reviews)
                    {
                        ContainerReviews.Children.Add(new VerticalStackLayout
                        {
                            Spacing = 2,
                            Children = {
                            new Label { Text = new string('⭐', rev.rating), TextColor = Colors.Orange },
                            new Label { Text = $"\"{rev.comment}\"", FontAttributes = FontAttributes.Italic, TextColor = Colors.DarkGray }
                        }
                        });
                    }
                }
                else ContainerReviews.Children.Add(new Label { Text = "Chưa có đánh giá.", FontAttributes = FontAttributes.Italic, TextColor = Colors.Gray });
            }
        }
        catch { LblPoiAddress.Text = "Lỗi kết nối mạng!"; }
    }

    private async void OnSubmitReviewClicked(object sender, EventArgs e)
    {
        var role = await SecureStorage.Default.GetAsync("role");
        if (role == "Guest" || string.IsNullOrEmpty(role))
        {
            bool answer = await DisplayAlert("Yêu cầu đăng nhập", "Bạn cần đăng nhập để viết đánh giá. Đến trang Đăng nhập?", "Đăng nhập", "Hủy");
            if (answer) { await Navigation.PushModalAsync(new LoginPage()); }
            return;
        }

        if (string.IsNullOrEmpty(EntryReview.Text)) { await DisplayAlert("Lỗi", "Hãy nhập bình luận!", "OK"); return; }
        var token = await SecureStorage.Default.GetAsync("authToken");
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var selectedPoi = _pinPoiMap.Values.FirstOrDefault(p => p.Latitude == _currentSelectedLocation.Latitude && p.Longitude == _currentSelectedLocation.Longitude);
        if (selectedPoi != null)
        {
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/Pois/{selectedPoi.Id}/review", new { Rating = (int)StepperRating.Value, Comment = EntryReview.Text });
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Thành công", "Đã gửi đánh giá!", "OK"); EntryReview.Text = "";
                ContainerMenu.Children.Clear(); ContainerReviews.Children.Clear();
                await FetchPoiDetails(selectedPoi.Id, selectedPoi.Description);
            }
            else await DisplayAlert("Lỗi", "Có lỗi xảy ra.", "OK");
        }
    }

    private async void OnGetDirectionsClicked(object sender, EventArgs e) { if (_currentSelectedLocation != null) await Microsoft.Maui.ApplicationModel.Map.OpenAsync(_currentSelectedLocation, new MapLaunchOptions { Name = LblPoiName.Text, NavigationMode = NavigationMode.Driving }); }

    protected override void OnDisappearing() { base.OnDisappearing(); if (_timer != null && _timer.IsRunning) _timer.Stop(); }

    private async void OnRefreshMapClicked(object sender, EventArgs e) { await SyncWithServer(); await ReloadMapData(); }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted) return;

        await Navigation.PushModalAsync(new ScannerPage(async (qrCodeData) =>
        {
            var poi = _pois.FirstOrDefault(p => p.QrCodeKey == qrCodeData);
            if (poi != null)
            {
                var pin = _pinPoiMap.Keys.FirstOrDefault(p => p.Location.Latitude == poi.Latitude && p.Location.Longitude == poi.Longitude);
                if (pin != null)
                {
                    await Task.Delay(500);
                    StopSpeech();
                    await OpenBottomSheetForPoi(pin, poi);
                    QueueAudioAndPlay(poi);
                }
            }
            else await DisplayAlert("Rất tiếc", "Mã QR này không thuộc hệ thống!", "OK");
        }));
    }

    private async void OnHistoryClicked(object sender, EventArgs e) { await Navigation.PushAsync(new HistoryPage()); }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        var role = await SecureStorage.Default.GetAsync("role");
        if (role == "Guest" || string.IsNullOrEmpty(role))
        {
            bool answer = await DisplayAlert("Yêu cầu đăng nhập", "Bạn cần có tài khoản để xem Hồ sơ cá nhân. Đến trang Đăng nhập?", "Đăng nhập", "Để sau");
            if (answer) { await Navigation.PushModalAsync(new LoginPage()); }
            return;
        }

        await Navigation.PushAsync(new ProfilePage());
    }

    public class PoiDetailDto { public List<MenuItemDto> menu { get; set; } public List<ReviewDto> reviews { get; set; } }
    public class MenuItemDto { public string itemName { get; set; } public decimal price { get; set; } }
    public class ReviewDto { public int rating { get; set; } public string comment { get; set; } }
}