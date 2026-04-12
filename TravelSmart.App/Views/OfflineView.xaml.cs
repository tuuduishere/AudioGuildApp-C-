namespace TravelSmart.App.Views;

public partial class OfflineView : ContentView
{
    public event EventHandler<PanUpdatedEventArgs> HeaderPanned;

    public OfflineView()
    {
        InitializeComponent();
    }

    private void OnHeaderPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        HeaderPanned?.Invoke(this, e);
    }
}