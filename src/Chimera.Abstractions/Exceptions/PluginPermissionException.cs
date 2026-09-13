// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Exceptions;

public class PluginPermissionException : Exception
{
    public string PluginId { get; }
    public string RequiredPermission { get; }

    public PluginPermissionException(string pluginId, string requiredPermission)
        : base($"Plugin '{pluginId}' requires permission '{requiredPermission}' which is not granted.")
    {
        PluginId = pluginId;
        RequiredPermission = requiredPermission;
    }

    public PluginPermissionException(string pluginId, string requiredPermission, string message)
        : base(message)
    {
        PluginId = pluginId;
        RequiredPermission = requiredPermission;
    }

    public PluginPermissionException(string pluginId, string requiredPermission, Exception innerException)
        : base($"Plugin '{pluginId}' requires permission '{requiredPermission}' which is not granted.", innerException)
    {
        PluginId = pluginId;
        RequiredPermission = requiredPermission;
    }
}
