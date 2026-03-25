namespace TravelSmart.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Đăng ký Route cho trang chi tiết để điều hướng được
        Routing.RegisterRoute("PlaceDetailPage", typeof(Views.PlaceDetailPage));
    }
}