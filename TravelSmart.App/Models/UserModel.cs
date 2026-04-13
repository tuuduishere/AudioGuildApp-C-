namespace TravelSmart.App.Models;

public class UserModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; }
    public string Role { get; set; } // "Admin", "Merchant", "User"
}

// Lớp tĩnh này dùng để lưu tạm thông tin người dùng đang đăng nhập (Session)
public static class AppSession
{
    public static UserModel CurrentUser { get; set; }
}