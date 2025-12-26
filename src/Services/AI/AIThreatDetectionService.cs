using System.Text.Json;
using PocketFence_Simple.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace PocketFence_Simple.Services.AI
{
    public class AIThreatDetectionService
    {
        private readonly ConcurrentDictionary<string, double> _threatScores = new();
        private readonly ILogger<AIThreatDetectionService> _logger;
        private readonly Timer _cleanupTimer;
        
        public event EventHandler<AIThreatAlert>? ThreatDetected;

        public AIThreatDetectionService(ILogger<AIThreatDetectionService> logger)
        {
            _logger = logger;
            
            // Clean up old threat scores every hour
            _cleanupTimer = new Timer(CleanupOldScores, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public AIThreatAssessment AnalyzeThreat(string url, string content, ConnectedDevice device)
        {
            var threatScore = CalculateThreatScore(url, content);
            var threatLevel = GetThreatLevel(threatScore);
            
            var assessment = new AIThreatAssessment
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Url = url,
                DeviceId = device.Id,
                DeviceName = device.Name,
                ThreatLevel = threatLevel,
                ThreatScore = threatScore
            };

            // Store for learning
            _threatScores[url] = threatScore;

            // Trigger alert for significant threats
            if (threatLevel >= ThreatLevel.High)
            {
                TriggerThreatAlert(assessment, device);
            }

            _logger.LogDebug("Threat analysis for {Url}: {Level} ({Score:F2})", url, threatLevel, threatScore);
            return assessment;
        }

        private double CalculateThreatScore(string url, string content)
        {
            double score = 0.0;

            // Domain analysis
            if (IsKnownThreatDomain(url)) score += 0.8;
            if (IsSuspiciousDomain(url)) score += 0.4;

            // Content analysis
            if (HasMaliciousKeywords(content)) score += 0.6;
            if (HasSuspiciousPatterns(content)) score += 0.3;

            return Math.Min(score, 1.0);
        }

        private ThreatLevel GetThreatLevel(double score) => score switch
        {
            >= 0.8 => ThreatLevel.Critical,
            >= 0.6 => ThreatLevel.High,
            >= 0.3 => ThreatLevel.Medium,
            _ => ThreatLevel.Low
        };

        private bool IsKnownThreatDomain(string url) =>
            url.Contains("malware") || url.Contains("virus") || url.Contains("phishing");

        private bool IsSuspiciousDomain(string url) =>
            url.Contains("free-download") || url.Contains("click-here") || url.EndsWith(".tk");

        private bool HasMaliciousKeywords(string content) =>
            content.Contains("download virus") || content.Contains("hack") || content.Contains("malware");

        private bool HasSuspiciousPatterns(string content) =>
            content.Contains("urgent") || content.Contains("limited time") || content.Contains("click now");

        private void TriggerThreatAlert(AIThreatAssessment assessment, ConnectedDevice device)
        {
            var alert = new AIThreatAlert
            {
                Id = assessment.Id,
                Timestamp = assessment.Timestamp,
                DeviceId = device.Id,
                DeviceName = device.Name,
                Url = assessment.Url,
                ThreatLevel = assessment.ThreatLevel,
                AutonomousAction = assessment.ThreatLevel >= ThreatLevel.Medium ? "Blocked automatically" : "Monitoring",
                ParentalReviewRequired = assessment.ThreatLevel == ThreatLevel.Critical
            };

            ThreatDetected?.Invoke(this, alert);
        }

        private void CleanupOldScores(object? state)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var oldScores = _threatScores.Where(s => s.Key.Contains(cutoff.ToString("yyyy"))).ToList();
            
            foreach (var score in oldScores)
            {
                _threatScores.TryRemove(score.Key, out _);
            }

            if (oldScores.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old threat scores", oldScores.Count);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }
}