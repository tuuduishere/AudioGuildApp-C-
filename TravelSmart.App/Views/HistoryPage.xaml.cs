using System.Text.Json;

namespace TravelSmart.App.Views;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadHistory();
    }

    private void LoadHistory()
    {
        HistoryContainer.Children.Clear();
        var historyJson = Preferences.Default.Get("AppHistory", "[]");
        var list = JsonSerializer.Deserialize<List<HistoryItem>>(historyJson);

        if (list != null && list.Count > 0)
        {
            LblEmpty.IsVisible = false;
            BtnClearHistory.IsVisible = true;

            foreach (var item in list)
            {
                var card = new Frame
                {
                    BackgroundColor = Colors.White,
                    CornerRadius = 12,
                    Padding = 15,
                    HasShadow = true,
                    BorderColor = Colors.Transparent,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children = {
                            new HorizontalStackLayout { Spacing = 10, Children = {
                                new Label { Text = "📍", FontSize = 18 },
                                new Label { Text = item.PoiName, FontAttributes = FontAttributes.Bold, TextColor = Colors.Black, FontSize = 16, VerticalOptions = LayoutOptions.Center }
                            }},
                            new Label { Text = item.Address, TextColor = Colors.Gray, FontSize = 12, Margin = new Thickness(28,0,0,0) },
                            new Label { Text = "🕒 " + item.Time, TextColor = Colors.DarkOrange, FontSize = 11, FontAttributes = FontAttributes.Italic, Margin = new Thickness(28,5,0,0) }
                        }
                    }
                };
                HistoryContainer.Children.Add(card);
            }
        }
        else
        {
            LblEmpty.IsVisible = true;
            BtnClearHistory.IsVisible = false;
        }
    }

    private async void OnClearHistoryClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Xác nhận", "Bạn muốn xóa toàn bộ lịch sử?", "Xóa", "Hủy");
        if (confirm)
        {
            Preferences.Default.Remove("AppHistory");
            LoadHistory();
        }
    }

    public class HistoryItem
    {
        public string PoiName { get; set; }
        public string Address { get; set; }
        public string Time { get; set; }
    }
}