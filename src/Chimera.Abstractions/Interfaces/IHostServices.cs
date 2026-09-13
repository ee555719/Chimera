// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Diagnostics;
using System.IO;

namespace Chimera.Abstractions.Interfaces;

public interface IHostServices
{
    // Filesystem operations
    Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    string[] GetFiles(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    string[] GetDirectories(string path, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    void DeleteFile(string path, bool recursive = false);
    void CopyFile(string source, string destination, bool overwrite = false);
    void MoveFile(string source, string destination);

    // Network operations
    HttpClient CreateHttpClient();
    Task<HttpResponseMessage> SendHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    // Process operations
    Process? StartProcess(string fileName, string arguments = "", string? workingDirectory = null);

    // Registry operations (if Windows-specific)
    object? ReadRegistryValue(string keyPath, string valueName);
    void WriteRegistryValue(string keyPath, string valueName, object value);

    // Clipboard operations
    string GetClipboardText();
    void SetClipboardText(string text);

    // Notification operations
    void ShowNotification(string title, string message, NotificationType type = NotificationType.Information);

    // Permission check
    bool HasPermission(string permission);
}

public enum NotificationType
{
    Information,
    Warning,
    Error,
    Success
}
