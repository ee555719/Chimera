// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Models;

public static class PermissionConstants
{
    public const string FilesystemRead = "filesystem:read";
    public const string FilesystemWrite = "filesystem:write";
    public const string Network = "network";
    public const string ProcessSpawn = "process:spawn";
    public const string RegistryRead = "registry:read";
    public const string RegistryWrite = "registry:write";
    public const string Clipboard = "clipboard";
    public const string Notifications = "notifications";

    public static class Scopes
    {
        public const string Documents = "documents";
        public const string AppData = "appdata";
        public const string Temp = "temp";
    }
}
