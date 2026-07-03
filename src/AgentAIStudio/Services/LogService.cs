using System.Collections.Concurrent;
using System.Text.Json;
using AgentAIStudio.Models;

namespace AgentAIStudio.Services;

public class LogService
{
    private static readonly Lazy<LogService> _instance = new(() => new LogService());
    public static LogService Instance => _instance.Value;

    private readonly string _logDir;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ConcurrentQueue<string> _pendingWrites = new();
    private readonly long _maxSizeBytes = 50L * 1024 * 1024;
    private readonly int _maxAgeDays = 7;
    private DateTime _lastCleanup = DateTime.MinValue;

    private LogService()
    {
        _logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgentAIStudio", "logs");
        Directory.CreateDirectory(_logDir);

        _ = RunCleanupAsync();
        _ = ProcessQueueAsync();
    }

    public void Log(LogLevel level, string category, string message, object? data = null, Exception? exception = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level.ToString(),
            Category = category,
            Message = message,
            Data = data,
            Exception = exception?.ToString()
        };

        _pendingWrites.Enqueue(entry.ToJson());

        System.Diagnostics.Debug.WriteLine($"[{level}] [{category}] {message}");
    }

    public void LogDebug(string category, string message, object? data = null)
        => Log(LogLevel.Debug, category, message, data);

    public void LogInfo(string category, string message, object? data = null)
        => Log(LogLevel.Info, category, message, data);

    public void LogWarning(string category, string message, object? data = null)
        => Log(LogLevel.Warning, category, message, data);

    public void LogError(string category, string message, Exception? ex = null, object? data = null)
        => Log(LogLevel.Error, category, message, data, ex);

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            try
            {
                if (_pendingWrites.TryDequeue(out var line))
                {
                    await WriteLineAsync(line);
                }
                else
                {
                    await Task.Delay(100);
                }
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
    }

    private async Task WriteLineAsync(string line)
    {
        await _writeLock.WaitAsync();
        try
        {
            var filePath = GetCurrentLogFile();
            await File.AppendAllTextAsync(filePath, line + Environment.NewLine);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private string GetCurrentLogFile()
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return Path.Combine(_logDir, $"app-{date}.json");
    }

    private async Task RunCleanupAsync()
    {
        while (true)
        {
            try
            {
                if ((DateTime.UtcNow - _lastCleanup).TotalHours >= 1)
                {
                    await CleanupOldLogs();
                    await EnforceSizeLimit();
                    _lastCleanup = DateTime.UtcNow;
                }
            }
            catch
            {
                // Silently continue
            }
            await Task.Delay(TimeSpan.FromHours(1));
        }
    }

    private Task CleanupOldLogs()
    {
        var cutoff = DateTime.UtcNow.AddDays(-_maxAgeDays);
        foreach (var file in Directory.GetFiles(_logDir, "app-*.json"))
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var datePart = name.Replace("app-", "");
                if (DateTime.TryParse(datePart, out var fileDate))
                {
                    if (fileDate < cutoff)
                    {
                        File.Delete(file);
                        System.Diagnostics.Debug.WriteLine($"[LogService] Deleted old log: {file}");
                    }
                }
            }
            catch { }
        }
        return Task.CompletedTask;
    }

    private Task EnforceSizeLimit()
    {
        var files = Directory.GetFiles(_logDir, "app-*.json")
            .OrderBy(f => f)
            .ToList();

        long totalSize = 0;
        foreach (var file in files)
        {
            try { totalSize += new FileInfo(file).Length; } catch { }
        }

        if (totalSize <= _maxSizeBytes) return Task.CompletedTask;

        var targetSize = (long)(_maxSizeBytes * 0.8);
        foreach (var file in files)
        {
            if (totalSize <= targetSize) break;
            try
            {
                var size = new FileInfo(file).Length;
                File.Delete(file);
                totalSize -= size;
                System.Diagnostics.Debug.WriteLine($"[LogService] Deleted oversized log: {file}");
            }
            catch { }
        }
        return Task.CompletedTask;
    }

    public string[] GetRecentLogs(int count = 100)
    {
        var filePath = GetCurrentLogFile();
        if (!File.Exists(filePath)) return [];

        try
        {
            var lines = File.ReadAllLines(filePath);
            return lines.Length <= count ? lines : lines[^count..];
        }
        catch
        {
            return [];
        }
    }
}
