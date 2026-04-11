using System.Collections.ObjectModel;
using TravelSmart.App.Models;

namespace TravelSmart.App.ViewModels;

public class OfflineViewModel
{
    public ObservableCollection<OfflinePackModel> Packs { get; set; }
    public string ProgressText => "3/10 pack";
    public string SizeText => "24.5 MB";

    public OfflineViewModel()
    {
        Packs = new ObservableCollection<OfflinePackModel>
        {
            new OfflinePackModel { Name = "Dinh Độc Lập", AudioInfo = "1 audio  2:15  8 MB", DownloadDate = "Đã tải 06/04/2026", IsDownloaded = true },
            new OfflinePackModel { Name = "Bưu điện Thành phố", AudioInfo = "1 audio  1:45  5 MB", DownloadDate = "", IsDownloaded = false },
            new OfflinePackModel { Name = "Chợ Bến Thành", AudioInfo = "1 audio  3:00  11 MB", DownloadDate = "Đã tải 07/04/2026", IsDownloaded = true },
            new OfflinePackModel { Name = "Nhà thờ Đức Bà", AudioInfo = "1 audio  2:30  0 B", DownloadDate = "", IsDownloaded = false }
        };
    }
}