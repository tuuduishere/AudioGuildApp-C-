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
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (role == "Admin")
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                var totalPois = await _context.Pois.CountAsync();
                return Ok(new { Role = role, TotalUsers = totalUsers, TotalRevenue = totalRevenue, TotalPois = totalPois });
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

        // 🔥 API TẠO DỮ LIỆU HEATMAP ĐA THỜI GIAN
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmap([FromQuery] int days = 7)
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

            var rawData = await query.Select(v => new { v.VisitTime }).ToListAsync();

            // Nhóm theo ngày để vẽ biểu đồ
            var heatmapData = rawData
                .Where(v => v.VisitTime.HasValue)
                .GroupBy(v => v.VisitTime.Value.Date)
                .Select(g => new {
                    Date = g.Key.ToString("dd/MM"),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(heatmapData);
        }
    }
}