// SPDX-License-Identifier: AGPL-3.0-or-later
using Chimera.Abstractions.Interfaces;

namespace Chimera.PluginLoader;

public class PluginLogger : IPluginLogger
{
    private readonly string _pluginId;
    private readonly string _logDirectory;

    public PluginLogger(string pluginId)
    {
        _pluginId = pluginId;
        _logDirectory = Path.Combine(pluginId, "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public void Debug(string message) => Log("DEBUG", message);
    public void Info(string message) => Log("INFO", message);
    public void Warning(string message) => Log("WARN", message);
    public void Error(string message, Exception? exception = null) => Log("ERROR", message, exception);
    public void Fatal(string message, Exception? exception = null) => Log("FATAL", message, exception);

    private void Log(string level, string message, Exception? exception = null)
    {
        var logFile = Path.Combine(_logDirectory, $"{_pluginId}.log");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var entry = $"[{timestamp}] [{level}] {message}";
        
        if (exception != null)
        {
            entry += Environment.NewLine + exception.ToString();
        }

        try
        {
            File.AppendAllText(logFile, entry + Environment.NewLine);
        }
        catch
        {
            // Silently fail if we can't write logs
        }
    }
}
