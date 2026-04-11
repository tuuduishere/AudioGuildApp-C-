namespace TravelSmart.API.Models;

public class PoiEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
    public string TtsContent { get; set; }
    public string QrCodeKey { get; set; } // Dùng để đối chiếu khi quét QR
}