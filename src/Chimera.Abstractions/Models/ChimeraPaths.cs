// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Models;

/// <summary>
/// Application configuration paths
/// </summary>
public static class ChimeraPaths
{
    /// <summary>
    /// Base installation directory
    /// </summary>
    public static readonly string BaseDirectory = @"D:\Chimera";
    
    /// <summary>
    /// Plugin storage directory
    /// </summary>
    public static readonly string PluginDirectory = @"D:\ChimeraPlugin";
    
    /// <summary>
    /// Python runtime directory
    /// </summary>
    public static readonly string PythonDirectory = @"D:\Chimera\python";
    
    /// <summary>
    /// Configuration directory
    /// </summary>
    public static readonly string ConfigDirectory = @"D:\Chimera\config";
    
    /// <summary>
    /// Logs directory
    /// </summary>
    public static readonly string LogsDirectory = @"D:\Chimera\logs";
    
    /// <summary>
    /// Temp directory
    /// </summary>
    public static readonly string TempDirectory = @"D:\Chimera\temp";

    /// <summary>
    /// Ensure all directories exist
    /// </summary>
    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(PluginDirectory);
        Directory.CreateDirectory(PythonDirectory);
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(TempDirectory);
    }

    /// <summary>
    /// Get plugin directory for a specific plugin
    /// </summary>
    public static string GetPluginPath(string pluginId)
    {
        return Path.Combine(PluginDirectory, pluginId);
    }
}
