namespace PocketFence_Simple.Interfaces
{
    public interface ISystemUtilsService
    {
        bool IsRunningAsAdministrator();
        void RestartAsAdministrator();
        bool CheckSystemVersion();
        Task<bool> CheckWifiAdapterAsync();
        void SetupApplicationDirectories();
        Task<string> GetPublicIpAddressAsync();
        string FormatBytes(long bytes);
        void LogEvent(string message, string level = "INFO");
        bool IsValidIpAddress(string ipAddress);
        bool IsValidMacAddress(string macAddress);
        string GetSystemInfo();
    }
}