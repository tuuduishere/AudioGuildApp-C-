namespace TravelSmart.App;

public partial class MapPage : ContentPage
{
    public MapPage()
    {
        InitializeComponent();
        LoadMap();
    }

    void LoadMap()
    {
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

                L.marker([10.772,106.698]).addTo(map).bindPopup('Ben Thanh');
                L.marker([10.795,106.721]).addTo(map).bindPopup('Landmark 81');
            </script>
        </body>
        </html>
        ";

        mapWeb.Source = new HtmlWebViewSource
        {
            Html = html
        };
    }

    void OnLocate(object sender, EventArgs e)
    {
        DisplayAlert("Info", "GPS demo trên Windows (mock)", "OK");
    }
}