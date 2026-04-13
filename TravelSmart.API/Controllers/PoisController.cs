using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoisController : ControllerBase
    {
        private readonly VinhKhanhTravelDbContext _context;
        public PoisController(VinhKhanhTravelDbContext context) => _context = context;

        public class PoiCreateDto { public string Name { get; set; } public string Description { get; set; } public string Address { get; set; } public double Latitude { get; set; } public double Longitude { get; set; } public string QrCodeKey { get; set; } }
        public class MenuItemCreateDto { public string ItemName { get; set; } public decimal Price { get; set; } }
        public class ReviewCreateDto { public int Rating { get; set; } public string Comment { get; set; } }

        [HttpGet]
        public async Task<IActionResult> GetPOIs()
        {
            var query = _context.Pois.AsQueryable();
            if (User.Identity.IsAuthenticated && User.FindFirstValue(ClaimTypes.Role) == "Merchant")
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                query = query.Where(p => p.OwnerId == userId);
            }
            return Ok(await query.Select(p => new {
                id = p.PoiId.ToString(),
                latitude = p.Latitude,
                longitude = p.Longitude,
                qrCodeKey = p.QrCodeKey,
                address = p.Address,
                name = _context.PoiTranslations.Where(t => t.PoiId == p.PoiId && t.LanguageCode == "vi").Select(t => t.Name).FirstOrDefault() ?? "Chưa có tên",
                description = _context.PoiTranslations.Where(t => t.PoiId == p.PoiId && t.LanguageCode == "vi").Select(t => t.Description).FirstOrDefault() ?? "",
                ttsContent = "Chào mừng bạn đến với " + (_context.PoiTranslations.Where(t => t.PoiId == p.PoiId && t.LanguageCode == "vi").Select(t => t.Name).FirstOrDefault() ?? "")
            }).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPoiDetail(Guid id)
        {
            var poi = await _context.Pois.FindAsync(id); if (poi == null) return NotFound();
            var menu = await _context.MenuItems.Where(m => m.PoiId == id).Select(m => new { m.ItemId, m.ItemName, m.Price }).ToListAsync();
            var reviews = await _context.Reviews.Where(r => r.PoiId == id).Select(r => new { r.Rating, r.Comment, r.CreatedAt }).ToListAsync();
            return Ok(new { menu = menu, reviews = reviews });
        }

        [HttpPost("{id}/menu")]
        [Authorize(Roles = "Merchant")]
        public async Task<IActionResult> AddMenuItem(Guid id, MenuItemCreateDto request)
        {
            _context.MenuItems.Add(new TravelSmart.API.Models.MenuItem { ItemId = Guid.NewGuid(), PoiId = id, ItemName = request.ItemName, Price = request.Price });
            await _context.SaveChangesAsync(); return Ok();
        }

        [HttpDelete("menu/{itemId}")]
        [Authorize(Roles = "Merchant")]
        public async Task<IActionResult> DeleteMenuItem(Guid itemId)
        {
            var item = await _context.MenuItems.FindAsync(itemId); if (item != null) { _context.MenuItems.Remove(item); await _context.SaveChangesAsync(); }
            return Ok();
        }

        [HttpPost("{id}/review")]
        [Authorize]
        public async Task<IActionResult> AddReview(Guid id, ReviewCreateDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var poi = await _context.Pois.FindAsync(id);
            if (poi != null && poi.OwnerId == userId) return BadRequest("Chủ quán không được tự đánh giá!");
            _context.Reviews.Add(new Review { ReviewId = Guid.NewGuid(), PoiId = id, UserId = userId, Rating = request.Rating, Comment = request.Comment, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync(); return Ok(new { message = "Đã đánh giá!" });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Merchant")]
        public async Task<IActionResult> CreatePOI(PoiCreateDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var newPoi = new Poi { PoiId = Guid.NewGuid(), Latitude = request.Latitude, Longitude = request.Longitude, QrCodeKey = request.QrCodeKey, RadiusMeter = 50, IsActive = true, CreatedAt = DateTime.Now, OwnerId = userId, Address = request.Address };
            _context.Pois.Add(newPoi);
            _context.PoiTranslations.Add(new PoiTranslation { TranslationId = Guid.NewGuid(), PoiId = newPoi.PoiId, LanguageCode = "vi", Name = request.Name, Description = request.Description });
            await _context.SaveChangesAsync(); return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Merchant")]
        public async Task<IActionResult> DeletePOI(Guid id)
        {
            var poi = await _context.Pois.FindAsync(id); if (poi == null) return NotFound();
            var role = User.FindFirstValue(ClaimTypes.Role); var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (role == "Merchant" && poi.OwnerId != userId) return StatusCode(403);
            _context.PoiTranslations.RemoveRange(_context.PoiTranslations.Where(t => t.PoiId == id));
            if (_context.MenuItems != null) _context.MenuItems.RemoveRange(_context.MenuItems.Where(m => m.PoiId == id));
            if (_context.Reviews != null) _context.Reviews.RemoveRange(_context.Reviews.Where(r => r.PoiId == id));
            if (_context.Orders != null) _context.Orders.RemoveRange(_context.Orders.Where(o => o.PoiId == id));
            _context.Pois.Remove(poi); await _context.SaveChangesAsync(); return Ok();
        }

        [HttpPost("history")]
        public IActionResult PostHistory([FromBody] object log) => Ok();
    }
}