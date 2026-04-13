using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls; // <-- NHỚ THÊM DÒNG USING NÀY

namespace TravelSmart.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .UseBarcodeReader() // <--- THÊM ĐÚNG DÒNG NÀY LÀ HẾT CRASH CAMERA
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }
}