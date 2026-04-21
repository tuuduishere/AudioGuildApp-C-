using SQLite;

namespace TravelSmart.App.Models;

public class PoiModel
{
    [PrimaryKey] // 🔥 CHÌA KHÓA VÀNG: Phải có cái này SQLite mới biết đường Thêm/Xóa/Sửa
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; }
    public string Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string TtsContent { get; set; }
    public string QrCodeKey { get; set; }
    public string AudioUrl { get; set; }

    public int Priority { get; set; } = 0;

    public string ImageUrl { get; set; }
}