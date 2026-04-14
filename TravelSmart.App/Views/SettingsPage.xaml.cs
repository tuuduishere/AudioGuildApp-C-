namespace TravelSmart.App.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Lấy ngôn ngữ đã lưu, mặc định là Tiếng Việt
        var lang = Preferences.Default.Get("DefaultLang", "vi");
        PickerLanguage.SelectedIndex = lang == "en" ? 1 : (lang == "ja" ? 2 : 0);
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        // Khi người dùng chọn ngôn ngữ khác thì lưu lại
        string code = "vi";
        if (PickerLanguage.SelectedIndex == 1) code = "en";
        else if (PickerLanguage.SelectedIndex == 2) code = "ja";

        Preferences.Default.Set("DefaultLang", code);
    }
}