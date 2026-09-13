// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

public class PluginPermissionManager
{
    private readonly string _configDirectory;
    private readonly SecurityAuditLogger _auditLogger;
    private PermissionGrantConfig? _config;
    private const string ConfigFileName = "permissions.json";

    public event Action<string, string, bool>? OnPermissionPromptRequired;

    public PluginPermissionManager(string? configDirectory = null, SecurityAuditLogger? auditLogger = null)
    {
        _configDirectory = configDirectory ?? ChimeraPaths.ConfigDirectory;
        _auditLogger = auditLogger ?? new SecurityAuditLogger();
        Directory.CreateDirectory(_configDirectory);
    }

    public async Task<PermissionGrantConfig> GetConfigAsync()
    {
        if (_config != null)
            return _config;

        var configPath = Path.Combine(_configDirectory, ConfigFileName);
        if (File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            _config = JsonSerializer.Deserialize<PermissionGrantConfig>(json) ?? new PermissionGrantConfig();
        }
        else
        {
            _config = new PermissionGrantConfig();
            await SaveConfigAsync(_config);
        }
        return _config;
    }

    public async Task SaveConfigAsync(PermissionGrantConfig config)
    {
        var configPath = Path.Combine(_configDirectory, ConfigFileName);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, json);
        _config = config;
    }

    public async Task<bool> HasPermissionAsync(string pluginId, string permission)
    {
        var config = await GetConfigAsync();
        var grant = config.Grants.FirstOrDefault(g => g.PluginId == pluginId);
        var hasPermission = grant?.Permissions.Contains(permission) ?? false;

        _auditLogger.LogPermissionCheck(pluginId, permission, hasPermission);
        return hasPermission;
    }

    public async Task<List<string>> GetGrantedPermissionsAsync(string pluginId)
    {
        var config = await GetConfigAsync();
        var grant = config.Grants.FirstOrDefault(g => g.PluginId == pluginId);
        return grant?.Permissions ?? new List<string>();
    }

    public async Task GrantPermissionAsync(string pluginId, string permission)
    {
        var config = await GetConfigAsync();
        var grant = config.Grants.FirstOrDefault(g => g.PluginId == pluginId);

        if (grant == null)
        {
            grant = new PluginPermissionGrant { PluginId = pluginId };
            config.Grants.Add(grant);
        }

        if (!grant.Permissions.Contains(permission))
        {
            grant.Permissions.Add(permission);
            grant.GrantedAt = DateTime.UtcNow;
            await SaveConfigAsync(config);
            _auditLogger.LogPermissionGrant(pluginId, permission);
        }
    }

    public async Task RevokePermissionAsync(string pluginId, string permission)
    {
        var config = await GetConfigAsync();
        var grant = config.Grants.FirstOrDefault(g => g.PluginId == pluginId);

        if (grant != null && grant.Permissions.Contains(permission))
        {
            grant.Permissions.Remove(permission);
            if (!grant.Permissions.Any())
            {
                config.Grants.Remove(grant);
            }
            await SaveConfigAsync(config);
            _auditLogger.LogPermissionRevoke(pluginId, permission);
        }
    }

    public async Task RevokeAllPermissionsAsync(string pluginId)
    {
        var config = await GetConfigAsync();
        var grant = config.Grants.FirstOrDefault(g => g.PluginId == pluginId);

        if (grant != null)
        {
            foreach (var permission in grant.Permissions)
            {
                _auditLogger.LogPermissionRevoke(pluginId, permission);
            }
            config.Grants.Remove(grant);
            await SaveConfigAsync(config);
        }
    }

    public async Task<bool> CheckAndRequestPermissionAsync(string pluginId, string permission)
    {
        if (await HasPermissionAsync(pluginId, permission))
            return true;

        OnPermissionPromptRequired?.Invoke(pluginId, permission, false);
        return false;
    }

    public async Task<Dictionary<string, List<string>>> GetAllGrantsAsync()
    {
        var config = await GetConfigAsync();
        return config.Grants.ToDictionary(g => g.PluginId, g => g.Permissions);
    }
}
