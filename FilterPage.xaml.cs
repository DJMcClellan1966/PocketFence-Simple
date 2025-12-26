using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PocketFence_Simple.Services;
using PocketFence_Simple.Models;

namespace PocketFence_Simple
{
    public partial class FilterPage : ContentPage, INotifyPropertyChanged
    {
        private readonly ContentFilterService _filterService;
        private ObservableCollection<FilterRule> _filterRules = new();
        private string _newRulePattern = string.Empty;
        private string _newRuleType = "Domain";
        private string _newRuleDescription = string.Empty;
        private bool _isFilteringEnabled = true;

        public FilterPage(ContentFilterService filterService)
        {
            InitializeComponent();
            _filterService = filterService;
            BindingContext = this;
            
            // Initialize commands first to avoid nullable warnings
            AddRuleCommand = new Command(async () => await AddRuleAsync());
            DeleteRuleCommand = new Command<object>(async (parameter) => 
            {
                string ruleId = parameter switch
                {
                    FilterRule rule => rule.Id,
                    string id => id,
                    _ => string.Empty
                };
                await DeleteRuleAsync(ruleId);
            });
            BlockSocialMediaCommand = new Command(async () => await AddSocialMediaRulesAsync());
            BlockGamingCommand = new Command(async () => await AddGamingRulesAsync());
            BlockAdultContentCommand = new Command(async () => await AddAdultContentRulesAsync());
            EnableSafeSearchCommand = new Command(async () => await EnableSafeSearchAsync());
            ImportRulesCommand = new Command(async () => await ImportRulesAsync());
            ExportRulesCommand = new Command(async () => await ExportRulesAsync());
            
            _ = LoadRulesAsync();
        }

