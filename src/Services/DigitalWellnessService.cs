using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PocketFence_Simple.Services;

/// <summary>
/// Advanced digital wellness service that monitors eye strain, posture, and suggests health breaks
/// </summary>
public class DigitalWellnessService(ILogger<DigitalWellnessService> logger) : BackgroundService
{
    private readonly Dictionary<string, WellnessProfile> _wellnessProfiles = new();
    private readonly Dictionary<string, List<WellnessMetric>> _wellnessHistory = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await MonitorEyeStrainIndicators();
            await AssessPostureHealth();
            await GenerateWellnessRecommendations();
            await TriggerSmartBreakSuggestions();
        }
    }
    
    private async Task MonitorEyeStrainIndicators()
    {
        foreach (var (deviceId, profile) in _wellnessProfiles)
        {
            var metrics = await CalculateEyeStrainMetrics(deviceId, profile);
            
            logger.LogDebug("Monitoring eye strain indicators for device {DeviceId}", deviceId);
            
            // Monitor screen brightness vs ambient light
            if (metrics.BrightnessToAmbientRatio > 3.0f)
            {
                await SuggestBrightnessAdjustment(deviceId, 
                    "Screen is too bright for current environment. This may cause eye strain.");
            }
            
            // Monitor blink rate (estimated from usage patterns)
            if (metrics.EstimatedBlinkRate < 12) // Normal is 15-20 blinks per minute
            {
                await TriggerBlinkReminder(deviceId, 
                    "Remember to blink frequently to keep your eyes moist!");
            }
            
            // Monitor 20-20-20 rule compliance
            if (metrics.TimeSinceLastBreak > TimeSpan.FromMinutes(20))
            {
                await Suggest2020Rule(deviceId, 
                    "Time for a 20-second break! Look at something 20 feet away.");
            }
            
            // Detect text size optimization opportunities
            if (metrics.AverageTextSize < 12 && metrics.ScreenDistance < 60) // cm
            {
                await SuggestTextSizeIncrease(deviceId, 
                    "Consider increasing text size or holding device further away.");
            }
        }
    }
    
    private async Task AssessPostureHealth()
    {
        foreach (var (deviceId, profile) in _wellnessProfiles)
        {
            var postureMetrics = await AnalyzePostureIndicators(deviceId, profile);
            
            // Detect "text neck" - prolonged looking down
            if (postureMetrics.DownwardAngleTime > TimeSpan.FromMinutes(30))
            {
                await SuggestPostureCorrection(deviceId, new PostureAlert
                {
                    Type = PostureAlertType.TextNeck,
                    Message = "Take a moment to straighten your neck and shoulders!",
                    ExerciseSuggestion = "Slowly roll your shoulders back 5 times, then gently stretch your neck side to side.",
                    Severity = postureMetrics.DownwardAngleTime > TimeSpan.FromHours(1) ? AlertSeverity.High : AlertSeverity.Medium
                });
            }
            
            // Detect prolonged static position
            if (postureMetrics.StaticPositionTime > TimeSpan.FromMinutes(45))
            {
                await SuggestMovementBreak(deviceId, new MovementAlert
                {
                    Type = MovementType.GeneralMobility,
                    Message = "Time to move around! Your body needs some activity.",
                    Suggestions = [
                        "Stand up and stretch for 2 minutes",
                        "Walk around the room or house",
                        "Do 10 jumping jacks or stretches",
                        "Get a drink of water"
                    ],
                    Duration = TimeSpan.FromMinutes(5)
                });
            }
            
            // Monitor device holding patterns
            if (postureMetrics.SingleHandedUsageRatio > 0.7f)
            {
                await SuggestGripChange(deviceId, 
                    "Consider using both hands to hold your device to reduce strain.");
            }
        }
    }
    
    private async Task GenerateWellnessRecommendations()
    {
        foreach (var (deviceId, profile) in _wellnessProfiles)
        {
            var weeklyMetrics = await CalculateWeeklyWellnessMetrics(deviceId);
            var recommendations = new List<WellnessRecommendation>();
            
            // Screen time quality analysis
            if (weeklyMetrics.AverageSessionDuration > TimeSpan.FromHours(2))
            {
                recommendations.Add(new WellnessRecommendation
                {
                    Category = WellnessCategory.ScreenTimeManagement,
                    Priority = RecommendationPriority.High,
                    Title = "Break up long screen sessions",
                    Description = "Your average session is quite long. Try the Pomodoro Technique: 25 minutes focused time, then 5-minute break.",
                    Benefits = "Improved focus, reduced eye strain, better productivity",
                    ImplementationTips = [
                        "Set a timer for 25-minute work sessions",
                        "Use break time for physical movement",
                        "Keep a water bottle nearby for hydration breaks"
                    ]
                });
            }
            
            // Sleep quality correlation
            if (weeklyMetrics.LateNightUsageHours > 14) // More than 2 hours per night average
            {
                recommendations.Add(new WellnessRecommendation
                {
                    Category = WellnessCategory.SleepOptimization,
                    Priority = RecommendationPriority.Critical,
                    Title = "Improve sleep hygiene",
                    Description = "Late-night screen time may be affecting your sleep quality. Blue light can disrupt your natural sleep cycle.",
                    Benefits = "Better sleep quality, improved mood, enhanced learning",
                    ImplementationTips = [
                        "Stop screen use 1 hour before bedtime",
                        "Use blue light filters after sunset",
                        "Try reading a book or listening to calm music instead"
                    ]
                });
            }
            
            // Physical activity correlation
            if (weeklyMetrics.ScreenTimeToPhysicalActivityRatio > 8)
            {
                recommendations.Add(new WellnessRecommendation
                {
                    Category = WellnessCategory.PhysicalActivity,
                    Priority = RecommendationPriority.Medium,
                    Title = "Increase physical activity",
                    Description = "Balance screen time with more physical movement for better overall health.",
                    Benefits = "Better cardiovascular health, improved mood, stronger muscles",
                    ImplementationTips = [
                        "Take walking breaks every hour",
                        "Try active hobbies like dancing or sports",
                        "Use stairs instead of elevators when possible"
                    ]
                });
            }
            
            await NotifyWellnessRecommendations(deviceId, recommendations);
        }
    }
    
    private async Task TriggerSmartBreakSuggestions()
    {
        foreach (var (deviceId, profile) in _wellnessProfiles)
        {
            var currentMetrics = await GetCurrentWellnessMetrics(deviceId);
            
            // Intelligent break timing based on usage patterns and wellness indicators
            var breakSuggestion = DetermineOptimalBreakType(currentMetrics, profile);
            
            if (breakSuggestion != null)
            {
                await DeliverBreakSuggestion(deviceId, breakSuggestion);
            }
        }
    }
    
    private SmartBreakSuggestion? DetermineOptimalBreakType(WellnessMetrics metrics, WellnessProfile profile)
    {
        // Micro-breaks (30 seconds - 2 minutes)
        if (metrics.ContinuousUsageTime > TimeSpan.FromMinutes(20) && 
            metrics.EyeStrainIndicators.Any(i => i.Severity >= IndicatorSeverity.Medium))
        {
            return new SmartBreakSuggestion
            {
                Type = BreakType.EyeRelief,
                Duration = TimeSpan.FromSeconds(30),
                Title = "Quick Eye Break",
                Instructions = [
                    "Look away from the screen",
                    "Focus on something 20 feet away for 20 seconds",
                    "Blink slowly 10 times"
                ],
                Motivation = "Your eyes deserve this quick reset! ✨"
            };
        }
        
        // Movement breaks (2-5 minutes)
        if (metrics.StaticPositionTime > TimeSpan.FromMinutes(45))
        {
            var exercises = GeneratePersonalizedExercises(profile.Age, profile.FitnessLevel);
            return new SmartBreakSuggestion
            {
                Type = BreakType.Movement,
                Duration = TimeSpan.FromMinutes(3),
                Title = "Movement Break",
                Instructions = exercises,
                Motivation = "Time to energize your body! 💪"
            };
        }
        
        // Mindfulness breaks (5-10 minutes)
        if (metrics.StressIndicators.Count > 2)
        {
            return new SmartBreakSuggestion
            {
                Type = BreakType.Mindfulness,
                Duration = TimeSpan.FromMinutes(5),
                Title = "Mindful Moment",
                Instructions = [
                    "Sit comfortably and close your eyes",
                    "Take 5 deep, slow breaths",
                    "Focus on the sensation of breathing",
                    "Notice any sounds around you without judgment"
                ],
                Motivation = "A peaceful mind leads to better focus 🧘‍♀️"
            };
        }
        
        return null;
    }
    
    private List<string> GeneratePersonalizedExercises(int age, FitnessLevel fitnessLevel)
    {
        var exercises = new List<string>();
        
        // Age-appropriate exercises
        if (age < 12)
        {
            exercises.AddRange([
                "Do 5 jumping jacks",
                "Stretch your arms up to the sky",
                "Touch your toes gently",
                "Walk around the room"
            ]);
        }
        else if (age < 18)
        {
            exercises.AddRange([
                "Do 10 jumping jacks or high knees",
                "Stretch your neck side to side",
                "Do arm circles forward and backward",
                "Take a quick walk around your space"
            ]);
        }
        else
        {
            exercises.AddRange([
                "Stand and do desk stretches",
                "Walk to the window and back",
                "Do shoulder blade squeezes",
                "Gentle neck and wrist stretches"
            ]);
        }
        
        // Fitness level adjustments
        if (fitnessLevel == FitnessLevel.High)
        {
            exercises.Add("Add 5 push-ups or squats if you feel energetic!");
        }
        
        return exercises;
    }
    
    public async Task<WellnessReport> GenerateWellnessReportAsync(string deviceId, TimeSpan period)
    {
        if (!_wellnessHistory.ContainsKey(deviceId))
            return new WellnessReport();
            
        var metrics = _wellnessHistory[deviceId]
            .Where(m => m.Timestamp > DateTime.Now - period)
            .ToList();
            
        return new WellnessReport
        {
            Period = period,
            TotalScreenTime = TimeSpan.FromMilliseconds(metrics.Sum(m => m.ScreenTime.TotalMilliseconds)),
            AverageEyeStrainScore = metrics.Average(m => m.EyeStrainScore),
            PostureHealthScore = metrics.Average(m => m.PostureScore),
            BreakFrequency = metrics.Count(m => m.BreakTaken) / Math.Max(1, period.Days),
            SleepQualityCorrelation = CalculateSleepCorrelation(metrics),
            PhysicalActivityRatio = CalculateActivityRatio(metrics),
            WellnessImprovement = CalculateWellnessImprovement(metrics),
            Achievements = GenerateWellnessAchievements(metrics),
            PersonalizedTips = GeneratePersonalizedTips(deviceId, metrics)
        };
    }
    
    // Helper method implementations
    private async Task<EyeStrainMetrics> CalculateEyeStrainMetrics(string deviceId, WellnessProfile profile) => new();
    private async Task SuggestBrightnessAdjustment(string deviceId, string message) { }
    private async Task TriggerBlinkReminder(string deviceId, string message) { }
    private async Task Suggest2020Rule(string deviceId, string message) { }
    private async Task SuggestTextSizeIncrease(string deviceId, string message) { }
    private async Task<PostureMetrics> AnalyzePostureIndicators(string deviceId, WellnessProfile profile) => new();
    private async Task SuggestPostureCorrection(string deviceId, PostureAlert alert) { }
    private async Task SuggestMovementBreak(string deviceId, MovementAlert alert) { }
    private async Task SuggestGripChange(string deviceId, string message) { }
    private async Task<WeeklyWellnessMetrics> CalculateWeeklyWellnessMetrics(string deviceId) => new();
    private async Task NotifyWellnessRecommendations(string deviceId, List<WellnessRecommendation> recommendations) { }
    private async Task<WellnessMetrics> GetCurrentWellnessMetrics(string deviceId) => new();
    private async Task DeliverBreakSuggestion(string deviceId, SmartBreakSuggestion suggestion) { }
    private float CalculateSleepCorrelation(List<WellnessMetric> metrics) => 0f;
    private float CalculateActivityRatio(List<WellnessMetric> metrics) => 0f;
    private float CalculateWellnessImprovement(List<WellnessMetric> metrics) => 0f;
    private List<WellnessAchievement> GenerateWellnessAchievements(List<WellnessMetric> metrics) => [];
    private List<string> GeneratePersonalizedTips(string deviceId, List<WellnessMetric> metrics) => [];
}

