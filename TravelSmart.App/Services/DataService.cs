using System.Net.Http.Json;
using SQLite;
using TravelSmart.App.Models;

namespace TravelSmart.App.Services;

public class DataService
{
    private SQLiteAsyncConnection _db;
    private HttpClient _http;

    private readonly string ApiUrl = DeviceInfo.Platform == DevicePlatform.Android
        ? "https://10.0.2.2:7008/api/poi"
        : "https://localhost:7008/api/poi";
    public DataService()
    {
        InitDB();
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
        _http = new HttpClient(handler);
    }

    private void InitDB()
    {
        if (_db != null) return;
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "TravelSmart.db");
        _db = new SQLiteAsyncConnection(dbPath);

        // TẠO 2 BẢNG: 1 Bảng Quán Ốc, 1 Bảng Lịch Sử
        _db.CreateTableAsync<PoiModel>().Wait();
        _db.CreateTableAsync<HistoryModel>().Wait();
    }

    // --- CÁC HÀM CỦA POI (QUÁN ỐC) CŨ ---
    public async Task<List<PoiModel>> GetPOIsAsync() => await _db.Table<PoiModel>().ToListAsync();

    public async Task<bool> SyncFromServerAsync()
    {
        try
        {
            var serverData = await _http.GetFromJsonAsync<List<PoiModel>>(ApiUrl);

            // Kết nối thành công rồi! Dọn dẹp sạch sẽ kho cũ trong điện thoại trước.
            await _db.DeleteAllAsync<PoiModel>();

            // Nếu Server có quán ốc thì nạp vào, không có thì bỏ qua (bản đồ trống)
            if (serverData != null && serverData.Count > 0)
            {
                await _db.InsertAllAsync(serverData);
            }

            return true; // Kết nối lấy data (dù rỗng hay không) đều tính là Thành Công!
        }
        catch (Exception ex)
        {
            // Nếu đứt mạng thật, in lỗi ra để anh em mình soi bệnh
            System.Diagnostics.Debug.WriteLine($"LỖI ĐỨT CÁP THẬT: {ex.Message}");
            return false;
        }
    }

    // ==========================================
    // CÁC HÀM MỚI CHO LỊCH SỬ (HISTORY)
    // ==========================================

    // 1. Ghi sổ khi khách ghé quán
    public async Task AddHistoryAsync(PoiModel poi)
    {
        var record = new HistoryModel
        {
            PoiId = poi.Id,
            PoiName = poi.Name,
            VisitedAt = DateTime.Now
        };
        await _db.InsertAsync(record);
    }

    // 2. Lấy danh sách lịch sử (Xếp mới nhất lên đầu)
    public async Task<List<HistoryModel>> GetHistoryAsync()
    {
        return await _db.Table<HistoryModel>().OrderByDescending(h => h.VisitedAt).ToListAsync();
    }

    // 3. Xóa trắng lịch sử (Dùng cho nút Cài đặt)
    public async Task ClearHistoryAsync()
    {
        await _db.DeleteAllAsync<HistoryModel>();
    }
    // ==========================================
    // TÍNH NĂNG ANALYTICS (GỬI LOG LÊN SERVER)
    // ==========================================
    public async Task SendAnalyticsAsync(int poiId, string actionType)
    {
        try
        {
            // Sửa đường dẫn từ /api/poi thành /api/analytics
            string analyticsUrl = ApiUrl.Replace("/api/poi", "/api/analytics");

            var log = new { PoiId = poiId, ActionType = actionType };

            // Âm thầm ném lên server, không cần đợi phản hồi
            await _http.PostAsJsonAsync(analyticsUrl, log);
            System.Diagnostics.Debug.WriteLine($"Đã gửi Log cho quán ID {poiId}");
        }
        catch
        {
            // Rớt mạng thì thôi, giấu nhẹm lỗi để không làm phiền trải nghiệm của khách
            System.Diagnostics.Debug.WriteLine("Gửi Log thất bại do lỗi mạng.");
        }
    }
}
