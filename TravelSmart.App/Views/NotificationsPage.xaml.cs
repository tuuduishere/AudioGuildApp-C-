using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace TravelSmart.App.Views;

public partial class NotificationsPage : ContentPage
{
    private const string ApiBaseUrl = "https://rule-twiddling-recoil.ngrok-free.dev/api";

    public NotificationsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRealNotifications();
    }

    private async Task LoadRealNotifications()
    {
        var token = await SecureStorage.Default.GetAsync("authToken");
        if (string.IsNullOrEmpty(token)) return;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var notis = await client.GetFromJsonAsync<List<NotificationDto>>($"{ApiBaseUrl}/Auth/notifications");

            // Xóa UI cũ Fake đi, thay bằng Stack Layout
            var mainStack = new VerticalStackLayout { Spacing = 15 };

            if (notis != null && notis.Count > 0)
            {
                foreach (var n in notis)
                {
                    mainStack.Children.Add(new Frame
                    {
                        BackgroundColor = Colors.White,
                        CornerRadius = 12,
                        Padding = 15,
                        HasShadow = true,
                        Content = new HorizontalStackLayout
                        {
                            Spacing = 15,
                            Children = {
                                new Label { Text = "📩", FontSize = 24, VerticalOptions = LayoutOptions.Center },
                                new VerticalStackLayout {
                                    VerticalOptions = LayoutOptions.Center,
                                    Children = {
                                        new Label { Text = n.title, FontAttributes = FontAttributes.Bold, TextColor = Colors.Black },
                                        new Label { Text = n.message, TextColor = Colors.Gray, FontSize = 13 }
                                    }
                                }
                            }
                        }
                    });
                }
            }
            else
            {
                mainStack.Children.Add(new Label { Text = "Bạn chưa có thông báo nào.", HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 20, 0, 0) });
            }

            // Đẩy vào Content của trang
            this.Content = new ScrollView { Padding = 15, Content = mainStack };
        }
        catch { }
    }

    public class NotificationDto { public string title { get; set; } public string message { get; set; } public DateTime createdAt { get; set; } }
}