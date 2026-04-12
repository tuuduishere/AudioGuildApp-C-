namespace TravelSmart.App.Models;

public class OfflinePackModel
{
    public string Name { get; set; }
    public string AudioInfo { get; set; } // Ví dụ: "1 audio   1:00   5 MB"
    public string DownloadDate { get; set; } // Ví dụ: "Đã tải 5/4/2026"
    public bool IsDownloaded { get; set; } // Xác định xem đã tải chưa để đổi Icon

    // Tự động chọn Icon: Đã tải hiện thùng rác (để xóa), chưa tải hiện mũi tên xuống (để tải)
    public string ActionIcon => IsDownloaded ? "🗑️" : "⬇️";
}