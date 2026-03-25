namespace TravelSmart.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Khởi tạo App bằng AppShell theo cách mới của .NET 10
        return new Window(new AppShell());
    }
}