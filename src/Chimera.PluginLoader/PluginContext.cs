// SPDX-License-Identifier: AGPL-3.0-or-later
using Chimera.Abstractions.Interfaces;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

public class PluginContext : IPluginContext
{
    public string PluginId { get; }
    public string PluginDirectory { get; }
    public string DataDirectory { get; }
    public string LogDirectory { get; }
    public IPluginLogger Logger { get; }
    public IPluginConfiguration Configuration { get; }
    public string HostVersion { get; }

    public PluginContext(PluginManifest manifest, string pluginDirectory, string? hostVersion = null)
    {
        PluginId = manifest.Id;
        PluginDirectory = pluginDirectory;
        DataDirectory = Path.Combine(pluginDirectory, "data");
        LogDirectory = Path.Combine(pluginDirectory, "logs");
        Logger = new PluginLogger(manifest.Id);
        Configuration = new PluginConfiguration(manifest.Id);
        HostVersion = hostVersion ?? "1.0.0";

        // Ensure directories exist
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
