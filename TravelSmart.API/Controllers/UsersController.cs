using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly VinhKhanhTravelDbContext _context;
        public UsersController(VinhKhanhTravelDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            return Ok(await _context.Users.Select(u => new { u.UserId, u.Username, u.Email, u.RoleId, u.CreatedAt, u.MerchantRequestStatus }).OrderByDescending(u => u.MerchantRequestStatus == "Pending").ToListAsync());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null) { _context.Users.Remove(user); await _context.SaveChangesAsync(); }
            return Ok();
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveMerchant(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.RoleId = 2; user.MerchantRequestStatus = "Approved";
                // BẮN THÔNG BÁO
                _context.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = id, Title = "✔ Xét duyệt thành công", Message = "Yêu cầu làm Chủ quán của bạn đã được Admin phê duyệt. Bạn có thể thêm quán ăn ngay bây giờ!", IsRead = false, CreatedAt = DateTime.Now });
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectMerchant(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.MerchantRequestStatus = "Rejected";
                _context.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = id, Title = "✖ Từ chối yêu cầu", Message = "Yêu cầu làm Chủ quán của bạn đã bị từ chối do không đủ điều kiện.", IsRead = false, CreatedAt = DateTime.Now });
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> ChangeRole(Guid id, [FromBody] int newRoleId)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.RoleId = newRoleId;
            if (newRoleId == 2) user.MerchantRequestStatus = "Approved";

            string roleName = newRoleId == 1 ? "Admin" : (newRoleId == 2 ? "Chủ quán" : "Khách du lịch");
            _context.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = id, Title = "👑 Cập nhật quyền", Message = $"Quyền của bạn đã được Admin thay đổi thành: {roleName}", IsRead = false, CreatedAt = DateTime.Now });

            await _context.SaveChangesAsync(); return Ok();
        }
    }
}