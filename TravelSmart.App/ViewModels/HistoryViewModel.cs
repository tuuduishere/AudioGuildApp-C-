using System.Collections.ObjectModel;
using TravelSmart.App.Models;

namespace TravelSmart.App.ViewModels;

public class HistoryViewModel
{
    // Danh sách rỗng để tránh lỗi Binding cũ (nếu có)
    public ObservableCollection<HistoryModel> Histories { get; set; }

    public HistoryViewModel()
    {
        // Khởi tạo rỗng vì toàn bộ logic lấy dữ liệu thật 
        // anh em mình đã chuyển sang HistoryView.xaml.cs rồi!
        Histories = new ObservableCollection<HistoryModel>();
    }
}