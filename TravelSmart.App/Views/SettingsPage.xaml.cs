using TravelSmart.App.Services;
using System.IO;

namespace TravelSmart.App.Views;

public partial class SettingsPage : ContentPage
{
    private readonly DataService _dataService;

    public SettingsPage()
    {
        InitializeComponent();
        _dataService = new DataService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var lang = Preferences.Default.Get("DefaultLang", "vi");
        PickerLanguage.SelectedIndex = lang == "en" ? 1 : (lang == "ja" ? 2 : 0);

        var radius = Preferences.Default.Get("GeofenceRadius", 50);
        SliderRadius.Value = radius;
        LblRadiusValue.Text = $"{radius}m";

        var speed = Preferences.Default.Get("TtsSpeed", 1.0f);
        SliderTtsSpeed.Value = speed;
        LblSpeedValue.Text = $"{speed:F1}x";

        CalculateCacheSize();
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        string code = "vi";
        if (PickerLanguage.SelectedIndex == 1) code = "en";
        else if (PickerLanguage.SelectedIndex == 2) code = "ja";
        Preferences.Default.Set("DefaultLang", code);
    }

    private void OnRadiusSliderChanged(object sender, ValueChangedEventArgs e)
    {
        int val = (int)e.NewValue;
        LblRadiusValue.Text = $"{val}m";
        Preferences.Default.Set("GeofenceRadius", val);
    }

    private void OnSpeedSliderChanged(object sender, ValueChangedEventArgs e)
    {
        float val = (float)Math.Round(e.NewValue, 1);
        LblSpeedValue.Text = $"{val:F1}x";
        Preferences.Default.Set("TtsSpeed", val);
    }

    private void CalculateCacheSize()
    {
        try
        {
            long totalBytes = 0;
            var cacheDir = new DirectoryInfo(FileSystem.CacheDirectory);
            var files = cacheDir.GetFiles("*.mp3");
            foreach (var file in files) totalBytes += file.Length;
            double mbSize = totalBytes / (1024.0 * 1024.0);
            LblCacheSize.Text = $"Đang chiếm: {mbSize:F2} MB";
        }
        catch { LblCacheSize.Text = "Đang chiếm: 0 MB"; }
    }

    private async void OnClearCacheClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Xác nhận", "Bạn có chắc muốn xóa toàn bộ file âm thanh offline đã tải?", "Xóa", "Hủy");
        if (!confirm) return;

        try
        {
            var cacheDir = new DirectoryInfo(FileSystem.CacheDirectory);
            var files = cacheDir.GetFiles("*.mp3");
            int count = 0;
            foreach (var file in files) { file.Delete(); count++; }
            CalculateCacheSize();
            await DisplayAlert("Thành công", $"Đã dọn dẹp sạch sẽ {count} file âm thanh!", "OK");
        }
        catch { await DisplayAlert("Lỗi", "Không thể xóa bộ nhớ đệm lúc này.", "OK"); }
    }

    // 🔥 MÁY X-QUANG ĐỂ BẮT BUG TẠI CHỖ
    private async void OnSyncDataClicked(object sender, EventArgs e)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await DisplayAlert("Lỗi mạng", "Bạn cần bật Wifi/4G.", "OK");
            return;
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

            // Nhớ thay link nếu sếp đổi Ngrok
            var response = await client.GetAsync("https://articles-covers-logs-dist.trycloudflare.com/api/Pois");

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Lỗi Server", $"Server trả về mã: {response.StatusCode}", "OK");
                return;
            }

            var jsonString = await response.Content.ReadAsStringAsync();

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var serverPois = System.Text.Json.JsonSerializer.Deserialize<List<TravelSmart.App.Models.PoiModel>>(jsonString, options);

                if (serverPois != null && serverPois.Count > 0)
                {
                    // Nếu hiện cái bảng này, tức là 100% lỗi do SQLite Database bị khóa (Locked)
                    await DisplayAlert("Chẩn đoán bệnh", $"API ngon lành, đọc được {serverPois.Count} quán! Nhưng SQLite lưu vào máy bị lỗi (Database Locked) do đụng độ với màn hình chính!", "OK");
                }
                else
                {
                    await DisplayAlert("Chẩn đoán bệnh", "API trả về mảng rỗng (0 quán).", "OK");
                }
            }
            catch (Exception parseEx)
            {
                // Nếu hiện bảng này, tức là Model trong App viết sai tên cột so với API
                await DisplayAlert("Lỗi Dữ Liệu", $"API ngon nhưng App đọc không hiểu cấu trúc (Sai Model). Lỗi: {parseEx.Message}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Hệ Thống", $"Văng thẳng cẳng: {ex.Message}", "OK");
        }
    }
}