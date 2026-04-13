namespace TravelSmart.App.Models;

public class VisitLogModel
{
    [SQLite.PrimaryKey, SQLite.AutoIncrement]
    public int Id { get; set; }
    public string PoiId { get; set; }
    public string Name { get; set; }
    public DateTime VisitTime { get; set; }
}