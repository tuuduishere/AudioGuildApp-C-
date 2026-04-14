using System.Net.Http.Json;
using SQLite;
using TravelSmart.App.Models;

namespace TravelSmart.App.Services;

public class DataService
{
    private SQLiteAsyncConnection _db;
    private readonly HttpClient _httpClient;

    // API Cổng 5088 dành cho máy ảo Android
    private const string ApiUrl = "https://rule-twiddling-recoil.ngrok-free.dev/api/Pois";

    public DataService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
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

    public async Task<List<PoiModel>> GetPOIsAsync()
    {
        await InitDbTask();
        return await _db.Table<PoiModel>().ToListAsync();
    }

    public async Task<bool> SyncFromServerAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var serverPois = await response.Content.ReadFromJsonAsync<List<PoiModel>>();
                if (serverPois != null && serverPois.Count > 0)
                {
                    await InitDbTask();
                    await _db.DeleteAllAsync<PoiModel>();
                    await _db.InsertAllAsync(serverPois);
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

    // ĐẤU NỐI: Lưu lịch sử vào điện thoại và báo lên Server
    public async Task AddHistoryAsync(PoiModel poi)
    {
        await InitDbTask();
        var history = new VisitLogModel
        {
            PoiId = poi.Id,
            Name = poi.Name,
            VisitTime = DateTime.Now
        };

        // 1. Lưu vào SQLite của điện thoại
        await _db.InsertAsync(history);

        // 2. Gọi điện báo cho Server
        try
        {
            await _httpClient.PostAsJsonAsync($"{ApiUrl}/history", history);
        }
        catch { } // Mất mạng thì bỏ qua
    }

    public async Task<List<VisitLogModel>> GetHistoryAsync()
    {
        await InitDbTask();
        return await _db.Table<VisitLogModel>().OrderByDescending(x => x.VisitTime).ToListAsync();
    }
}