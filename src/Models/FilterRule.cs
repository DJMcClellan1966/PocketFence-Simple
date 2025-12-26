namespace PocketFence_Simple.Models
{
    public class FilterRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FilterType Type { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public FilterAction Action { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string RuleType { get; set; } = "Domain";
        public List<string> Categories { get; set; } = new List<string>();
        public int Priority { get; set; }
    }

    public enum FilterType
    {
        Domain,
        URL,
        Keyword,
        Category,
        IPAddress,
        Port
    }

    public enum FilterAction
    {
        Block,
        Allow,
        Redirect,
        Monitor
    }

    public class BlockedSite
    {
        public string Url { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; }
        public string DeviceMac { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}