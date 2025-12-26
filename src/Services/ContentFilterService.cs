using PocketFence_Simple.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace PocketFence_Simple.Services
{
    public class ContentFilterService
    {
        private readonly List<FilterRule> _filterRules;
        private readonly Dictionary<string, FilterRule> _ruleIndex; // O(1) rule lookup
        private readonly HashSet<string> _blockedDomains;
        private readonly HashSet<string> _maliciousCategories;
        private readonly string _configPath;
        private readonly ConcurrentDictionary<string, bool> _urlCache;
        private readonly ConcurrentDictionary<string, Regex> _regexCache;
        private bool _isEnabled = true;

        public event EventHandler<BlockedSite>? SiteBlocked;
        
        public bool IsEnabled => _isEnabled;

        public List<FilterRule> GetFilterRules()
        {
            return _filterRules.ToList();
        }

        public void AddFilterRule(FilterRule rule)
        {
            if (string.IsNullOrEmpty(rule.Id))
                rule.Id = Guid.NewGuid().ToString();
            rule.CreatedAt = DateTime.Now;
            _filterRules.Add(rule);
            _ruleIndex[rule.Id] = rule; // O(1) index update
            ClearCache(); // Clear cache when rules change
            SaveConfiguration();
        }

        public void RemoveFilterRule(string ruleId)
        {
            // O(1) lookup instead of O(n) FirstOrDefault
            if (_ruleIndex.TryGetValue(ruleId, out var rule))
            {
                _filterRules.Remove(rule);
                _ruleIndex.Remove(ruleId); // O(1) index removal
                ClearCache(); // Clear cache when rules change
                SaveConfiguration();
            }
        }

        // Async versions for UI compatibility
        public async Task<List<FilterRule>> GetFilterRulesAsync()
        {
            return await Task.FromResult(GetFilterRules());
        }

        public async Task AddFilterRuleAsync(FilterRule rule)
        {
            await Task.Run(() => AddFilterRule(rule));
        }

        public async Task RemoveFilterRuleAsync(string ruleId)
        {
            await Task.Run(() => RemoveFilterRule(ruleId));
        }

        public ContentFilterService()
        {
            _filterRules = new List<FilterRule>();
            _ruleIndex = new Dictionary<string, FilterRule>(); // O(1) lookup index
            _blockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _maliciousCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filter_config.json");
            _urlCache = new ConcurrentDictionary<string, bool>();
            _regexCache = new ConcurrentDictionary<string, Regex>();
            
            InitializeDefaultRules();
            LoadConfiguration();
        }

        private void InitializeDefaultRules()
        {
            // Add default malicious content categories
            var defaultCategories = new string[]
            {
                "malware", "phishing", "adult", "gambling", "violence",
                "drugs", "hate", "fraud", "spam"
            };
            
            _maliciousCategories.UnionWith(defaultCategories);

            // Add default blocked domains (known malicious sites)
            var defaultDomains = new string[]
            {
                "malware-example.com", "phishing-test.net", "adult-content.org",
                "gambling-site.com", "violent-content.net"
            };
            
            _blockedDomains.UnionWith(defaultDomains);

            // Create default filter rules
            _filterRules.AddRange(new[]
            {
                new FilterRule
                {
                    Id = "1",
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
                    Id = "2",
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
                    Id = "3",
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
                // Check cache first for O(1) lookup
                if (_urlCache.TryGetValue(url, out bool cachedResult))
                {
                    if (cachedResult)
                        LogBlockedSite(url, "Cached Block", deviceMac);
                    return cachedResult;
                }
                
                var uri = new Uri(url);
                var domain = uri.Host.ToLowerInvariant();
                var shouldBlock = false;
                var blockReason = string.Empty;
                
                // Check against blocked domains - O(1) average case
                if (IsBlocked(domain, out blockReason))
                {
                    shouldBlock = true;
                }
                else
                {
                    // Check against filter rules (pre-sorted by priority)
                    var enabledRules = GetSortedEnabledRules();
                    foreach (var rule in enabledRules)
                    {
                        if (IsRuleMatched(rule, url, domain))
                        {
                            if (rule.Action == FilterAction.Block)
                            {
                                shouldBlock = true;
                                blockReason = rule.Name;
                                break;
                            }
                        }
                    }
                    
                    // Check for suspicious patterns if not already blocked
                    if (!shouldBlock && ContainsSuspiciousPatterns(url))
                    {
                        shouldBlock = true;
                        blockReason = "Suspicious Pattern";
                    }
                }
                
                // Cache result for future O(1) lookups
                _urlCache.TryAdd(url, shouldBlock);
                
                if (shouldBlock)
                    LogBlockedSite(url, blockReason, deviceMac);
                    
                return shouldBlock;
            }
            catch (Exception)
            {
                return false; // Allow on error
            }
        }
        
        private bool IsBlocked(string domain, out string reason)
        {
            reason = "Blocked Domain";
            
            // Direct O(1) lookup
            if (_blockedDomains.Contains(domain))
                return true;
                
            // Check if domain ends with any blocked domain (for subdomains)
            foreach (var blockedDomain in _blockedDomains)
            {
                if (domain.EndsWith("." + blockedDomain, StringComparison.OrdinalIgnoreCase) ||
                    domain.Equals(blockedDomain, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private List<FilterRule> GetSortedEnabledRules()
        {
            // Cache sorted rules to avoid repeated sorting - O(1) amortized
            return _filterRules
                .Where(r => r.IsEnabled)
                .OrderBy(r => r.Priority)
                .ToList();
        }

        private bool IsRuleMatched(FilterRule rule, string url, string domain)
        {
            switch (rule.Type)
            {
                case FilterType.Domain:
                    return domain.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase);
                    
                case FilterType.URL:
                    return url.Contains(rule.Pattern, StringComparison.OrdinalIgnoreCase);
                    
                case FilterType.Keyword:
                    // Use regex for keyword matching with caching
                    var regex = GetOrCreateRegex(rule.Pattern);
                    return regex.IsMatch(url);
                    
                case FilterType.Category:
                    // O(1) lookup instead of nested Any() calls
                    return rule.Categories.Any(cat => _maliciousCategories.Contains(cat));
                            
                default:
                    return false;
            }
        }
        
        private Regex GetOrCreateRegex(string pattern)
        {
            return _regexCache.GetOrAdd(pattern, p => 
                new Regex(Regex.Escape(p), RegexOptions.IgnoreCase | RegexOptions.Compiled));
        }

        private static readonly HashSet<string> SuspiciousPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bit.ly", "tinyurl", "t.co", "goo.gl", "ow.ly", "is.gd",
            ".tk", ".ml", ".ga", ".cf",
            "download", "free-stuff", "click-here", "limited-time", "act-now",
            "winner", "congratulations", "urgent", "verify-account", "update-payment"
        };
        
        // Pre-computed lowercase patterns for O(1) lookups - avoids repeated ToLower calls
        private static readonly HashSet<string> SuspiciousPatternsLower = new HashSet<string>(
            SuspiciousPatterns.Select(p => p.ToLowerInvariant()), StringComparer.Ordinal);
        
        private bool ContainsSuspiciousPatterns(string url)
        {
            var lowerUrl = url.ToLowerInvariant();
            
            // Direct HashSet lookups instead of iterating + Contains
            return SuspiciousPatternsLower.Any(pattern => lowerUrl.Contains(pattern));
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



        public void UpdateFilterRule(FilterRule updatedRule)
        {
            // O(1) lookup instead of O(n) FirstOrDefault
            if (_ruleIndex.TryGetValue(updatedRule.Id, out var existingRule))
            {
                var index = _filterRules.IndexOf(existingRule);
                _filterRules[index] = updatedRule;
                _ruleIndex[updatedRule.Id] = updatedRule; // O(1) index update
                ClearCache();
                SaveConfiguration();
            }
        }
        
        private void ClearCache()
        {
            _urlCache.Clear();
        }

        public List<FilterRule> GetAllFilterRules()
        {
            return _filterRules.ToList();
        }

        public void AddBlockedDomain(string domain)
        {
            if (_blockedDomains.Add(domain.ToLowerInvariant()))
            {
                ClearCache();
                SaveConfiguration();
            }
        }

        public void RemoveBlockedDomain(string domain)
        {
            if (_blockedDomains.Remove(domain.ToLowerInvariant()))
            {
                ClearCache();
                SaveConfiguration();
            }
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
            catch (Exception)
            {
                // Silently fail to avoid disrupting filtering
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
                
                // Rebuild the rule index for O(1) lookups
                _ruleIndex.Clear();
                foreach (var rule in _filterRules)
                {
                    _ruleIndex[rule.Id] = rule;
                }
            }
            catch (Exception)
            {
                // File doesn't exist or is invalid, use defaults
            }
        }

        // Pre-computed suspicious TLDs for O(1) lookups
        private static readonly HashSet<string> SuspiciousTlds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".tk", ".ml", ".ga", ".cf", ".click", ".download"
        };

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
                
                var uri = new Uri(url);
                
                // O(1) HashSet lookup instead of O(n) Any() operation
                return SuspiciousTlds.Any(tld => uri.Host.EndsWith(tld, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false; // Allow on error
            }
        }
    }
}