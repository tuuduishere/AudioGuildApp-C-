using TravelSmart.App.Models;
using TravelSmart.App.Services;

namespace TravelSmart.App;

public partial class FavoritesPage : ContentPage
{
    public FavoritesPage()
    {
        InitializeComponent();
        LoadFavorites();
        TravelSmart.App.Services.FavoriteService.FavoritesChanged += LoadFavorites;
    }

    void LoadFavorites()
    {
        var favs = FavoriteService.Load();
        var all = MockData.Destinations.Where(d => favs.Contains(d.Name)).ToList();
        favList.ItemsSource = all;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadFavorites();
    }

    public void Refresh() => LoadFavorites();

    void OnOpen(object sender, EventArgs e)
    {
        if (sender is Button b && b.BindingContext is Destination d)
        {
            Shell.Current.Navigation.PushAsync(new DetailPage(d));
        }
    }
}
