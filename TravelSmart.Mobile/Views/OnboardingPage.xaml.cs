namespace TravelSmart.Mobile.Views;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
    }

    // Hàm này phải tồn tại để file XAML không báo lỗi
    private async void OnStartClicked(object? sender, EventArgs e)
    {
        // Điều hướng sang HomePage
        await Shell.Current.GoToAsync("//HomePage");
    }
}