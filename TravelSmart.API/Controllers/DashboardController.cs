using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bùa chú: Phải có Token đăng nhập mới được coi số liệu
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
            // Bóc Token ra xem ai đang truy cập
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (role == "Admin")
            {
                // ADMIN: Quét toàn bộ hệ thống
                var totalUsers = await _context.Users.CountAsync();
                var totalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
                var totalPois = await _context.Pois.CountAsync();

                return Ok(new { Role = role, TotalUsers = totalUsers, TotalRevenue = totalRevenue, TotalPois = totalPois });
            }
            else if (role == "Merchant")
            {
                // CHỦ QUÁN: Chỉ được coi số liệu của mình
                // 1. Tìm các quán do ông này làm chủ
                var myPoiIds = await _context.Pois.Where(p => p.OwnerId == userId).Select(p => p.PoiId).ToListAsync();

                // 2. Tính tiền và số đơn hàng từ các quán đó
                var myOrders = await _context.Orders.Where(o => myPoiIds.Contains(o.PoiId)).CountAsync();
                var myRevenue = await _context.Orders.Where(o => myPoiIds.Contains(o.PoiId)).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // 3. Tính điểm đánh giá trung bình
                var ratings = await _context.Reviews.Where(r => myPoiIds.Contains(r.PoiId)).Select(r => r.Rating).ToListAsync();
                var avgRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0;

                return Ok(new { Role = role, TotalOrders = myOrders, TotalRevenue = myRevenue, AverageRating = avgRating });
            }

            return BadRequest("Không có quyền truy cập");
        }
    }
}