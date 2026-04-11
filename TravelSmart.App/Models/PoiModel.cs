using SQLite;

namespace TravelSmart.App.Models;

public class PoiModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Thêm = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TtsContent { get; set; } = string.Empty;
    public string QrCodeKey { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
}