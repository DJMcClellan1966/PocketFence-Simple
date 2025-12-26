using System;
using System.IO;

namespace PocketFence.Utils
{
    public static class DebugLogger
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
        
        public static void Log(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, logEntry);
                Console.WriteLine($"DEBUG: {message}");
            }
            catch
            {
                // Ignore logging errors
            }
        }
        
        public static void ClearLog()
        {
            try
            {
                if (File.Exists(LogPath))
                    File.Delete(LogPath);
            }
            catch
            {
                // Ignore deletion errors
            }
        }
    }
}