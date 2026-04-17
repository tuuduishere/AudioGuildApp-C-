using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelSmart.API.Models;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;
using System.Text.Json;

namespace TravelSmart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoisController : ControllerBase
    {
        private readonly VinhKhanhTravelDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PoisController(VinhKhanhTravelDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public class PoiCreateDto { public string? Name { get; set; } public string? Description { get; set; } public string? Address { get; set; } public double Latitude { get; set; } public double Longitude { get; set; } public string? QrCodeKey { get; set; } }
        public class MenuItemCreateDto { public string? ItemName { get; set; } public decimal Price { get; set; } }
        public class ReviewCreateDto { public int Rating { get; set; } public string? Comment { get; set; } }

        [HttpGet]
        public async Task<IActionResult> GetPOIs()
        {
            var query = _context.Pois.AsQueryable();
            if (User.Identity.IsAuthenticated && User.FindFirstValue(ClaimTypes.Role) == "Merchant")
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                query = query.Where(p => p.OwnerId == userId);
            }

            var pois = await query.ToListAsync();
            var translations = await _context.PoiTranslations.ToListAsync();

            var result = pois.Select(p => {
                var transVi = translations.FirstOrDefault(t => t.PoiId == p.PoiId && t.LanguageCode == "vi");
                string name = transVi?.Name ?? "Chưa có tên";
                string desc = transVi?.Description ?? "";

                return new
                {
                    id = p.PoiId.ToString(),
                    latitude = p.Latitude,
                    longitude = p.Longitude,
                    qrCodeKey = p.QrCodeKey,
                    address = p.Address,
                    audioUrl = p.AudioUrl != null ? $"{Request.Scheme}://{Request.Host}{p.AudioUrl}" : null,
                    imageUrl = p.ImageUrl != null ? $"{Request.Scheme}://{Request.Host}{p.ImageUrl}" : null,
                    name = name,
                    description = desc,
                    ttsContent = $"Chào mừng bạn đến với {name}. {desc}"
                };
            }).ToList();

            return Ok(result);
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
        [Authorize(Roles = "Merchant,Admin")]
        public async Task<IActionResult> AddMenuItem(Guid id, MenuItemCreateDto request)
        {
            _context.MenuItems.Add(new TravelSmart.API.Models.MenuItem { ItemId = Guid.NewGuid(), PoiId = id, ItemName = request.ItemName ?? "Món mới", Price = request.Price });
            await _context.SaveChangesAsync(); return Ok();
        }

        [HttpDelete("menu/{itemId}")]
        [Authorize(Roles = "Merchant,Admin")]
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
            _context.Reviews.Add(new Review { ReviewId = Guid.NewGuid(), PoiId = id, UserId = userId, Rating = request.Rating, Comment = request.Comment ?? "", CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync(); return Ok(new { message = "Đã đánh giá!" });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Merchant")]
        public async Task<IActionResult> CreatePOI(PoiCreateDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var newPoi = new Poi { PoiId = Guid.NewGuid(), Latitude = request.Latitude, Longitude = request.Longitude, QrCodeKey = request.QrCodeKey ?? $"QR_{Guid.NewGuid()}", RadiusMeter = 50, IsActive = true, CreatedAt = DateTime.Now, OwnerId = userId, Address = request.Address ?? "" };

            _context.Pois.Add(newPoi);
            _context.PoiTranslations.Add(new PoiTranslation { TranslationId = Guid.NewGuid(), PoiId = newPoi.PoiId, LanguageCode = "vi", Name = request.Name ?? "Quán mới", Description = request.Description ?? "" });

            await _context.SaveChangesAsync();

            string baseText = $"Chào mừng bạn đến với {request.Name}. {request.Description}";
            _ = Task.Run(async () => {
                await GenerateEdgeTts(newPoi.PoiId, baseText, "vi-VN-HoaiMyNeural", "vi");
                string enText = await TranslateText(baseText, "en");
                await GenerateEdgeTts(newPoi.PoiId, enText, "en-US-AriaNeural", "en");
                string jaText = await TranslateText(baseText, "ja");
                await GenerateEdgeTts(newPoi.PoiId, jaText, "ja-JP-NanamiNeural", "ja");
            });

            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Merchant")]
        public async Task<IActionResult> UpdatePOI(Guid id, PoiCreateDto request)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return NotFound();

            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (role == "Merchant" && poi.OwnerId != userId) return StatusCode(403);

            poi.Latitude = request.Latitude;
            poi.Longitude = request.Longitude;
            poi.Address = request.Address ?? poi.Address;

            var transVi = await _context.PoiTranslations.FirstOrDefaultAsync(t => t.PoiId == id && t.LanguageCode == "vi");
            if (transVi != null)
            {
                transVi.Name = request.Name ?? transVi.Name;
                transVi.Description = request.Description ?? transVi.Description;
            }

            await _context.SaveChangesAsync();

            // Sinh lại file AI ngầm khi update
            string baseText = $"Chào mừng bạn đến với {request.Name}. {request.Description}";
            _ = Task.Run(async () => {
                await GenerateEdgeTts(id, baseText, "vi-VN-HoaiMyNeural", "vi");
                string enText = await TranslateText(baseText, "en");
                await GenerateEdgeTts(id, enText, "en-US-AriaNeural", "en");
                string jaText = await TranslateText(baseText, "ja");
                await GenerateEdgeTts(id, jaText, "ja-JP-NanamiNeural", "ja");
            });

            return Ok();
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

            var visitLogs = await _context.VisitLogs.Where(v => v.PoiId == id).ToListAsync();
            if (visitLogs.Any()) _context.VisitLogs.RemoveRange(visitLogs);

            var tourDetails = await _context.TourDetails.Where(t => t.PoiId == id).ToListAsync();
            if (tourDetails.Any()) _context.TourDetails.RemoveRange(tourDetails);

            _context.Pois.Remove(poi);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{id}/upload-audio")]
        [Authorize(Roles = "Admin,Merchant")]
        public async Task<IActionResult> UploadAudio(Guid id, IFormFile file)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return NotFound();
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (role == "Merchant" && poi.OwnerId != userId) return StatusCode(403);

            if (file == null || file.Length == 0) return BadRequest("File rỗng.");
            if (!file.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return BadRequest("Chỉ hỗ trợ file .mp3");

            string webRootPath = string.IsNullOrWhiteSpace(_env.WebRootPath) ? Path.Combine(_env.ContentRootPath, "wwwroot") : _env.WebRootPath;
            var uploadsFolder = Path.Combine(webRootPath, "audio");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = $"manual_{id}_{DateTime.Now.Ticks}.mp3";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

            poi.AudioUrl = $"/audio/{fileName}";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tải file âm thanh thành công!", audioUrl = poi.AudioUrl });
        }

        [HttpPost("{id}/upload-image")]
        [Authorize(Roles = "Admin,Merchant")]
        public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return NotFound();
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (role == "Merchant" && poi.OwnerId != userId) return StatusCode(403);

            if (file == null || file.Length == 0) return BadRequest("File rỗng.");
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") return BadRequest("Chỉ hỗ trợ file ảnh .jpg, .png");

            string webRootPath = string.IsNullOrWhiteSpace(_env.WebRootPath) ? Path.Combine(_env.ContentRootPath, "wwwroot") : _env.WebRootPath;
            var uploadsFolder = Path.Combine(webRootPath, "images");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{id}_{DateTime.Now.Ticks}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

            poi.ImageUrl = $"/images/{fileName}";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tải ảnh thành công!", imageUrl = poi.ImageUrl });
        }

        // Model hứng Data Log
        public class HistoryLogDto
        {
            public Guid PoiId { get; set; }
            public string? DeviceName { get; set; }
            public string? LanguageCode { get; set; }
            public double DurationMinutes { get; set; }
        }

        // 🔥 FIX: Lưu Log Lịch sử của khách vào DB để lên Heatmap
        [HttpPost("history")]
        [AllowAnonymous] // App gửi mà ko cần đăng nhập
        public async Task<IActionResult> PostHistory([FromBody] HistoryLogDto log)
        {
            try
            {
                var newLog = new VisitLog
                {
                    LogId = Guid.NewGuid(),
                    PoiId = log.PoiId,
                    DeviceName = string.IsNullOrWhiteSpace(log.DeviceName) ? "TravelSmart App" : log.DeviceName,
                    LanguageCode = string.IsNullOrWhiteSpace(log.LanguageCode) ? "vi" : log.LanguageCode,
                    DurationMinutes = log.DurationMinutes > 0 ? log.DurationMinutes : 1.5,
                    VisitTime = DateTime.Now
                };

                _context.VisitLogs.Add(newLog);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch { return BadRequest(); }
        }

        [HttpGet("generate-missing-audio")]
        public async Task<IActionResult> GenerateMissingAudio()
        {
            var pois = await _context.Pois.ToListAsync();
            var translations = await _context.PoiTranslations.Where(t => t.LanguageCode == "vi").ToListAsync();

            string webRootPath = string.IsNullOrWhiteSpace(_env.WebRootPath) ? Path.Combine(_env.ContentRootPath, "wwwroot") : _env.WebRootPath;
            var uploadsFolder = Path.Combine(webRootPath, "audio");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            _ = Task.Run(async () =>
            {
                foreach (var p in pois)
                {
                    string viPath = Path.Combine(uploadsFolder, $"{p.PoiId}_vi.mp3");

                    if (!System.IO.File.Exists(viPath))
                    {
                        var trans = translations.FirstOrDefault(t => t.PoiId == p.PoiId);
                        if (trans != null && !string.IsNullOrWhiteSpace(trans.Name))
                        {
                            string baseText = $"Chào mừng bạn đến với {trans.Name}. {trans.Description}";
                            await GenerateEdgeTts(p.PoiId, baseText, "vi-VN-HoaiMyNeural", "vi");
                            string enText = await TranslateText(baseText, "en");
                            await GenerateEdgeTts(p.PoiId, enText, "en-US-AriaNeural", "en");
                            string jaText = await TranslateText(baseText, "ja");
                            await GenerateEdgeTts(p.PoiId, jaText, "ja-JP-NanamiNeural", "ja");
                        }
                    }
                }
            });

            return Ok(new { message = "🚀 Đang kích hoạt AI chạy ngầm để bổ sung MP3 cho quán SQL!" });
        }

        private async Task<string> TranslateText(string text, string targetLang)
        {
            try
            {
                string cleanText = text.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
                if (cleanText.Length > 1000) cleanText = cleanText.Substring(0, 995) + "...";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl={targetLang}&dt=t&q={Uri.EscapeDataString(cleanText)}";
                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                string translatedText = "";
                foreach (var chunk in doc.RootElement[0].EnumerateArray())
                {
                    if (chunk[0].ValueKind == JsonValueKind.String) translatedText += chunk[0].GetString();
                }
                return string.IsNullOrWhiteSpace(translatedText) ? text : translatedText;
            }
            catch { return text; }
        }

        private async Task<string> GenerateEdgeTts(Guid poiId, string text, string voice, string langCode)
        {
            try
            {
                string webRootPath = string.IsNullOrWhiteSpace(_env.WebRootPath) ? Path.Combine(_env.ContentRootPath, "wwwroot") : _env.WebRootPath;
                var uploadsFolder = Path.Combine(webRootPath, "audio");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = $"{poiId}_{langCode}.mp3";
                string filePath = Path.Combine(uploadsFolder, fileName);
                string safeText = text.Replace("\"", "\\\"");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "edge-tts",
                    Arguments = $"--voice {voice} --text \"{safeText}\" --write-media \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                await process.WaitForExitAsync();
                return $"/audio/{fileName}";
            }
            catch { return null; }
        }
    }
}