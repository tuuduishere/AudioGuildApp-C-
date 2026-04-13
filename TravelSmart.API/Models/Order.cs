using System.ComponentModel.DataAnnotations;

namespace TravelSmart.API.Models
{
    public class Order
    {
        [Key] // Dán bùa Khóa chính
        public Guid OrderId { get; set; }

        public Guid PoiId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
    }
}