using TravelSmart.App.Models;

namespace TravelSmart.App.Services;

public class GeofenceService
{
    private readonly LocationService _locationService;
    private readonly DataService _dataService;
    private readonly Dictionary<int, DateTime> _cooldowns = new();

    public event EventHandler<PoiModel> OnPoiEntered;

    public GeofenceService(LocationService locationService, DataService dataService)
    {
        _locationService = locationService;
        _dataService = dataService;
        _locationService.OnLocationUpdated += CheckGeofences;
    }

    private async void CheckGeofences(object sender, Location myLocation)
    {
        if (myLocation == null) return;

        var pois = await _dataService.GetPOIsAsync();

        foreach (var poi in pois)
        {
            double distance = myLocation.CalculateDistance(
                new Location(poi.Latitude, poi.Longitude), DistanceUnits.Kilometers) * 1000;

            if (distance <= poi.Radius)
            {
                if (_cooldowns.TryGetValue(poi.Id, out var lastPlayed))
                {
                    if ((DateTime.Now - lastPlayed).TotalMinutes < 5)
                        continue; // Vẫn trong thời gian cooldown
                }

                _cooldowns[poi.Id] = DateTime.Now;
                OnPoiEntered?.Invoke(this, poi);
            }
        }
    }
}