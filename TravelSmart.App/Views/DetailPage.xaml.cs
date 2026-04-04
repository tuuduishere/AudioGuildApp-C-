using TravelSmart.App.Models;
using TravelSmart.App.Services;
using Microsoft.Maui.Controls;
using System;
using System.Linq;

namespace TravelSmart.App;

[QueryProperty(nameof(DestinationName), "name")]
public partial class DetailPage : ContentPage
{
    Destination? Item;

    // Parameterless ctor required for Shell routing
    public DetailPage()
    {
        InitializeComponent();
    }

    // Existing convenience ctor
    public DetailPage(Destination item) : this()
    {
        InitializeFromDestination(item);
    }

    string destinationName;
    public string DestinationName
    {
        get => destinationName;
        set
        {
            destinationName = value;
            if (!string.IsNullOrEmpty(destinationName))
            {
                var dest = MockData.Destinations.FirstOrDefault(d => d.Name == destinationName);
                if (dest != null) InitializeFromDestination(dest);
            }
        }
    }

    void InitializeFromDestination(Destination item)
    {
        Item = item;
        BindingContext = Item;

        carousel.ItemsSource = item.Images?.Count > 0 ? item.Images : new List<string> { item.Image };
        title.Text = item.Name;
        subtitle.Text = $"⭐ {item.Rating} • {item.Price}";
        desc.Text = item.Description;
        favBtn.Text = item.IsFavorite ? "♥" : "♡";
    }

    void OnShowMap(object sender, EventArgs e)
    {
        if (Item != null)
        {
            MapService.SelectedLat = Item.Lat;
            MapService.SelectedLng = Item.Lng;
            MapService.SelectedName = Item.Name;
            // disable route mode until user explicitly chooses directions
            MapService.RouteMode = false;
            Shell.Current.GoToAsync("//map");
        }
    }

    void OnToggleFav(object sender, EventArgs e)
    {
        if (Item == null) return;
        Item.IsFavorite = !Item.IsFavorite;
        var favs = FavoriteService.Load();
        if (Item.IsFavorite) favs.Add(Item.Name); else favs.Remove(Item.Name);
        FavoriteService.Save(favs);
        favBtn.Text = Item.IsFavorite ? "♥" : "♡";
    }

    async void OnShare(object sender, EventArgs e)
    {
        if (Item == null) return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = $"{Item.Name} - {Item.Description}",
            Title = "Chia sẻ địa điểm"
        });
    }

    async void OnDirections(object sender, EventArgs e)
    {
        if (Item == null) return;

        // Try to obtain current device location; fallback to a nearby mock point
        double fromLat = Item.Lat - 0.005;
        double fromLng = Item.Lng - 0.005;
        try
        {
            var req = new Microsoft.Maui.Devices.Sensors.GeolocationRequest(Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Medium);
            var loc = await Microsoft.Maui.Devices.Sensors.Geolocation.Default.GetLocationAsync(req);
            if (loc != null)
            {
                fromLat = loc.Latitude;
                fromLng = loc.Longitude;
            }
        }
        catch { /* ignore permission errors */ }

        MapService.RouteFromLat = fromLat;
        MapService.RouteFromLng = fromLng;
        MapService.RouteMode = true;
        MapService.SelectedLat = Item.Lat;
        MapService.SelectedLng = Item.Lng;
        MapService.SelectedName = Item.Name;

        await Shell.Current.GoToAsync("//map");
    }
}