        public ObservableCollection<FilterRule> FilterRules
        {
            get => _filterRules;
            set
            {
                _filterRules = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalRules));
                OnPropertyChanged(nameof(ActiveRules));
            }
        }

        public string NewRulePattern
        {
            get => _newRulePattern;
            set
            {
                _newRulePattern = value;
                OnPropertyChanged();
            }
        }

        public string NewRuleType
        {
            get => _newRuleType;
            set
            {
                _newRuleType = value;
                OnPropertyChanged();
            }
        }

        public string NewRuleDescription
        {
            get => _newRuleDescription;
            set
            {
                _newRuleDescription = value;
                OnPropertyChanged();
            }
        }

        public bool IsFilteringEnabled
        {
            get => _isFilteringEnabled;
            set
            {
                _isFilteringEnabled = value;
                OnPropertyChanged();
            }
        }

        public int TotalRules => FilterRules.Count;
        public int ActiveRules => FilterRules.Count(r => r.IsEnabled);

        public ICommand AddRuleCommand { get; private set; }
        public ICommand DeleteRuleCommand { get; private set; }
        public ICommand BlockSocialMediaCommand { get; private set; }
        public ICommand BlockGamingCommand { get; private set; }
        public ICommand BlockAdultContentCommand { get; private set; }
        public ICommand EnableSafeSearchCommand { get; private set; }
        public ICommand ImportRulesCommand { get; private set; }
        public ICommand ExportRulesCommand { get; private set; }



        private async Task LoadRulesAsync()
        {
            try
            {
                var rules = await _filterService.GetFilterRulesAsync();
                FilterRules = new ObservableCollection<FilterRule>(rules);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load filter rules: {ex.Message}", "OK");
            }
        }

        private async Task AddRuleAsync()
        {
            if (string.IsNullOrWhiteSpace(NewRulePattern))
            {
                await DisplayAlert("Error", "Please enter a pattern for the rule", "OK");
                return;
            }

            try
            {
                var rule = new FilterRule
                {
                    Id = Guid.NewGuid().ToString(),
                    Pattern = NewRulePattern.Trim(),
                    RuleType = NewRuleType,
                    Description = NewRuleDescription,
                    IsEnabled = true,
                    CreatedDate = DateTime.Now,
                    Type = FilterType.Domain
                };

                await _filterService.AddFilterRuleAsync(rule);
                FilterRules.Add(rule);
                
                // Clear form
                NewRulePattern = string.Empty;
                NewRuleDescription = string.Empty;
                
                await DisplayAlert("Success", "Filter rule added successfully", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add filter rule: {ex.Message}", "OK");
            }
        }

        private async Task DeleteRuleAsync(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId)) return;
            
            var rule = FilterRules.FirstOrDefault(r => r.Id == ruleId);
            if (rule == null) return;

            var confirm = await DisplayAlert("Confirm", $"Delete rule '{rule.Pattern}'?", "Yes", "No");
            if (!confirm) return;

            try
            {
                await _filterService.RemoveFilterRuleAsync(ruleId);
                FilterRules.Remove(rule);
                await DisplayAlert("Success", "Filter rule deleted successfully", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete filter rule: {ex.Message}", "OK");
            }
        }

        private async Task AddSocialMediaRulesAsync()
        {
            var socialMediaSites = new[]
            {
                "facebook.com", "instagram.com", "twitter.com", "snapchat.com",
                "tiktok.com", "youtube.com", "pinterest.com", "linkedin.com"
            };

            foreach (var site in socialMediaSites)
            {
                var rule = new FilterRule
                {
                    Id = Guid.NewGuid().ToString(),
                    Pattern = site,
                    RuleType = "Domain",
                    Description = "Social Media Block",
                    IsEnabled = true,
                    CreatedDate = DateTime.Now,
                    Type = FilterType.Domain
                };

                if (!FilterRules.Any(r => r.Pattern.Equals(site, StringComparison.OrdinalIgnoreCase)))
                {
                    await _filterService.AddFilterRuleAsync(rule);
                    FilterRules.Add(rule);
                }
            }

            await DisplayAlert("Success", "Social media blocking rules added", "OK");
        }

        private async Task AddGamingRulesAsync()
        {
            var gamingSites = new[]
            {
                "steam.com", "epicgames.com", "battle.net", "minecraft.net",
                "roblox.com", "fortnite.com", "league-of-legends.com"
            };

            foreach (var site in gamingSites)
            {
                var rule = new FilterRule
                {
                    Id = Guid.NewGuid().ToString(),
                    Pattern = site,
                    RuleType = "Domain",
                    Description = "Gaming Block",
                    IsEnabled = true,
                    CreatedDate = DateTime.Now,
                    Type = FilterType.Domain
                };

                if (!FilterRules.Any(r => r.Pattern.Equals(site, StringComparison.OrdinalIgnoreCase)))
                {
                    await _filterService.AddFilterRuleAsync(rule);
                    FilterRules.Add(rule);
                }
            }

            await DisplayAlert("Success", "Gaming blocking rules added", "OK");
        }

        private async Task AddAdultContentRulesAsync()
        {
            // Add common adult content patterns (keeping it general for safety)
            var adultContentPatterns = new[]
            {
                "*.xxx", "*.sex", "*.adult", "*.porn"
            };

            foreach (var pattern in adultContentPatterns)
            {
                var rule = new FilterRule
                {
                    Id = Guid.NewGuid().ToString(),
                    Pattern = pattern,
                    RuleType = "Domain",
                    Description = "Adult Content Block",
                    IsEnabled = true,
                    CreatedDate = DateTime.Now,
                    Type = FilterType.Domain
                };

                if (!FilterRules.Any(r => r.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase)))
                {
                    await _filterService.AddFilterRuleAsync(rule);
                    FilterRules.Add(rule);
                }
            }

            await DisplayAlert("Success", "Adult content blocking rules added", "OK");
        }

        private async Task EnableSafeSearchAsync()
        {
            // Add safe search enforcement rules
            var safeSearchRules = new[]
            {
                "google.com/search?safe=off",
                "bing.com/search?adlt=off",
                "duckduckgo.com/?safe-search=-1"
            };

            foreach (var pattern in safeSearchRules)
            {
                var rule = new FilterRule
                {
                    Id = Guid.NewGuid().ToString(),
                    Pattern = pattern,
                    RuleType = "URL",
                    Description = "Safe Search Enforcement",
                    IsEnabled = true,
                    CreatedDate = DateTime.Now,
                    Type = FilterType.URL
                };

                if (!FilterRules.Any(r => r.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase)))
                {
                    await _filterService.AddFilterRuleAsync(rule);
                    FilterRules.Add(rule);
                }
            }

            await DisplayAlert("Success", "Safe search enforcement rules added", "OK");
        }

        private async Task ImportRulesAsync()
        {
            try
            {
                // Placeholder for file picker implementation
                await DisplayAlert("Info", "Import feature not yet implemented", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to import rules: {ex.Message}", "OK");
            }
        }

        private async Task ExportRulesAsync()
        {
            try
            {
                // Placeholder for file export implementation
                await DisplayAlert("Info", "Export feature not yet implemented", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to export rules: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteRuleClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string ruleId)
            {
                await DeleteRuleAsync(ruleId);
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}