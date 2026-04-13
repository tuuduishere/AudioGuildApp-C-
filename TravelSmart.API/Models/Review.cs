using System.ComponentModel.DataAnnotations;

namespace TravelSmart.API.Models
{
    public class Review
    {
        [Key] // Dán bùa Khóa chính
        public Guid ReviewId { get; set; }

        public Guid PoiId { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}