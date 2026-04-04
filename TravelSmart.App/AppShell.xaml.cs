namespace TravelSmart.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("detail", typeof(DetailPage));
        Routing.RegisterRoute("favorites", typeof(FavoritesPage));
    }
}
