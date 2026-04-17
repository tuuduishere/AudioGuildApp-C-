using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Maui.ApplicationModel;

namespace TravelSmart.App.Views;

public partial class ProfilePage : ContentPage
{
    private const string ApiBaseUrl = "https://rule-twiddling-recoil.ngrok-free.dev/api";

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var username = await SecureStorage.Default.GetAsync("username") ?? "Khách hàng";
        var role = await SecureStorage.Default.GetAsync("role") ?? "Guest";
        UpdateUI(username, role);

        await ForceSyncProfileRole();
    }

    private void UpdateUI(string username, string role)
    {
        if (role == "Guest" || string.IsNullOrEmpty(role))
        {
            LblUsername.Text = "Khách Vãng Lai";
            LblRole.Text = "Vui lòng đăng nhập để lưu trữ";
            FrameMerchantRequest.IsVisible = false;
            BtnLogout.IsVisible = false;
            BtnLogin.IsVisible = true;
        }
        else
        {
            LblUsername.Text = username;
            LblRole.Text = role == "Merchant" ? "Chủ quán" : (role == "Admin" ? "Quản trị viên" : "Khách du lịch");

            FrameMerchantRequest.IsVisible = (role == "User");
            BtnLogout.IsVisible = true;
            BtnLogin.IsVisible = false;
        }
    }

    private async Task ForceSyncProfileRole()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("authToken");
            if (string.IsNullOrEmpty(token)) return;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{ApiBaseUrl}/Auth/sync");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<MainPage.SyncDataDto>();
                if (data != null)
                {
                    string realRole = data.roleId == 1 ? "Admin" : (data.roleId == 2 ? "Merchant" : "User");
                    await SecureStorage.Default.SetAsync("role", realRole);
                    UpdateUI(LblUsername.Text, realRole);
                }
            }
        }
        catch { }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage());
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new LoginPage());
    }

    // 🔥 FIX LUỒNG ĐĂNG XUẤT AN TOÀN
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        SecureStorage.Default.RemoveAll();
        Preferences.Default.Clear();

        await SecureStorage.Default.SetAsync("role", "Guest");

        UpdateUI("Khách Vãng Lai", "Guest");

        // Lùi về màn hình Bản Đồ tự nhiên, không khởi tạo lại từ đầu gây sốc
        await Navigation.PopAsync();
    }

    private async void OnRequestMerchantClicked(object sender, EventArgs e)
    {
        var token = await SecureStorage.Default.GetAsync("authToken");
        if (string.IsNullOrEmpty(token)) return;

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.PostAsync($"{ApiBaseUrl}/Auth/request-merchant", null);
            if (response.IsSuccessStatusCode)
                await DisplayAlert("Tuyệt vời", "Đã gửi yêu cầu thành công! Admin sẽ sớm liên hệ duyệt đơn cho bạn.", "OK");
            else
                await DisplayAlert("Thông báo", "Yêu cầu của bạn đang chờ duyệt rồi, đừng bấm nữa!", "OK");
        }
        catch
        {
            await DisplayAlert("Lỗi mạng", "Không kết nối được với máy chủ.", "OK");
        }
    }
}