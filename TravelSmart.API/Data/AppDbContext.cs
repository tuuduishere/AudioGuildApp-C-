using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Models;

namespace TravelSmart.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Place> Places { get; set; }
    }
}