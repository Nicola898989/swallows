using System;

namespace Swallows.Core.Services;

// Simple logger implementation
public static class LoggerService
{
    // Event to subscribe to for UI updates (if needed)
    public static event Action<string>? OnLog;

    public static void Info(string message)
    {
        Log($"[INFO] {message}");
    }

    public static void Warn(string message)
    {
        Log($"[WARN] {message}");
    }

    public static void Error(string message)
    {
        Log($"[ERROR] {message}");
    }

    public static void Error(string message, Exception ex)
    {
        Log($"[ERROR] {message}. Exception: {ex.Message}");
    }

    public static void Debug(string message)
    {
        Log($"[DEBUG] {message}");
    }

    private static void Log(string formattedMessage)
    {
        var msg = $"{DateTime.Now:HH:mm:ss} {formattedMessage}";
        Console.WriteLine(msg);
        OnLog?.Invoke(msg);
    }
}
