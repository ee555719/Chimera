// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface ILifecycleHook
{
    Task OnHostStartingAsync();
    Task OnHostStartedAsync();
    Task OnHostStoppingAsync();
    Task OnHostStoppedAsync();
    Task OnPluginLoadedAsync(string pluginId);
    Task OnPluginUnloadedAsync(string pluginId);
}
