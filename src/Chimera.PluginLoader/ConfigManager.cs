// SPDX-License-Identifier: AGPL-3.0-or-later
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

/// <summary>
/// Manages configuration import/export
/// </summary>
public class ConfigManager
{
    private readonly string _configDirectory;
    private readonly string _pluginDirectory;
    
    private const string ConfigFileName = "chimera-config.json";
    private const string ConfigFileExtension = ".chimera-config";

    public ConfigManager(string? configDirectory = null, string? pluginDirectory = null)
    {
        _configDirectory = configDirectory ?? ChimeraPaths.ConfigDirectory;
        _pluginDirectory = pluginDirectory ?? ChimeraPaths.PluginDirectory;
    }

    /// <summary>
    /// Export current configuration
    /// </summary>
    public async Task ExportAsync(string outputPath)
    {
        var config = await CollectConfigurationAsync();
        
        var tempDir = Path.Combine(ChimeraPaths.TempDirectory, "export_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        try
        {
            // Save config JSON
            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(tempDir, ConfigFileName), configJson);

            // Copy plugin data directories
            if (Directory.Exists(_pluginDirectory))
            {
                foreach (var pluginDir in Directory.GetDirectories(_pluginDirectory))
                {
                    var pluginId = Path.GetFileName(pluginDir);
                    var dataDir = Path.Combine(pluginDir, "data");
                    if (Directory.Exists(dataDir))
                    {
                        var exportDataDir = Path.Combine(tempDir, "plugins", pluginId, "data");
                        CopyDirectory(dataDir, exportDataDir);
                    }
                }
            }

            // Create zip
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            ZipFile.CreateFromDirectory(tempDir, outputPath);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    /// <summary>
    /// Import configuration
    /// </summary>
    public async Task<ConfigImportResult> ImportAsync(string configPath)
    {
        var result = new ConfigImportResult();
        var tempDir = Path.Combine(ChimeraPaths.TempDirectory, "import_" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            // Extract zip
            ZipFile.ExtractToDirectory(configPath, tempDir);

            // Read config
            var configJsonPath = Path.Combine(tempDir, ConfigFileName);
            if (!File.Exists(configJsonPath))
            {
                result.Errors.Add("配置文件不存在");
                return result;
            }

            var configJson = await File.ReadAllTextAsync(configJsonPath);
            var config = JsonSerializer.Deserialize<ChimeraConfig>(configJson);
            if (config == null)
            {
                result.Errors.Add("无法解析配置文件");
                return result;
            }

            // Check version compatibility
            if (!IsVersionCompatible(config.Version))
            {
                result.Warnings.Add($"配置版本 {config.Version} 可能与当前版本不兼容");
            }

            // Restore plugins data
            var pluginsDataDir = Path.Combine(tempDir, "plugins");
            if (Directory.Exists(pluginsDataDir))
            {
                foreach (var pluginDataDir in Directory.GetDirectories(pluginsDataDir))
                {
                    var pluginId = Path.GetFileName(pluginDataDir);
                    var targetDir = Path.Combine(_pluginDirectory, pluginId, "data");
                    
                    Directory.CreateDirectory(targetDir);
                    CopyDirectory(pluginDataDir, targetDir);
                    
                    result.RestoredPlugins.Add(pluginId);
                }
            }

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"导入失败: {ex.Message}");
            return result;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    /// <summary>
    /// Collect current configuration
    /// </summary>
    private async Task<ChimeraConfig> CollectConfigurationAsync()
    {
        var config = new ChimeraConfig
        {
            Version = "0.4.0",
            ExportDate = DateTime.UtcNow,
            InstalledPlugins = new List<InstalledPluginInfo>()
        };

        // Collect installed plugins
        if (Directory.Exists(_pluginDirectory))
        {
            foreach (var pluginDir in Directory.GetDirectories(_pluginDirectory))
            {
                var manifestPath = Path.Combine(pluginDir, "plugin.json");
                if (File.Exists(manifestPath))
                {
                    var json = await File.ReadAllTextAsync(manifestPath);
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(json);
                    if (manifest != null)
                    {
                        config.InstalledPlugins.Add(new InstalledPluginInfo
                        {
                            Id = manifest.Id,
                            Version = manifest.Version,
                            Name = manifest.Name
                        });
                    }
                }
            }
        }

        return config;
    }

    private static bool IsVersionCompatible(string configVersion)
    {
        if (Version.TryParse(configVersion, out var cv) && Version.TryParse("0.4.0", out var currentV))
        {
            return cv.Major == currentV.Major;
        }
        return true;
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
/// Chimera configuration
/// </summary>
public class ChimeraConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.4.0";

    [JsonPropertyName("exportDate")]
    public DateTime ExportDate { get; set; }

    [JsonPropertyName("installedPlugins")]
    public List<InstalledPluginInfo> InstalledPlugins { get; set; } = new();
}

/// <summary>
/// Installed plugin info
/// </summary>
public class InstalledPluginInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Config import result
/// </summary>
public class ConfigImportResult
{
    public bool Success { get; set; }
    public List<string> RestoredPlugins { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
