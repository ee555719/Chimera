// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface IStartupTask
{
    Task ExecuteAsync(IPluginContext context);
}
