using System.Collections.ObjectModel;
using TravelSmart.App.Models;
using TravelSmart.App.Services;

namespace TravelSmart.App.ViewModels;

public class MapViewModel
{
    // Chỉ cần gọi thủ kho (DataService) ra để lấy dữ liệu
    private readonly DataService _dataService;

    // Danh sách POI đẩy lên giao diện (UI)
    public ObservableCollection<PoiModel> Pois { get; set; }

    // DÙNG DEPENDENCY INJECTION: Yêu cầu MAUI tự động bơm DataService vào đây
    public MapViewModel(DataService dataService)
    {
        _dataService = dataService;

        // Gọi hàm tải dữ liệu khi vừa khởi tạo
        LoadPoisAsync();
    }

    private async void LoadPoisAsync()
    {
        // 1. Nhờ thủ kho lấy data từ SQLite
        var list = await _dataService.GetPOIsAsync();

        // 2. Nạp đạn vào danh sách cho UI nó hiện lên
        Pois = new ObservableCollection<PoiModel>(list);
    }
}