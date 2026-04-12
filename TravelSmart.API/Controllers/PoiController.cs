using Microsoft.AspNetCore.Mvc;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoiController : ControllerBase
{
    // Tạm thời dùng List trong bộ nhớ để ông THỬ NGAY mà không cần cài SQL Server phức tạp
    private static List<PoiEntity> _data = new List<PoiEntity>
    {
        new PoiEntity { Id = 1, Name = "Ốc Oanh Vĩnh Khánh", Description = "Quán ốc nổi tiếng nhất Quận 4", Latitude = 10.7600, Longitude = 106.7050, Radius = 50, TtsContent = "Chào mừng bạn đến với Ốc Oanh, món đỉnh nhất ở đây là ốc hương trứng muối.", QrCodeKey = "VK_OC_OANH" }
    };

    // API Lấy danh sách quán (Cho App MAUI gọi)
    [HttpGet]
    public IEnumerable<PoiEntity> Get() => _data;

    // API Thêm quán mới (Cho Admin dùng)
    [HttpPost]
    public IActionResult Post([FromBody] PoiEntity poi)
    {
        poi.Id = _data.Max(x => x.Id) + 1;
        _data.Add(poi);
        return Ok(poi);
    }
}