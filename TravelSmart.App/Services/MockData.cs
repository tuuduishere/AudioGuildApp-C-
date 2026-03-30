using TravelSmart.App.Models;

namespace TravelSmart.App.Services;

public static class MockData
{
    public static List<Destination> Destinations = new()
    {
        new() { Name="Chợ Bến Thành", Image="dotnet_bot.png", Rating=4.6, Price="Free", Lat=10.772, Lng=106.698 },
        new() { Name="Nhà thờ Đức Bà", Image="dotnet_bot.png", Rating=4.7, Price="Free", Lat=10.779, Lng=106.699 },
        new() { Name="Bitexco", Image="dotnet_bot.png", Rating=4.5, Price="200k", Lat=10.771, Lng=106.704 },
        new() { Name="Landmark 81", Image="dotnet_bot.png", Rating=4.8, Price="300k", Lat=10.795, Lng=106.721 }
    };

    public static List<Food> Foods = new()
    {
        new() { Name="Cơm tấm", Category="Cơm", Image="dotnet_bot.png" },
        new() { Name="Hủ tiếu", Category="Nước", Image="dotnet_bot.png" },
        new() { Name="Bánh mì", Category="Nhanh", Image="dotnet_bot.png" }
    };
}
