using System.Collections.ObjectModel;
using TravelSmart.App.Models;
using TravelSmart.App.Services;

namespace TravelSmart.App.ViewModels;

public class POIViewModel : BindableObject // Thêm BindableObject để UI tự cập nhật
{
    private readonly DataService _dbService;

    // Danh sách hiển thị lên màn hình
    private ObservableCollection<PoiModel> _pois;
    public ObservableCollection<PoiModel> Pois
    {
        get => _pois;
        set { _pois = value; OnPropertyChanged(); }
    }

    public int TotalCount => Pois?.Count ?? 0;

    public POIViewModel()
    {
        // Tạm thời khởi tạo _dbService ở đây (Cách tốt hơn là dùng Dependency Injection qua Constructor)
        _dbService = new DataService();
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        // 1. Kéo dữ liệu từ SQLite
        var listFromDb = await _dbService.GetPOIsAsync();

        // 2. Gắn lên UI
        Pois = new ObservableCollection<PoiModel>(listFromDb);
        OnPropertyChanged(nameof(TotalCount));
    }
}