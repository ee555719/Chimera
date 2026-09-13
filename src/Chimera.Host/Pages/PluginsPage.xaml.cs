// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Chimera.Abstractions.Models;
using Chimera.PluginLoader;

namespace Chimera.Host.Pages;

public partial class PluginsPage : UserControl
{
    private const string PluginsDirectory = "plugins";
    private bool _needsRestart;
    private readonly EmbeddablePythonManager _pythonManager;

    public PluginsPage()
    {
        InitializeComponent();
        _pythonManager = new EmbeddablePythonManager();
        Loaded += PluginsPage_Loaded;
    }

    private void PluginsPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPlugins();
    }

    private void LoadPlugins()
    {
        var plugins = new List<PluginInfo>();
        
        if (Directory.Exists(PluginsDirectory))
        {
            foreach (var pluginDir in Directory.GetDirectories(PluginsDirectory))
            {
                var manifestPath = Path.Combine(pluginDir, "plugin.json");
                if (File.Exists(manifestPath))
                {
                    try
                    {
                        var json = File.ReadAllText(manifestPath);
                        var manifest = JsonSerializer.Deserialize<PluginManifest>(json);
                        if (manifest != null)
                        {
                            plugins.Add(new PluginInfo
                            {
                                Id = manifest.Id,
                                Name = manifest.Name,
                                Version = manifest.Version,
                                Author = manifest.Author,
                                Status = "已加载",
                                IsEnabled = true,
                                Runtime = manifest.Runtime ?? "dotnet"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error
                    }
                }
            }
        }

        PluginListView.ItemsSource = plugins;
        FooterInfo.Text = $"共 {plugins.Count} 个插件";
        
        // Show restart button if needed
        RestartButton.Visibility = _needsRestart ? Visibility.Visible : Visibility.Collapsed;
    }

    private void InstallPlugin_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ZIP 文件 (*.zip)|*.zip",
            Title = "选择要安装的插件"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                InstallPluginFromZip(dialog.FileName);
                _needsRestart = true;
                RestartButton.Visibility = Visibility.Visible;
                
                var result = MessageBox.Show(
                    "插件安装成功！需要重启应用才能生效。\n\n是否立即重启？",
                    "安装成功",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                
                if (result == MessageBoxResult.Yes)
                {
                    RestartApplication();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void InstallPluginFromZip(string zipPath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "chimera_plugin_" + Guid.NewGuid().ToString("N")[..8]);
        
        try
        {
            // Extract zip to temp directory
            ZipFile.ExtractToDirectory(zipPath, tempDir);
            
            // Find plugin.json
            var manifestPath = FindFileRecursive(tempDir, "plugin.json");
            if (manifestPath == null)
            {
                throw new InvalidOperationException("ZIP 文件中未找到 plugin.json");
            }
            
            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);
            if (manifest == null)
            {
                throw new InvalidOperationException("无法解析 plugin.json");
            }
            
            // Check if this is a Python plugin
            var isPythonPlugin = manifest.Runtime?.Equals("python", StringComparison.OrdinalIgnoreCase) == true;
            
            // Move to plugins directory
            var pluginDir = Path.Combine(PluginsDirectory, manifest.Id);
            if (Directory.Exists(pluginDir))
            {
                Directory.Delete(pluginDir, true);
            }
            
            // Copy all files from the extracted directory (which might be in a subdirectory)
            var sourceDir = Path.GetDirectoryName(manifestPath)!;
            CopyDirectory(sourceDir, pluginDir);
            
            // If Python plugin, install dependencies
            if (isPythonPlugin)
            {
                InstallPythonDependencies(pluginDir);
            }
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    private async void InstallPythonDependencies(string pluginDir)
    {
        var libraryFile = Path.Combine(pluginDir, "library.txt");
        if (!File.Exists(libraryFile))
        {
            return;
        }
        
        // Ensure Python is available
        if (!_pythonManager.IsPythonInstalled())
        {
            var result = MessageBox.Show(
                "Python 插件需要 Python 运行时。是否自动安装 Embeddable Python？",
                "Python 未找到",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    FooterInfo.Text = "正在下载 Python...";
                    await _pythonManager.InstallPythonAsync();
                    FooterInfo.Text = "Python 安装完成";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"安装 Python 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                return;
            }
        }
        
        // Install dependencies
        try
        {
            FooterInfo.Text = "正在安装 Python 依赖...";
            await _pythonManager.InstallDependenciesAsync(libraryFile, new Progress<string>(line =>
            {
                FooterInfo.Text = line;
            }));
            FooterInfo.Text = "依赖安装完成";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"安装依赖失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? FindFileRecursive(string directory, string fileName)
    {
        var files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
        return files.Length > 0 ? files[0] : null;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }
        
        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }

    private void Restart_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定要重启应用吗？",
            "确认重启",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            RestartApplication();
        }
    }

    private void RestartApplication()
    {
        var exePath = Environment.ProcessPath;
        if (exePath != null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }
        
        Application.Current.Shutdown();
    }

    private void Pack_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "可执行文件 (*.exe)|*.exe",
            FileName = "Chimera.exe",
            Title = "保存打包后的文件"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                PackApplication(dialog.FileName);
                MessageBox.Show(
                    $"打包成功！\n\n文件已保存到：{dialog.FileName}",
                    "打包完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打包失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PackApplication(string outputPath)
    {
        // Create a distribution manifest
        var manifest = new DistributionManifest
        {
            Id = "custom.distribution",
            Name = "Custom Distribution",
            Version = "1.0.0",
            Window = new WindowSettings
            {
                Title = "Chimera",
                Width = 900,
                Height = 600
            }
        };
        
        // Collect installed plugins
        if (Directory.Exists(PluginsDirectory))
        {
            foreach (var pluginDir in Directory.GetDirectories(PluginsDirectory))
            {
                var manifestPath = Path.Combine(pluginDir, "plugin.json");
                if (File.Exists(manifestPath))
                {
                    var json = File.ReadAllText(manifestPath);
                    var pluginManifest = JsonSerializer.Deserialize<PluginManifest>(json);
                    if (pluginManifest != null)
                    {
                        manifest.Plugins.Add(pluginManifest.Id);
                    }
                }
            }
        }
        
        // Create output directory
        var outputDir = Path.Combine(Path.GetTempPath(), "chimera_pack_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(outputDir);
        
        try
        {
            // Copy host executable
            var hostExe = Environment.ProcessPath;
            if (hostExe != null)
            {
                File.Copy(hostExe, Path.Combine(outputDir, "Chimera.exe"));
            }
            
            // Copy plugins directory
            if (Directory.Exists(PluginsDirectory))
            {
                CopyDirectory(PluginsDirectory, Path.Combine(outputDir, PluginsDirectory));
            }
            
            // Write manifest
            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(outputDir, "distribution.json"), manifestJson);
            
            // Create zip
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            ZipFile.CreateFromDirectory(outputDir, outputPath);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                try { Directory.Delete(outputDir, true); } catch { }
            }
        }
    }

    private void UninstallPlugin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pluginId)
        {
            var result = MessageBox.Show($"确定要卸载插件 {pluginId} 吗？", "确认卸载",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var pluginDir = Path.Combine(PluginsDirectory, pluginId);
                    if (Directory.Exists(pluginDir))
                    {
                        Directory.Delete(pluginDir, true);
                    }
                    
                    _needsRestart = true;
                    RestartButton.Visibility = Visibility.Visible;
                    LoadPlugins();
                    
                    MessageBox.Show(
                        "插件已卸载。需要重启应用才能完全生效。",
                        "卸载成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"卸载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void TogglePlugin_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox && checkbox.Tag is string pluginId)
        {
            // TODO: Enable the plugin
            _needsRestart = true;
            RestartButton.Visibility = Visibility.Visible;
        }
    }

    private void TogglePlugin_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox && checkbox.Tag is string pluginId)
        {
            // TODO: Disable the plugin
            _needsRestart = true;
            RestartButton.Visibility = Visibility.Visible;
        }
    }
}

public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Status { get; set; } = "已加载";
    public bool IsEnabled { get; set; } = true;
    public string Runtime { get; set; } = "dotnet";
}
