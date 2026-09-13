// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Chimera.Abstractions.Interfaces;

namespace Chimera.PluginLoader;

public class PluginConfiguration : IPluginConfiguration
{
    private readonly string _configFile;
    private readonly Dictionary<string, object> _config = new();

    public PluginConfiguration(string pluginId)
    {
        _configFile = Path.Combine(pluginId, "config.json");
        Load();
    }

    public T? Get<T>(string key)
    {
        if (_config.TryGetValue(key, out var value))
        {
            if (value is JsonElement element)
            {
                return element.Deserialize<T>();
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }
        return default;
    }

    public void Set<T>(string key, T value)
    {
        _config[key] = value!;
        Save();
    }

    public bool ContainsKey(string key)
    {
        return _config.ContainsKey(key);
    }

    public IReadOnlyDictionary<string, object> GetAll()
    {
        return _config;
    }

    private void Load()
    {
        if (File.Exists(_configFile))
        {
            try
            {
                var json = File.ReadAllText(_configFile);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (loaded != null)
                {
                    foreach (var kvp in loaded)
                    {
                        _config[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
                // Ignore load errors
            }
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFile, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}
