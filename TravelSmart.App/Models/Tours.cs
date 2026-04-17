using System;
using System.ComponentModel.DataAnnotations;

namespace TravelSmart.App.Models
{
    // 🔥 Dữ liệu Tour và Thống kê analytics
    public class Tour { [Key] public Guid TourId { get; set; } public string Name { get; set; } public bool IsActive { get; set; } }
    public class TourDetail { [Key] public Guid TourDetailId { get; set; } public Guid TourId { get; set; } public Guid PoiId { get; set; } public int Order { get; set; } }
    public class VisitDurationDto { public string PoiId { get; set; } public double DurationMinutes { get; set; } }
}