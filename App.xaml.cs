namespace PocketFence_Simple;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        
        window.Title = "PocketFence-Simple";
        window.Page = new AppShell(); // Use Window.Page instead of deprecated MainPage
        
        // Set window size for desktop platforms
        const int newWidth = 1200;
        const int newHeight = 800;

        window.Width = newWidth;
        window.Height = newHeight;
        window.X = (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - newWidth) / 2;
        window.Y = (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density - newHeight) / 2;

        return window;
    }
}