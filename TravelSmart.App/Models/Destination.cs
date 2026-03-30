namespace TravelSmart.App.Models;
    

public class Destination
{
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public double Rating { get; set; }
    public string Price { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
}
