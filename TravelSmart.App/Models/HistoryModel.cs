using SQLite;

namespace TravelSmart.App.Models;

public class HistoryModel
{
    [PrimaryKey, AutoIncrement] // Tự động tăng số thứ tự (1, 2, 3...)
    public int Id { get; set; }

    public int PoiId { get; set; }
    public string PoiName { get; set; }

    // Lưu lại chính xác ngày giờ khách ghé thăm
    public DateTime VisitedAt { get; set; }
}