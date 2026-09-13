// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using Chimera.Abstractions.Models;
using Chimera.Host.Pages.Security;
using Chimera.PluginLoader;

namespace Chimera.Host.Services;

public class PermissionManagerService
{
    private readonly PluginPermissionManager _permissionManager;
    private readonly SecurityAuditLogger _auditLogger;

    public PermissionManagerService()
    {
        _auditLogger = new SecurityAuditLogger();
        _permissionManager = new PluginPermissionManager(auditLogger: _auditLogger);
        _permissionManager.OnPermissionPromptRequired += OnPermissionPromptRequired;
    }

    public PluginPermissionManager PermissionManager => _permissionManager;
    public SecurityAuditLogger AuditLogger => _auditLogger;

    private void OnPermissionPromptRequired(string pluginId, string permission, bool rememberChoice)
    {
        // This will be called from a background thread, so we need to marshal to UI thread
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            ShowPermissionPrompt(pluginId, permission);
        });
    }

    public bool ShowPermissionPrompt(string pluginId, string permission)
    {
        var dialog = new PermissionPromptDialog(pluginId, permission)
        {
            Owner = Application.Current.MainWindow
        };

        var result = dialog.ShowDialog();
        if (result == true)
        {
            _ = _permissionManager.GrantPermissionAsync(pluginId, permission);
            _auditLogger.LogPermissionGrant(pluginId, permission, "user");
            return true;
        }
        else
        {
            _auditLogger.LogSecurityViolation(pluginId, permission, 
                $"User denied permission {permission} for plugin {pluginId}");
            return false;
        }
    }

    public async Task<bool> CheckAndRequestPermissionAsync(string pluginId, string permission)
    {
        if (await _permissionManager.HasPermissionAsync(pluginId, permission))
        {
            return true;
        }

        // Need to request permission
        var granted = false;
        var resetEvent = new ManualResetEventSlim(false);

        Application.Current?.Dispatcher?.Invoke(() =>
        {
            granted = ShowPermissionPrompt(pluginId, permission);
            resetEvent.Set();
        });

        resetEvent.Wait();
        return granted;
    }

    public async Task<List<string>> GetPluginPermissionsAsync(string pluginId)
    {
        return await _permissionManager.GetGrantedPermissionsAsync(pluginId);
    }

    public async Task RevokePluginPermissionAsync(string pluginId, string permission)
    {
        await _permissionManager.RevokePermissionAsync(pluginId, permission);
    }

    public async Task RevokeAllPluginPermissionsAsync(string pluginId)
    {
        await _permissionManager.RevokeAllPermissionsAsync(pluginId);
    }

    public async Task<Dictionary<string, List<string>>> GetAllPermissionsAsync()
    {
        return await _permissionManager.GetAllGrantsAsync();
    }
}
