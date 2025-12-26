using System.ComponentModel.DataAnnotations;

namespace PocketFence_Simple.Models
{
    public class AIThreatAssessment
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Url { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public double ThreatScore { get; set; }
        public ThreatLevel ThreatLevel { get; set; }
        public List<ThreatIndicator> Indicators { get; set; } = new();
        public string? ContentSample { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ThreatIndicator
    {
        public string Type { get; set; } = "";
        public double Score { get; set; }
        public string Description { get; set; } = "";
        public List<string> Details { get; set; } = new();
    }

    public class AIThreatAlert
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string Url { get; set; } = "";
        public double ThreatScore { get; set; }
        public ThreatLevel ThreatLevel { get; set; }
        public string Description { get; set; } = "";
        public string AutonomousAction { get; set; } = "";
        public string ActionJustification { get; set; } = "";
        public bool WasBlocked { get; set; }
        public bool ParentalReviewRequired { get; set; }
        public bool ParentalOverride { get; set; }
        public string? ParentalNote { get; set; }
    }

    public class ThreatPattern
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = ""; // Domain, Content, Behavioral, etc.
        public string Pattern { get; set; } = "";
        public double ThreatWeight { get; set; }
        public int ConfidenceLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public int MatchCount { get; set; }
        public int FalsePositiveCount { get; set; }
    }

    public class ThreatResponse
    {
        public string AssessmentId { get; set; } = "";
        public string Action { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Justification { get; set; } = "";
    }

    public class AIThreatResponse
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string ThreatId { get; set; } = "";
        public string Action { get; set; } = "";
        public string Justification { get; set; } = "";
        public bool RequiresParentalReview { get; set; }
        public bool CanBeOverridden { get; set; }
        public string? AutomationLevel { get; set; }
    }

    public class AIInsight
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = ""; // Pattern, Trend, Recommendation, etc.
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public double Confidence { get; set; } // 0.0 to 1.0
        public string ActionRecommended { get; set; } = "";
        public Dictionary<string, object> Data { get; set; } = new();
        public InsightSeverity Severity { get; set; } = InsightSeverity.Info;
        public bool IsActionable { get; set; } = true;
        public bool HasBeenActedUpon { get; set; } = false;
    }

    public class AIParentalGuidance
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Category { get; set; } = ""; // Navigation, Recommendation, Alert, etc.
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public List<string> Steps { get; set; } = new();
        public string? HelpLink { get; set; }
        public GuidancePriority Priority { get; set; } = GuidancePriority.Normal;
        public bool RequiresAttention { get; set; }
    }

    public class AIUsagePattern
    {
        public string DeviceId { get; set; } = "";
        public string PatternType { get; set; } = ""; // Time, Content, Behavioral
        public Dictionary<string, double> Metrics { get; set; } = new();
        public DateTime AnalyzedAt { get; set; }
        public string Description { get; set; } = "";
        public double Confidence { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    public class AISystemHealth
    {
        public DateTime Timestamp { get; set; }
        public bool IsOperational { get; set; } = true;
        public double SystemLoad { get; set; }
        public List<string> ActiveModules { get; set; } = new();
        public List<SystemError> Errors { get; set; } = new();
        public Dictionary<string, double> Performance { get; set; } = new();
        public int ThreatsProcessed { get; set; }
        public int ActionsAutomated { get; set; }
        public double AccuracyScore { get; set; }
    }

    public class SystemError
    {
        public DateTime Timestamp { get; set; }
        public string Module { get; set; } = "";
        public string ErrorType { get; set; } = "";
        public string Message { get; set; } = "";
        public bool WasRecovered { get; set; }
        public string? RecoveryAction { get; set; }
        public ErrorSeverity Severity { get; set; }
    }

    public class AIUpdateInfo
    {
        public string Version { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public string Description { get; set; } = "";
        public List<string> Features { get; set; } = new();
        public List<string> SecurityFixes { get; set; } = new();
        public UpdatePriority Priority { get; set; }
        public bool IsSecurityUpdate { get; set; }
        public string DownloadUrl { get; set; } = "";
        public long FileSize { get; set; }
        public string ChecksumMD5 { get; set; } = "";
    }

    public class AINotification
    {
        public string Id { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public Dictionary<string, object> Data { get; set; } = new();
        public NotificationPriority Priority { get; set; }
        public bool RequiresAction { get; set; }
        public bool HasBeenRead { get; set; }
        public List<NotificationAction> Actions { get; set; } = new();
    }

    public class NotificationAction
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string Action { get; set; } = "";
        public bool IsPrimary { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public enum ThreatLevel
    {
        Minimal = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
        Unknown = 5
    }

    public enum InsightSeverity
    {
        Info = 0,
        Warning = 1,
        Alert = 2,
        Critical = 3
    }

    public enum GuidancePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    public enum ErrorSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3,
        Fatal = 4
    }

    public enum UpdatePriority
    {
        Optional = 0,
        Recommended = 1,
        Important = 2,
        Critical = 3,
        Security = 4
    }

    public enum NotificationType
    {
        ThreatDetected = 0,
        ActionTaken = 1,
        SystemUpdate = 2,
        InsightDiscovered = 3,
        ParentalGuidance = 4,
        SystemHealth = 5,
        SecurityAlert = 6
    }

    public enum NotificationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3,
        Critical = 4
    }
}