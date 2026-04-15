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

    // 🔥 THÊM CỘT NÀY ĐỂ ĐÁP ỨNG SPEC: LƯU FILE MP3 THU ÂM SẴN
    public string AudioUrl { get; set; }

    public string ImageUrl { get; set; }
}