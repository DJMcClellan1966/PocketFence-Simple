using PocketFence.Models;
using System.Text.Json;

namespace PocketFence.Services
{
    public class ContentFilterService
    {
        private readonly List<FilterRule> _filterRules;
        private readonly List<string> _blockedDomains;
        private readonly List<string> _maliciousCategories;
        private readonly string _configPath;

        public event EventHandler<BlockedSite>? SiteBlocked;

        public ContentFilterService()
        {
            _filterRules = new List<FilterRule>();
            _blockedDomains = new List<string>();
            _maliciousCategories = new List<string>();
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filter_config.json");
            
            InitializeDefaultRules();
            LoadConfiguration();
        }

        private void InitializeDefaultRules()
        {
            // Add default malicious content categories
            _maliciousCategories.AddRange(new[]
            {
                "malware",
                "phishing", 
                "adult",
                "gambling",
                "violence",
                "drugs",
                "hate",
                "fraud",
                "spam"
            });

            // Add default blocked domains (known malicious sites)
            _blockedDomains.AddRange(new[]
            {
                "malware-example.com",
                "phishing-test.net",
                "adult-content.org",
                "gambling-site.com",
                "violent-content.net"
            });

            // Create default filter rules
            _filterRules.AddRange(new[]
            {
                new FilterRule
                {
                    Id = 1,
                    Name = "Block Adult Content",
                    Description = "Blocks access to adult websites",
                    Type = FilterType.Category,
                    Pattern = "adult",
                    Action = FilterAction.Block,
                    IsEnabled = true,
                    CreatedAt = DateTime.Now,
                    Categories = new List<string> { "adult" },
                    Priority = 1
                },
                new FilterRule
                {
                    Id = 2,
                    Name = "Block Malware Sites",
                    Description = "Blocks known malware and phishing sites",
                    Type = FilterType.Category,
                    Pattern = "malware|phishing",
                    Action = FilterAction.Block,
                    IsEnabled = true,
                    CreatedAt = DateTime.Now,
                    Categories = new List<string> { "malware", "phishing" },
                    Priority = 1
                },
                new FilterRule
                {
                    Id = 3,
                    Name = "Block Gambling",
                    Description = "Blocks gambling websites",
                    Type = FilterType.Category,
                    Pattern = "gambling",
                    Action = FilterAction.Block,
                    IsEnabled = true,
                    CreatedAt = DateTime.Now,
                    Categories = new List<string> { "gambling" },
                    Priority = 2
                }
            });
        }

