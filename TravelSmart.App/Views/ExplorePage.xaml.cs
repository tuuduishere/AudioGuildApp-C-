using TravelSmart.App.Services;
using TravelSmart.App.Models;
using System.Collections.ObjectModel;

namespace TravelSmart.App;

public partial class ExplorePage : ContentPage
{
    ObservableCollection<Destination> all = new(MockData.Destinations);
    HashSet<string> favorites = new();

    public ExplorePage()
    {
        InitializeComponent();
        favorites = FavoriteService.Load();
        foreach (var d in all)
            d.IsFavorite = favorites.Contains(d.Name);

        listView.ItemsSource = all;
    }

    private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var sel = e.CurrentSelection.FirstOrDefault() as Destination;
        if (sel == null) return;
        // Clear any previous route selection when picking a new item
        MapService.RouteMode = false;
        MapService.RouteFromLat = null;
        MapService.RouteFromLng = null;
        MapService.SelectedName = null;
        MapService.SelectedLat = sel.Lat;
        MapService.SelectedLng = sel.Lng;

        // Open detail page first; user can choose to view on map from there
        await Shell.Current.Navigation.PushAsync(new DetailPage(sel));
        (sender as CollectionView).SelectedItem = null;
    }

    void OnSearch(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            listView.ItemsSource = all;
            return;
        }

        var q = e.NewTextValue.Trim().ToLower();
        listView.ItemsSource = all.Where(x => x.Name.ToLower().Contains(q) || x.Price.ToLower().Contains(q));
    }

    void OnFavoriteClicked(object sender, EventArgs e)
    {
        try
        {
            var btn = sender as Button;
            if (btn?.BindingContext is Destination dest)
            {
                dest.IsFavorite = !dest.IsFavorite;
                if (dest.IsFavorite) favorites.Add(dest.Name); else favorites.Remove(dest.Name);
                FavoriteService.Save(favorites);
                    // refresh FavoritesPage if present in shell
                    try
                    {
                        var shell = Shell.Current;
                        // find the favorites shellcontent
                        foreach (var item in shell.Items)
                        {
                            foreach (var sec in item.Items)
                            {
                                foreach (var content in sec.Items)
                                {
                                    if (content.Route == "favorites" && content.Content is Page p && p is FavoritesPage fp)
                                    {
                                        fp.Refresh();
                                    }
                                }
                            }
                        }
                    }
                    catch { }
            }
        }
        catch { }
    }
}