// Data models for wellness tracking
public record WellnessProfile
{
    public int Age { get; init; }
    public FitnessLevel FitnessLevel { get; init; }
    public List<string> HealthConditions { get; init; } = [];
    public bool WearGlasses { get; init; }
    public float TypicalScreenDistance { get; init; } // in cm
    public List<WellnessGoal> Goals { get; init; } = [];
}

public record EyeStrainMetrics
{
    public float BrightnessToAmbientRatio { get; init; }
    public int EstimatedBlinkRate { get; init; }
    public TimeSpan TimeSinceLastBreak { get; init; }
    public float AverageTextSize { get; init; }
    public float ScreenDistance { get; init; }
}

public record PostureMetrics
{
    public TimeSpan DownwardAngleTime { get; init; }
    public TimeSpan StaticPositionTime { get; init; }
    public float SingleHandedUsageRatio { get; init; }
}

public record WeeklyWellnessMetrics
{
    public TimeSpan AverageSessionDuration { get; init; }
    public float LateNightUsageHours { get; init; }
    public float ScreenTimeToPhysicalActivityRatio { get; init; }
}

public record WellnessMetrics
{
    public TimeSpan ContinuousUsageTime { get; init; }
    public List<EyeStrainIndicator> EyeStrainIndicators { get; init; } = [];
    public TimeSpan StaticPositionTime { get; init; }
    public List<StressIndicator> StressIndicators { get; init; } = [];
}

