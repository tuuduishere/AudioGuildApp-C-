using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly VinhKhanhTravelDbContext _context;

        public ToursController(VinhKhanhTravelDbContext context) { _context = context; }

        [HttpGet]
        public async Task<IActionResult> GetTours()
        {
            return Ok(await _context.Tours.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> CreateTour(Tour request)
        {
            request.TourId = Guid.NewGuid();
            request.CreatedAt = DateTime.Now;
            _context.Tours.Add(request);
            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetTourDetails(Guid id)
        {
            var details = await _context.TourDetails
                .Where(td => td.TourId == id)
                .OrderBy(td => td.Order)
                .Join(_context.Pois, td => td.PoiId, p => p.PoiId, (td, p) => new {
                    p.PoiId,
                    Name = _context.PoiTranslations.Where(t => t.PoiId == p.PoiId && t.LanguageCode == "vi").Select(t => t.Name).FirstOrDefault(),
                    td.Order
                }).ToListAsync();
            return Ok(details);
        }

        [HttpPost("{id}/add-poi")]
        public async Task<IActionResult> AddPoiToTour(Guid id, [FromBody] Guid poiId)
        {
            var count = await _context.TourDetails.Where(t => t.TourId == id).CountAsync();
            var detail = new TourDetail
            {
                TourDetailId = Guid.NewGuid(),
                TourId = id,
                PoiId = poiId,
                Order = count + 1
            };
            _context.TourDetails.Add(detail);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}