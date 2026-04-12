namespace TravelSmart.API.Models;

public class AnalyticsLog
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string ActionType { get; set; }
    public DateTime Timestamp { get; set; }
}