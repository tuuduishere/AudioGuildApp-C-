using System.Net.Http.Json;
using System.Text.Json;
using SQLite;
using TravelSmart.App.Models;
using System.Net.Http.Headers;

namespace TravelSmart.App.Services;

public class DataService
{
    private static SQLiteAsyncConnection _db;
    private readonly HttpClient _httpClient;

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

    public async Task<bool> SyncFromServerAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

        try
        {
            string noCacheUrl = $"{AppConfig.ApiBaseUrl}/Pois?_t={DateTime.Now.Ticks}";
            var response = await _httpClient.GetAsync(noCacheUrl).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var serverPois = JsonSerializer.Deserialize<List<PoiModel>>(jsonString, options);

                if (serverPois != null)
                {
                    await InitDbTask();
                    var localPois = await _db.Table<PoiModel>().ToListAsync();

                    await Task.Run(async () =>
                    {
                        var serverIds = serverPois.Select(p => p.Id).ToList();
                        var localIds = localPois.Select(p => p.Id).ToList();

                        var toDelete = localPois.Where(p => !serverIds.Contains(p.Id)).ToList();
                        foreach (var item in toDelete) await _db.DeleteAsync(item);

                        var toInsert = new List<PoiModel>();
                        var toUpdate = new List<PoiModel>();

                        foreach (var sp in serverPois)
                        {
                            if (localIds.Contains(sp.Id)) toUpdate.Add(sp);
                            else toInsert.Add(sp);
                        }

                        if (toInsert.Any()) await _db.InsertAllAsync(toInsert);
                        if (toUpdate.Any()) await _db.UpdateAllAsync(toUpdate);

                    }).ConfigureAwait(false);

                    Preferences.Default.Set("LastSyncTime", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    return true;
                }
            }
            return false;
        }
        catch { return false; }
    }

    public async Task AddHistoryAsync(PoiModel poi)
    {
        await InitDbTask();
        var history = new VisitLogModel { PoiId = poi.Id, Name = poi.Name, VisitTime = DateTime.Now };
        await _db.InsertAsync(history);

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

        try
        {
            var payload = new
            {
                PoiId = poi.Id,
                DeviceName = DeviceInfo.Current.Name,
                LanguageCode = Preferences.Default.Get("DefaultLang", "vi"),
                DurationMinutes = new Random().Next(2, 15)
            };

            var token = await SecureStorage.Default.GetAsync("authToken");
            var request = new HttpRequestMessage(HttpMethod.Post, $"{AppConfig.ApiBaseUrl}/Pois/history");
            request.Content = JsonContent.Create(payload);
            if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await _httpClient.SendAsync(request);
        }
        catch { }
    }

    public async Task<List<VisitLogModel>> GetHistoryAsync()
    {
        await InitDbTask();
        return await _db.Table<VisitLogModel>().OrderByDescending(x => x.VisitTime).ToListAsync();
    }
}