using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PocketFence_Simple.Services;

/// <summary>
/// Advanced geofencing service that automatically adjusts filtering rules based on location context
/// </summary>
public partial class SmartGeofenceService(ILogger<SmartGeofenceService> logger) : BackgroundService
{
    private readonly Dictionary<string, GeofenceZone> _geofenceZones = new();
    private readonly Dictionary<string, DeviceLocation> _deviceLocations = new();
    private readonly Dictionary<string, LocationBasedRuleset> _locationRulesets = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeGeofenceZonesAsync();
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await UpdateDeviceLocationsAsync();
            await ApplyLocationBasedRulesAsync();
            await DetectLocationAnomaliesAsync();
        }
    }
    
    private async Task InitializeGeofenceZonesAsync()
    {
        // Home zone - Most restrictive
        _geofenceZones["home"] = new GeofenceZone
        {
            Name = "Home",
            Type = ZoneType.Home,
            RestrictiveLevel = RestrictionLevel.High,
            AllowedCategories = ["Educational", "Communication", "Utilities"],
            TimeRestrictions = new Dictionary<DayOfWeek, TimeWindow[]>
            {
                [DayOfWeek.Monday] = [new(TimeOnly.Parse("07:00"), TimeOnly.Parse("20:00"))],
                [DayOfWeek.Tuesday] = [new(TimeOnly.Parse("07:00"), TimeOnly.Parse("20:00"))],
                [DayOfWeek.Wednesday] = [new(TimeOnly.Parse("07:00"), TimeOnly.Parse("20:00"))],
                [DayOfWeek.Thursday] = [new(TimeOnly.Parse("07:00"), TimeOnly.Parse("20:00"))],
                [DayOfWeek.Friday] = [new(TimeOnly.Parse("07:00"), TimeOnly.Parse("21:00"))],
                [DayOfWeek.Saturday] = [new(TimeOnly.Parse("08:00"), TimeOnly.Parse("22:00"))],
                [DayOfWeek.Sunday] = [new(TimeOnly.Parse("08:00"), TimeOnly.Parse("21:00"))]
            }
        };
        
        // School zone - Very restrictive
        _geofenceZones["school"] = new GeofenceZone
        {
            Name = "School",
            Type = ZoneType.School,
            RestrictiveLevel = RestrictionLevel.Maximum,
            AllowedCategories = ["Educational", "Emergency"],
            TimeRestrictions = new Dictionary<DayOfWeek, TimeWindow[]>
            {
                [DayOfWeek.Monday] = [new(TimeOnly.Parse("08:00"), TimeOnly.Parse("15:30"))],
                [DayOfWeek.Tuesday] = [new(TimeOnly.Parse("08:00"), TimeOnly.Parse("15:30"))],
                [DayOfWeek.Wednesday] = [new(TimeOnly.Parse("08:00"), TimeOnly.Parse("15:30"))],
                [DayOfWeek.Thursday] = [new(TimeOnly.Parse("08:00"), TimeOnly.Parse("15:30"))],
                [DayOfWeek.Friday] = [new(TimeOnly.Parse("08:00"), TimeOnly.Parse("15:30"))]
            },
            EmergencyContactsAllowed = true,
            NotificationSettings = new NotificationSettings
            {
                NotifyParentOnAccess = true,
                NotifyOnViolation = true,
                SilentMode = true
            }
        };
        
        // Friend's house - Moderate
        _geofenceZones["social"] = new GeofenceZone
        {
            Name = "Friend's House",
            Type = ZoneType.Social,
            RestrictiveLevel = RestrictionLevel.Medium,
            AllowedCategories = ["Educational", "Communication", "Social", "Games"],
            TimeRestrictions = new Dictionary<DayOfWeek, TimeWindow[]>
            {
                [DayOfWeek.Saturday] = [new(TimeOnly.Parse("10:00"), TimeOnly.Parse("21:00"))],
                [DayOfWeek.Sunday] = [new(TimeOnly.Parse("10:00"), TimeOnly.Parse("20:00"))]
            }
        };
        
        // Public spaces - Enhanced safety
        _geofenceZones["public"] = new GeofenceZone
        {
            Name = "Public Space",
            Type = ZoneType.Public,
            RestrictiveLevel = RestrictionLevel.High,
            AllowedCategories = ["Educational", "Communication", "Navigation", "Emergency"],
            SafetyFeatures = new SafetyFeatures
            {
                LocationTrackingEnabled = true,
                EmergencyButtonVisible = true,
                StrangerDangerMode = true,
                NoiseMonitoring = true
            }
        };
        
        logger.LogInformation("Initialized {ZoneCount} geofence zones", _geofenceZones.Count);
    }
    
    public async Task<string> AddCustomGeofenceAsync(double latitude, double longitude, double radiusMeters, GeofenceZone zone)
    {
        var zoneId = Guid.NewGuid().ToString();
        zone.Latitude = latitude;
        zone.Longitude = longitude;
        zone.Radius = radiusMeters;
        
        _geofenceZones[zoneId] = zone;
        
        logger.LogInformation("Added custom geofence zone {ZoneId} at ({Lat}, {Lng}) with radius {Radius}m", 
            zoneId, latitude, longitude, radiusMeters);
            
        return zoneId;
    }
    
    private async Task UpdateDeviceLocationsAsync()
    {
        // In real implementation, this would get actual GPS coordinates from devices
        // For now, simulate based on network information and time patterns
        
        foreach (var deviceId in GetConnectedDeviceIds())
        {
            var currentLocation = await EstimateDeviceLocationAsync(deviceId);
            _deviceLocations[deviceId] = currentLocation;
            
            // Check which geofence zone the device is in
            var currentZone = DetermineCurrentZone(currentLocation);
            
            if (currentLocation.CurrentZone != currentZone)
            {
                await OnZoneChangedAsync(deviceId, currentLocation.CurrentZone, currentZone);
                currentLocation.CurrentZone = currentZone;
            }
        }
    }
    
    private async Task ApplyLocationBasedRulesAsync()
    {
        foreach (var (deviceId, location) in _deviceLocations)
        {
            if (location.CurrentZone != null && _geofenceZones.ContainsKey(location.CurrentZone))
            {
                var zone = _geofenceZones[location.CurrentZone];
                await ApplyZoneRulesAsync(deviceId, zone);
            }
        }
    }
    
    private async Task ApplyZoneRulesAsync(string deviceId, GeofenceZone zone)
    {
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);
        var currentDay = DateTime.Now.DayOfWeek;
        
        // Check if current time is within allowed time window
        var isTimeAllowed = zone.TimeRestrictions?.GetValueOrDefault(currentDay)?
            .Any(window => currentTime >= window.Start && currentTime <= window.End) ?? true;
        
        if (!isTimeAllowed)
        {
            await BlockAllAccessAsync(deviceId, $"Outside allowed time for {zone.Name}");
            return;
        }
        
        // Apply category restrictions
        foreach (var category in GetAllContentCategories())
        {
            if (zone.AllowedCategories.Contains(category))
            {
                await AllowCategoryAsync(deviceId, category);
            }
            else
            {
                await BlockCategoryAsync(deviceId, category, $"Not allowed in {zone.Name}");
            }
        }
        
        // Apply safety features if in public space
        if (zone.SafetyFeatures != null)
        {
            await EnableSafetyFeaturesAsync(deviceId, zone.SafetyFeatures);
        }
        
        logger.LogDebug("Applied zone rules for device {DeviceId} in {ZoneName}", deviceId, zone.Name);
    }
    
    private async Task DetectLocationAnomaliesAsync()
    {
        foreach (var (deviceId, location) in _deviceLocations)
        {
            // Detect if device is in unexpected location during school hours
            if (DateTime.Now.DayOfWeek >= DayOfWeek.Monday && 
                DateTime.Now.DayOfWeek <= DayOfWeek.Friday &&
                DateTime.Now.Hour >= 8 && DateTime.Now.Hour <= 15)
            {
                if (location.CurrentZone != "school" && location.CurrentZone != "home")
                {
                    await NotifyLocationAnomalyAsync(deviceId, new LocationAnomaly
                    {
                        Type = AnomalyType.UnexpectedLocation,
                        Description = $"Device detected outside home/school during school hours",
                        Location = location,
                        Severity = AnomalySeverity.Medium,
                        RecommendedAction = "Verify child's whereabouts"
                    });
                }
            }
            
            // Detect rapid location changes (possible sharing/lending)
            var recentLocations = GetRecentLocations(deviceId, TimeSpan.FromMinutes(30));
            if (recentLocations.Count > 3)
            {
                await NotifyLocationAnomalyAsync(deviceId, new LocationAnomaly
                {
                    Type = AnomalyType.RapidMovement,
                    Description = "Unusual rapid location changes detected",
                    Location = location,
                    Severity = AnomalySeverity.High,
                    RecommendedAction = "Check if device was shared or lost"
                });
            }
        }
    }
    
    private async Task OnZoneChangedAsync(string deviceId, string? previousZone, string? newZone)
    {
        logger.LogInformation("Device {DeviceId} moved from {PreviousZone} to {NewZone}", 
            deviceId, previousZone ?? "unknown", newZone ?? "unknown");
        
        // Send notification to parents about zone change
        await NotifyZoneChangeAsync(deviceId, previousZone, newZone);
        
        // Log location history
        await LogLocationChangeAsync(deviceId, previousZone, newZone, DateTime.Now);
    }
    
    // Smart location prediction based on patterns
    public async Task<LocationPrediction> PredictNextLocationAsync(string deviceId)
    {
        var history = await GetLocationHistoryAsync(deviceId, TimeSpan.FromDays(30));
        var currentTime = DateTime.Now;
        
        // Analyze patterns for current day of week and time
        var similarTimeSlots = history.Where(h => 
            h.Timestamp.DayOfWeek == currentTime.DayOfWeek &&
            Math.Abs((h.Timestamp.TimeOfDay - currentTime.TimeOfDay).TotalMinutes) < 60);
        
        var predictions = similarTimeSlots
            .GroupBy(h => h.Location)
            .Select(g => new LocationPrediction
            {
                Location = g.Key,
                Probability = g.Count() / (float)similarTimeSlots.Count(),
                EstimatedArrival = currentTime.AddMinutes(g.Average(h => h.TravelTime?.TotalMinutes ?? 0))
            })
            .OrderByDescending(p => p.Probability)
            .FirstOrDefault() ?? new LocationPrediction();
        
        return predictions;
    }
    
    // Helper methods
    private IEnumerable<string> GetConnectedDeviceIds() => []; // Implementation needed
    private async Task<DeviceLocation> EstimateDeviceLocationAsync(string deviceId) => new(); // Implementation needed
    private string? DetermineCurrentZone(DeviceLocation location) => null; // Implementation needed
    private IEnumerable<string> GetAllContentCategories() => []; // Implementation needed
    private async Task BlockAllAccessAsync(string deviceId, string reason) { } // Implementation needed
    private async Task AllowCategoryAsync(string deviceId, string category) { } // Implementation needed
    private async Task BlockCategoryAsync(string deviceId, string category, string reason) { } // Implementation needed
    private async Task EnableSafetyFeaturesAsync(string deviceId, SafetyFeatures features) { } // Implementation needed
    private async Task NotifyLocationAnomalyAsync(string deviceId, LocationAnomaly anomaly) { } // Implementation needed
    private List<DeviceLocation> GetRecentLocations(string deviceId, TimeSpan timeSpan) => []; // Implementation needed
    private async Task NotifyZoneChangeAsync(string deviceId, string? previousZone, string? newZone) { } // Implementation needed
    private async Task LogLocationChangeAsync(string deviceId, string? previousZone, string? newZone, DateTime timestamp) { } // Implementation needed
    private async Task<List<LocationHistoryEntry>> GetLocationHistoryAsync(string deviceId, TimeSpan timeSpan) => []; // Implementation needed
}

