using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TravelSmart.Mobile.Models;
using TravelSmart.Mobile.Services;

namespace TravelSmart.Mobile.ViewModels;

public class HomeViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    public ObservableCollection<Place> Places { get; } = new();

    public HomeViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            var items = await _apiService.GetPlacesAsync();
            if (items != null)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    foreach (var item in items) Places.Add(item);
                });
            }
        }
        catch { }
    }
}