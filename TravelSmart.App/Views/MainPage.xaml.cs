using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TravelSmart.App.Services;
using TravelSmart.App.Models;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using System.Net.Http.Json;
using System.Net.Http.Headers;
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

    private async void OnNotificationClicked(object sender, EventArgs e)
    {
        BadgeNoti.IsVisible = false; // Ẩn badge khi bấm vào
        await Navigation.PushAsync(new NotificationsPage());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(10.7605, 106.7025), Distance.FromKilometers(1)));
        await SyncWithServer(); // Tự cập nhật ngay khi mở màn hình
        await ReloadMapData(); await StartTrackingGPS();
    }

    // ĐỒNG BỘ NGẦM (Sửa để hiện SỐ ĐẾM)
    public async Task SyncWithServer()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("authToken");
            if (string.IsNullOrEmpty(token)) return;
            using var client = new HttpClient(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{ApiBaseUrl}/Auth/sync");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<SyncDataDto>();
                if (data != null)
                {
                    string realRole = data.roleId == 1 ? "Admin" : (data.roleId == 2 ? "Merchant" : "User");
                    await SecureStorage.Default.SetAsync("role", realRole);

                    // CẬP NHẬT BADGE SỐ
                    if (data.unreadCount > 0)
                    {
                        BadgeNoti.IsVisible = true;
                        LblUnreadCount.Text = data.unreadCount > 9 ? "9+" : data.unreadCount.ToString();
                    }
                    else
                    {
                        BadgeNoti.IsVisible = false;
                    }
                }
            }
        }
        catch { }
    }
    public class SyncDataDto { public int roleId { get; set; } public int unreadCount { get; set; } }

    private async void OnPinClicked(object sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;
        if (sender is Pin pin && _pinPoiMap.TryGetValue(pin, out PoiModel poi))
        {
            _currentSelectedLocation = pin.Location; LblPoiName.Text = poi.Name; LblPoiAddress.Text = "Đang tải thông tin...";
            ContainerMenu.Children.Clear(); ContainerReviews.Children.Clear(); EntryReview.Text = "";
            var role = await SecureStorage.Default.GetAsync("role"); FrameWriteReview.IsVisible = (role == "User");
            SheetOverlay.IsVisible = true; SheetOverlay.InputTransparent = false;
            await Task.WhenAll(SheetOverlay.FadeTo(0, 250), BottomSheet.TranslateTo(0, 0, 300, Easing.CubicOut));
            await FetchPoiDetails(poi.Id, poi.Description);
        }
    }

    private async void CloseBottomSheet(object sender, EventArgs e)
    {
        await Task.WhenAll(SheetOverlay.FadeTo(0, 250), BottomSheet.TranslateTo(0, 700, 300, Easing.CubicIn));
        SheetOverlay.InputTransparent = true; SheetOverlay.IsVisible = false;
    }

    private async Task FetchPoiDetails(string poiId, string description)
    {
        try
        {
            LblPoiAddress.Text = description; using var client = new HttpClient();
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
        if (string.IsNullOrEmpty(EntryReview.Text)) { await DisplayAlert("Lỗi", "Hãy nhập bình luận!", "OK"); return; }
        var token = await SecureStorage.Default.GetAsync("authToken");
        using var client = new HttpClient(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
            else await DisplayAlert("Lỗi", "Có lỗi xảy ra (Có thể do bạn là chủ quán).", "OK");
        }
    }

    private async void OnGetDirectionsClicked(object sender, EventArgs e)
    {
        if (_currentSelectedLocation != null) await Microsoft.Maui.ApplicationModel.Map.OpenAsync(_currentSelectedLocation, new MapLaunchOptions { Name = LblPoiName.Text, NavigationMode = NavigationMode.Driving });
    }

    private async Task StartTrackingGPS()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
        {
            _timer = Application.Current.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += async (s, e) => {
                await CheckGeofence();
                await SyncWithServer();
            };
            _timer.Start();
        }
    }

    private async Task CheckGeofence() { /* Logic hàng rào ảo */ }
    protected override void OnDisappearing() { base.OnDisappearing(); if (_timer != null && _timer.IsRunning) _timer.Stop(); }

    // CẬP NHẬT THỦ CÔNG KHI BẤM NÚT BẢN ĐỒ
    private async void OnRefreshMapClicked(object sender, EventArgs e)
    {
        await SyncWithServer(); // Ép check thông báo ngay lập tức
        await ReloadMapData();
    }

    private async void OnScanClicked(object sender, EventArgs e) { await Navigation.PushModalAsync(new ScannerPage()); }
    private async void OnHistoryClicked(object sender, EventArgs e) { await Navigation.PushAsync(new HistoryPage()); }
    private async void OnProfileClicked(object sender, EventArgs e) { await Navigation.PushAsync(new ProfilePage()); }

    public class PoiDetailDto { public List<MenuItemDto> menu { get; set; } public List<ReviewDto> reviews { get; set; } }
    public class MenuItemDto { public string itemName { get; set; } public decimal price { get; set; } }
    public class ReviewDto { public int rating { get; set; } public string comment { get; set; } }
}