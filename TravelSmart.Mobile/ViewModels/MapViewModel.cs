using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using TravelSmart.Mobile.Models;
using TravelSmart.Mobile.Services;
using Plugin.Maui.Audio;

namespace TravelSmart.Mobile.ViewModels;

public class MapViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _activePlayer;

    public ObservableCollection<Place> Places { get; } = new();

    public MapViewModel(ApiService apiService, IAudioManager audioManager)
    {
        _apiService = apiService;
        _audioManager = audioManager;
        _ = StartTrackingLocation();
    }

    private async Task StartTrackingLocation()
    {
        try
        {
            // Lấy danh sách địa điểm từ API
            var items = await _apiService.GetPlacesAsync();
            foreach (var item in items) Places.Add(item);

            // Vòng lặp theo dõi vị trí mỗi 10 giây
            while (true)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    CheckProximityAndPlayAudio(location);
                }
                await Task.Delay(10000);
            }
        }
        catch { }
    }

    private async void CheckProximityAndPlayAudio(Location userLoc)
    {
        foreach (var place in Places)
        {
            Location placeLoc = new Location(place.Latitude, place.Longitude);
            double distance = userLoc.CalculateDistance(placeLoc, DistanceUnits.Kilometers) * 1000; // Đổi ra mét

            // Nếu gần dưới 200m và chưa phát audio nào
            if (distance <= 200 && (_activePlayer == null || !_activePlayer.IsPlaying))
            {
                using var client = new HttpClient();
                var stream = await client.GetStreamAsync(place.AudioUrl);
                _activePlayer = _audioManager.CreatePlayer(stream);
                _activePlayer.Play();

                await Shell.Current.DisplayAlert("Thông báo", $"Bạn đang ở gần {place.Name}. Đang phát thuyết minh tự động!", "OK");
                break;
            }
        }
    }
}