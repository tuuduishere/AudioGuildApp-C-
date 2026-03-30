using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Data;
using TravelSmart.API.Models;

namespace TravelSmart.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlacesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlacesController(AppDbContext context)
    {
        _context = context;
    }

    // Lấy toàn bộ danh sách: GET /api/places
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Place>>> GetPlaces()
    {
        return await _context.Places.ToListAsync();
    }

    // Lấy chi tiết 1 điểm: GET /api/places/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Place>> GetPlace(int id)
    {
        var place = await _context.Places.FindAsync(id);
        if (place == null) return NotFound();
        return place;
    }

    // API tìm điểm gần nhất: GET /api/places/nearby?userLat=...&userLon=...
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] double userLat, [FromQuery] double userLon, [FromQuery] double radiusKm = 0.5)
    {
        var allPlaces = await _context.Places.ToListAsync();

        var nearbyPlaces = allPlaces.Where(p =>
            CalculateDistance(userLat, userLon, p.Latitude, p.Longitude) <= radiusKm)
            .ToList();

        return Ok(nearbyPlaces);
    }

    // Thuật toán tính khoảng cách (km)
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371; // Bán kính Trái Đất
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double angle) => angle * Math.PI / 180;
}