public record GeofenceZone
{
    public string Name { get; init; } = "";
    public ZoneType Type { get; init; }
    public RestrictionLevel RestrictiveLevel { get; init; }
    public List<string> AllowedCategories { get; init; } = [];
    public Dictionary<DayOfWeek, TimeWindow[]>? TimeRestrictions { get; init; }
    public bool EmergencyContactsAllowed { get; init; }
    public NotificationSettings? NotificationSettings { get; init; }
    public SafetyFeatures? SafetyFeatures { get; init; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
}

public record TimeWindow(TimeOnly Start, TimeOnly End);

public record DeviceLocation
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public float Accuracy { get; init; }
    public DateTime Timestamp { get; init; }
    public string? CurrentZone { get; set; }
}

public record NotificationSettings
{
    public bool NotifyParentOnAccess { get; init; }
    public bool NotifyOnViolation { get; init; }
    public bool SilentMode { get; init; }
}

public record SafetyFeatures
{
    public bool LocationTrackingEnabled { get; init; }
    public bool EmergencyButtonVisible { get; init; }
    public bool StrangerDangerMode { get; init; }
    public bool NoiseMonitoring { get; init; }
}

public record LocationAnomaly
{
    public AnomalyType Type { get; init; }
    public string Description { get; init; } = "";
    public DeviceLocation Location { get; init; } = new();
    public AnomalySeverity Severity { get; init; }
    public string RecommendedAction { get; init; } = "";
}

public record LocationPrediction
{
    public string Location { get; init; } = "";
    public float Probability { get; init; }
    public DateTime EstimatedArrival { get; init; }
}

public record LocationHistoryEntry
{
    public string Location { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public TimeSpan? TravelTime { get; init; }
}

public record LocationBasedRuleset
{
    public string ZoneId { get; init; } = "";
    public Dictionary<string, bool> CategoryPermissions { get; init; } = new();
    public TimeSpan? DailyLimit { get; init; }
    public List<string> BlockedApps { get; init; } = [];
    public List<string> AllowedApps { get; init; } = [];
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

public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}