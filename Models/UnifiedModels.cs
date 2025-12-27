using System;
using System.Collections.Generic;

namespace PocketFence_Simple.Models
{
    // Simplified request models for the unified services
    public class InsightRequest
    {
        public string DeviceId { get; set; } = string.Empty;
    }

    public class LocationUpdateRequest 
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ContentAnalysisRequest
    {
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    // Response models matching the simplified services
    public class UnifiedInsightResponse
    {
        public string DeviceId { get; set; } = string.Empty;
        public double BehaviorScore { get; set; }
        public double WellnessScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}