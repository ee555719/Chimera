// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

/// <summary>
/// Manages plugin version history and rollback
/// </summary>
public class PluginVersionManager
{
    private readonly string _pluginDirectory;
    private readonly string _historyDirectory;
    
    private const int MaxVersionsToKeep = 3;
    private const string HistoryFileName = "version-history.json";

    public PluginVersionManager(string? pluginDirectory = null)
    {
        _pluginDirectory = pluginDirectory ?? ChimeraPaths.PluginDirectory;
        _historyDirectory = Path.Combine(_pluginDirectory, ".history");
        Directory.CreateDirectory(_historyDirectory);
    }

    /// <summary>
    /// Save current plugin version before update
    /// </summary>
    public async Task SaveVersionAsync(string pluginId)
    {
        var pluginDir = Path.Combine(_pluginDirectory, pluginId);
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        
        if (!File.Exists(manifestPath))
        {
            return;
        }

        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);
        if (manifest == null)
        {
            return;
        }

        var history = await GetHistoryAsync(pluginId);
        
        // Create backup of current version
        var backupDir = Path.Combine(_historyDirectory, pluginId, manifest.Version);
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, true);
        }
        
        CopyDirectory(pluginDir, backupDir);

        // Add to history
        var versionEntry = new PluginVersionEntry
        {
            Version = manifest.Version,
            Timestamp = DateTime.UtcNow,
            BackupPath = backupDir
        };

        history.Versions.Insert(0, versionEntry);

        // Keep only the last N versions
        if (history.Versions.Count > MaxVersionsToKeep)
        {
            var versionsToRemove = history.Versions.Skip(MaxVersionsToKeep).ToList();
            foreach (var v in versionsToRemove)
            {
                if (Directory.Exists(v.BackupPath))
                {
                    try { Directory.Delete(v.BackupPath, true); } catch { }
                }
                history.Versions.Remove(v);
            }
        }

        await SaveHistoryAsync(pluginId, history);
    }

    /// <summary>
    /// Rollback to the previous version
    /// </summary>
    public async Task<bool> RollbackAsync(string pluginId)
    {
        var history = await GetHistoryAsync(pluginId);
        
        if (history.Versions.Count == 0)
        {
            return false;
        }

        var previousVersion = history.Versions[0];
        var pluginDir = Path.Combine(_pluginDirectory, pluginId);
        
        // Delete current version
        if (Directory.Exists(pluginDir))
        {
            Directory.Delete(pluginDir, true);
        }

        // Restore previous version
        if (Directory.Exists(previousVersion.BackupPath))
        {
            CopyDirectory(previousVersion.BackupPath, pluginDir);
        }

        // Remove from history
        history.Versions.RemoveAt(0);
        await SaveHistoryAsync(pluginId, history);

        return true;
    }

    /// <summary>
    /// Get version history for a plugin
    /// </summary>
    public async Task<PluginVersionHistory> GetHistoryAsync(string pluginId)
    {
        var historyPath = Path.Combine(_historyDirectory, pluginId, HistoryFileName);
        
        if (File.Exists(historyPath))
        {
            var json = await File.ReadAllTextAsync(historyPath);
            return JsonSerializer.Deserialize<PluginVersionHistory>(json) ?? new PluginVersionHistory();
        }

        return new PluginVersionHistory();
    }

    /// <summary>
    /// Get available versions for rollback
    /// </summary>
    public async Task<List<PluginVersionEntry>> GetAvailableVersionsAsync(string pluginId)
    {
        var history = await GetHistoryAsync(pluginId);
        return history.Versions;
    }

    private async Task SaveHistoryAsync(string pluginId, PluginVersionHistory history)
    {
        var historyDir = Path.Combine(_historyDirectory, pluginId);
        Directory.CreateDirectory(historyDir);
        
        var historyPath = Path.Combine(historyDir, HistoryFileName);
        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(historyPath, json);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        
        foreach (var file in Directory.GetFiles(source))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        
        foreach (var dir in Directory.GetDirectories(source))
        {
            var destDir = Path.Combine(destination, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }
}

/// <summary>
/// Plugin version history
/// </summary>
public class PluginVersionHistory
{
    public List<PluginVersionEntry> Versions { get; set; } = new();
}

/// <summary>
/// Plugin version entry
/// </summary>
public class PluginVersionEntry
{
    public string Version { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string BackupPath { get; set; } = string.Empty;
}