        public bool ShouldBlockRequest(string url, string deviceMac)
        {
            try
            {
                var uri = new Uri(url);
                var domain = uri.Host.ToLower();
                
                // Check against blocked domains
                if (_blockedDomains.Any(blocked => domain.Contains(blocked.ToLower())))
                {
                    LogBlockedSite(url, "Blocked Domain", deviceMac);
                    return true;
                }
                
                // Check against filter rules
                foreach (var rule in _filterRules.Where(r => r.IsEnabled).OrderBy(r => r.Priority))
                {
                    if (IsRuleMatched(rule, url, domain))
                    {
                        if (rule.Action == FilterAction.Block)
                        {
                            LogBlockedSite(url, rule.Name, deviceMac);
                            return true;
                        }
                    }
                }
                
                // Check for suspicious patterns
                if (ContainsSuspiciousPatterns(url))
                {
                    LogBlockedSite(url, "Suspicious Pattern", deviceMac);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking URL {url}: {ex.Message}");
                return false; // Allow on error
            }
        }

        private bool IsRuleMatched(FilterRule rule, string url, string domain)
        {
            switch (rule.Type)
            {
                case FilterType.Domain:
                    return domain.Contains(rule.Pattern.ToLower());
                    
                case FilterType.URL:
                    return url.ToLower().Contains(rule.Pattern.ToLower());
                    
                case FilterType.Keyword:
                    return url.ToLower().Contains(rule.Pattern.ToLower());
                    
                case FilterType.Category:
                    return rule.Categories.Any(cat => 
                        _maliciousCategories.Any(malCat => 
                            malCat.Equals(cat, StringComparison.OrdinalIgnoreCase)));
                            
                default:
                    return false;
            }
        }

        private bool ContainsSuspiciousPatterns(string url)
        {
            var suspiciousPatterns = new[]
            {
                "bit.ly",
                "tinyurl",
                "t.co",
                "goo.gl",
                "ow.ly",
                "is.gd",
                ".tk",
                ".ml",
                ".ga",
                ".cf",
                "download",
                "free-stuff",
                "click-here",
                "limited-time",
                "act-now",
                "winner",
                "congratulations",
                "urgent",
                "verify-account",
                "update-payment"
            };
            
            return suspiciousPatterns.Any(pattern => 
                url.ToLower().Contains(pattern.ToLower()));
        }

        private void LogBlockedSite(string url, string reason, string deviceMac)
        {
            var blockedSite = new BlockedSite
            {
                Url = url,
                Category = reason,
                BlockedAt = DateTime.Now,
                DeviceMac = deviceMac,
                Reason = reason
            };
            
            SiteBlocked?.Invoke(this, blockedSite);
            
            // Log to file
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - BLOCKED: {url} for device {deviceMac} - Reason: {reason}";
            File.AppendAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_sites.log"), logEntry + Environment.NewLine);
        }

        public void AddFilterRule(FilterRule rule)
        {
            rule.Id = _filterRules.Any() ? _filterRules.Max(r => r.Id) + 1 : 1;
            rule.CreatedAt = DateTime.Now;
            _filterRules.Add(rule);
            SaveConfiguration();
        }

        public void RemoveFilterRule(int ruleId)
        {
            var rule = _filterRules.FirstOrDefault(r => r.Id == ruleId);
            if (rule != null)
            {
                _filterRules.Remove(rule);
                SaveConfiguration();
            }
        }

        public void UpdateFilterRule(FilterRule updatedRule)
        {
            var existingRule = _filterRules.FirstOrDefault(r => r.Id == updatedRule.Id);
            if (existingRule != null)
            {
                var index = _filterRules.IndexOf(existingRule);
                _filterRules[index] = updatedRule;
                SaveConfiguration();
            }
        }

        public List<FilterRule> GetAllFilterRules()
        {
            return _filterRules.ToList();
        }

        public void AddBlockedDomain(string domain)
        {
            if (!_blockedDomains.Contains(domain.ToLower()))
            {
                _blockedDomains.Add(domain.ToLower());
                SaveConfiguration();
            }
        }

        public void RemoveBlockedDomain(string domain)
        {
            _blockedDomains.Remove(domain.ToLower());
            SaveConfiguration();
        }

        public List<string> GetBlockedDomains()
        {
            return _blockedDomains.ToList();
        }

        private void SaveConfiguration()
        {
            try
            {
                var config = new
                {
                    FilterRules = _filterRules,
                    BlockedDomains = _blockedDomains,
                    MaliciousCategories = _maliciousCategories
                };
                
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<dynamic>(json);
                    
                    // Load saved configuration
                    // Note: In a real implementation, you'd properly deserialize the JSON
                    // This is simplified for the example
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        public async Task<bool> CheckUrlReputationAsync(string url)
        {
            try
            {
                // In a real implementation, you might integrate with services like:
                // - Google Safe Browsing API
                // - VirusTotal API
                // - OpenDNS/Cisco Umbrella
                // - Microsoft Defender SmartScreen
                
                // For now, simulate with basic checks
                await Task.Delay(100); // Simulate API call
                
                var suspiciousTlds = new[] { ".tk", ".ml", ".ga", ".cf", ".click", ".download" };
                var uri = new Uri(url);
                
                return suspiciousTlds.Any(tld => uri.Host.EndsWith(tld, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false; // Allow on error
            }
        }
    }
}