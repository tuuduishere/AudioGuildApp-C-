using System.Net.Http.Json;
using System.Text.Json;
using SQLite;
using TravelSmart.App.Models;

namespace TravelSmart.App.Services;

public class DataService
{
    private static SQLiteAsyncConnection _db;
    private readonly HttpClient _httpClient;

    private const string ApiUrl = "https://rule-twiddling-recoil.ngrok-free.dev/api/Pois";
    private const string BaseUrl = "https://rule-twiddling-recoil.ngrok-free.dev";

    public DataService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    }

    private async Task InitDbTask()
    {
        if (_db == null)
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "TravelSmartLocal.db3");
            var flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
            _db = new SQLiteAsyncConnection(dbPath, flags);

            await _db.CreateTableAsync<PoiModel>();
            await _db.CreateTableAsync<VisitLogModel>();
        }
    }

    public async Task<List<PoiModel>> GetPOIsAsync()
    {
        await InitDbTask();
        return await _db.Table<PoiModel>().ToListAsync();
    }

    // 🔥 FIX LUỒNG ĐỒNG BỘ: XÓA SẠCH NẠP MỚI ĐỂ 100% LUÔN KHỚP SERVER
    public async Task<bool> SyncFromServerAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

        try
        {
            // Ép API lấy data nóng hổi, chống cache
            string noCacheUrl = $"{ApiUrl}?_t={DateTime.Now.Ticks}";
            var response = await _httpClient.GetAsync(noCacheUrl);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonString = await response.Content.ReadAsStringAsync();
                var serverPois = JsonSerializer.Deserialize<List<PoiModel>>(jsonString, options);

                if (serverPois != null)
                {
                    await InitDbTask();

                    // NUKE & PAVE: Xóa sạch dữ liệu cũ và Insert toàn bộ đồ mới vào để không lỗi khóa
                    await _db.DeleteAllAsync<PoiModel>();
                    await _db.InsertAllAsync(serverPois);

                    // Quét và tải MP3 ngầm
                    _ = DownloadAudioOffline(serverPois);

                    Preferences.Default.Set("LastSyncTime", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    return true;
                }
            }
            return false;
        }
        catch { return false; }
    }

    private async Task DownloadAudioOffline(List<PoiModel> pois)
    {
        string[] langs = { "vi", "en", "ja" };
        foreach (var poi in pois)
        {
            foreach (var lang in langs)
            {
                string fileName = $"{poi.Id}_{lang}.mp3";
                string localPath = Path.Combine(FileSystem.CacheDirectory, fileName);

                if (!File.Exists(localPath))
                {
                    try
                    {
                        var audioBytes = await _httpClient.GetByteArrayAsync($"{BaseUrl}/audio/{fileName}");
                        await File.WriteAllBytesAsync(localPath, audioBytes);
                    }
                    catch { }
                }
            }
        }
    }

    public async Task AddHistoryAsync(PoiModel poi)
    {
        await InitDbTask();
        var history = new VisitLogModel { PoiId = poi.Id, Name = poi.Name, VisitTime = DateTime.Now };
        await _db.InsertAsync(history);

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;
        try { await _httpClient.PostAsJsonAsync($"{ApiUrl}/history", history); } catch { }
    }

    public async Task<List<VisitLogModel>> GetHistoryAsync()
    {
        await InitDbTask();
        return await _db.Table<VisitLogModel>().OrderByDescending(x => x.VisitTime).ToListAsync();
    }
}