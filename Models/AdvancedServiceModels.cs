namespace PocketFence_Simple.Models;

public record GeofenceRequest
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double Radius { get; init; }
    public GeofenceZone Zone { get; init; } = new();
}

public record ContentAnalysisRequest
{
    public string Content { get; init; } = "";
    public string DeviceId { get; init; } = "";
    public ContentContext Context { get; init; } = new();
}

public record GeofenceZone
{
    public string Name { get; init; } = "";
    public ZoneType Type { get; init; }
    public RestrictionLevel RestrictiveLevel { get; init; }
    public List<string> AllowedCategories { get; init; } = [];
}

public record ContentContext
{
    public string Source { get; init; } = "";
    public string Platform { get; init; } = "";
    public TimeSpan TimeOfDay { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public string UserAge { get; init; } = "";
    public List<string> RecentActivity { get; init; } = [];
}

public enum ZoneType
{
    Home,
    School,
    Social,
    Public,
    Transportation,
    Emergency,
    Custom
}

public enum RestrictionLevel
{
    Minimal,
    Low,
    Medium,
    High,
    Maximum
}