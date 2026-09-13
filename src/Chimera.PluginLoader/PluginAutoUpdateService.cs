// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

public class PluginAutoUpdateService
{
    private readonly PluginStoreService _storeService;
    private readonly PluginVersionManager _versionManager;
    private readonly PluginPermissionManager? _permissionManager;
    private readonly SecurityAuditLogger? _auditLogger;
    private readonly string _pluginsDirectory;
    
    private Timer? _checkTimer;
    private readonly Dictionary<string, PluginManifest> _installedPlugins = new();

    public event EventHandler<PluginUpdateAvailableEventArgs>? UpdateAvailable;
    public event EventHandler<PluginUpdateProgressEventArgs>? UpdateProgress;
    public event EventHandler<PluginUpdateCompletedEventArgs>? UpdateCompleted;

    public PluginAutoUpdateService(
        string pluginsDirectory,
        PluginStoreService? storeService = null,
        PluginVersionManager? versionManager = null,
        PluginPermissionManager? permissionManager = null,
        SecurityAuditLogger? auditLogger = null)
    {
        _pluginsDirectory = pluginsDirectory;
        _storeService = storeService ?? new PluginStoreService();
        _versionManager = versionManager ?? new PluginVersionManager(pluginsDirectory);
        _permissionManager = permissionManager;
        _auditLogger = auditLogger;
    }

    public async Task StartAutoCheckAsync(int intervalMinutes = 60)
    {
        await CheckForUpdatesAsync();
        _checkTimer = new Timer(async _ => await CheckForUpdatesAsync(), null, 
            TimeSpan.FromMinutes(intervalMinutes), TimeSpan.FromMinutes(intervalMinutes));
    }

    public void StopAutoCheck()
    {
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    public async Task<List<PluginUpdateInfo>> CheckForUpdatesAsync()
    {
        var updates = new List<PluginUpdateInfo>();
        
        try
        {
            await LoadInstalledPluginsAsync();
            
            foreach (var (pluginId, manifest) in _installedPlugins)
            {
                var update = await _storeService.CheckForUpdateAsync(pluginId, manifest.Version);
                if (update != null)
                {
                    var info = new PluginUpdateInfo
                    {
                        PluginId = pluginId,
                        CurrentVersion = manifest.Version,
                        AvailableVersion = update.Version,
                        StoreEntry = update,
                        ReleaseNotes = update.Description
                    };
                    updates.Add(info);
                    UpdateAvailable?.Invoke(this, new PluginUpdateAvailableEventArgs(info));
                }
            }
        }
        catch (Exception ex)
        {
            _auditLogger?.LogSuspiciousActivity("system", "update_check_failed", ex.Message);
        }

        return updates;
    }

    public async Task<bool> UpdatePluginAsync(string pluginId, bool createBackup = true)
    {
        try
        {
            var updateInfo = await GetUpdateInfoAsync(pluginId);
            if (updateInfo == null)
            {
                return false;
            }

            // Check permissions
            if (_permissionManager != null)
            {
                var hasPermission = await _permissionManager.HasPermissionAsync(pluginId, "update");
                if (!hasPermission)
                {
                    _auditLogger?.LogSecurityViolation(pluginId, "update", 
                        $"Plugin {pluginId} requires update permission");
                    return false;
                }
            }

            // Create backup if requested
            if (createBackup)
            {
                await _versionManager.SaveVersionAsync(pluginId);
            }

            // Download update
            var progress = new Progress<double>(p =>
            {
                UpdateProgress?.Invoke(this, new PluginUpdateProgressEventArgs(pluginId, p));
            });

            var zipPath = await _storeService.DownloadPluginAsync(updateInfo.StoreEntry, progress);

            // Install update
            var installDir = Path.Combine(_pluginsDirectory, pluginId);
            if (Directory.Exists(installDir))
            {
                Directory.Delete(installDir, true);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, installDir);
            
            // Clean up temp file
            try { File.Delete(zipPath); } catch { }

            _auditLogger?.LogPluginLoad(pluginId, true, $"Updated to version {updateInfo.AvailableVersion}");
            UpdateCompleted?.Invoke(this, new PluginUpdateCompletedEventArgs(pluginId, updateInfo.AvailableVersion));
            
            return true;
        }
        catch (Exception ex)
        {
            _auditLogger?.LogSuspiciousActivity(pluginId, "update_failed", ex.Message);
            return false;
        }
    }

    public async Task<bool> UpdateAllPluginsAsync()
    {
        var updates = await CheckForUpdatesAsync();
        var allSuccess = true;

        foreach (var update in updates)
        {
            var success = await UpdatePluginAsync(update.PluginId);
            if (!success)
            {
                allSuccess = false;
            }
        }

        return allSuccess;
    }

    private async Task LoadInstalledPluginsAsync()
    {
        _installedPlugins.Clear();
        
        if (!Directory.Exists(_pluginsDirectory))
        {
            return;
        }

        foreach (var pluginDir in Directory.GetDirectories(_pluginsDirectory))
        {
            var manifestPath = Path.Combine(pluginDir, "plugin.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(manifestPath);
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(json);
                    if (manifest != null)
                    {
                        _installedPlugins[manifest.Id] = manifest;
                    }
                }
                catch
                {
                    // Skip invalid manifests
                }
            }
        }
    }

    private async Task<PluginUpdateInfo?> GetUpdateInfoAsync(string pluginId)
    {
        await LoadInstalledPluginsAsync();
        
        if (!_installedPlugins.TryGetValue(pluginId, out var manifest))
        {
            return null;
        }

        var storeEntry = await _storeService.CheckForUpdateAsync(pluginId, manifest.Version);
        if (storeEntry == null)
        {
            return null;
        }

        return new PluginUpdateInfo
        {
            PluginId = pluginId,
            CurrentVersion = manifest.Version,
            AvailableVersion = storeEntry.Version,
            StoreEntry = storeEntry,
            ReleaseNotes = storeEntry.Description
        };
    }

    public void Dispose()
    {
        _checkTimer?.Dispose();
    }
}

public class PluginUpdateInfo
{
    public string PluginId { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string AvailableVersion { get; set; } = string.Empty;
    public PluginStoreEntry StoreEntry { get; set; } = new();
    public string ReleaseNotes { get; set; } = string.Empty;
}

public class PluginUpdateAvailableEventArgs : EventArgs
{
    public PluginUpdateInfo UpdateInfo { get; }

    public PluginUpdateAvailableEventArgs(PluginUpdateInfo updateInfo)
    {
        UpdateInfo = updateInfo;
    }
}

public class PluginUpdateProgressEventArgs : EventArgs
{
    public string PluginId { get; }
    public double Progress { get; }

    public PluginUpdateProgressEventArgs(string pluginId, double progress)
    {
        PluginId = pluginId;
        Progress = progress;
    }
}

public class PluginUpdateCompletedEventArgs : EventArgs
{
    public string PluginId { get; }
    public string NewVersion { get; }

    public PluginUpdateCompletedEventArgs(string pluginId, string newVersion)
    {
        PluginId = pluginId;
        NewVersion = newVersion;
    }
}
