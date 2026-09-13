// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

/// <summary>
/// Handles the installer behavior when Chimera.exe is first run
/// </summary>
public class ChimeraInstaller : IDisposable
{
    private readonly EmbeddablePythonManager _pythonManager;
    
    private const string PayloadMagic = "CHIMBOOT1";
    private const string DistributionEnvVar = "CHIMERA_DIST_DIR";
    private const string DistributionIdEnvVar = "CHIMERA_DIST_ID";

    public ChimeraInstaller()
    {
        _pythonManager = new EmbeddablePythonManager(ChimeraPaths.PythonDirectory);
    }

    /// <summary>
    /// Check if this is the first run and perform installation if needed
    /// </summary>
    public async Task<bool> CheckAndInstallAsync(IProgress<string>? progress = null)
    {
        // Check if already installed
        if (IsInstalled())
        {
            return false;
        }

        progress?.Report("首次运行，正在安装 Chimera...");

        // Ensure directories exist
        ChimeraPaths.EnsureDirectories();

        // Extract payload
        progress?.Report("正在解压程序文件...");
        ExtractPayload();

        // Install Python
        progress?.Report("正在检查 Python 环境...");
        await EnsurePythonInstalledAsync(progress);

        // Copy built-in plugins
        progress?.Report("正在安装内置插件...");
        CopyBuiltinPlugins();

        progress?.Report("安装完成！");

        return true;
    }

    /// <summary>
    /// Check if Chimera is already installed
    /// </summary>
    public bool IsInstalled()
    {
        // Check if running from D:\Chimera
        var exePath = Environment.ProcessPath;
        if (exePath != null && exePath.StartsWith(ChimeraPaths.BaseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if D:\Chimera exists and has the host executable
        var hostExe = Path.Combine(ChimeraPaths.BaseDirectory, "Chimera.Host.exe");
        return File.Exists(hostExe);
    }

    /// <summary>
    /// Extract payload from the executable
    /// </summary>
    private void ExtractPayload()
    {
        var exePath = Environment.ProcessPath;
        if (exePath == null)
        {
            throw new InvalidOperationException("无法获取可执行文件路径");
        }

        using var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);

        // Read the footer: [magic][payload length]
        fs.Seek(-8 - PayloadMagic.Length, SeekOrigin.End);
        var magic = new string(reader.ReadChars(PayloadMagic.Length));
        
        if (magic != PayloadMagic)
        {
            throw new InvalidOperationException("无效的 Chimera 可执行文件格式");
        }

        var payloadLength = reader.ReadInt64();
        
        // Read payload
        fs.Seek(-(8 + PayloadMagic.Length + payloadLength), SeekOrigin.Current);
        var payload = reader.ReadBytes((int)payloadLength);

        // Extract to temp directory first
        var tempZip = Path.Combine(ChimeraPaths.TempDirectory, "payload.zip");
        Directory.CreateDirectory(ChimeraPaths.TempDirectory);
        File.WriteAllBytes(tempZip, payload);

        // Extract to base directory
        ZipFile.ExtractToDirectory(tempZip, ChimeraPaths.BaseDirectory, true);

        // Cleanup
        File.Delete(tempZip);
    }

    /// <summary>
    /// Ensure Python is installed
    /// </summary>
    private async Task EnsurePythonInstalledAsync(IProgress<string>? progress = null)
    {
        if (!_pythonManager.IsPythonInstalled())
        {
            progress?.Report("正在下载并安装 Python...");
            await _pythonManager.InstallPythonAsync();
        }
        else
        {
            progress?.Report("Python 已安装");
        }
    }

    /// <summary>
    /// Copy built-in plugins from the extracted payload
    /// </summary>
    private void CopyBuiltinPlugins()
    {
        var pluginsSource = Path.Combine(ChimeraPaths.BaseDirectory, "plugins");
        if (Directory.Exists(pluginsSource))
        {
            CopyDirectory(pluginsSource, ChimeraPaths.PluginDirectory);
        }
    }

    /// <summary>
    /// Start the main host application
    /// </summary>
    public void StartHost(string[] args)
    {
        var hostExe = Path.Combine(ChimeraPaths.BaseDirectory, "Chimera.Host.exe");
        if (!File.Exists(hostExe))
        {
            throw new FileNotFoundException($"宿主程序未找到: {hostExe}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = hostExe,
            Arguments = string.Join(" ", args),
            UseShellExecute = true,
            WorkingDirectory = ChimeraPaths.BaseDirectory
        };

        Process.Start(startInfo);
    }

    /// <summary>
    /// Start the main host application and wait for it to exit
    /// </summary>
    public int StartHostAndWait(string[] args)
    {
        var hostExe = Path.Combine(ChimeraPaths.BaseDirectory, "Chimera.Host.exe");
        if (!File.Exists(hostExe))
        {
            throw new FileNotFoundException($"宿主程序未找到: {hostExe}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = hostExe,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            WorkingDirectory = ChimeraPaths.BaseDirectory
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("无法启动宿主程序");
        }

        process.WaitForExit();
        return process.ExitCode;
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

    public void Dispose()
    {
        _pythonManager?.Dispose();
        GC.SuppressFinalize(this);
    }
}
