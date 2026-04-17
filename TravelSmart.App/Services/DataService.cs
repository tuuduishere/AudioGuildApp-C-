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

    // 🔥 FIX LUỒNG ĐỒNG BỘ SIÊU NHẠY
    public async Task<bool> SyncFromServerAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

        try
        {
            // 🔥 THUỐC GIẢI CACHE: Gắn thêm thời gian thực vào link để ép HttpClient tải mới 100%
            string noCacheUrl = $"{ApiUrl}?_t={DateTime.Now.Ticks}";
            var response = await _httpClient.GetAsync(noCacheUrl);

            if (response.IsSuccessStatusCode)
            {
                // 🔥 ĐỌC JSON CHỐNG LỖI CHỮ HOA CHỮ THƯỜNG
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonString = await response.Content.ReadAsStringAsync();
                var serverPois = JsonSerializer.Deserialize<List<PoiModel>>(jsonString, options);

                if (serverPois != null)
                {
                    await InitDbTask();
                    var localPois = await _db.Table<PoiModel>().ToListAsync();

                    // Chuyển ID về chữ thường hết để so sánh không bị trượt
                    var serverIds = serverPois.Select(p => p.Id.ToLower()).ToList();
                    var localIds = localPois.Select(p => p.Id.ToLower()).ToList();

                    // 1. XÓA (Delete)
                    var toDeleteIds = localIds.Except(serverIds).ToList();
                    foreach (var id in toDeleteIds)
                    {
                        var poiToDelete = localPois.FirstOrDefault(p => p.Id.ToLower() == id);
                        if (poiToDelete != null)
                        {
                            await _db.DeleteAsync(poiToDelete);
                            // Dọn rác MP3
                            string[] langs = { "vi", "en", "ja" };
                            foreach (var lang in langs)
                            {
                                string filePath = Path.Combine(FileSystem.CacheDirectory, $"{poiToDelete.Id}_{lang}.mp3");
                                if (File.Exists(filePath)) File.Delete(filePath);
                            }
                        }
                    }

                    // 2. THÊM MỚI (Insert) & CẬP NHẬT (Update)
                    var toInsert = new List<PoiModel>();
                    var toUpdate = new List<PoiModel>();
                    var newPoisForAudio = new List<PoiModel>();

                    foreach (var serverPoi in serverPois)
                    {
                        if (localIds.Contains(serverPoi.Id.ToLower()))
                        {
                            toUpdate.Add(serverPoi);
                        }
                        else
                        {
                            toInsert.Add(serverPoi);
                            newPoisForAudio.Add(serverPoi);
                        }
                    }

                    if (toInsert.Any()) await _db.InsertAllAsync(toInsert);
                    if (toUpdate.Any()) await _db.UpdateAllAsync(toUpdate);

                    // 3. Tải MP3 cho Quán MỚI
                    if (newPoisForAudio.Any())
                    {
                        _ = DownloadAudioOffline(newPoisForAudio);
                    }

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