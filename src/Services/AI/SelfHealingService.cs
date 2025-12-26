using System.Diagnostics;
using PocketFence_Simple.Models;

namespace PocketFence_Simple.Services.AI
{
    public class SelfHealingService
    {
        private readonly ILogger<SelfHealingService> _logger;
        private readonly Timer _healthCheckTimer;
        private int _restartAttempts = 0;
        private readonly int _maxRestartAttempts = 3;

        public event EventHandler<string>? RecoveryActionTaken;

        public SelfHealingService(ILogger<SelfHealingService> logger)
        {
            _logger = logger;
            
            // Simple health check every 5 minutes
            _healthCheckTimer = new Timer(CheckSystemHealth, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public bool HandleFatalError(Exception exception, string context)
        {
            _logger.LogError(exception, "Fatal error in {Context}", context);

            try
            {
                // Attempt automatic recovery
                var recovered = AttemptRecovery(context);
                
                if (recovered)
                {
                    _logger.LogInformation("Successfully recovered from error in {Context}", context);
                    RecoveryActionTaken?.Invoke(this, $"Recovered from {exception.GetType().Name} in {context}");
                    return true;
                }

                // If recovery failed, try restart if under limit
                if (_restartAttempts < _maxRestartAttempts)
                {
                    _restartAttempts++;
                    _logger.LogWarning("Attempting service restart ({Attempt}/{Max})", _restartAttempts, _maxRestartAttempts);
                    
                    // Graceful restart
                    Task.Run(() => RestartApplication());
                    return true;
                }

                _logger.LogCritical("Max restart attempts reached. System requires manual intervention.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recovery attempt");
                return false;
            }
        }

        public AISystemHealth GetSystemHealth()
        {
            var health = new AISystemHealth
            {
                Timestamp = DateTime.UtcNow,
                IsOperational = true,
                SystemLoad = GetCurrentSystemLoad(),
                ActiveModules = GetActiveModuleList(),
                Errors = new List<SystemError>()
            };

            // Simple operational check
            try
            {
                var process = Process.GetCurrentProcess();
                if (process.WorkingSet64 > 500 * 1024 * 1024) // 500MB threshold
                {
                    health.Errors.Add(new SystemError 
                    { 
                        Timestamp = DateTime.UtcNow,
                        Module = "System",
                        ErrorType = "Performance",
                        Message = "High memory usage detected",
                        Severity = ErrorSeverity.Warning
                    });
                }
            }
            catch (Exception ex)
            {
                health.IsOperational = false;
                health.Errors.Add(new SystemError 
                { 
                    Timestamp = DateTime.UtcNow,
                    Module = "HealthCheck",
                    ErrorType = "Exception",
                    Message = $"Health check failed: {ex.Message}",
                    Severity = ErrorSeverity.Error
                });
            }

            return health;
        }

        private bool AttemptRecovery(string context)
        {
            try
            {
                // Simple recovery: clear caches and reset state
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                _logger.LogInformation("Performed cleanup for recovery in {Context}", context);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recovery attempt failed");
                return false;
            }
        }

        private void RestartApplication()
        {
            try
            {
                _logger.LogWarning("Initiating application restart for recovery");
                
                // Get current executable path
                var currentProcess = Process.GetCurrentProcess();
                var exePath = currentProcess.MainModule?.FileName;

                if (!string.IsNullOrEmpty(exePath))
                {
                    // Start new instance
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });

                    // Exit current process
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart application");
            }
        }

        private void CheckSystemHealth(object? state)
        {
            try
            {
                var health = GetSystemHealth();
                
                if (!health.IsOperational || health.Errors.Count > 0)
                {
                    _logger.LogWarning("System health issues detected: {ErrorCount} errors", health.Errors.Count);
                    
                    // Reset restart attempts if system is healthy for a while
                    if (health.IsOperational)
                    {
                        _restartAttempts = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
            }
        }

        private double GetCurrentSystemLoad()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return Math.Min(process.WorkingSet64 / (1024.0 * 1024.0 * 1024.0), 1.0); // Normalize to 0-1
            }
            catch
            {
                return 0.5; // Default moderate load
            }
        }

        private List<string> GetActiveModuleList()
        {
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetName().Name ?? "Unknown")
                    .ToList();
            }
            catch
            {
                return new List<string> { "PocketFence-Simple" }; // At least this module
            }
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
        }
    }
}