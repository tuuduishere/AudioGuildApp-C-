namespace TravelSmart.App.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();

        // Tải lại cấu hình cũ đã lưu trong máy
        SliderTtsSpeed.Value = Preferences.Default.Get("tts_speed", 1.0f);
        PickerLanguage.SelectedIndex = Preferences.Default.Get("app_lang_index", 0); // Mặc định 0 là Tiếng Việt

        // Cập nhật chữ khi kéo thanh trượt
        SliderTtsSpeed.ValueChanged += (s, e) =>
        {
            LblSpeedValue.Text = $"Tốc độ: {e.NewValue:F1}x";
        };
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Lưu vĩnh viễn vào bộ nhớ điện thoại
        Preferences.Default.Set("tts_speed", (float)SliderTtsSpeed.Value);
        Preferences.Default.Set("app_lang_index", PickerLanguage.SelectedIndex);

        await DisplayAlert("Thành công", "Đã lưu cài đặt!", "OK");
        await Navigation.PopAsync(); // Đóng trang Cài đặt, quay về Bản đồ
    }
}