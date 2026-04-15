namespace TravelSmart.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // 🔥 CHỐT HẠ MỌI VẤN ĐỀ: Xóa LoginPage, ép vào thẳng MainPage (Bản đồ)
        MainPage = new NavigationPage(new Views.MainPage());
    }
}