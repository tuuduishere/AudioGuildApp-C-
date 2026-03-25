using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Data; // Dùng chung Data từ API
using TravelSmart.API.Models;

namespace TravelSmart.Admin.Controllers
{
    public class PlacesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PlacesController(AppDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // Trang danh sách địa điểm
        public async Task<IActionResult> Index()
        {
            return View(await _context.Places.ToListAsync());
        }

        // Trang thêm mới
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Place place, IFormFile? imageFile, IFormFile? audioFile)
        {
            if (imageFile != null) place.ImageUrl = await SaveFile(imageFile, "images");
            if (audioFile != null) place.AudioUrl = await SaveFile(audioFile, "audio");

            _context.Add(place);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string path = Path.Combine(wwwRootPath, folder, fileName);

            Directory.CreateDirectory(Path.Combine(wwwRootPath, folder));
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return $"/{folder}/{fileName}";
        }

        public async Task<IActionResult> Delete(int id)
        {
            var place = await _context.Places.FindAsync(id);
            if (place != null)
            {
                _context.Places.Remove(place);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}