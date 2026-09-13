// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Net.Http;
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

/// <summary>
/// Plugin store service for fetching and managing plugins
/// </summary>
public class PluginStoreService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _configDirectory;
    private PluginStoreConfig? _config;
    
    private const string ConfigFileName = "plugin-store.json";

    public PluginStoreService(string? configDirectory = null)
    {
        _configDirectory = configDirectory ?? ChimeraPaths.ConfigDirectory;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Load store configuration
    /// </summary>
    public async Task<PluginStoreConfig> GetConfigAsync()
    {
        if (_config != null)
        {
            return _config;
        }

        var configPath = Path.Combine(_configDirectory, ConfigFileName);
        if (File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            _config = JsonSerializer.Deserialize<PluginStoreConfig>(json) ?? new PluginStoreConfig();
        }
        else
        {
            _config = new PluginStoreConfig();
            await SaveConfigAsync(_config);
        }

        return _config;
    }

    /// <summary>
    /// Save store configuration
    /// </summary>
    public async Task SaveConfigAsync(PluginStoreConfig config)
    {
        Directory.CreateDirectory(_configDirectory);
        var configPath = Path.Combine(_configDirectory, ConfigFileName);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, json);
        _config = config;
    }

    /// <summary>
    /// Fetch plugins from the store
    /// </summary>
    public async Task<List<PluginStoreEntry>> FetchPluginsAsync(int page = 1, int pageSize = 50, string? search = null, string? tag = null)
    {
        try
        {
            var config = await GetConfigAsync();
            var response = await _httpClient.GetStringAsync(config.SourceUrl);
            
            var storeResponse = JsonSerializer.Deserialize<PluginStoreResponse>(response);
            if (storeResponse == null)
            {
                return new List<PluginStoreEntry>();
            }

            var plugins = storeResponse.Plugins.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                plugins = plugins.Where(p => 
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Id.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(tag))
            {
                plugins = plugins.Where(p => p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
            }

            // Apply pagination
            return plugins
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch plugins from store: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Download a plugin from the store
    /// </summary>
    public async Task<string> DownloadPluginAsync(PluginStoreEntry plugin, IProgress<double>? progress = null)
    {
        var tempPath = Path.Combine(ChimeraPaths.TempDirectory, $"{plugin.Id}_{plugin.Version}.zip");
        Directory.CreateDirectory(ChimeraPaths.TempDirectory);

        try
        {
            using var response = await _httpClient.GetAsync(plugin.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var totalBytesRead = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                if (totalBytes > 0)
                {
                    var progressPercentage = (double)totalBytesRead / totalBytes * 100;
                    progress?.Report(progressPercentage);
                }
            }

            return tempPath;
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            throw;
        }
    }

    /// <summary>
    /// Check if a plugin update is available
    /// </summary>
    public async Task<PluginStoreEntry?> CheckForUpdateAsync(string pluginId, string currentVersion)
    {
        try
        {
            var plugins = await FetchPluginsAsync(search: pluginId);
            var latest = plugins.FirstOrDefault(p => p.Id == pluginId);
            
            if (latest != null && IsNewerVersion(latest.Version, currentVersion))
            {
                return latest;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsNewerVersion(string newVersion, string currentVersion)
    {
        if (Version.TryParse(newVersion, out var newV) && Version.TryParse(currentVersion, out var currentV))
        {
            return newV > currentV;
        }
        return string.Compare(newVersion, currentVersion, StringComparison.Ordinal) > 0;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
