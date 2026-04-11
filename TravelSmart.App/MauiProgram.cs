using Microsoft.Extensions.Logging; // Fix lỗi CS1061 (AddDebug)
using CommunityToolkit.Maui;        // Dùng cho Toolkit
using ZXing.Net.Maui.Controls;

namespace TravelSmart.App; // Tui thấy trong ảnh project ông viết hoa chữ APP

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // Fix lỗi MCT001: Bắt buộc nằm ngay sát dòng trên
            .UseMauiMaps()             // Khởi tạo Map
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<TravelSmart.App.Services.LocationService>();
        builder.Services.AddSingleton<TravelSmart.App.Services.DataService>();
        builder.Services.AddSingleton<TravelSmart.App.Services.GeofenceService>();
        builder.Services.AddTransient<TravelSmart.App.ViewModels.MapViewModel>();
        return builder.Build();
    }
}