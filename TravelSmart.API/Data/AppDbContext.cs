using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Models; // Gọi thư mục Models ra để xài PoiEntity

namespace TravelSmart.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Đổi Place thành PoiEntity, và đặt tên bảng là Pois
    public DbSet<PoiEntity> Pois { get; set; }
}