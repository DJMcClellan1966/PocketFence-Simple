namespace PocketFence_Simple.Models
{
    public class NetworkInformation
    {
        public string? LocalIP { get; set; }
        public string? Gateway { get; set; }
        public string[]? DNSServers { get; set; }
        public string? SubnetMask { get; set; }
        public string? AdapterName { get; set; }
        public bool IsConnected { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
    }
}