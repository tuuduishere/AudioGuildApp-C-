using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls;
using Plugin.Maui.Audio; // 🔥 Kéo thư viện Audio xịn vào

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
            .UseBarcodeReader() // KHỞI ĐỘNG MẮT THẦN
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 🔥 CẤP QUYỀN CHO APP DÙNG LOA ĐỂ PHÁT MP3
        builder.Services.AddSingleton(AudioManager.Current);

        return builder.Build();
    }
}