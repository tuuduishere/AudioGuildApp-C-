using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly VinhKhanhTravelDbContext _context;

        public DashboardController(VinhKhanhTravelDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;

            if (role == "Admin")
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                var totalPois = await _context.Pois.CountAsync();

                var totalTours = await _context.Tours.CountAsync();
                var totalListens = await _context.VisitLogs.CountAsync();
                var uniqueUsers = await _context.VisitLogs.Select(v => v.DeviceName).Distinct().CountAsync();

                var totalDurationMins = await _context.VisitLogs.SumAsync(v => (double?)v.DurationMinutes) ?? 0;
                double avgListenDurationSeconds = totalListens > 0 ? (totalDurationMins * 60) / totalListens : 0;

                return Ok(new
                {
                    Role = role,
                    TotalUsers = totalUsers,
                    TotalRevenue = totalRevenue,
                    TotalPois = totalPois,
                    TotalTours = totalTours,
                    TotalListens = totalListens,
                    UniqueUsers = uniqueUsers,
                    AvgListenDurationSeconds = Math.Round(avgListenDurationSeconds, 1)
                });
            }
            else if (role == "Merchant")
            {
                var myPoiIds = await _context.Pois.Where(p => p.OwnerId == userId).Select(p => p.PoiId).ToListAsync();
                var myOrders = await _context.Orders.Where(o => myPoiIds.Contains(o.PoiId)).CountAsync();
                var myRevenue = await _context.Orders.Where(o => myPoiIds.Contains(o.PoiId)).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                var ratings = await _context.Reviews.Where(r => myPoiIds.Contains(r.PoiId)).Select(r => r.Rating).ToListAsync();
                var avgRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0;
                return Ok(new { Role = role, TotalOrders = myOrders, TotalRevenue = myRevenue, AverageRating = avgRating });
            }
            return BadRequest("Không có quyền truy cập");
        }

        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmap([FromQuery] int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.VisitLogs.Where(v => v.VisitTime >= startDate);

            if (role == "Merchant" && Guid.TryParse(userIdStr, out var userId))
            {
                var myPoiIds = await _context.Pois.Where(p => p.OwnerId == userId).Select(p => p.PoiId).ToListAsync();
                query = query.Where(v => myPoiIds.Contains(v.PoiId));
            }

            var rawData = await query.Select(v => new { v.VisitTime, v.DeviceName, v.DurationMinutes }).ToListAsync();

            var heatmapData = rawData
                .Where(v => v.VisitTime.HasValue)
                .GroupBy(v => v.VisitTime.Value.Date)
                .Select(g => new {
                    Date = g.Key.ToString("dd/MM/yyyy"),
                    Count = g.Count(),
                    UniqueUsers = g.Select(x => x.DeviceName).Distinct().Count(),
                    AvgDurationSeconds = Math.Round((g.Average(x => (double?)x.DurationMinutes) ?? 0) * 60, 0)
                })
                .OrderByDescending(x => DateTime.ParseExact(x.Date, "dd/MM/yyyy", null))
                .ToList();

            return Ok(heatmapData);
        }

        [HttpGet("top-pois")]
        public async Task<IActionResult> GetTopPois()
        {
            var topPois = await _context.VisitLogs
                .GroupBy(v => v.PoiId)
                .Select(g => new {
                    PoiId = g.Key,
                    ListenCount = g.Count(),
                    AvgDurationSeconds = Math.Round((g.Average(x => (double?)x.DurationMinutes) ?? 0) * 60, 0)
                })
                .OrderByDescending(x => x.ListenCount)
                .Take(5)
                .ToListAsync();

            return Ok(topPois);
        }

        // =======================================================
        // 🔥 ĐỘ THÊM API: THỐNG KÊ SỐ LƯỢNG QUÉT QR THEO THIẾT BỊ
        // =======================================================
        [HttpGet("qr-stats")]
        public async Task<IActionResult> GetQrStats()
        {
            // Lọc ra mấy dòng Lịch sử có gắn mác [QR_SCAN]
            var qrLogs = await _context.VisitLogs
                .Where(v => v.DeviceName.Contains("[QR_SCAN]"))
                .GroupBy(v => v.DeviceName)
                .Select(g => new {
                    DeviceName = g.Key.Replace(" [QR_SCAN]", ""), // Bỏ chữ tag đi cho UI sạch đẹp
                    ScanCount = g.Count(), // Đếm số lần quét
                    LastScan = g.Max(x => x.VisitTime) // Lần quét gần nhất
                })
                .OrderByDescending(x => x.ScanCount)
                .Take(10) // Lấy top 10 máy siêng quét nhất
                .ToListAsync();

            return Ok(qrLogs);
        }
    }
}