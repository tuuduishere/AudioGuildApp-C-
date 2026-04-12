using TravelSmart.App.Services;
using TravelSmart.App.Models;
using System.Collections.ObjectModel;

namespace TravelSmart.App.Views;

public partial class POIListView : ContentView
{
    private readonly DataService _databaseService;

    // Đây là cái túi chứa dữ liệu để hiển thị lên màn hình
    public ObservableCollection<PoiModel> PoiList { get; set; } = new();

    public POIListView()
    {
        InitializeComponent();
        _databaseService = new DataService();
        this.BindingContext = this;
        LoadData(); // Gọi nạp dữ liệu ngay khi vừa hiện ra
    }

    public async void LoadData()
    {
        try
        {
            var data = await _databaseService.GetPOIsAsync();
            PoiList.Clear();
            foreach (var item in data)
            {
                PoiList.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi load danh sách: {ex.Message}");
        }
    }
}