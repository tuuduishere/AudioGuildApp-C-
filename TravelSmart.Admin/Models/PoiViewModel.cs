namespace TravelSmart.Admin.Models;

public class PoiViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
    public string TtsContent { get; set; }
    public string QrCodeKey { get; set; }
}