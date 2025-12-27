using Microsoft.AspNetCore.Mvc;
using PocketFence_Simple.Services;
using PocketFence_Simple.Models;

namespace PocketFence_Simple.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ContentFilterService _filterService;
    private readonly HotspotService _hotspotService;
    private readonly NetworkTrafficService _networkService;
    private readonly SmartBehaviorAnalysisService _behaviorService;
    private readonly SmartGeofenceService _geofenceService;
    private readonly DigitalWellnessService _wellnessService;
    private readonly QuantumContentAnalysisService _quantumService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ContentFilterService filterService,
        HotspotService hotspotService,
        NetworkTrafficService networkService,
        SmartBehaviorAnalysisService behaviorService,
        SmartGeofenceService geofenceService,
        DigitalWellnessService wellnessService,
        QuantumContentAnalysisService quantumService,
        ILogger<DashboardController> logger)
    {
        _filterService = filterService;
        _hotspotService = hotspotService;
        _networkService = networkService;
        _behaviorService = behaviorService;
        _geofenceService = geofenceService;
        _wellnessService = wellnessService;
        _quantumService = quantumService;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var status = new
            {
                hotspotEnabled = _hotspotService.IsActive,
                filterEnabled = _filterService.IsEnabled,
                deviceCount = (await _hotspotService.GetConnectedDevicesAsync()).Count,
                systemStatus = "online"
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard status");
            return StatusCode(500, new { error = "Failed to retrieve status" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var rules = await _filterService.GetFilterRulesAsync();
            var devices = await _hotspotService.GetConnectedDevicesAsync();
            
            var stats = new
            {
                blockedCount = GetBlockedRequestsCount(),
                dataUsage = GetDataUsage(),
                activeRules = rules.Count(r => r.IsEnabled),
                warningCount = GetWarningCount(),
                protectedDevices = devices.Count(d => d.IsFiltered),
                unprotectedDevices = devices.Count(d => !d.IsFiltered)
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard statistics");
            return StatusCode(500, new { error = "Failed to retrieve statistics" });
        }
    }

    [HttpGet("activity")]
    public IActionResult GetRecentActivity()
    {
        try
        {
            // In a real implementation, this would come from a logging service
            var activity = GetMockActivity();
            return Ok(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent activity");
            return StatusCode(500, new { error = "Failed to retrieve activity" });
        }
    }

    [HttpGet("wellness/{deviceId}")]
    public async Task<IActionResult> GetWellnessInsights(string deviceId)
    {
        try
        {
            var report = await _wellnessService.GenerateWellnessReportAsync(deviceId, TimeSpan.FromDays(7));
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get wellness insights for device {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to retrieve wellness data" });
        }
    }

    [HttpGet("behavior/{deviceId}")]
    public async Task<IActionResult> GetBehaviorInsights(string deviceId)
    {
        try
        {
            var insights = await _behaviorService.GetBehaviorInsightsAsync(deviceId);
            return Ok(insights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get behavior insights for device {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to retrieve behavior data" });
        }
    }

    [HttpPost("geofence")]
    public async Task<IActionResult> AddGeofence([FromBody] GeofenceRequest request)
    {
        try
        {
            var zone = new Services.GeofenceZone
            {
                Name = request.Zone.Name,
                Type = (Services.ZoneType)request.Zone.Type,
                RestrictiveLevel = (Services.RestrictionLevel)request.Zone.RestrictiveLevel,
                AllowedCategories = request.Zone.AllowedCategories
            };
            
            var zoneId = await _geofenceService.AddCustomGeofenceAsync(
                request.Latitude, 
                request.Longitude, 
                request.Radius, 
                zone
            );
            return Ok(new { zoneId, message = "Geofence created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create geofence");
            return StatusCode(500, new { error = "Failed to create geofence" });
        }
    }

    [HttpPost("analyze-content")]
    public async Task<IActionResult> AnalyzeContent([FromBody] Models.ContentAnalysisRequest request)
    {
        try
        {
            var context = new Services.ContentContext
            {
                Source = request.Context.Source,
                Platform = request.Context.Platform,
                TimeOfDay = request.Context.TimeOfDay,
                DayOfWeek = request.Context.DayOfWeek,
                UserAge = request.Context.UserAge,
                RecentActivity = request.Context.RecentActivity
            };
            
            var result = await _quantumService.AnalyzeContentAsync(
                request.Content, 
                request.DeviceId, 
                context
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze content");
            return StatusCode(500, new { error = "Failed to analyze content" });
        }
    }

    private int GetBlockedRequestsCount()
    {
        // In a real implementation, this would track blocked requests
        return Random.Shared.Next(0, 100);
    }

    private double GetDataUsage()
    {
        // In a real implementation, this would track actual data usage
        return Math.Round(Random.Shared.NextDouble() * 10, 2);
    }

    private int GetWarningCount()
    {
        // In a real implementation, this would track security warnings
        return Random.Shared.Next(0, 5);
    }

    private object[] GetMockActivity()
    {
        var activities = new[]
        {
            new { icon = "🚫", message = "Blocked access to social-media.com", device = "iPhone-12", time = "2 minutes ago" },
            new { icon = "📱", message = "New device connected: Android-Phone", device = "Android-Phone", time = "5 minutes ago" },
            new { icon = "🛡️", message = "Added new filter rule: gaming sites", device = "System", time = "10 minutes ago" },
            new { icon = "⚠️", message = "Suspicious activity detected", device = "Laptop-01", time = "15 minutes ago" },
            new { icon = "🔄", message = "System update completed", device = "System", time = "1 hour ago" }
        };

        return activities.Take(Random.Shared.Next(3, activities.Length)).ToArray();
    }
}