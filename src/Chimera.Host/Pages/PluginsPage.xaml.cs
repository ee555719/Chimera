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
    private bool _needsRestart;
    private readonly EmbeddablePythonManager _pythonManager;
    private readonly PluginStoreService _storeService;
    private readonly PluginDependencyResolver _dependencyResolver;
    private readonly PluginVersionManager _versionManager;
    private readonly ConfigManager _configManager;

    public PluginsPage()
    {
        InitializeComponent();
        _pythonManager = new EmbeddablePythonManager(ChimeraPaths.PythonDirectory);
        _storeService = new PluginStoreService();
        _dependencyResolver = new PluginDependencyResolver();
        _versionManager = new PluginVersionManager();
        _configManager = new ConfigManager();
        Loaded += PluginsPage_Loaded;
    }

    private void PluginsPage_Loaded(object sender, RoutedEventArgs e)
    {
        ChimeraPaths.EnsureDirectories();
        LoadPlugins();
    }

    private void LoadPlugins()
    {
        var plugins = new List<PluginInfo>();
        
        if (Directory.Exists(ChimeraPaths.PluginDirectory))
        {
            foreach (var pluginDir in Directory.GetDirectories(ChimeraPaths.PluginDirectory))
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
                    catch
                    {
                        // Log error
                    }
                }
            }
        }

        PluginListView.ItemsSource = plugins;
        FooterInfo.Text = $"共 {plugins.Count} 个插件 | 插件目录: {ChimeraPaths.PluginDirectory}";
        
        // Show restart button if needed
        RestartButton.Visibility = _needsRestart ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void LoadMarketPlugins()
    {
        try
        {
            FooterInfo.Text = "正在加载插件市场...";
            var plugins = await _storeService.FetchPluginsAsync();
            MarketListView.ItemsSource = plugins;
            FooterInfo.Text = $"市场中共 {plugins.Count} 个插件";
        }
        catch (Exception ex)
        {
            FooterInfo.Text = $"加载市场失败: {ex.Message}";
        }
    }

    private void TabInstalled_Checked(object sender, RoutedEventArgs e)
    {
        if (PluginListView != null && MarketPanel != null)
        {
            PluginListView.Visibility = Visibility.Visible;
            MarketPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void TabMarket_Checked(object sender, RoutedEventArgs e)
    {
        if (PluginListView != null && MarketPanel != null)
        {
            PluginListView.Visibility = Visibility.Collapsed;
            MarketPanel.Visibility = Visibility.Visible;
            LoadMarketPlugins();
        }
    }

    private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            Search_Click(sender, e);
        }
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            FooterInfo.Text = "正在搜索...";
            var search = SearchBox.Text.Trim();
            var plugins = await _storeService.FetchPluginsAsync(search: string.IsNullOrEmpty(search) ? null : search);
            MarketListView.ItemsSource = plugins;
            FooterInfo.Text = $"找到 {plugins.Count} 个插件";
        }
        catch (Exception ex)
        {
            FooterInfo.Text = $"搜索失败: {ex.Message}";
        }
    }

    private async void InstallFromMarket_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pluginId)
        {
            try
            {
                // Check dependencies first
                var plugins = await _storeService.FetchPluginsAsync();
                var plugin = plugins.FirstOrDefault(p => p.Id == pluginId);
                
                if (plugin == null)
                {
                    MessageBox.Show("插件未找到", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Check for circular dependencies
                if (_dependencyResolver.HasCircularDependency(pluginId, plugins))
                {
                    MessageBox.Show("检测到循环依赖，无法安装", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Resolve dependencies
                var resolution = _dependencyResolver.Resolve(pluginId, plugins);
                if (!resolution.Success)
                {
                    MessageBox.Show($"依赖解析失败:\n{string.Join("\n", resolution.Errors)}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Download and install
                foreach (var pluginToInstall in resolution.ResolvedPlugins)
                {
                    FooterInfo.Text = $"正在下载 {pluginToInstall.Name}...";
                    var zipPath = await _storeService.DownloadPluginAsync(pluginToInstall);
                    
                    FooterInfo.Text = $"正在安装 {pluginToInstall.Name}...";
                    InstallPluginFromZip(zipPath);
                    
                    // Cleanup
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }
                }

                _needsRestart = true;
                RestartButton.Visibility = Visibility.Visible;
                FooterInfo.Text = "插件安装完成";
                
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
                FooterInfo.Text = $"安装失败: {ex.Message}";
                MessageBox.Show($"安装失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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
                FooterInfo.Text = "正在安装插件...";
                InstallPluginFromZip(dialog.FileName);
                _needsRestart = true;
                RestartButton.Visibility = Visibility.Visible;
                FooterInfo.Text = "插件安装完成";
                
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
                FooterInfo.Text = $"安装失败: {ex.Message}";
                MessageBox.Show($"安装失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void InstallPluginFromZip(string zipPath)
    {
        var tempDir = Path.Combine(ChimeraPaths.TempDirectory, "plugin_" + Guid.NewGuid().ToString("N")[..8]);
        
        try
        {
            ZipFile.ExtractToDirectory(zipPath, tempDir);
            
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
            
            var isPythonPlugin = manifest.Runtime?.Equals("python", StringComparison.OrdinalIgnoreCase) == true;
            
            if (isPythonPlugin)
            {
                var mainPyPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "main", "main.py");
                if (!File.Exists(mainPyPath))
                {
                    throw new InvalidOperationException("Python 插件必须包含 main/main.py 入口文件");
                }
            }
            
            var pluginDir = ChimeraPaths.GetPluginPath(manifest.Id);
            if (Directory.Exists(pluginDir))
            {
                // Save version before update for rollback
                _versionManager.SaveVersionAsync(manifest.Id).GetAwaiter().GetResult();
                Directory.Delete(pluginDir, true);
            }
            
            var sourceDir = Path.GetDirectoryName(manifestPath)!;
            CopyDirectory(sourceDir, pluginDir);
            
            if (isPythonPlugin)
            {
                InstallPythonDependencies(pluginDir);
            }
        }
        finally
        {
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
        
        if (!_pythonManager.IsPythonInstalled())
        {
            FooterInfo.Text = "正在安装 Python 运行时...";
            try
            {
                await _pythonManager.InstallPythonAsync();
                FooterInfo.Text = "Python 安装完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装 Python 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        
        try
        {
            FooterInfo.Text = "正在安装 Python 依赖...";
            await _pythonManager.InstallDependenciesAsync(libraryFile, new Progress<string>(line =>
            {
                Dispatcher.Invoke(() => FooterInfo.Text = line);
            }));
            FooterInfo.Text = "依赖安装完成";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"安装依赖失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RollbackPlugin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pluginId)
        {
            var availableVersions = await _versionManager.GetAvailableVersionsAsync(pluginId);
            
            if (availableVersions.Count == 0)
            {
                MessageBox.Show("没有可用的历史版本进行回滚", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var versionList = string.Join("\n", availableVersions.Select(v => $"{v.Version} ({v.Timestamp:yyyy-MM-dd HH:mm})"));
            var result = MessageBox.Show(
                $"可用的历史版本:\n{versionList}\n\n确定要回滚到上一个版本吗？",
                "版本回滚",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    FooterInfo.Text = "正在回滚...";
                    await _versionManager.RollbackAsync(pluginId);
                    _needsRestart = true;
                    RestartButton.Visibility = Visibility.Visible;
                    LoadPlugins();
                    FooterInfo.Text = "回滚完成";
                    
                    MessageBox.Show("回滚完成，需要重启应用才能生效", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    FooterInfo.Text = $"回滚失败: {ex.Message}";
                    MessageBox.Show($"回滚失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Chimera 配置文件 (*.chimera-config)|*.chimera-config",
            FileName = $"chimera-backup-{DateTime.Now:yyyyMMdd-HHmmss}.chimera-config",
            Title = "导出配置"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                FooterInfo.Text = "正在导出配置...";
                await _configManager.ExportAsync(dialog.FileName);
                FooterInfo.Text = "导出完成";
                MessageBox.Show($"配置已导出到:\n{dialog.FileName}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                FooterInfo.Text = $"导出失败: {ex.Message}";
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Chimera 配置文件 (*.chimera-config)|*.chimera-config",
            Title = "导入配置"
        };

        if (dialog.ShowDialog() == true)
        {
            var result = MessageBox.Show(
                "导入配置将恢复插件数据。当前数据可能会被覆盖。\n\n是否继续？",
                "确认导入",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    FooterInfo.Text = "正在导入配置...";
                    var importResult = await _configManager.ImportAsync(dialog.FileName);
                    
                    if (importResult.Success)
                    {
                        _needsRestart = true;
                        RestartButton.Visibility = Visibility.Visible;
                        LoadPlugins();
                        FooterInfo.Text = "导入完成";
                        
                        var message = $"配置导入成功！\n\n已恢复插件: {string.Join(", ", importResult.RestoredPlugins)}";
                        if (importResult.Warnings.Count > 0)
                        {
                            message += $"\n\n警告:\n{string.Join("\n", importResult.Warnings)}";
                        }
                        
                        MessageBox.Show(message, "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"导入失败:\n{string.Join("\n", importResult.Errors)}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    FooterInfo.Text = $"导入失败: {ex.Message}";
                    MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
                UseShellExecute = true,
                WorkingDirectory = ChimeraPaths.BaseDirectory
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
                FooterInfo.Text = "正在打包...";
                PackApplication(dialog.FileName);
                FooterInfo.Text = "打包完成";
                MessageBox.Show(
                    $"打包成功！\n\n文件已保存到：{dialog.FileName}",
                    "打包完成",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                FooterInfo.Text = $"打包失败: {ex.Message}";
                MessageBox.Show($"打包失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PackApplication(string outputPath)
    {
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
        
        if (Directory.Exists(ChimeraPaths.PluginDirectory))
        {
            foreach (var pluginDir in Directory.GetDirectories(ChimeraPaths.PluginDirectory))
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
        
        var outputDir = Path.Combine(ChimeraPaths.TempDirectory, "pack_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(outputDir);
        
        try
        {
            var hostExe = Environment.ProcessPath;
            if (hostExe != null)
            {
                File.Copy(hostExe, Path.Combine(outputDir, "Chimera.exe"));
            }
            
            var pluginsDir = Path.Combine(outputDir, "plugins");
            if (Directory.Exists(ChimeraPaths.PluginDirectory))
            {
                CopyDirectory(ChimeraPaths.PluginDirectory, pluginsDir);
            }
            
            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(outputDir, "distribution.json"), manifestJson);
            
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

    private async void UninstallPlugin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pluginId)
        {
            var result = MessageBox.Show($"确定要卸载插件 {pluginId} 吗？", "确认卸载",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var pluginDir = ChimeraPaths.GetPluginPath(pluginId);
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
            _needsRestart = true;
            RestartButton.Visibility = Visibility.Visible;
        }
    }

    private void TogglePlugin_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox && checkbox.Tag is string pluginId)
        {
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
