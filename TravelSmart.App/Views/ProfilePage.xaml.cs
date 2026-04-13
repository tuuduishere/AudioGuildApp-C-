using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TravelSmart.App.Views;

public partial class ProfilePage : ContentPage
{
    private const string ApiBaseUrl = "http://10.0.2.2:5088/api";

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Tải tốc độ cao từ bộ nhớ tạm (Giao diện không bị giật)
        var username = await SecureStorage.Default.GetAsync("username") ?? "Khách hàng";
        var role = await SecureStorage.Default.GetAsync("role") ?? "User";
        UpdateUI(username, role);

        // 2. Tự động đâm API ngầm để lấy Quyền mới nhất (Sợ lúc nãy Timer chưa kịp chạy)
        await ForceSyncProfileRole();
    }

    // Hàm đổi chữ giao diện
    private void UpdateUI(string username, string role)
    {
        LblUsername.Text = username;
        LblRole.Text = role == "Merchant" ? "Chủ quán" : (role == "Admin" ? "Quản trị viên" : "Khách du lịch");

        // Ẩn bảng xin làm chủ quán nếu đã là Chủ quán hoặc Admin
        if (role == "Merchant" || role == "Admin")
        {
            FrameMerchantRequest.IsVisible = false;
        }
    }

    // HÀM ÉP CẬP NHẬT QUYỀN
    private async Task ForceSyncProfileRole()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("authToken");
            if (string.IsNullOrEmpty(token)) return;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{ApiBaseUrl}/Auth/sync");
            if (response.IsSuccessStatusCode)
            {
                // Tận dụng lại class SyncDataDto của MainPage cho tiện
                var data = await response.Content.ReadFromJsonAsync<MainPage.SyncDataDto>();
                if (data != null)
                {
                    string realRole = data.roleId == 1 ? "Admin" : (data.roleId == 2 ? "Merchant" : "User");
                    await SecureStorage.Default.SetAsync("role", realRole);
                    UpdateUI(LblUsername.Text, realRole); // Cập nhật lại UI lặp tức nếu có thay đổi!
                }
            }
        }
        catch { } // Rớt mạng thì thôi, xài đồ cũ
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage());
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        SecureStorage.Default.RemoveAll();
        Preferences.Default.Clear();
        Application.Current.MainPage = new NavigationPage(new LoginPage());
    }

    private async void OnRequestMerchantClicked(object sender, EventArgs e)
    {
        var token = await SecureStorage.Default.GetAsync("authToken");
        if (string.IsNullOrEmpty(token)) return;

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.PostAsync($"{ApiBaseUrl}/Auth/request-merchant", null);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Tuyệt vời", "Đã gửi yêu cầu thành công! Admin sẽ sớm liên hệ duyệt đơn cho bạn.", "OK");
            }
            else
            {
                await DisplayAlert("Thông báo", "Yêu cầu của bạn đang chờ duyệt rồi, đừng bấm nữa!", "OK");
            }
        }
        catch
        {
            await DisplayAlert("Lỗi mạng", "Không kết nối được với máy chủ.", "OK");
        }
    }
}