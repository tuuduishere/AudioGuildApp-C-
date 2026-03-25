using CommunityToolkit.Maui;
using Plugin.Maui.Audio;
using TravelSmart.Mobile.Services;
using TravelSmart.Mobile.ViewModels;
using TravelSmart.Mobile.Views;

namespace TravelSmart.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            // .UseMauiMaps() // Tạm tắt để tránh crash Android nãy
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 1. Đăng ký Services
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);

        // 2. Đăng ký ViewModels (Phải có đủ thì app mới không văng)
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<PlaceDetailViewModel>();
        builder.Services.AddTransient<MapViewModel>();

        // 3. Đăng ký Pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<PlaceDetailPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<OnboardingPage>();

        return builder.Build();
    }
}