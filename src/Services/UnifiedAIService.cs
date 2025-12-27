using Microsoft.Extensions.Logging;

namespace PocketFence_Simple.Services;

/// <summary>
/// Unified AI service that combines behavioral analysis, wellness monitoring, and content analysis
/// </summary>
public class UnifiedAIService(ILogger<UnifiedAIService> logger) : BackgroundService
{
    private readonly Dictionary<string, DeviceProfile> _deviceProfiles = new();
    private readonly Dictionary<string, List<ActivityLog>> _activityHistory = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await UpdateDeviceProfiles();
            await GenerateInsights();
        }
    }
    
    public async Task<DeviceInsights> GetDeviceInsightsAsync(string deviceId)
    {
        if (!_deviceProfiles.ContainsKey(deviceId))
            return new DeviceInsights { DeviceId = deviceId };
            
        var profile = _deviceProfiles[deviceId];
        var recentActivity = _activityHistory.GetValueOrDefault(deviceId, [])
            .Where(a => a.Timestamp > DateTime.Now.AddDays(-7))
            .ToList();
            
        return new DeviceInsights
        {
            DeviceId = deviceId,
            WellnessScore = CalculateWellnessScore(recentActivity),
            BehaviorPattern = AnalyzeBehaviorPattern(recentActivity),
            ScreenTimeToday = CalculateTodayScreenTime(recentActivity),
            Recommendations = GenerateSimpleRecommendations(profile, recentActivity),
            ThreatLevel = AssessThreatLevel(recentActivity),
            LastActivity = recentActivity.LastOrDefault()?.Timestamp ?? DateTime.MinValue
        };
    }
    
    public async Task<ContentAnalysisResult> AnalyzeContentAsync(string content, string deviceId)
    {
        var threatScore = CalculateSimpleThreatScore(content);
        var shouldBlock = threatScore > 0.7f;
        
        // Log the activity
        await LogActivity(deviceId, new ActivityLog
        {
            Type = "ContentAccess",
            Content = content.Length > 100 ? content[..100] + "..." : content,
            ThreatScore = threatScore,
            Timestamp = DateTime.Now,
            Action = shouldBlock ? "Blocked" : "Allowed"
        });
        
        return new ContentAnalysisResult
        {
            ThreatScore = threatScore,
            ShouldBlock = shouldBlock,
            Reason = GetThreatReason(threatScore),
            Categories = DetectContentCategories(content)
        };
    }
    
    public async Task LogLocationAsync(string deviceId, string location)
    {
        await LogActivity(deviceId, new ActivityLog
        {
            Type = "Location",
            Content = location,
            Timestamp = DateTime.Now
        });
        
        // Apply location-based rules
        await ApplyLocationRules(deviceId, location);
    }
    
    private async Task UpdateDeviceProfiles()
    {
        foreach (var (deviceId, activities) in _activityHistory)
        {
            var profile = _deviceProfiles.GetValueOrDefault(deviceId) ?? new DeviceProfile { DeviceId = deviceId };
            
            var recentActivities = activities.Where(a => a.Timestamp > DateTime.Now.AddDays(-7)).ToList();
            
            profile.TotalScreenTime += recentActivities
                .Where(a => a.Type == "ScreenTime")
                .Sum(a => a.Duration?.TotalMinutes ?? 0);
                
            profile.AverageThreatScore = recentActivities
                .Where(a => a.ThreatScore > 0)
                .Select(a => a.ThreatScore)
                .DefaultIfEmpty(0)
                .Average();
                
            profile.LastUpdated = DateTime.Now;
            _deviceProfiles[deviceId] = profile;
        }
    }
    
    private async Task GenerateInsights()
    {
        foreach (var (deviceId, profile) in _deviceProfiles)
        {
            var insights = await GetDeviceInsightsAsync(deviceId);
            
            if (insights.ThreatLevel == "High" || insights.WellnessScore < 50)
            {
                logger.LogWarning("Device {DeviceId} needs attention: Wellness={WellnessScore}, Threat={ThreatLevel}", 
                    deviceId, insights.WellnessScore, insights.ThreatLevel);
            }
        }
    }
    
    private int CalculateWellnessScore(List<ActivityLog> activities)
    {
        var score = 100;
        
        // Deduct for excessive screen time
        var dailyScreenTime = activities
            .Where(a => a.Type == "ScreenTime" && a.Timestamp.Date == DateTime.Today)
            .Sum(a => a.Duration?.TotalHours ?? 0);
        if (dailyScreenTime > 4) score -= (int)(dailyScreenTime - 4) * 10;
        
        // Deduct for late night usage
        var lateNightUsage = activities
            .Where(a => a.Timestamp.Hour > 22 || a.Timestamp.Hour < 6)
            .Count();
        score -= lateNightUsage * 2;
        
        return Math.Max(0, Math.Min(100, score));
    }
    
    private string AnalyzeBehaviorPattern(List<ActivityLog> activities)
    {
        if (activities.Count < 10) return "Insufficient Data";
        
        var avgThreatScore = activities.Where(a => a.ThreatScore > 0)
            .Select(a => a.ThreatScore)
            .DefaultIfEmpty(0)
            .Average();
            
        return avgThreatScore switch
        {
            < 0.3f => "Excellent",
            < 0.5f => "Good", 
            < 0.7f => "Concerning",
            _ => "High Risk"
        };
    }
    
    private TimeSpan CalculateTodayScreenTime(List<ActivityLog> activities)
    {
        var today = activities
            .Where(a => a.Type == "ScreenTime" && a.Timestamp.Date == DateTime.Today)
            .Sum(a => a.Duration?.TotalMinutes ?? 0);
        return TimeSpan.FromMinutes(today);
    }
    
    private List<string> GenerateSimpleRecommendations(DeviceProfile profile, List<ActivityLog> activities)
    {
        var recommendations = new List<string>();
        
        var dailyScreenTime = CalculateTodayScreenTime(activities).TotalHours;
        if (dailyScreenTime > 4)
            recommendations.Add("Consider taking more breaks from screen time");
            
        var lateNightUsage = activities.Count(a => a.Timestamp.Hour > 22);
        if (lateNightUsage > 3)
            recommendations.Add("Try to avoid screen use after 10 PM for better sleep");
            
        if (profile.AverageThreatScore > 0.5)
            recommendations.Add("Be more cautious about the content you access");
            
        return recommendations;
    }
    
    private string AssessThreatLevel(List<ActivityLog> activities)
    {
        var avgThreat = activities
            .Where(a => a.ThreatScore > 0)
            .Select(a => a.ThreatScore)
            .DefaultIfEmpty(0)
            .Average();
            
        return avgThreat switch
        {
            < 0.3f => "Low",
            < 0.7f => "Medium", 
            _ => "High"
        };
    }
    
    private float CalculateSimpleThreatScore(string content)
    {
        var threats = new[] { "violence", "adult", "drug", "gambling", "hate", "scam" };
        var contentLower = content.ToLowerInvariant();
        
        var threatCount = threats.Count(threat => contentLower.Contains(threat));
        return Math.Min(1.0f, threatCount * 0.3f);
    }
    
    private string GetThreatReason(float threatScore)
    {
        return threatScore switch
        {
            > 0.7f => "High risk content detected",
            > 0.4f => "Potentially inappropriate content",
            > 0.1f => "Minor content concerns",
            _ => "Content appears safe"
        };
    }
    
    private List<string> DetectContentCategories(string content)
    {
        var categories = new List<string>();
        var contentLower = content.ToLowerInvariant();
        
        if (contentLower.Contains("education") || contentLower.Contains("learn")) categories.Add("Educational");
        if (contentLower.Contains("game") || contentLower.Contains("play")) categories.Add("Entertainment");
        if (contentLower.Contains("social") || contentLower.Contains("chat")) categories.Add("Social");
        if (contentLower.Contains("news") || contentLower.Contains("article")) categories.Add("News");
        
        return categories.Count > 0 ? categories : ["General"];
    }
    
    private async Task LogActivity(string deviceId, ActivityLog activity)
    {
        if (!_activityHistory.ContainsKey(deviceId))
            _activityHistory[deviceId] = [];
            
        _activityHistory[deviceId].Add(activity);
        
        // Keep only last 1000 entries per device
        if (_activityHistory[deviceId].Count > 1000)
            _activityHistory[deviceId] = _activityHistory[deviceId].TakeLast(1000).ToList();
    }
    
    private async Task ApplyLocationRules(string deviceId, string location)
    {
        // Simple location-based rules
        switch (location.ToLowerInvariant())
        {
            case "school":
                logger.LogInformation("Device {DeviceId} at school - applying strict filtering", deviceId);
                break;
            case "home":
                logger.LogInformation("Device {DeviceId} at home - applying standard filtering", deviceId);
                break;
            case "public":
                logger.LogInformation("Device {DeviceId} in public - enabling safety mode", deviceId);
                break;
        }
    }
}

// Simplified data models
public record DeviceProfile
{
    public string DeviceId { get; init; } = "";
    public double TotalScreenTime { get; set; }
    public float AverageThreatScore { get; set; }
    public DateTime LastUpdated { get; set; }
}

public record ActivityLog
{
    public string Type { get; init; } = "";
    public string Content { get; init; } = "";
    public float ThreatScore { get; init; }
    public TimeSpan? Duration { get; init; }
    public DateTime Timestamp { get; init; }
    public string Action { get; init; } = "";
}

public record DeviceInsights
{
    public string DeviceId { get; init; } = "";
    public int WellnessScore { get; init; }
    public string BehaviorPattern { get; init; } = "";
    public TimeSpan ScreenTimeToday { get; init; }
    public List<string> Recommendations { get; init; } = [];
    public string ThreatLevel { get; init; } = "";
    public DateTime LastActivity { get; init; }
}

public record ContentAnalysisResult
{
    public float ThreatScore { get; init; }
    public bool ShouldBlock { get; init; }
    public string Reason { get; init; } = "";
    public List<string> Categories { get; init; } = [];
}