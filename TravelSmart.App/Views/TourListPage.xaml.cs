using System.Net.Http.Json;
using System.Windows.Input;

namespace TravelSmart.App.Views;

public partial class TourListPage : ContentPage
{
    private Action<string> _onTourSelected;

    public ICommand SelectTourCommand { get; private set; }

    public TourListPage(Action<string> onTourSelected)
    {
        InitializeComponent();
        _onTourSelected = onTourSelected;

        SelectTourCommand = new Command<Guid>(async (tourId) =>
        {
            await Navigation.PopModalAsync();
            _onTourSelected?.Invoke(tourId.ToString());
        });

        BindingContext = this;
        LoadTours();
    }

    private async void LoadTours()
    {
        RefreshTours.IsRefreshing = true;
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            var tours = await client.GetFromJsonAsync<List<TourDto>>($"{AppConfig.ApiBaseUrl}/Tours");

            if (tours != null)
            {
                ListTours.ItemsSource = tours.Where(t => t.IsActive).ToList();
            }
        }
        catch { await DisplayAlert("Lỗi", "Mất kết nối với máy chủ.", "OK"); }
        finally { RefreshTours.IsRefreshing = false; }
    }

    private void OnRefreshTours(object sender, EventArgs e)
    {
        LoadTours();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnClearFilterClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        _onTourSelected?.Invoke("");
    }

    public class TourDto
    {
        public Guid TourId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}