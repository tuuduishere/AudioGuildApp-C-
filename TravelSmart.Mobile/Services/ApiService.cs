using Newtonsoft.Json;
using TravelSmart.Mobile.Models; // DÙNG LOCAL MODEL

namespace TravelSmart.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient = new();
    // Sửa Port đúng với Port của API đang chạy (ví dụ 7113)
    private string BaseUrl = "https://10.0.2.2:7113";
    public async Task<List<Place>> GetPlacesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"{BaseUrl}/api/places");
            return JsonConvert.DeserializeObject<List<Place>>(response) ?? new();
        }
        catch { return new(); }
    }
}