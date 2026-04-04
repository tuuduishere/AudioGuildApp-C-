using System;
using Microsoft.Maui.Controls;

namespace TravelSmart.App
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            // legacy counter removed from UI — keep method as no-op
        }

        async void OpenExplore(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//explore");
        }

        async void OpenMap(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//map");
        }

        async void OpenFood(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//food");
        }

        async void OpenFavorites(object sender, EventArgs e)
        {
            // For now navigate to Explore where favorites are visible
            await Shell.Current.GoToAsync("//explore");
        }
    }
}
