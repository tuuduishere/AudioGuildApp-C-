using TravelSmart.Mobile.ViewModels;

namespace TravelSmart.Mobile.Views;

public partial class MapPage : ContentPage
{
    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var viewModel = BindingContext as MapViewModel;
        if (viewModel != null)
        {
            MyMap.Pins.Clear(); // Xóa cũ để không bị trùng Pin
            foreach (var place in viewModel.Places)
            {
                MyMap.Pins.Add(new Microsoft.Maui.Controls.Maps.Pin
                {
                    Label = place.Name,
                    Address = place.Address,
                    Type = Microsoft.Maui.Controls.Maps.PinType.Place,
                    Location = new Location(place.Latitude, place.Longitude)
                });
            }
        }
    }
}