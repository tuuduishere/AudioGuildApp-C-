using System.Net.Http.Json;
using SQLite;
using TravelSmart.App.Models;

namespace TravelSmart.App.Services;

public class DataService
{
    private SQLiteAsyncConnection _db;
    private readonly HttpClient _httpClient;

    // Dùng ngrok để test trên máy thật luôn sếp nhé
    private const string ApiUrl = "https://rule-twiddling-recoil.ngrok-free.dev/api/Pois";

    public DataService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        // 🔥 Thêm cái thẻ VIP vượt rào Ngrok cho DataService luôn
        _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    }

    private async Task InitDbTask()
    {
        if (_db == null)
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "TravelSmartLocal.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<PoiModel>();
            await _db.CreateTableAsync<VisitLogModel>();
        }
    }

    // 🔥 HÀM NÀY GIỜ CHẠY OFFLINE TẸT GA VÌ LẤY TỪ SQLITE
    public async Task<List<PoiModel>> GetPOIsAsync()
    {
        await InitDbTask();
        return await _db.Table<PoiModel>().ToListAsync();
    }

    public async Task<bool> SyncFromServerAsync()
    {
        // 🔥 NẾU KHÔNG CÓ MẠNG -> KHÔNG LÀM GÌ CẢ (GIỮ NGUYÊN DATA CŨ TRONG MÁY)
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

        try
        {
            var response = await _httpClient.GetAsync(ApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var serverPois = await response.Content.ReadFromJsonAsync<List<PoiModel>>();
                if (serverPois != null && serverPois.Count > 0)
                {
                    await InitDbTask();
                    await _db.DeleteAllAsync<PoiModel>(); // Xóa cái cũ
                    await _db.InsertAllAsync(serverPois); // Bơm cái mới về đi cất
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task AddHistoryAsync(PoiModel poi)
    {
        await InitDbTask();
        var history = new VisitLogModel
        {
            PoiId = poi.Id,
            Name = poi.Name,
            VisitTime = DateTime.Now
        };

        // 1. Lưu vào SQLite của điện thoại ngay lập tức (Offline vẫn lưu)
        await _db.InsertAsync(history);

        // NẾU MẤT MẠNG THÌ DỪNG LẠI (Không gọi Server nữa để khỏi lỗi)
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

        // 2. Có mạng thì gọi điện báo cho Server
        try { await _httpClient.PostAsJsonAsync($"{ApiUrl}/history", history); } catch { }
    }

    public async Task<List<VisitLogModel>> GetHistoryAsync()
    {
        await InitDbTask();
        return await _db.Table<VisitLogModel>().OrderByDescending(x => x.VisitTime).ToListAsync();
    }
}