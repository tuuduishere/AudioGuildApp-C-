using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly VinhKhanhTravelDbContext _context;
        private readonly IConfiguration _config;
        public AuthController(VinhKhanhTravelDbContext context, IConfiguration config) { _context = context; _config = config; }

        public class LoginRequest { public string Username { get; set; } public string Password { get; set; } }
        public class RegisterRequest { public string Username { get; set; } public string Password { get; set; } public string Email { get; set; } }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return Unauthorized("Sai tài khoản/mật khẩu!");
            string roleName = user.RoleId switch { 1 => "Admin", 2 => "Merchant", _ => "User" };

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.Role, roleName)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            return Ok(new { token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor)), role = roleName });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username)) return BadRequest("Tên đăng nhập đã tồn tại!");
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = 3,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _context.Users.Add(newUser); await _context.SaveChangesAsync();
            return Ok(new { message = "Đăng ký thành công!" });
        }

        [HttpPost("request-merchant")]
        [Authorize]
        public async Task<IActionResult> RequestMerchant()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Lỗi tài khoản!");
            if (user.RoleId == 1 || user.RoleId == 2) return BadRequest("Bạn đã có quyền quản lý rồi!");
            if (user.MerchantRequestStatus == "Pending") return BadRequest("Đơn đang chờ Admin duyệt!");
            user.MerchantRequestStatus = "Pending"; await _context.SaveChangesAsync();
            return Ok(new { message = "Đã gửi yêu cầu!" });
        }
        // 1. API ĐỒNG BỘ NGẦM (Lấy Quyền mới nhất & Số thông báo chưa đọc)
        [HttpGet("sync")]
        [Authorize]
        public async Task<IActionResult> SyncData()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            int unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Ok(new { roleId = user.RoleId, unreadCount = unreadCount });
        }

        // 2. API LẤY DANH SÁCH THÔNG BÁO VÀ ĐÁNH DẤU ĐÃ ĐỌC
        [HttpGet("notifications")]
        [Authorize]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notis = await _context.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ToListAsync();

            // Đánh dấu đã đọc
            foreach (var n in notis.Where(n => !n.IsRead)) n.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(notis);
        }
    }


}
