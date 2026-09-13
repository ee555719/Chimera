// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Reflection;
using System.Text.Json;
using Chimera.Abstractions.Interfaces;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

public class PluginManager
{
    private readonly string _pluginsDirectory;
    private readonly Dictionary<string, PluginContext> _loadedPlugins = new();
    private readonly Dictionary<string, PluginManifest> _manifests = new();
    private readonly IPluginLogger? _logger;

    public IReadOnlyDictionary<string, PluginContext> LoadedPlugins => _loadedPlugins;
    public IReadOnlyDictionary<string, PluginManifest> Manifests => _manifests;

    public PluginManager(string pluginsDirectory, IPluginLogger? logger = null)
    {
        _pluginsDirectory = pluginsDirectory;
        _logger = logger;
        
        if (!Directory.Exists(_pluginsDirectory))
        {
            Directory.CreateDirectory(_pluginsDirectory);
        }
    }

    public async Task DiscoverPluginsAsync()
    {
        _logger?.Info($"Discovering plugins in {_pluginsDirectory}");
        
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
                        _manifests[manifest.Id] = manifest;
                        _logger?.Info($"Discovered plugin: {manifest.Name} ({manifest.Id})");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Failed to load manifest for plugin in {pluginDir}", ex);
                }
            }
        }
    }

    public async Task LoadPluginAsync(string pluginId)
    {
        if (!_manifests.TryGetValue(pluginId, out var manifest))
        {
            throw new InvalidOperationException($"Plugin {pluginId} not found");
        }

        var pluginDir = Path.Combine(_pluginsDirectory, pluginId);
        var entryAssemblyPath = Path.Combine(pluginDir, manifest.EntryAssembly);

        if (!File.Exists(entryAssemblyPath))
        {
            throw new FileNotFoundException($"Entry assembly not found: {entryAssemblyPath}");
        }

        var context = new PluginContext(manifest, pluginDir);
        var assembly = Assembly.LoadFrom(entryAssemblyPath);
        
        var pluginType = assembly.GetType(manifest.EntryType);
        if (pluginType == null)
        {
            throw new TypeLoadException($"Type {manifest.EntryType} not found in {manifest.EntryAssembly}");
        }

        // Execute startup tasks
        if (typeof(IStartupTask).IsAssignableFrom(pluginType))
        {
            var startupTask = (IStartupTask)Activator.CreateInstance(pluginType)!;
            await startupTask.ExecuteAsync(context);
        }

        _loadedPlugins[pluginId] = context;
        _logger?.Info($"Plugin loaded: {manifest.Name}");
    }

    public void UnloadPlugin(string pluginId)
    {
        if (_loadedPlugins.TryGetValue(pluginId, out var context))
        {
            // TODO: Unload AssemblyLoadContext when implemented
            _loadedPlugins.Remove(pluginId);
            _logger?.Info($"Plugin unloaded: {pluginId}");
        }
    }

    public bool IsPluginLoaded(string pluginId)
    {
        return _loadedPlugins.ContainsKey(pluginId);
    }
}
