using PocketFence_Simple.Models;

namespace PocketFence_Simple.Services.AI
{
    public class AIParentalAssistantService
    {
        private readonly ILogger<AIParentalAssistantService> _logger;
        private readonly Dictionary<string, List<ParentalGuidance>> _guidanceHistory;

        public AIParentalAssistantService(ILogger<AIParentalAssistantService> logger)
        {
            _logger = logger;
            _guidanceHistory = new Dictionary<string, List<ParentalGuidance>>();
        }

        public ParentalGuidance GetNavigationHelp(string feature)
        {
            var guidance = new ParentalGuidance
            {
                Type = "Navigation Help",
                Content = GetFeatureHelp(feature),
                Timestamp = DateTime.UtcNow
            };

            StoreGuidance("navigation", guidance);
            return guidance;
        }

        public ParentalGuidance GetUsageInsights(List<ConnectedDevice> devices)
        {
            var totalDevices = devices.Count;
            var blockedCount = devices.Count(d => d.IsBlocked);
            
            var insights = $"You have {totalDevices} connected devices. {blockedCount} devices have restrictions active. ";
            
            if (blockedCount > 0)
            {
                insights += "Consider reviewing device usage patterns and adjusting filters as needed.";
            }
            else
            {
                insights += "All devices have free access. Monitor activity regularly.";
            }

            var guidance = new ParentalGuidance
            {
                Type = "Usage Insights",
                Content = insights,
                Timestamp = DateTime.UtcNow
            };

            StoreGuidance("insights", guidance);
            return guidance;
        }

        public List<string> GetConversationStarters(string childAge = "teen")
        {
            var starters = new List<string>();

            if (childAge.ToLower().Contains("teen"))
            {
                starters.AddRange(new[]
                {
                    "How was your online experience today?",
                    "What's your favorite app or website right now?",
                    "Have you encountered anything online that made you uncomfortable?",
                    "Would you like to talk about any online interactions you had?"
                });
            }
            else
            {
                starters.AddRange(new[]
                {
                    "What games did you play online today?",
                    "Did you learn anything new on the computer?",
                    "Who did you talk to online?",
                    "What's your favorite website for kids?"
                });
            }

            return starters;
        }

        public List<ParentalGuidance> GetGuidanceHistory(string category, int limit = 10)
        {
            if (_guidanceHistory.TryGetValue(category, out var history))
            {
                return history.TakeLast(limit).ToList();
            }
            
            return new List<ParentalGuidance>();
        }

        private string GetFeatureHelp(string feature)
        {
            return feature.ToLower() switch
            {
                "dashboard" => "The dashboard shows connected devices and their status. Click on any device to view details and adjust settings.",
                "filters" => "Content filters block inappropriate content. You can add custom rules or use predefined categories.",
                "devices" => "Manage connected devices by clicking on them. You can set time limits, content restrictions, and view activity.",
                "reports" => "View detailed reports of internet usage, blocked content, and device activity patterns.",
                "settings" => "Adjust global settings, notification preferences, and security options.",
                _ => "For help with this feature, please refer to the user guide or contact support."
            };
        }

        private void StoreGuidance(string category, ParentalGuidance guidance)
        {
            if (!_guidanceHistory.ContainsKey(category))
            {
                _guidanceHistory[category] = new List<ParentalGuidance>();
            }

            _guidanceHistory[category].Add(guidance);
            
            // Keep only last 50 items per category
            if (_guidanceHistory[category].Count > 50)
            {
                _guidanceHistory[category] = _guidanceHistory[category].TakeLast(50).ToList();
            }
        }
    }

    public class ParentalGuidance
    {
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}