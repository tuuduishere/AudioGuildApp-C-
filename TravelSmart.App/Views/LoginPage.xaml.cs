using System.Net.Http.Json;
using TravelSmart.App.Models;

namespace TravelSmart.App.Views;

public partial class LoginPage : ContentPage
{
    private const string ApiLoginUrl = "https://rule-twiddling-recoil.ngrok-free.dev/api/Auth/login";
    private readonly HttpClient _httpClient;

    public LoginPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    }

    public class AuthResponse { public string token { get; set; } public string role { get; set; } }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = EntryUsername.Text?.Trim() ?? "";
        string password = EntryPassword.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) { await DisplayAlert("Lỗi", "Vui lòng nhập tên đăng nhập và mật khẩu!", "OK"); return; }

        BtnLogin.IsEnabled = false; BtnLogin.Text = "ĐANG KẾT NỐI...";
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiLoginUrl, new { Username = username, Password = password });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    Preferences.Default.Set("jwt_token", result.token);
                    await SecureStorage.Default.SetAsync("authToken", result.token);
                    await SecureStorage.Default.SetAsync("username", username);
                    await SecureStorage.Default.SetAsync("role", result.role);
                    AppSession.CurrentUser = new UserModel { Username = username, Role = result.role };

                    Application.Current.MainPage = new NavigationPage(new MainPage());
                }
            }
            else await DisplayAlert("Thất bại", "Sai tên đăng nhập hoặc mật khẩu!", "OK");
        }
        catch { await DisplayAlert("Lỗi", "Không thể kết nối đến máy chủ!", "OK"); }
        finally { BtnLogin.IsEnabled = true; BtnLogin.Text = "ĐĂNG NHẬP"; }
    }

    // Chuyển sang trang Đăng Ký
    private async void OnGoToRegisterClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}