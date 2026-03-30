using TravelSmart.App.Services;
using TravelSmart.App.Models;

namespace TravelSmart.App;

public partial class FoodPage : ContentPage
{
    List<Food> all = MockData.Foods;

    public FoodPage()
    {
        InitializeComponent();
        foodList.ItemsSource = all;
    }

    void OnFilter(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        var selected = picker.SelectedItem?.ToString();

        if (selected == "All")
            foodList.ItemsSource = all;
        else
            foodList.ItemsSource = all.Where(x => x.Category == selected);
    }
}