<<<<<<< HEAD
using TravelSmart.App.Services;
using TravelSmart.App.Models;
using System.Web;
using Microsoft.Maui.Controls;
using System.Linq;

namespace TravelSmart.App;
=======
namespace TravelSmart.App.Views;
>>>>>>> master

public partial class MapPage : ContentPage
{
    public MapPage()
    {
        InitializeComponent();
<<<<<<< HEAD
        // Do not call LoadMap here so the map can be refreshed on every appearance
    }

    async Task LoadMap()
    {
        // synchronous wrapper; the heavy async work is handled in OnAppearing which calls LoadMap
        string html = @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
            <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
        </head>
        <body style='margin:0'>
            <div id='map' style='width:100%;height:100vh'></div>
            <script>
                var map = L.map('map').setView([10.776, 106.700], 13);

                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19
                }).addTo(map);

                // add POI markers from app
                var pois = {pois};
                pois.forEach(function(p) {
                    var content = '<div><strong>' + p.name + '</strong><br/>' +
                                  '<a href=\'app://detail?name=' + encodeURIComponent(p.name) + '\'>Xem chi tiết</a><br/>' +
                                  '<a href=\'app://directions?name=' + encodeURIComponent(p.name) + '\'>Chỉ đường</a>' +
                                  '</div>';
                    var m = L.marker([p.lat, p.lng]).addTo(map).bindPopup(content);
                });
                // selection from app
                var selLat = {selLat};
                var selLng = {selLng};
                if (selLat != null && selLng != null) {
                    map.setView([selLat, selLng], 15);
                    var popupText = '<div><strong>' + {selName} + '</strong><br/>';
                    // If route info provided, show a simple directions info in the popup
                    var routeFromLat = {routeFromLat};
                    var routeFromLng = {routeFromLng};
                    var routeMode = {routeMode};
                    if (routeMode && routeFromLat != null && routeFromLng != null) {
                        popupText += '<div>Đang hiển thị tuyến đường</div>';
                        // draw a line
                        var latlngs = [ [routeFromLat, routeFromLng], [selLat, selLng] ];
                        var poly = L.polyline(latlngs, {color: 'blue'}).addTo(map);
                        map.fitBounds(poly.getBounds());
                    }
                    popupText += '<div><a target=\'_blank\' href=\'https://www.google.com/maps/dir/?api=1&destination=' + selLat + ',' + selLng + '\'>Chỉ đường chi tiết</a></div>';
                    popupText += '</div>';
                    L.marker([selLat, selLng]).addTo(map).bindPopup(popupText).openPopup();
                }
            </script>
        </body>
        </html>
        ";
        // Use invariant culture to ensure decimal separator is a dot (JS expects dot)
        var lat = MapService.SelectedLat?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        var lng = MapService.SelectedLng?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        var nameJson = System.Text.Json.JsonSerializer.Serialize(MapService.SelectedName ?? string.Empty);
        // serialize POI list for injection into the HTML/JS (include rating and price)
        var poisList = MockData.Destinations.Select(d => new { lat = d.Lat, lng = d.Lng, name = d.Name, rating = d.Rating, price = d.Price }).ToList();
        var poisJson = System.Text.Json.JsonSerializer.Serialize(poisList);
        var routeFromLat = MapService.RouteFromLat?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        var routeFromLng = MapService.RouteFromLng?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        var routeMode = MapService.RouteMode ? "true" : "false";

        html = html.Replace("{selLat}", lat)
                   .Replace("{selLng}", lng)
                   .Replace("{selName}", nameJson)
                   .Replace("{routeFromLat}", routeFromLat)
                   .Replace("{routeFromLng}", routeFromLng)
                   .Replace("{routeMode}", routeMode)
                   .Replace("{pois}", poisJson);

        // Ensure we set the WebView source on the main/UI thread to avoid
        // "interface marshalled for a different thread" COM exceptions.
        try
        {
            this.IsBusy = true;
            await MainThread.InvokeOnMainThreadAsync(() => mapWeb.Source = new HtmlWebViewSource { Html = html });
        }
        finally
        {
            // hide loading indicator after a short delay so users see the transition
            await Task.Delay(150);
            this.IsBusy = false;
        }

        return;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // If there is no selected destination, clear route mode
        if (!MapService.SelectedLat.HasValue || !MapService.SelectedLng.HasValue)
        {
            MapService.RouteMode = false;
            await LoadMap();
            return;
        }

        // If route mode is requested but no RouteFrom is set, attempt to get device location
        if (MapService.RouteMode && (!MapService.RouteFromLat.HasValue || !MapService.RouteFromLng.HasValue))
        {
            try
            {
                var req = new Microsoft.Maui.Devices.Sensors.GeolocationRequest(Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Medium);
                using var cancel = new System.Threading.CancellationTokenSource(3000);
                var loc = await Microsoft.Maui.Devices.Sensors.Geolocation.Default.GetLocationAsync(req, cancel.Token);
                if (loc != null)
                {
                    MapService.RouteFromLat = loc.Latitude;
                    MapService.RouteFromLng = loc.Longitude;
                }
            }
            catch {
                // ignore geolocation failures for now
            }
        }

        // load map (will include any route info if RouteMode + RouteFrom present)
        await LoadMap();
    }

    async void OnCancelRoute(object sender, EventArgs e)
    {
        // Clear routing info and reload map without route
        MapService.RouteMode = false;
        MapService.RouteFromLat = null;
        MapService.RouteFromLng = null;
        await LoadMap();
    }

    void OnLocate(object sender, EventArgs e)
    {
        DisplayAlert("Info", "GPS demo trên Windows (mock)", "OK");
=======
>>>>>>> master
    }

    // Intercept navigation from the web content (e.g. custom app://detail?name=...)
    void OnMapWebNavigating(object sender, WebNavigatingEventArgs e)
    {
        try
        {
            var uri = new Uri(e.Url);
                if (uri.Scheme == "app")
            {
                // cancel navigation inside the WebView
                e.Cancel = true;
                    var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var name = q.Get("name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        // find destination by name
                        var dest = MockData.Destinations.FirstOrDefault(d => d.Name == name);
                        if (dest != null)
                        {
                            if (uri.Host == "detail")
                            {
                                Shell.Current.GoToAsync($"//detail?name={Uri.EscapeDataString(dest.Name)}");
                            }
                            else if (uri.Host == "directions")
                            {
                                // set directions and open map
                                MapService.RouteFromLat = dest.Lat - 0.005; // crude mock origin
                                MapService.RouteFromLng = dest.Lng - 0.005;
                                MapService.RouteMode = true;
                                MapService.SelectedLat = dest.Lat;
                                MapService.SelectedLng = dest.Lng;
                                MapService.SelectedName = dest.Name;
                                Shell.Current.GoToAsync("//map");
                            }
                        }
                    }
            }
        }
        catch
        {
            // ignore malformed urls
        }
    }
}