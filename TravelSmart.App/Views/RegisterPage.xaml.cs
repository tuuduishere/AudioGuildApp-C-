using System.Net.Http.Json;

namespace TravelSmart.App.Views;

public partial class RegisterPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public RegisterPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string username = EntryUsername.Text?.Trim() ?? "";
        string email = EntryEmail.Text?.Trim() ?? "";
        string password = EntryPassword.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
        {
            await DisplayAlert("Lỗi", "Vui lòng điền đủ Tên đăng nhập, Email và Mật khẩu!", "OK"); return;
        }

        BtnRegister.IsEnabled = false; BtnRegister.Text = "ĐANG XỬ LÝ...";
        try
        {
            // 🔥 Ăn link từ AppConfig
            var response = await _httpClient.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Auth/register", new { Username = username, Password = password, Email = email });
            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Thành công", "Đăng ký thành công! Hãy đăng nhập.", "OK");
                await Navigation.PopModalAsync();
            }
            else await DisplayAlert("Lỗi", "Tên đăng nhập đã tồn tại!", "OK");
        }
        catch { await DisplayAlert("Lỗi", "Không thể kết nối đến máy chủ!", "OK"); }
        finally { BtnRegister.IsEnabled = true; BtnRegister.Text = "ĐĂNG KÝ NGAY"; }
    }

    private async void OnGoToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}