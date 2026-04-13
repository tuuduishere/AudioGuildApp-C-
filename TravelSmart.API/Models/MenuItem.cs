using System.ComponentModel.DataAnnotations;

namespace TravelSmart.API.Models
{
    public class MenuItem
    {
        [Key] // Dán bùa Khóa chính vào đây!
        public Guid ItemId { get; set; }

        public Guid PoiId { get; set; }
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}