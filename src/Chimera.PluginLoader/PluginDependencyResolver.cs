// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

/// <summary>
/// Resolves plugin dependencies
/// </summary>
public class PluginDependencyResolver
{
    private readonly string _pluginDirectory;

    public PluginDependencyResolver(string? pluginDirectory = null)
    {
        _pluginDirectory = pluginDirectory ?? ChimeraPaths.PluginDirectory;
    }

    /// <summary>
    /// Resolve dependencies for a plugin
    /// </summary>
    public DependencyResolutionResult Resolve(string pluginId, List<PluginStoreEntry> availablePlugins)
    {
        var result = new DependencyResolutionResult();
        var visited = new HashSet<string>();
        var resolving = new HashSet<string>();

        ResolveInternal(pluginId, availablePlugins, result, visited, resolving);
        return result;
    }

    private void ResolveInternal(
        string pluginId, 
        List<PluginStoreEntry> availablePlugins, 
        DependencyResolutionResult result, 
        HashSet<string> visited,
        HashSet<string> resolving)
    {
        // Check for circular dependency
        if (resolving.Contains(pluginId))
        {
            result.Errors.Add($"检测到循环依赖: {pluginId}");
            return;
        }

        // Already resolved
        if (visited.Contains(pluginId))
        {
            return;
        }

        resolving.Add(pluginId);

        // Find the plugin
        var plugin = availablePlugins.FirstOrDefault(p => p.Id == pluginId);
        if (plugin == null)
        {
            // Check if already installed
            var manifestPath = Path.Combine(_pluginDirectory, pluginId, "plugin.json");
            if (!File.Exists(manifestPath))
            {
                result.Errors.Add($"依赖插件未找到: {pluginId}");
                return;
            }

            // Plugin is already installed
            visited.Add(pluginId);
            resolving.Remove(pluginId);
            return;
        }

        // Resolve dependencies first
        foreach (var dependencyId in plugin.Dependencies)
        {
            ResolveInternal(dependencyId, availablePlugins, result, visited, resolving);
            
            if (result.Errors.Any(e => e.Contains(dependencyId)))
            {
                result.Errors.Add($"无法解析 {pluginId} 的依赖 {dependencyId}");
                return;
            }
        }

        // Add to resolution order
        if (!result.ResolutionOrder.Contains(pluginId))
        {
            result.ResolutionOrder.Add(pluginId);
        }
        
        result.ResolvedPlugins.Add(plugin);
        visited.Add(pluginId);
        resolving.Remove(pluginId);
    }

    /// <summary>
    /// Check for circular dependencies
    /// </summary>
    public bool HasCircularDependency(string pluginId, List<PluginStoreEntry> availablePlugins)
    {
        var visited = new HashSet<string>();
        var stack = new HashSet<string>();
        return HasCircularDependencyInternal(pluginId, availablePlugins, visited, stack);
    }

    private bool HasCircularDependencyInternal(
        string pluginId, 
        List<PluginStoreEntry> availablePlugins, 
        HashSet<string> visited,
        HashSet<string> stack)
    {
        if (stack.Contains(pluginId))
        {
            return true;
        }

        if (visited.Contains(pluginId))
        {
            return false;
        }

        visited.Add(pluginId);
        stack.Add(pluginId);

        var plugin = availablePlugins.FirstOrDefault(p => p.Id == pluginId);
        if (plugin != null)
        {
            foreach (var dependencyId in plugin.Dependencies)
            {
                if (HasCircularDependencyInternal(dependencyId, availablePlugins, visited, stack))
                {
                    return true;
                }
            }
        }

        stack.Remove(pluginId);
        return false;
    }
}

/// <summary>
/// Result of dependency resolution
/// </summary>
public class DependencyResolutionResult
{
    public List<string> ResolutionOrder { get; set; } = new();
    public List<PluginStoreEntry> ResolvedPlugins { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0;
}
