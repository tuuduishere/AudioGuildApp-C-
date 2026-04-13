using TravelSmart.App.Services;
namespace TravelSmart.App.Views;

public partial class HistoryPage : ContentPage
{
    private readonly DataService _dataService = new();
    public HistoryPage() { InitializeComponent(); }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ListHistory.ItemsSource = await _dataService.GetHistoryAsync();
    }
}