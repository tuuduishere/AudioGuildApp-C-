namespace TravelSmart.App.ViewModels;

public class SettingsViewModel
{
    // Thông số giọng đọc
    public double ReadingSpeed { get; set; } = 1.0;
    public double Volume { get; set; } = 100;

    // Thông số GPS
    public double ActivationRadius { get; set; } = 50;
    public double WarningRadius { get; set; } = 100;
    public double WaitTime { get; set; } = 300;

    // Nút gạt hành vi
    public bool AutoPlay { get; set; } = true;
    public bool NotifyNear { get; set; } = true;
    public bool BackgroundTrack { get; set; } = true;
    public bool PowerSave { get; set; } = false;
    public bool OfflineMode { get; set; } = false;

    // Đồng bộ
    public string LastSync { get; set; } = "Lần cuối: Hôm nay lúc 03:50";
}   