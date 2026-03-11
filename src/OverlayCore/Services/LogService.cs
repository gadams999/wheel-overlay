using System;
using System.IO;

namespace OpenDash.OverlayCore.Services;

/// <summary>
/// File-based logging service with log rotation (truncation at 1 MB).
/// Log files are stored at %APPDATA%/{appName}/logs.txt
/// </summary>
public static class LogService
{
    private static string? _logPath;
    private static readonly object _lock = new object();
    private static bool _initialized = false;

    /// <summary>
    /// Initializes the log service with the application name.
    /// Log files are stored at %APPDATA%/{appName}/logs.txt
    /// </summary>
    /// <param name="appName">The application name (e.g., "WheelOverlay")</param>
    public static void Initialize(string appName)
    {
        if (_initialized)
        {
            return;
        }

        _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            appName,
            "logs.txt");

        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Append session start separator
            Log($"=== Application Started: {DateTime.Now} ===");
            _initialized = true;
        }
        catch (Exception)
        {
            // If we can't create log dir, we are in trouble, but try not to crash
        }
    }

    public static void Info(string message) => Log($"[INFO] {message}");

    public static void Error(string message) => Log($"[ERROR] {message}");

    public static void Error(string message, Exception ex)
    {
        Log($"[ERROR] {message}");
        Log($"[EXCEPTION] {ex}");
    }

    public static string GetLogPath()
    {
        if (_logPath == null)
        {
            throw new InvalidOperationException("LogService has not been initialized. Call Initialize(appName) first.");
        }
        return _logPath;
    }

    private static void Log(string line)
    {
        if (_logPath == null)
        {
            return; // Not initialized, silently skip
        }

        try
        {
            lock (_lock)
            {
                // Truncate if > 1MB
                var fileInfo = new FileInfo(_logPath);
                if (fileInfo.Exists && fileInfo.Length > 1 * 1024 * 1024)
                {
                    File.WriteAllText(_logPath, $"[LOG TRUNCATED - New Session {DateTime.Now}]\n");
                }

                File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {line}{Environment.NewLine}");
            }
        }
        catch
        {
            // Swallowing log exceptions to prevent app crash due to logging
        }
    }
}
