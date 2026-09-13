// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface IPluginContext
{
    string PluginId { get; }
    string PluginDirectory { get; }
    string DataDirectory { get; }
    string LogDirectory { get; }
    IPluginLogger Logger { get; }
    IPluginConfiguration Configuration { get; }
    string HostVersion { get; }
}
