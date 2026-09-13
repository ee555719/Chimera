// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

/// <summary>
/// Manages keyboard shortcuts
/// </summary>
public class KeyboardShortcutManager
{
    private readonly string _configDirectory;
    private KeyboardShortcutConfig? _config;
    
    private const string ConfigFileName = "keyboard-shortcuts.json";

    public KeyboardShortcutManager(string? configDirectory = null)
    {
        _configDirectory = configDirectory ?? ChimeraPaths.ConfigDirectory;
    }

    /// <summary>
    /// Load shortcuts configuration
    /// </summary>
    public async Task<KeyboardShortcutConfig> GetConfigAsync()
    {
        if (_config != null)
        {
            return _config;
        }

        var configPath = Path.Combine(_configDirectory, ConfigFileName);
        if (File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            _config = JsonSerializer.Deserialize<KeyboardShortcutConfig>(json) ?? new KeyboardShortcutConfig();
        }
        else
        {
            _config = new KeyboardShortcutConfig();
            await SaveConfigAsync(_config);
        }

        return _config;
    }

    /// <summary>
    /// Save shortcuts configuration
    /// </summary>
    public async Task SaveConfigAsync(KeyboardShortcutConfig config)
    {
        Directory.CreateDirectory(_configDirectory);
        var configPath = Path.Combine(_configDirectory, ConfigFileName);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, json);
        _config = config;
    }

    /// <summary>
    /// Register a shortcut
    /// </summary>
    public async Task RegisterShortcutAsync(KeyboardShortcut shortcut)
    {
        var config = await GetConfigAsync();
        
        // Check for conflicts
        var conflicts = config.Shortcuts.Where(s => 
            s.Id != shortcut.Id &&
            s.Key == shortcut.Key &&
            s.Modifiers.SequenceEqual(shortcut.Modifiers) &&
            s.IsEnabled).ToList();
        
        if (conflicts.Any())
        {
            throw new InvalidOperationException($"快捷键冲突: {shortcut.Key} 已被 {conflicts.First().Name} 使用");
        }
        
        // Add or update
        var existing = config.Shortcuts.FirstOrDefault(s => s.Id == shortcut.Id);
        if (existing != null)
        {
            existing.Name = shortcut.Name;
            existing.Key = shortcut.Key;
            existing.Modifiers = shortcut.Modifiers;
            existing.PluginId = shortcut.PluginId;
            existing.CommandId = shortcut.CommandId;
            existing.IsEnabled = shortcut.IsEnabled;
        }
        else
        {
            config.Shortcuts.Add(shortcut);
        }
        
        await SaveConfigAsync(config);
    }

    /// <summary>
    /// Unregister a shortcut
    /// </summary>
    public async Task UnregisterShortcutAsync(string shortcutId)
    {
        var config = await GetConfigAsync();
        config.Shortcuts.RemoveAll(s => s.Id == shortcutId);
        await SaveConfigAsync(config);
    }

    /// <summary>
    /// Get all shortcuts
    /// </summary>
    public async Task<List<KeyboardShortcut>> GetShortcutsAsync()
    {
        var config = await GetConfigAsync();
        return config.Shortcuts;
    }

    /// <summary>
    /// Check for conflicts
    /// </summary>
    public async Task<List<KeyboardShortcut>> GetConflictsAsync(string key, List<string> modifiers, string? excludeId = null)
    {
        var config = await GetConfigAsync();
        return config.Shortcuts.Where(s => 
            s.Id != excludeId &&
            s.Key == key &&
            s.Modifiers.SequenceEqual(modifiers) &&
            s.IsEnabled).ToList();
    }

    /// <summary>
    /// Enable or disable a shortcut
    /// </summary>
    public async Task SetEnabledAsync(string shortcutId, bool enabled)
    {
        var config = await GetConfigAsync();
        var shortcut = config.Shortcuts.FirstOrDefault(s => s.Id == shortcutId);
        if (shortcut != null)
        {
            shortcut.IsEnabled = enabled;
            await SaveConfigAsync(config);
        }
    }
}
