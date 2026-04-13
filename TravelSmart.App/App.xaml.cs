namespace TravelSmart.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Ép App khi vừa bật lên là phải vào trang LoginPage (Nằm trong NavigationPage để chuyển trang cho mượt)
        MainPage = new NavigationPage(new Views.LoginPage());
    }
}