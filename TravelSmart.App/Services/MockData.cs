using TravelSmart.App.Models;

namespace TravelSmart.App.Services;

public static class MockData
{
    public static List<Destination> Destinations = new()
    {
        new() { Name="Chợ Bến Thành", Image="dotnet_bot.png", Rating=4.6, Price="Free", Lat=10.772, Lng=106.698, IsFavorite=true, Description="Chợ truyền thống nổi tiếng, nhiều món ăn đường phố.", Images=new List<string>{"dotnet_bot.png","dotnet_bot.png"} },
        new() { Name="Nhà thờ Đức Bà", Image="dotnet_bot.png", Rating=4.7, Price="Free", Lat=10.779, Lng=106.699, Description="Kiến trúc Pháp cổ, biểu tượng của Sài Gòn.", Images=new List<string>{"dotnet_bot.png"} },
        new() { Name="Bitexco", Image="dotnet_bot.png", Rating=4.5, Price="200k", Lat=10.771, Lng=106.704, Description="Trung tâm thương mại, tòa nhà chọc trời với observation deck.", Images=new List<string>{"dotnet_bot.png"} },
        new() { Name="Landmark 81", Image="dotnet_bot.png", Rating=4.8, Price="300k", Lat=10.795, Lng=106.721, IsFavorite=true, Description="Toà nhà cao nhất Việt Nam với view toàn cảnh.", Images=new List<string>{"dotnet_bot.png"} }
    };

    public static List<Food> Foods = new()
    {
        new() { Name="Cơm tấm", Category="Cơm", Image="dotnet_bot.png" },
        new() { Name="Hủ tiếu", Category="Nước", Image="dotnet_bot.png" },
        new() { Name="Bánh mì", Category="Nhanh", Image="dotnet_bot.png" }
    };
}
