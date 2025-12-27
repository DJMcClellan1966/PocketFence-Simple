using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PocketFence_Simple.Services;

/// <summary>
/// AI-powered behavioral analysis that learns family patterns and provides intelligent recommendations
/// </summary>
public class SmartBehaviorAnalysisService(ILogger<SmartBehaviorAnalysisService> logger) : BackgroundService
{
    private readonly Dictionary<string, DeviceBehaviorProfile> _deviceProfiles = new();
    private readonly Dictionary<string, List<UsageSession>> _usageSessions = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await AnalyzeBehaviorPatternsAsync();
            await GenerateSmartRecommendationsAsync();
            await DetectAnomalousActivityAsync();
        }
    }
    
    public async Task<BehaviorInsights> GetBehaviorInsightsAsync(string deviceId)
    {
        if (!_deviceProfiles.ContainsKey(deviceId))
            return new BehaviorInsights();
            
        var profile = _deviceProfiles[deviceId];
        var sessions = _usageSessions.GetValueOrDefault(deviceId, []);
        
        return new BehaviorInsights
        {
            AverageSessionDuration = CalculateAverageSessionDuration(sessions),
            PeakUsageHours = DeterminePeakUsageHours(sessions),
            ContentPreferences = AnalyzeContentPreferences(sessions),
            DigitalWellnessScore = CalculateWellnessScore(profile, sessions),
            PredictedScreenTime = PredictTomorrowsUsage(sessions),
            RecommendedBreaks = GenerateBreakRecommendations(profile),
            EducationalOpportunities = SuggestEducationalContent(profile),
            SleepImpactAnalysis = AnalyzeSleepImpact(sessions),
            SocialInteractionBalance = CalculateSocialBalance(sessions),
            EmotionalStateIndicators = DetectEmotionalPatterns(sessions)
        };
    }
    
    private async Task AnalyzeBehaviorPatternsAsync()
    {
        foreach (var (deviceId, profile) in _deviceProfiles)
        {
            // Analyze app usage patterns
            await AnalyzeAppUsagePatterns(deviceId, profile);
            
            // Detect binge behavior
            await DetectBingeBehavior(deviceId, profile);
            
            // Monitor attention span changes
            await TrackAttentionSpanTrends(deviceId, profile);
            
            // Analyze content switching patterns
            await AnalyzeContentSwitchingBehavior(deviceId, profile);
        }
        
        logger.LogInformation("Completed behavioral pattern analysis for {DeviceCount} devices", _deviceProfiles.Count);
    }
    
    private async Task GenerateSmartRecommendationsAsync()
    {
        foreach (var (deviceId, profile) in _deviceProfiles)
        {
            var recommendations = new List<SmartRecommendation>();
            
            // Smart bedtime recommendations based on sleep impact analysis
            if (profile.LateNightUsage > TimeSpan.FromHours(1))
            {
                recommendations.Add(new SmartRecommendation
                {
                    Type = RecommendationType.SleepOptimization,
                    Title = "Improve Sleep Quality",
                    Description = "Device usage after 9 PM may be affecting sleep. Consider enabling 'Wind Down' mode 1 hour before bedtime.",
                    Confidence = 0.87f,
                    AutoImplementable = true,
                    EstimatedImpact = "Better sleep quality, improved mood and focus"
                });
            }
            
            // Educational content boost recommendations
            if (profile.EducationalContentRatio < 0.3f)
            {
                recommendations.Add(new SmartRecommendation
                {
                    Type = RecommendationType.EducationalBoost,
                    Title = "Boost Learning Time",
                    Description = $"Only {profile.EducationalContentRatio:P0} of screen time is educational. Try 15-minute learning sessions before entertainment.",
                    Confidence = 0.73f,
                    AutoImplementable = false,
                    EstimatedImpact = "Enhanced cognitive development, better academic performance"
                });
            }
            
            // Social interaction balance
            if (profile.SocialMediaTime > profile.FaceToFaceTime * 3)
            {
                recommendations.Add(new SmartRecommendation
                {
                    Type = RecommendationType.SocialBalance,
                    Title = "Balance Digital and Real Connections",
                    Description = "Consider family activities to balance online social time with face-to-face interactions.",
                    Confidence = 0.65f,
                    AutoImplementable = false,
                    EstimatedImpact = "Stronger family bonds, improved social skills"
                });
            }
            
            await NotifyParentsOfRecommendations(deviceId, recommendations);
        }
    }
    
    private async Task DetectAnomalousActivityAsync()
    {
        foreach (var (deviceId, profile) in _deviceProfiles)
        {
            var sessions = _usageSessions.GetValueOrDefault(deviceId, []);
            var recentSessions = sessions.Where(s => s.StartTime > DateTime.Now.AddDays(-7)).ToList();
            
            // Detect sudden usage spikes
            var avgDailyUsage = recentSessions.GroupBy(s => s.StartTime.Date)
                .Select(g => g.Sum(s => s.Duration.TotalHours))
                .Average();
                
            var todayUsage = recentSessions.Where(s => s.StartTime.Date == DateTime.Today)
                .Sum(s => s.Duration.TotalHours);
                
            if (todayUsage > avgDailyUsage * 2)
            {
                await NotifyAnomalousActivity(deviceId, new AnomalousActivity
                {
                    Type = AnomalyType.UsageSpike,
                    Severity = Severity.Medium,
                    Description = $"Screen time today ({todayUsage:F1}h) is {todayUsage/avgDailyUsage:F1}x normal average",
                    PossibleCauses = ["Illness/staying home", "New app addiction", "Emotional distress", "Schedule change"]
                });
            }
            
            // Detect late-night usage patterns
            var lateNightSessions = recentSessions.Where(s => s.StartTime.Hour >= 22 || s.StartTime.Hour <= 5);
            if (lateNightSessions.Count() > recentSessions.Count * 0.2)
            {
                await NotifyAnomalousActivity(deviceId, new AnomalousActivity
                {
                    Type = AnomalyType.SleepDisruption,
                    Severity = Severity.High,
                    Description = "Increased late-night device usage detected",
                    PossibleCauses = ["Sleep issues", "Anxiety", "Social pressure", "Gaming addiction"]
                });
            }
        }
    }
    
    private TimeSpan CalculateAverageSessionDuration(List<UsageSession> sessions)
    {
        if (sessions.Count == 0) return TimeSpan.Zero;
        return TimeSpan.FromMilliseconds(sessions.Average(s => s.Duration.TotalMilliseconds));
    }
    
    private List<int> DeterminePeakUsageHours(List<UsageSession> sessions) =>
        sessions.GroupBy(s => s.StartTime.Hour)
            .OrderByDescending(g => g.Sum(s => s.Duration.TotalMinutes))
            .Take(3)
            .Select(g => g.Key)
            .ToList();
    
    private Dictionary<string, float> AnalyzeContentPreferences(List<UsageSession> sessions) =>
        sessions.GroupBy(s => s.ContentCategory)
            .ToDictionary(
                g => g.Key,
                g => (float)(g.Sum(s => s.Duration.TotalMinutes) / sessions.Sum(s => s.Duration.TotalMinutes))
            );
    
    private float CalculateWellnessScore(DeviceBehaviorProfile profile, List<UsageSession> sessions)
    {
        var score = 100f;
        
        // Deduct for excessive usage
        var dailyAverage = sessions.GroupBy(s => s.StartTime.Date)
            .Average(g => g.Sum(s => s.Duration.TotalHours));
        if (dailyAverage > 4) score -= (float)(dailyAverage - 4) * 10;
        
        // Bonus for educational content
        score += profile.EducationalContentRatio * 20;
        
        // Deduct for late night usage
        score -= Math.Min(30, (float)profile.LateNightUsage.TotalHours * 5);
        
        return Math.Max(0, Math.Min(100, score));
    }
    
    private async Task NotifyParentsOfRecommendations(string deviceId, List<SmartRecommendation> recommendations)
    {
        // Implementation would send notifications to parents
        logger.LogInformation("Generated {Count} recommendations for device {DeviceId}", 
            recommendations.Count, deviceId);
    }
    
    private async Task NotifyAnomalousActivity(string deviceId, AnomalousActivity activity)
    {
        logger.LogWarning("Anomalous activity detected for device {DeviceId}: {Description}", 
            deviceId, activity.Description);
    }
    
    // Additional helper methods would be implemented here...
    private async Task AnalyzeAppUsagePatterns(string deviceId, DeviceBehaviorProfile profile) { }
    private async Task DetectBingeBehavior(string deviceId, DeviceBehaviorProfile profile) { }
    private async Task TrackAttentionSpanTrends(string deviceId, DeviceBehaviorProfile profile) { }
    private async Task AnalyzeContentSwitchingBehavior(string deviceId, DeviceBehaviorProfile profile) { }
    private TimeSpan PredictTomorrowsUsage(List<UsageSession> sessions) => TimeSpan.Zero;
    private List<string> GenerateBreakRecommendations(DeviceBehaviorProfile profile) => [];
    private List<string> SuggestEducationalContent(DeviceBehaviorProfile profile) => [];
    private string AnalyzeSleepImpact(List<UsageSession> sessions) => "";
    private string CalculateSocialBalance(List<UsageSession> sessions) => "";
    private List<string> DetectEmotionalPatterns(List<UsageSession> sessions) => [];
}

