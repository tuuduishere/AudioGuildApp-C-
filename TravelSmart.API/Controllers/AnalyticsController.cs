using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly VinhKhanhTravelDbContext _context;
        public AnalyticsController(VinhKhanhTravelDbContext context) { _context = context; }

        // 1. Lấy toàn bộ lịch sử (cho trang Lịch sử sử dụng)
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var rawLogs = await _context.VisitLogs
                .OrderByDescending(l => l.VisitTime)
                .Take(100)
                .ToListAsync();

            var poiIds = rawLogs.Select(l => l.PoiId).Distinct().ToList();
            var poiNames = await _context.PoiTranslations
                .Where(t => poiIds.Contains(t.PoiId) && t.LanguageCode == "vi")
                .ToDictionaryAsync(t => t.PoiId, t => t.Name);

            var logs = rawLogs.Select(l => new {
                time = l.VisitTime?.ToString("dd/MM/yyyy HH:mm"),
                device = l.DeviceName ?? "Ẩn danh",
                poiName = poiNames.ContainsKey(l.PoiId) ? poiNames[l.PoiId] : "Không xác định",
                duration = l.DurationMinutes ?? 0.0,
                language = l.LanguageCode == "vi" ? "Tiếng Việt" : (l.LanguageCode == "en" ? "Tiếng Anh" : "Tiếng Nhật")
            });

            return Ok(logs);
        }

        // 2. Lấy Top 10 quán nghe nhiều nhất (cho trang Top địa điểm)
        [HttpGet("top-pois")]
        public async Task<IActionResult> GetTopPois()
        {
            var top = await _context.VisitLogs
                .GroupBy(l => l.PoiId)
                .Select(g => new { PoiId = g.Key, ListenCount = g.Count() })
                .OrderByDescending(x => x.ListenCount)
                .Take(10)
                .ToListAsync();

            var poiIds = top.Select(x => x.PoiId).ToList();
            var poiNames = await _context.PoiTranslations.Where(t => poiIds.Contains(t.PoiId) && t.LanguageCode == "vi").ToDictionaryAsync(t => t.PoiId, t => t.Name);
            var poiAddresses = await _context.Pois.Where(p => poiIds.Contains(p.PoiId)).ToDictionaryAsync(p => p.PoiId, p => p.Address);

            var result = top.Select(x => new {
                name = poiNames.ContainsKey(x.PoiId) ? poiNames[x.PoiId] : "Không xác định",
                address = poiAddresses.ContainsKey(x.PoiId) ? poiAddresses[x.PoiId] : "",
                listenCount = x.ListenCount,
                rating = 5.0
            });

            return Ok(result);
        }

        // 3. Lấy tọa độ để vẽ bản đồ nhiệt (cho trang Heatmap)
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmap()
        {
            var data = await _context.VisitLogs
                .Join(_context.Pois, l => l.PoiId, p => p.PoiId, (l, p) => new {
                    name = "POI",
                    lat = p.Latitude,
                    lng = p.Longitude
                }).ToListAsync();
            return Ok(data);
        }
    }
}