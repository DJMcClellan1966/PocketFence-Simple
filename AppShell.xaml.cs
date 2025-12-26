namespace PocketFence_Simple;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes
        Routing.RegisterRoute("DevicesPage", typeof(DevicesPage));
        Routing.RegisterRoute("FilterPage", typeof(FilterPage));
        Routing.RegisterRoute("NetworkPage", typeof(NetworkPage));
        Routing.RegisterRoute("SettingsPage", typeof(SettingsPage));
    }
}