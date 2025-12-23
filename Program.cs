using PocketFence.UI;
using PocketFence.Utils;

namespace PocketFence_Simple
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Display welcome banner
                Console.WriteLine("🛡️  PocketFence-Simple v1.0");
                Console.WriteLine("=============================");
                Console.WriteLine("Parental Control Hotspot Application");
                Console.WriteLine();
                
                // Check system requirements
                if (!SystemUtils.CheckWindowsVersion())
                {
                    Console.WriteLine("❌ Error: Windows 10 version 1607 or later is required.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
                
                // Check for administrator privileges
                if (!SystemUtils.IsRunningAsAdministrator())
                {
                    Console.WriteLine("⚠️  Administrator privileges required for network operations.");
                    Console.WriteLine("Attempting to restart as administrator...");
                    SystemUtils.RestartAsAdministrator();
                    return;
                }
                
                // Check WiFi adapter support
                Console.WriteLine("🔍 Checking WiFi adapter compatibility...");
                var wifiSupported = await SystemUtils.CheckWifiAdapterAsync();
                if (!wifiSupported)
                {
                    Console.WriteLine("⚠️  Warning: WiFi adapter may not support hosted networks.");
                    Console.WriteLine("Some features may be limited.");
                }
                else
                {
                    Console.WriteLine("✅ WiFi adapter supports hosted networks.");
                }
                
                // Setup application directories
                SystemUtils.SetupApplicationDirectories();
                SystemUtils.CheckFirewallSettings();
                
                Console.WriteLine();
                Console.WriteLine("✅ System checks completed.");
                Console.WriteLine("🚀 Starting PocketFence-Simple...");
                Console.WriteLine();
                
                // Start the console UI
                var consoleUI = new ConsoleUI();
                await consoleUI.RunAsync();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fatal Error: {ex.Message}");
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
