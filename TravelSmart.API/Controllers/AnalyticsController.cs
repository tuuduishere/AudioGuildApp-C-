using Microsoft.AspNetCore.Mvc;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    // Lưu tạm log vào RAM để ông test lẹ
    private static List<AnalyticsLog> _logs = new List<AnalyticsLog>();

    // 1. API CHO APP MOBILE GỌI: Gửi báo cáo lên
    [HttpPost]
    public IActionResult PostLog([FromBody] AnalyticsLog log)
    {
        log.Id = _logs.Count + 1;
        log.Timestamp = DateTime.Now; // Đóng dấu thời gian lúc nhận
        _logs.Add(log);

        return Ok(new { Message = "Đã ghi nhận dữ liệu", TotalLogs = _logs.Count });
    }

    // 2. API CHO GIAO DIỆN ADMIN GỌI: Thống kê Top Quán Ốc
    [HttpGet("top-pois")]
    public IActionResult GetTopPois()
    {
        var top = _logs.GroupBy(x => x.PoiId)
                       .Select(g => new { PoiId = g.Key, ViewCount = g.Count() })
                       .OrderByDescending(x => x.ViewCount)
                       .ToList();

        return Ok(top);
    }
}