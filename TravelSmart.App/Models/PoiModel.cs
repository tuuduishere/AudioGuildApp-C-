namespace TravelSmart.App.Models;

public class PoiModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string TtsContent { get; set; }
    public string QrCodeKey { get; set; }
}