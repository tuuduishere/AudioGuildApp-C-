using TravelSmart.App.Services;
using TravelSmart.App.Models;

namespace TravelSmart.App;

public partial class ExplorePage : ContentPage
{
    List<Destination> all = MockData.Destinations;

    public ExplorePage()
    {
        InitializeComponent();
        listView.ItemsSource = all;
    }

    void OnSearch(object sender, TextChangedEventArgs e)
    {
        listView.ItemsSource = all
            .Where(x => x.Name.ToLower().Contains(e.NewTextValue.ToLower()));
    }
}
