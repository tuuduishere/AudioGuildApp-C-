using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TravelSmart.App.Models;
using TravelSmart.App.Services;

namespace TravelSmart.App.Views;

public partial class MainPage : ContentPage
{
    private enum SheetState { ThreeQuarters, Hidden }
    private SheetState _currentState = SheetState.Hidden;

    double _threeQuartersY, _hiddenY, _startY;
    bool _isInitialized = false;
    private bool _hasSynced = false;

    private readonly LocationService _locationService;
    private readonly DataService _databaseService;
    private readonly GeofenceService _geofenceService;
    private readonly TTSService _ttsService;

    public MainPage()
    {
        InitializeComponent();

        _databaseService = new DataService();
        _locationService = new LocationService();
        _geofenceService = new GeofenceService(_locationService, _databaseService);
        _ttsService = new TTSService();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (height > 0 && !_isInitialized)
        {
            BottomSheet.HeightRequest = height * 0.75;
            _threeQuartersY = height * 0.25;
            _hiddenY = height;
            BottomSheet.TranslationY = _hiddenY;
            _isInitialized = true;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_hasSynced)
        {
            await _databaseService.SyncFromServerAsync();
            _hasSynced = true;
        }

        await RefreshMapAndList();
        _geofenceService.OnPoiEntered += GeofenceService_OnPoiEntered;
        await _locationService.StartTrackingAsync();

        await CenterMapToUserAsync(false);

#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
        {
            try { Platform.CurrentActivity.StartForegroundService(new Android.Content.Intent(Platform.AppContext, typeof(Platforms.Android.AndroidLocationService))); }
            catch { }
        }
#endif
    }

    private async Task RefreshMapAndList()
    {
        var pois = await _databaseService.GetPOIsAsync();
        MyMap.Pins.Clear();
        foreach (var p in pois)
        {
            var pin = new Pin { Label = p.Name, Address = p.Description, Type = PinType.Place, Location = new Location(p.Latitude, p.Longitude) };
            pin.InfoWindowClicked += async (s, args) =>
            {
                if (await DisplayAlert("Chỉ đường", $"Mở Google Maps dẫn đến {p.Name}?", "Đi ngay", "Hủy"))
                    await Microsoft.Maui.ApplicationModel.Map.OpenAsync(p.Latitude, p.Longitude, new MapLaunchOptions { Name = p.Name });
            };
            MyMap.Pins.Add(pin);
        }
        if (TabPOIView is POIListView poiList) poiList.LoadData();
    }

    private void GeofenceService_OnPoiEntered(object sender, PoiModel poi)
    {
        if (!Preferences.Default.Get("AutoPlayTTS", true)) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await SnapToState(SheetState.ThreeQuarters);
            await _ttsService.SpeakAsync(poi.TtsContent);
        });
    }

    private async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _startY = BottomSheet.TranslationY;
                break;
            case GestureStatus.Running:
                double newY = _startY + e.TotalY;
                if (newY >= _threeQuartersY) BottomSheet.TranslationY = newY;
                break;
            case GestureStatus.Completed:
                if (e.TotalY > 50) await SnapToState(SheetState.Hidden);
                else await SnapToState(SheetState.ThreeQuarters);
                break;
        }
    }

    private async Task SnapToState(SheetState targetState)
    {
        bool wasHidden = _currentState == SheetState.Hidden;
        _currentState = targetState;

        double targetY = targetState == SheetState.ThreeQuarters ? _threeQuartersY : _hiddenY;

        Overlay.IsVisible = targetState != SheetState.Hidden;
        Overlay.Opacity = targetState == SheetState.ThreeQuarters ? 0.3 : 0;

        if (targetState == SheetState.Hidden) _ttsService?.CancelSpeech();

        if (targetState == SheetState.ThreeQuarters && wasHidden)
        {
            await CenterMapToUserAsync(true);
        }
        else if (targetState == SheetState.Hidden && !wasHidden)
        {
            await CenterMapToUserAsync(false);
        }

        await BottomSheet.TranslateTo(0, targetY, 350, Easing.SpringOut);
    }

    private async Task CenterMapToUserAsync(bool isSheetOpen)
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync() ?? await Geolocation.GetLocationAsync();
            if (location != null)
            {
                double offsetLat = isSheetOpen ? -0.003 : 0;
                var targetCenter = new Location(location.Latitude + offsetLat, location.Longitude);
                MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(targetCenter, Distance.FromKilometers(1)));
            }
        }
        catch { }
    }

    private async void OnOverlayTapped(object sender, TappedEventArgs e) => await SnapToState(SheetState.Hidden);

    private void ResetTabColors()
    {
        // TUI ĐÃ BỎ TextQR VÀ IconQR Ở ĐÂY ĐỂ TRÁNH LỖI ĐỎ (Vì nút MoMo đã fix cứng màu vàng)
        TextPOI.TextColor = TextHistory.TextColor = TextSettings.TextColor = Colors.Gray;
        IconPOI.Opacity = IconHistory.Opacity = IconSettings.Opacity = 0.5;
        TextPOI.FontAttributes = TextHistory.FontAttributes = FontAttributes.None;
    }

    private async void SwitchTab(string tabName)
    {
        TabPOIView.IsVisible = (tabName == "POI");
        TabHistoryView.IsVisible = (tabName == "History");

        ResetTabColors();

        if (tabName == "POI")
        {
            TextPOI.TextColor = IconPOI.TextColor = Color.FromArgb("#FFC107");
            TextPOI.FontAttributes = FontAttributes.Bold;
            IconPOI.Opacity = 1;
            await RefreshMapAndList();
        }
        else if (tabName == "History")
        {
            TextHistory.TextColor = IconHistory.TextColor = Color.FromArgb("#FFC107");
            TextHistory.FontAttributes = FontAttributes.Bold;
            IconHistory.Opacity = 1;
            TabHistoryView.RefreshHistory();
        }

        await SnapToState(SheetState.ThreeQuarters);
    }

    private void OnTabPOIClicked(object sender, TappedEventArgs e) => SwitchTab("POI");
    private void OnTabHistoryClicked(object sender, TappedEventArgs e) => SwitchTab("History");
    private async void OnTabSettingsClicked(object sender, TappedEventArgs e) => await Navigation.PushModalAsync(new SettingsPage());

    private async void OnTabQRClicked(object sender, TappedEventArgs e)
    {
        var scannerPage = new ScannerPage();
        scannerPage.OnQRCodeScanned += async (s, qrContent) =>
        {
            await Navigation.PopModalAsync();
            var pois = await _databaseService.GetPOIsAsync();
            var match = pois.FirstOrDefault(p =>
                (p.QrCodeKey != null && p.QrCodeKey.Equals(qrContent, StringComparison.OrdinalIgnoreCase)) ||
                (p.Name != null && p.Name.ToLower().Contains(qrContent.ToLower())));

            if (match != null)
            {
                SwitchTab("POI");
                await SnapToState(SheetState.ThreeQuarters);
                await _databaseService.AddHistoryAsync(match);
                await _ttsService.SpeakAsync(match.TtsContent);
            }
            else
            {
                await DisplayAlert("Lỗi", "Mã QR không hợp lệ!", "Đóng");
            }
        };
        await Navigation.PushModalAsync(scannerPage);
    }
}