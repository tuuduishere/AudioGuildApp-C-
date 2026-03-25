using TravelSmart.Mobile.Models;

namespace TravelSmart.Mobile.Views;

public partial class PlaceDetailPage : ContentPage
{
    public PlaceDetailPage(ViewModels.PlaceDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}