using Microsoft.AspNetCore.Mvc;
using TravelSmart.Admin.Models;

namespace TravelSmart.Admin.Controllers;

public class HomeController : Controller
{
    private readonly HttpClient _http;

    private readonly string ApiUrl = "https://localhost:7008/api/poi";

    public HomeController()
    {
        // Bỏ qua lỗi SSL khi chạy ở localhost
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
        _http = new HttpClient(handler);
    }

    // 1. TRANG CHỦ: GỌI LÊN API LẤY DANH SÁCH QUÁN ỐC HIỂN THỊ
    public async Task<IActionResult> Index()
    {
        try
        {
            var pois = await _http.GetFromJsonAsync<List<PoiViewModel>>(ApiUrl);
            return View(pois ?? new List<PoiViewModel>());
        }
        catch
        {
            // Lỡ API chưa bật thì trả về list rỗng tránh sập web
            return View(new List<PoiViewModel>());
        }
    }

    // 2. MỞ TRANG: FORM ĐIỀN THÔNG TIN QUÁN MỚI
    public IActionResult Create()
    {
        return View();
    }

    // 3. XỬ LÝ: KHI BẤM NÚT LƯU TRÊN FORM
    [HttpPost]
    public async Task<IActionResult> Create(PoiViewModel model)
    {
        await _http.PostAsJsonAsync(ApiUrl, model);
        return RedirectToAction("Index"); // Lưu xong quay về Trang chủ
    }
}