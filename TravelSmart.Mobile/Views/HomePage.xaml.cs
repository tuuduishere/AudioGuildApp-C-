
using TravelSmart.Mobile.Models;
using TravelSmart.Mobile.ViewModels;

namespace TravelSmart.Mobile.Views;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnPlaceSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Place selectedPlace)
            return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Place", selectedPlace }
        };

        await Shell.Current.GoToAsync(nameof(PlaceDetailPage), navigationParameter);

        // Reset selection
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;
    }
}