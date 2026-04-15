using System.Net.Http.Json;
using SQLite;
using TravelSmart.App.Models;

namespace TravelSmart.App.Services;

public class DataService
{
    private SQLiteAsyncConnection _db;
    private readonly HttpClient _httpClient;

    // API Ngrok để test trên máy ảo/máy thật
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

    // 🔥 BƯỚC 1: ĐỒNG BỘ TĂNG DẦN (INCREMENTAL SYNC)
    public async Task<bool> SyncFromServerAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

        try
        {
            var response = await _httpClient.GetAsync(ApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var serverPois = await response.Content.ReadFromJsonAsync<List<PoiModel>>();
                if (serverPois != null)
                {
                    await InitDbTask();
                    var localPois = await _db.Table<PoiModel>().ToListAsync();

                    var serverIds = serverPois.Select(p => p.Id).ToList();
                    var localIds = localPois.Select(p => p.Id).ToList();

                    // 1. XÓA (Delete): Quán nào Server đã xóa thì App cũng phải xóa + Dọn rác MP3
                    var toDeleteIds = localIds.Except(serverIds).ToList();
                    foreach (var id in toDeleteIds)
                    {
                        var poiToDelete = localPois.First(p => p.Id == id);
                        await _db.DeleteAsync(poiToDelete);

                        // Dọn rác MP3 để không nặng máy khách
                        string[] langs = { "vi", "en", "ja" };
                        foreach (var lang in langs)
                        {
                            string filePath = Path.Combine(FileSystem.CacheDirectory, $"{id}_{lang}.mp3");
                            if (File.Exists(filePath)) File.Delete(filePath);
                        }
                    }

                    // 2. THÊM MỚI (Insert) & CẬP NHẬT (Update)
                    var toInsert = new List<PoiModel>();
                    var toUpdate = new List<PoiModel>();
                    var newPoisForAudio = new List<PoiModel>();

                    foreach (var serverPoi in serverPois)
                    {
                        if (localIds.Contains(serverPoi.Id))
                        {
                            toUpdate.Add(serverPoi); // Quán cũ -> Chỉ update data text
                        }
                        else
                        {
                            toInsert.Add(serverPoi); // Quán mới -> Thêm vào List chờ Insert
                            newPoisForAudio.Add(serverPoi); // Đưa vào List chờ tải MP3
                        }
                    }

                    if (toInsert.Any()) await _db.InsertAllAsync(toInsert);
                    if (toUpdate.Any()) await _db.UpdateAllAsync(toUpdate);

                    // 3. Chỉ tải MP3 cho những Quán MỚI được thêm vào (Tiết kiệm 4G cực mạnh)
                    if (newPoisForAudio.Any())
                    {
                        _ = DownloadAudioOffline(newPoisForAudio);
                    }

                    // Lưu vết thời gian đồng bộ cuối cùng
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
                    catch { /* Bỏ qua nếu server chưa kịp tạo file MP3 */ }
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