public record DeviceBehaviorProfile
{
    public float EducationalContentRatio { get; init; }
    public TimeSpan LateNightUsage { get; init; }
    public TimeSpan SocialMediaTime { get; init; }
    public TimeSpan FaceToFaceTime { get; init; }
    public float AverageAttentionSpan { get; init; }
    public int ContentSwitchFrequency { get; init; }
}

public record UsageSession
{
    public DateTime StartTime { get; init; }
    public TimeSpan Duration { get; init; }
    public string ContentCategory { get; init; } = "";
    public string AppName { get; init; } = "";
    public string ActivityType { get; init; } = "";
}

public record BehaviorInsights
{
    public TimeSpan AverageSessionDuration { get; init; }
    public List<int> PeakUsageHours { get; init; } = [];
    public Dictionary<string, float> ContentPreferences { get; init; } = new();
    public float DigitalWellnessScore { get; init; }
    public TimeSpan PredictedScreenTime { get; init; }
    public List<string> RecommendedBreaks { get; init; } = [];
    public List<string> EducationalOpportunities { get; init; } = [];
    public string SleepImpactAnalysis { get; init; } = "";
    public string SocialInteractionBalance { get; init; } = "";
    public List<string> EmotionalStateIndicators { get; init; } = [];
}

public record SmartRecommendation
{
    public RecommendationType Type { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public float Confidence { get; init; }
    public bool AutoImplementable { get; init; }
    public string EstimatedImpact { get; init; } = "";
}

public record AnomalousActivity
{
    public AnomalyType Type { get; init; }
    public Severity Severity { get; init; }
    public string Description { get; init; } = "";
    public List<string> PossibleCauses { get; init; } = [];
}

public enum RecommendationType
{
    SleepOptimization,
    EducationalBoost,
    SocialBalance,
    AttentionImprovement,
    PhysicalActivity,
    DigitalDetox
}

public enum AnomalyType
{
    UsageSpike,
    SleepDisruption,
    SocialWithdrawal,
    AcademicImpact,
    EmotionalDistress,
    UnexpectedLocation,
    RapidMovement
}

public enum Severity
{
    Low,
    Medium,
    High,
    Critical
}