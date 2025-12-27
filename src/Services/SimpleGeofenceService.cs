using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocketFence_Simple.Services
{
    // Simplified geofence service that handles location-aware content filtering
    public class SimpleGeofenceService
    {
        private readonly ILogger<SimpleGeofenceService> _logger;
        private readonly Dictionary<string, DeviceLocation> _deviceLocations;
        private readonly List<GeofenceZone> _geofenceZones;
        private readonly Random _random;

        public SimpleGeofenceService(ILogger<SimpleGeofenceService> logger)
        {
            _logger = logger;
            _deviceLocations = new Dictionary<string, DeviceLocation>();
            _geofenceZones = InitializeGeofenceZones();
            _random = new Random();
        }

        public async Task<bool> UpdateDeviceLocationAsync(string deviceId, double latitude, double longitude)
        {
            await Task.Delay(10); // Simulate processing
            
            _deviceLocations[deviceId] = new DeviceLocation
            {
                DeviceId = deviceId,
                Latitude = latitude,
                Longitude = longitude,
                LastUpdated = DateTime.Now
            };

            _logger.LogInformation($"Updated location for {deviceId}: {latitude:F4}, {longitude:F4}");
            return true;
        }

        public async Task<GeofenceStatus> GetDeviceGeofenceStatusAsync(string deviceId)
        {
            await Task.Delay(10);
            
            if (!_deviceLocations.ContainsKey(deviceId))
            {
                // Simulate location for demo
                await UpdateDeviceLocationAsync(deviceId, 
                    40.7589 + (_random.NextDouble() - 0.5) * 0.01, // NYC area
                    -73.9851 + (_random.NextDouble() - 0.5) * 0.01);
            }

            var location = _deviceLocations[deviceId];
            var currentZone = DetermineCurrentZone(location);
            
            return new GeofenceStatus
            {
                DeviceId = deviceId,
                CurrentZone = currentZone?.Name ?? "Outside defined zones",
                IsInRestrictedArea = currentZone?.IsRestricted ?? false,
                FilteringLevel = DetermineFilteringLevel(currentZone),
                AllowedCategories = GetAllowedCategories(currentZone),
                LastLocationUpdate = location.LastUpdated
            };
        }

        public async Task<bool> ShouldBlockContentAsync(string deviceId, string content, string url = null)
        {
            var geofenceStatus = await GetDeviceGeofenceStatusAsync(deviceId);
            
            if (geofenceStatus.IsInRestrictedArea && geofenceStatus.FilteringLevel == "Strict")
            {
                // In strict zones, block most content except educational
                return !IsEducationalContent(content, url);
            }

            if (geofenceStatus.FilteringLevel == "Moderate")
            {
                // In moderate zones, block potentially harmful content
                return IsHarmfulContent(content, url);
            }

            // In lenient zones, only block clearly inappropriate content
            return IsInappropriateContent(content, url);
        }

        private List<GeofenceZone> InitializeGeofenceZones()
        {
            return new List<GeofenceZone>
            {
                new GeofenceZone
                {
                    Name = "Home",
                    CenterLatitude = 40.7589,
                    CenterLongitude = -73.9851,
                    Radius = 100, // 100 meters
                    IsRestricted = false,
                    FilteringLevel = "Moderate"
                },
                new GeofenceZone
                {
                    Name = "School",
                    CenterLatitude = 40.7614,
                    CenterLongitude = -73.9776,
                    Radius = 200,
                    IsRestricted = true,
                    FilteringLevel = "Strict"
                },
                new GeofenceZone
                {
                    Name = "Library",
                    CenterLatitude = 40.7505,
                    CenterLongitude = -73.9934,
                    Radius = 150,
                    IsRestricted = false,
                    FilteringLevel = "Moderate"
                }
            };
        }

        private GeofenceZone DetermineCurrentZone(DeviceLocation location)
        {
            foreach (var zone in _geofenceZones)
            {
                var distance = CalculateDistance(
                    location.Latitude, location.Longitude,
                    zone.CenterLatitude, zone.CenterLongitude);
                
                if (distance <= zone.Radius)
                {
                    return zone;
                }
            }
            return null;
        }

        private string DetermineFilteringLevel(GeofenceZone zone)
        {
            if (zone == null) return "Lenient";
            return zone.FilteringLevel;
        }

        private List<string> GetAllowedCategories(GeofenceZone zone)
        {
            if (zone?.IsRestricted == true)
            {
                return new List<string> { "Educational", "Reference", "News" };
            }

            if (zone != null)
            {
                return new List<string> { "Educational", "Entertainment", "Social", "News", "Games" };
            }

            return new List<string> { "All categories allowed" };
        }

        private bool IsEducationalContent(string content, string url)
        {
            var educationalKeywords = new[] { "education", "learning", "school", "study", "homework", "research" };
            var contentLower = content.ToLower();
            
            if (!string.IsNullOrEmpty(url))
            {
                var urlLower = url.ToLower();
                if (educationalKeywords.Any(keyword => urlLower.Contains(keyword)))
                    return true;
            }

            return educationalKeywords.Any(keyword => contentLower.Contains(keyword));
        }

        private bool IsHarmfulContent(string content, string url)
        {
            var harmfulKeywords = new[] { "violence", "explicit", "drugs", "weapons", "hate" };
            var contentLower = content.ToLower();
            
            if (!string.IsNullOrEmpty(url))
            {
                var urlLower = url.ToLower();
                if (harmfulKeywords.Any(keyword => urlLower.Contains(keyword)))
                    return true;
            }

            return harmfulKeywords.Any(keyword => contentLower.Contains(keyword));
        }

        private bool IsInappropriateContent(string content, string url)
        {
            var inappropriateKeywords = new[] { "explicit", "adult", "inappropriate", "18+" };
            var contentLower = content.ToLower();
            
            if (!string.IsNullOrEmpty(url))
            {
                var urlLower = url.ToLower();
                if (inappropriateKeywords.Any(keyword => urlLower.Contains(keyword)))
                    return true;
            }

            return inappropriateKeywords.Any(keyword => contentLower.Contains(keyword));
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Simple distance calculation using Haversine formula (simplified)
            var R = 6371e3; // Earth's radius in meters
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // Distance in meters
        }
    }

    // Supporting classes for the geofence service
    public class DeviceLocation
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class GeofenceZone
    {
        public string Name { get; set; } = string.Empty;
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double Radius { get; set; } // in meters
        public bool IsRestricted { get; set; }
        public string FilteringLevel { get; set; } = "Moderate"; // Strict, Moderate, Lenient
    }

    public class GeofenceStatus
    {
        public string DeviceId { get; set; } = string.Empty;
        public string CurrentZone { get; set; } = string.Empty;
        public bool IsInRestrictedArea { get; set; }
        public string FilteringLevel { get; set; } = string.Empty;
        public List<string> AllowedCategories { get; set; } = new();
        public DateTime LastLocationUpdate { get; set; }
    }
}