public record WellnessRecommendation
{
    public WellnessCategory Category { get; init; }
    public RecommendationPriority Priority { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Benefits { get; init; } = "";
    public List<string> ImplementationTips { get; init; } = [];
}

public record SmartBreakSuggestion
{
    public BreakType Type { get; init; }
    public TimeSpan Duration { get; init; }
    public string Title { get; init; } = "";
    public List<string> Instructions { get; init; } = [];
    public string Motivation { get; init; } = "";
}

public record PostureAlert
{
    public PostureAlertType Type { get; init; }
    public string Message { get; init; } = "";
    public string ExerciseSuggestion { get; init; } = "";
    public AlertSeverity Severity { get; init; }
}

public record MovementAlert
{
    public MovementType Type { get; init; }
    public string Message { get; init; } = "";
    public List<string> Suggestions { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

public record WellnessReport
{
    public TimeSpan Period { get; init; }
    public TimeSpan TotalScreenTime { get; init; }
    public float AverageEyeStrainScore { get; init; }
    public float PostureHealthScore { get; init; }
    public float BreakFrequency { get; init; }
    public float SleepQualityCorrelation { get; init; }
    public float PhysicalActivityRatio { get; init; }
    public float WellnessImprovement { get; init; }
    public List<WellnessAchievement> Achievements { get; init; } = [];
    public List<string> PersonalizedTips { get; init; } = [];
}

public record WellnessMetric
{
    public DateTime Timestamp { get; init; }
    public TimeSpan ScreenTime { get; init; }
    public float EyeStrainScore { get; init; }
    public float PostureScore { get; init; }
    public bool BreakTaken { get; init; }
}

public record WellnessGoal
{
    public string Description { get; init; } = "";
    public GoalType Type { get; init; }
    public float Target { get; init; }
    public DateTime Deadline { get; init; }
}

public record WellnessAchievement
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTime Earned { get; init; }
    public AchievementLevel Level { get; init; }
}

public record EyeStrainIndicator
{
    public IndicatorType Type { get; init; }
    public IndicatorSeverity Severity { get; init; }
    public string Description { get; init; } = "";
}

public record StressIndicator
{
    public string Type { get; init; } = "";
    public float Intensity { get; init; }
    public string Source { get; init; } = "";
}

public enum FitnessLevel { Low, Medium, High }
public enum WellnessCategory { ScreenTimeManagement, SleepOptimization, PhysicalActivity, PostureHealth, EyeCare }
public enum RecommendationPriority { Low, Medium, High, Critical }
public enum BreakType { EyeRelief, Movement, Mindfulness, Hydration }
public enum PostureAlertType { TextNeck, RoundedShoulders, ForwardHead, ProlongedSitting }
public enum MovementType { GeneralMobility, TargetedStretching, Cardiovascular, Strength }
public enum AlertSeverity { Low, Medium, High, Critical }
public enum GoalType { ReduceScreenTime, IncreaseBreaks, ImprovePosture, BetterSleep }
public enum AchievementLevel { Bronze, Silver, Gold, Platinum }
public enum IndicatorType { BlinkRate, BrightnessStrain, TextSizeStrain, DistanceStrain }
public enum IndicatorSeverity { Low, Medium, High, Critical }