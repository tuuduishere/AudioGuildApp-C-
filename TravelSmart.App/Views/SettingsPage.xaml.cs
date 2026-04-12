using TravelSmart.App.Services;

namespace TravelSmart.App.Views;

public partial class SettingsPage : ContentPage
{
    private readonly DataService _databaseService;

    public SettingsPage()
    {
        InitializeComponent();
        _databaseService = new DataService();

        // Tải cấu hình đã lưu
        SwitchAutoPlay.IsToggled = Preferences.Default.Get("AutoPlayTTS", true);

        double savedSpeed = Preferences.Default.Get("TTSSpeed", 1.0);
        SliderSpeed.Value = savedSpeed;
        LabelSpeed.Text = $"{savedSpeed:F1}x";
    }

    // 🛑 NÚT QUAY LẠI AN TOÀN (Lách lỗi JavaProxyThrowable)
    private async void BtnBack_Clicked(object sender, TappedEventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    // LƯU CÀI ĐẶT
    private void OnSettingChanged(object sender, EventArgs e)
    {
        Preferences.Default.Set("AutoPlayTTS", SwitchAutoPlay.IsToggled);
        Preferences.Default.Set("TTSSpeed", SliderSpeed.Value);
    }

    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        LabelSpeed.Text = $"{e.NewValue:F1}x";
    }

    // ĐỒNG BỘ
    private async void BtnSync_Clicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        btn.IsEnabled = false;
        btn.Text = "Đang đồng bộ...";

        bool success = await _databaseService.SyncFromServerAsync();

        btn.IsEnabled = true;
        btn.Text = "🔄 Đồng bộ dữ liệu mới nhất";

        await DisplayAlert("Thông báo", success ? "Cập nhật dữ liệu thành công!" : "Lỗi kết nối máy chủ!", "OK");
    }

    // XÓA LỊCH SỬ
    private async void BtnClearHistory_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Cảnh báo", "Bạn có chắc chắn muốn xóa toàn bộ lịch sử tham quan không?", "Xóa", "Hủy");
        if (confirm)
        {
            await _databaseService.ClearHistoryAsync();
            await DisplayAlert("Thành công", "Đã xóa lịch sử tham quan.", "OK");
        }
    }
}