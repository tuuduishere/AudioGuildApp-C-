using System.ComponentModel.DataAnnotations;

namespace TravelSmart.API.Models
{
    public class Tour
    {
        [Key]
        public Guid TourId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class TourDetail
    {
        [Key]
        public Guid TourDetailId { get; set; }
        public Guid TourId { get; set; }
        public Guid PoiId { get; set; }
        public int Order { get; set; } // Thứ tự đi trong tour
    }
}