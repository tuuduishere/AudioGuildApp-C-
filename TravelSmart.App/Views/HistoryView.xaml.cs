using TravelSmart.App.Services;

namespace TravelSmart.App.Views;

public partial class HistoryView : ContentView
{
    private readonly DataService _databaseService;

    public HistoryView()
    {
        InitializeComponent();
        _databaseService = new DataService();
    }

    // Hàm này sẽ được MainPage gọi mỗi khi chuyển sang Tab Lịch sử
    public async void RefreshHistory()
    {
        var history = await _databaseService.GetHistoryAsync();

        if (history.Count > 0)
        {
            EmptyStateView.IsVisible = false;
            HistoryList.IsVisible = true;
            HistoryList.ItemsSource = history; // Đổ data vào giao diện
        }
        else
        {
            EmptyStateView.IsVisible = true;
            HistoryList.IsVisible = false;
        }
    }
}