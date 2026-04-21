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
using Microsoft.AspNetCore.SignalR.Client;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace TravelSmart.App.Views;

public partial class MainPage : ContentPage
{
    private readonly DataService _dataService;
    private List<PoiModel> _pois = new();

    private Dictionary<string, DateTime> _poiCooldowns = new();
    private Dictionary<string, DateTime> _entryTimes = new();

    private IDispatcherTimer _timer;
    private Dictionary<Pin, PoiModel> _pinPoiMap = new();

    private Location _currentSelectedLocation;
    private string _currentPoiTts = "";
    private PoiModel _currentActivePoi;

    private CancellationTokenSource _ttsCancellationTokenSource;
    private IAudioPlayer _audioPlayer;

    private Circle _currentRadar;
    private Polyline _currentTourLine;

    private HubConnection _hubConnection;

    public MainPage() { InitializeComponent(); _dataService = new DataService(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(500);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try { MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(10.7605, 106.7025), Distance.FromKilometers(1))); } catch { }
        });

        if (!Preferences.Default.ContainsKey("DefaultLang"))
        {
            string action = await DisplayActionSheet("Chọn ngôn ngữ ưu tiên / Choose your default language", null, null, "🇻🇳 Tiếng Việt", "🇬🇧 English", "🇯🇵 日本語");
            string langCode = "vi";
            if (action == "🇬🇧 English") langCode = "en";
            else if (action == "🇯🇵 日本語") langCode = "ja";
            Preferences.Default.Set("DefaultLang", langCode);
        }

        await ConnectToSignalR();
        await SyncWithServer();
        await ReloadMapData();
        await StartTrackingGPS();
    }

    private async Task ConnectToSignalR()
    {
        try
        {
            string hubUrl = AppConfig.ApiBaseUrl.Replace("/api", "/travelhub?clientType=app");
            if (_hubConnection == null)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.Headers.Add("ngrok-skip-browser-warning", "true");
                    })
                    .WithAutomaticReconnect()
                    .Build();
            }

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
            }
        }
        catch { }
    }

    private async Task ReloadMapData()
    {
        _pois = await _dataService.GetPOIsAsync();
        MainThread.BeginInvokeOnMainThread(() => { FilterMapPins(""); });
    }

    private void FilterMapPins(string keyword)
    {
        MyMap.Pins.Clear();
        _pinPoiMap.Clear();
        ClearHighlight();

        var filtered = string.IsNullOrWhiteSpace(keyword) ? _pois : _pois.Where(p => p.Name.ToLower().Contains(keyword.ToLower())).ToList();
        foreach (var poi in filtered)
        {
            var pin = new Pin { Label = poi.Name, Address = poi.Description, Type = PinType.Place, Location = new Location(poi.Latitude, poi.Longitude) };
            pin.MarkerClicked += OnPinClicked;
            _pinPoiMap[pin] = poi;
            MyMap.Pins.Add(pin);
        }
    }

    private void OnSearchButtonPressed(object sender, EventArgs e) { FilterMapPins(SearchPoi.Text); }
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) { if (string.IsNullOrWhiteSpace(e.NewTextValue)) FilterMapPins(""); }
    private async void OnNotificationClicked(object sender, EventArgs e) { BadgeNoti.IsVisible = false; await Navigation.PushAsync(new NotificationsPage()); }

    public class TourDto { public Guid TourId { get; set; } public string Name { get; set; } }
    public class TourDetailDto { public Guid PoiId { get; set; } public string Name { get; set; } public int Order { get; set; } }

    private async void OnTourClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new TourListPage(async (selectedTourId) =>
        {
            if (string.IsNullOrEmpty(selectedTourId))
            {
                if (_currentTourLine != null) { MyMap.MapElements.Remove(_currentTourLine); _currentTourLine = null; }
                FilterMapPins("");
                return;
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                var details = await client.GetFromJsonAsync<List<TourDetailDto>>($"{AppConfig.ApiBaseUrl}/Tours/{selectedTourId}/details");
                if (details != null && details.Any())
                {
                    DrawTourRoute(details);
                }
                else
                {
                    await DisplayAlert("Thông báo", "Tour này chưa có địa điểm nào.", "OK");
                }
            }
            catch { await DisplayAlert("Lỗi", "Không tải được lịch trình Tour.", "OK"); }
        }));
    }

    private void DrawTourRoute(List<TourDetailDto> tourDetails)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (_currentTourLine != null) MyMap.MapElements.Remove(_currentTourLine);
                ClearHighlight();
                MyMap.Pins.Clear();
                _pinPoiMap.Clear();

                _currentTourLine = new Polyline { StrokeColor = Color.FromArgb("#1565C0"), StrokeWidth = 8 };

                foreach (var td in tourDetails.OrderBy(t => t.Order))
                {
                    var poi = _pois.FirstOrDefault(p => p.Id.ToLower() == td.PoiId.ToString().ToLower());
                    if (poi != null)
                    {
                        var loc = new Location(poi.Latitude, poi.Longitude);
                        _currentTourLine.Geopath.Add(loc);

                        var pin = new Pin { Label = $"Trạm {td.Order}: {poi.Name}", Address = poi.Description, Type = PinType.Place, Location = loc };
                        pin.MarkerClicked += OnPinClicked;
                        _pinPoiMap[pin] = poi;
                        MyMap.Pins.Add(pin);
                    }
                }

                MyMap.MapElements.Add(_currentTourLine);

                if (_currentTourLine.Geopath.Any())
                {
                    MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(_currentTourLine.Geopath.First(), Distance.FromKilometers(1.5)));
                }
            }
            catch { }
        });
    }

    public async Task SyncWithServer()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("authToken");
            if (string.IsNullOrEmpty(token)) { await SecureStorage.Default.SetAsync("role", "Guest"); return; }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{AppConfig.ApiBaseUrl}/Auth/sync");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<SyncDataDto>();
                if (data != null)
                {
                    await SecureStorage.Default.SetAsync("role", data.roleId == 1 ? "Admin" : (data.roleId == 2 ? "Merchant" : "User"));
                    MainThread.BeginInvokeOnMainThread(() => {
                        BadgeNoti.IsVisible = data.unreadCount > 0;
                        LblUnreadCount.Text = data.unreadCount > 9 ? "9+" : data.unreadCount.ToString();
                    });
                }
            }
        }
        catch { }
    }
    public class SyncDataDto { public int roleId { get; set; } public int unreadCount { get; set; } }

    private async Task OpenBottomSheetForPoi(Pin pin, PoiModel poi)
    {
        _currentSelectedLocation = pin?.Location;
        _currentActivePoi = poi;

        LblPoiName.Text = poi.Name;
        LblPoiAddress.Text = "Đang tải thông tin chi tiết...";
        _currentPoiTts = poi.TtsContent;

        if (!string.IsNullOrEmpty(poi.ImageUrl))
            ImgPoi.Source = poi.ImageUrl.Replace("https://localhost:7008", "http://10.0.2.2:5088").Replace("localhost", "10.0.2.2");
        else
            ImgPoi.Source = "https://images.unsplash.com/photo-1514933651103-005eec06c04b?q=80&w=800&auto=format&fit=crop";

        ContainerMenu.Children.Clear(); ContainerReviews.Children.Clear(); EntryReview.Text = "";
        FrameWriteReview.IsVisible = await SecureStorage.Default.GetAsync("role") == "User";
        SheetOverlay.IsVisible = true; SheetOverlay.InputTransparent = false;

        await BottomSheet.TranslateTo(0, 0, 300, Easing.CubicOut);

        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("JoinPoi", poi.Id.ToString());

        await FetchPoiDetails(poi.Id, poi.Description);

        _ = _dataService.AddHistoryAsync(poi);
    }

    private async Task StartTrackingGPS()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return;

            MainThread.BeginInvokeOnMainThread(() => { try { MyMap.IsShowingUser = true; } catch { } });

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
        catch { }
    }

    private void HighlightActivePoi(Location poiLocation, double radiusKm)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (_currentRadar != null) MyMap.MapElements.Remove(_currentRadar);

                _currentRadar = new Circle
                {
                    Center = poiLocation,
                    Radius = Distance.FromKilometers(radiusKm),
                    StrokeColor = Color.FromArgb("#FF0000"),
                    StrokeWidth = 6,
                    FillColor = Color.FromArgb("#33FF0000")
                };

                MyMap.MapElements.Add(_currentRadar);
            }
            catch { }
        });
    }

    private void ClearHighlight()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (_currentRadar != null)
                {
                    MyMap.MapElements.Remove(_currentRadar);
                    _currentRadar = null;
                }
            }
            catch { }
        });
    }

    private async Task CheckGeofence(Location userLoc)
    {
        var pOfEnteredPois = new List<Tuple<PoiModel, double>>();
        double currentRadiusKm = Preferences.Default.Get("GeofenceRadius", 50) / 1000.0;

        foreach (var poi in _pois)
        {
            var poiLoc = new Location(poi.Latitude, poi.Longitude);
            var distanceKm = Location.CalculateDistance(userLoc, poiLoc, DistanceUnits.Kilometers);

            if (distanceKm <= currentRadiusKm)
            {
                pOfEnteredPois.Add(new Tuple<PoiModel, double>(poi, distanceKm));
            }
        }

        if (pOfEnteredPois.Any())
        {
            // 🔥 ĐÃ NHÚNG THUẬT TOÁN XỬ LÝ TRÙNG LẶP (COLLISION RESOLUTION ALGORITHM)
            var closestData = pOfEnteredPois
                .OrderBy(x => x.Item2) // Ưu tiên 1: Gần nhất theo khoảng cách
                .ThenByDescending(x => x.Item1.Priority) // Ưu tiên 2: Trả tiền làm Premium (Dùng tạm cột Priority)
                .ThenBy(x => x.Item1.Name) // Ưu tiên 3: Theo tên quán A-Z
                .First();

            var poi = closestData.Item1;

            bool isCoolingDown = _poiCooldowns.ContainsKey(poi.Id) && (DateTime.Now - _poiCooldowns[poi.Id]).TotalMinutes < 3;

            if (!isCoolingDown)
            {
                if (_currentActivePoi != null && _currentActivePoi.Id != poi.Id) StopSpeech();

                _poiCooldowns[poi.Id] = DateTime.Now;
                _entryTimes[poi.Id] = DateTime.Now;

                var pin = _pinPoiMap.Keys.FirstOrDefault(p => p.Location.Latitude == poi.Latitude && p.Location.Longitude == poi.Longitude);

                HighlightActivePoi(new Location(poi.Latitude, poi.Longitude), currentRadiusKm);

                _ = PlayPoiAudio(poi);
                MainThread.BeginInvokeOnMainThread(async () => { await OpenBottomSheetForPoi(pin, poi); });
            }
        }

        foreach (var poiId in _entryTimes.Keys.ToList())
        {
            var poi = _pois.FirstOrDefault(p => p.Id == poiId);
            if (poi == null) continue;

            var poiLoc = new Location(poi.Latitude, poi.Longitude);
            var distanceKm = Location.CalculateDistance(userLoc, poiLoc, DistanceUnits.Kilometers);

            if (distanceKm > (currentRadiusKm + 0.05))
            {
                var durationMinutes = (DateTime.Now - _entryTimes[poiId]).TotalMinutes;

                if (_currentActivePoi != null && _currentActivePoi.Id == poiId) ClearHighlight();

                _entryTimes.Remove(poiId);
            }
        }
    }

    private void OnLangClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && _currentActivePoi != null)
        {
            string param = btn.CommandParameter?.ToString()?.ToUpper() ?? "VN";
            string tempLangCode = "vi";
            if (param == "EN" || param == "ENGLISH") tempLangCode = "en";
            else if (param == "JP" || param == "JA" || param == "JAPANESE") tempLangCode = "ja";

            _ = PlayPoiAudio(_currentActivePoi, tempLangCode);
        }
    }

    private void StopSpeech()
    {
        try
        {
            if (_ttsCancellationTokenSource != null && !_ttsCancellationTokenSource.IsCancellationRequested)
                _ttsCancellationTokenSource.Cancel();

            if (_audioPlayer != null && _audioPlayer.IsPlaying)
            {
                _audioPlayer.Stop();
                _audioPlayer.Dispose();
            }
            _audioPlayer = null;
        }
        catch { }
    }

    private async Task PlayPoiAudio(PoiModel poi, string tempLangOverride = null)
    {
        StopSpeech();

        string targetLang = tempLangOverride ?? Preferences.Default.Get("DefaultLang", "vi");
        string safeBaseUrl = AppConfig.ApiBaseUrl.Replace("/api", "").Replace("https://localhost:7008", "http://10.0.2.2:5088").Replace("localhost", "10.0.2.2");

        if (targetLang == "vi")
        {
            if (!string.IsNullOrEmpty(poi.AudioUrl))
            {
                try
                {
                    string customAudioUrl = poi.AudioUrl.Replace("https://localhost:7008", "http://10.0.2.2:5088").Replace("localhost", "10.0.2.2");
                    if (!customAudioUrl.StartsWith("http")) customAudioUrl = $"{safeBaseUrl}{customAudioUrl}";

                    string localManualPath = Path.Combine(FileSystem.CacheDirectory, $"manual_{poi.Id}.mp3");

                    if (!File.Exists(localManualPath))
                    {
                        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true };
                        using var httpClient = new HttpClient(handler);
                        httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

                        var audioBytes = await httpClient.GetByteArrayAsync(customAudioUrl);
                        await File.WriteAllBytesAsync(localManualPath, audioBytes);
                    }

                    var stream = File.OpenRead(localManualPath);
                    _audioPlayer = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(stream);
                    _audioPlayer.Play();
                    return;
                }
                catch { Console.WriteLine("Lỗi load Audio gốc, chuyển xuống AI."); }
            }

            string localFilePathVi = Path.Combine(FileSystem.CacheDirectory, $"{poi.Id}_vi.mp3");
            if (File.Exists(localFilePathVi))
            {
                try
                {
                    var stream = File.OpenRead(localFilePathVi);
                    _audioPlayer = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(stream);
                    _audioPlayer.Play();
                    return;
                }
                catch { }
            }

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    string expectedAudioUrl = $"{safeBaseUrl}/audio/{poi.Id}_vi.mp3";
                    var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true };
                    using var httpClient = new HttpClient(handler);
                    httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

                    var audioBytes = await httpClient.GetByteArrayAsync(expectedAudioUrl);
                    await File.WriteAllBytesAsync(localFilePathVi, audioBytes);

                    var stream = File.OpenRead(localFilePathVi);
                    _audioPlayer = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(stream);
                    _audioPlayer.Play();
                    return;
                }
                catch { }
            }
        }

        await SmartSpeak(poi.TtsContent, targetLang);
    }

    private async Task SmartSpeak(string text, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        _ttsCancellationTokenSource = new CancellationTokenSource();
        string textToSpeak = text;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet && targetLang != "vi")
        {
            textToSpeak = targetLang == "en" ? "App is offline." : "オフラインです";
            targetLang = "vi";
        }
        else if (targetLang != "vi")
        {
            try
            {
                string cleanText = text.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
                if (cleanText.Length > 1000) cleanText = cleanText.Substring(0, 995) + "...";

                var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (msg, cert, chain, err) => true };
                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl={targetLang}&dt=t&q={Uri.EscapeDataString(cleanText)}";
                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                textToSpeak = "";
                foreach (var chunk in doc.RootElement[0].EnumerateArray())
                {
                    if (chunk[0].ValueKind == JsonValueKind.String) textToSpeak += chunk[0].GetString();
                }

                if (string.IsNullOrWhiteSpace(textToSpeak)) throw new Exception("Rỗng");
            }
            catch (Exception ex)
            {
                textToSpeak = targetLang == "en" ? "Translation error." : "翻訳エラー";
                Console.WriteLine("Lỗi dịch: " + ex.Message);
            }
        }

        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var locale = locales.FirstOrDefault(l => l.Language.StartsWith(targetLang, StringComparison.OrdinalIgnoreCase));

        try
        {
            float ttsSpeed = Preferences.Default.Get("TtsSpeed", 1.0f);
            await TextToSpeech.Default.SpeakAsync(textToSpeak, new SpeechOptions { Locale = locale, Pitch = 1.0f, Volume = 1.0f, }, _ttsCancellationTokenSource.Token);
        }
        catch { }
    }

    private async void OnPinClicked(object sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;
        if (sender is Pin pin && _pinPoiMap.TryGetValue(pin, out PoiModel poi))
        {
            if (_currentActivePoi != null && _currentActivePoi.Id != poi.Id) StopSpeech();

            double currentRadiusKm = Preferences.Default.Get("GeofenceRadius", 50) / 1000.0;
            HighlightActivePoi(new Location(poi.Latitude, poi.Longitude), currentRadiusKm);

            _ = PlayPoiAudio(poi);
            await OpenBottomSheetForPoi(pin, poi);
        }
    }

    private async void CloseBottomSheet(object sender, EventArgs e)
    {
        await BottomSheet.TranslateTo(0, 700, 300, Easing.CubicIn);
        SheetOverlay.InputTransparent = true; SheetOverlay.IsVisible = false;

        if (_currentActivePoi != null && _hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("LeavePoi", _currentActivePoi.Id.ToString());

        StopSpeech();
        ClearHighlight();
    }

    private async Task FetchPoiDetails(string poiId, string description)
    {
        try
        {
            LblPoiAddress.Text = description; using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            var details = await client.GetFromJsonAsync<PoiDetailDto>($"{AppConfig.ApiBaseUrl}/Pois/{poiId}");
            if (details != null)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    if (details.menu != null && details.menu.Count > 0)
                    {
                        foreach (var item in details.menu)
                        {
                            ContainerMenu.Children.Add(new HorizontalStackLayout { Children = { new Label { Text = item.itemName, FontAttributes = FontAttributes.Bold, WidthRequest = 200, TextColor = Colors.Black }, new Label { Text = $"{item.price:N0} đ", TextColor = Colors.Green, FontAttributes = FontAttributes.Bold } } });
                        }
                    }
                    else ContainerMenu.Children.Add(new Label { Text = "Chưa có thực đơn.", FontAttributes = FontAttributes.Italic, TextColor = Colors.Gray });

                    if (details.reviews != null && details.reviews.Count > 0)
                    {
                        foreach (var rev in details.reviews)
                        {
                            ContainerReviews.Children.Add(new VerticalStackLayout { Spacing = 2, Children = { new Label { Text = new string('⭐', rev.rating), TextColor = Colors.Orange }, new Label { Text = $"\"{rev.comment}\"", FontAttributes = FontAttributes.Italic, TextColor = Colors.DarkGray } } });
                        }
                    }
                    else ContainerReviews.Children.Add(new Label { Text = "Chưa có đánh giá.", FontAttributes = FontAttributes.Italic, TextColor = Colors.Gray });
                });
            }
        }
        catch { MainThread.BeginInvokeOnMainThread(() => LblPoiAddress.Text = "Lỗi kết nối mạng!"); }
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
            var response = await client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Pois/{selectedPoi.Id}/review", new { Rating = (int)StepperRating.Value, Comment = EntryReview.Text });
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_timer != null && _timer.IsRunning) _timer.Stop();
    }

    private async void OnRefreshMapClicked(object sender, EventArgs e)
    {
        bool isSuccess = await _dataService.SyncFromServerAsync();
        await ReloadMapData();
        if (isSuccess)
        {
            await DisplayAlert("Thành công", "Đã đồng bộ dữ liệu mới nhất từ máy chủ!", "OK");
        }
        else
        {
            await DisplayAlert("Cảnh báo", "Không thể lấy dữ liệu mới. Vui lòng kiểm tra mạng.", "OK");
        }
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted) return;

        await Navigation.PushModalAsync(new ScannerPage(async (qrCodeData) =>
        {
            var key = qrCodeData;
            if (key.Contains("poi=")) { key = key.Split("poi=")[1].Split("&")[0]; }

            var poi = _pois.FirstOrDefault(p => p.QrCodeKey == key);
            if (poi != null)
            {
                var pin = _pinPoiMap.Keys.FirstOrDefault(p => p.Location.Latitude == poi.Latitude && p.Location.Longitude == poi.Longitude);
                if (pin != null)
                {
                    await Task.Delay(500);
                    _ = PlayPoiAudio(poi);
                    await OpenBottomSheetForPoi(pin, poi);
                }
            }
            else await DisplayAlert("Rất tiếc", "Mã QR này không thuộc hệ thống!", "OK");
        }));
    }

    private async void OnHistoryClicked(object sender, EventArgs e) { await Navigation.PushAsync(new HistoryPage()); }

    private async void OnProfileClicked(object sender, EventArgs e) { await Navigation.PushAsync(new ProfilePage()); }

    public class PoiDetailDto { public List<MenuItemDto> menu { get; set; } public List<ReviewDto> reviews { get; set; } }
    public class MenuItemDto { public string itemName { get; set; } public decimal price { get; set; } }
    public class ReviewDto { public int rating { get; set; } public string comment { get; set; } }
}