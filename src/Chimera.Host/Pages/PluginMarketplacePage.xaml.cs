// SPDX-License-Identifier: AGPL-3.0-or-later
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chimera.Abstractions.Models;
using Chimera.Host.Services;
using Chimera.PluginLoader;

namespace Chimera.Host.Pages;

public partial class PluginMarketplacePage : UserControl
{
    private readonly PluginStoreService _storeService;
    private readonly PluginAutoUpdateService _autoUpdateService;
    private readonly PermissionManagerService _permissionService;
    
    private List<PluginStoreEntry> _allPlugins = new();
    private int _currentPage = 1;
    private const int PageSize = 20;
    private string? _currentSearch;

    public PluginMarketplacePage()
    {
        InitializeComponent();
        
        _storeService = new PluginStoreService();
        _permissionService = new PermissionManagerService();
        _autoUpdateService = new PluginAutoUpdateService(
            ChimeraPaths.PluginDirectory,
            _storeService,
            permissionManager: _permissionService.PermissionManager,
            auditLogger: _permissionService.AuditLogger);
        
        Loaded += PluginMarketplacePage_Loaded;
    }

    private async void PluginMarketplacePage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPluginsAsync();
    }

    private async Task LoadPluginsAsync()
    {
        try
        {
            StatusText.Text = "正在加载插件...";
            
            _allPlugins = await _storeService.FetchPluginsAsync(page: 1, pageSize: 100, search: _currentSearch);
            _currentPage = 1;
            
            UpdateDisplayedPlugins();
            StatusText.Text = $"共 {_allPlugins.Count} 个插件";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载插件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "加载失败";
        }
    }

    private void UpdateDisplayedPlugins()
    {
        var displayedPlugins = _allPlugins
            .Skip((_currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        
        PluginsList.ItemsSource = displayedPlugins;
        PageInfo.Text = $"第 {_currentPage} 页 / 共 {Math.Ceiling((double)_allPlugins.Count / PageSize)} 页";
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        _currentSearch = SearchBox.Text;
        await LoadPluginsAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _currentSearch = SearchBox.Text;
            await LoadPluginsAsync();
        }
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "正在检查更新...";
            var updates = await _autoUpdateService.CheckForUpdatesAsync();
            
            if (updates.Any())
            {
                var message = $"发现 {updates.Count} 个插件有更新:\n" +
                             string.Join("\n", updates.Select(u => $"• {u.PluginId}: {u.CurrentVersion} → {u.AvailableVersion}"));
                
                var result = MessageBox.Show(message, "检查更新", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    await UpdateAllPluginsAsync(updates);
                }
            }
            else
            {
                MessageBox.Show("所有插件已是最新版本", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
            StatusText.Text = "更新检查完成";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"检查更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void InstallPlugin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PluginStoreEntry plugin)
        {
            try
            {
                StatusText.Text = $"正在安装 {plugin.Name}...";
                
                // Check if plugin requires permissions
                var manifest = await GetPluginManifestAsync(plugin);
                if (manifest != null && manifest.Permissions.Any())
                {
                    var permissionMessage = $"该插件需要以下权限:\n" +
                                           string.Join("\n", manifest.Permissions) +
                                           "\n\n是否继续安装?";
                    
                    var result = MessageBox.Show(permissionMessage, "权限请求", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        StatusText.Text = "安装已取消";
                        return;
                    }
                }

                // Download plugin
                var zipPath = await _storeService.DownloadPluginAsync(plugin);
                
                // Install plugin
                var installDir = Path.Combine(ChimeraPaths.PluginDirectory, plugin.Id);
                if (Directory.Exists(installDir))
                {
                    Directory.Delete(installDir, true);
                }
                
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, installDir);
                
                // Clean up temp file
                try { File.Delete(zipPath); } catch { }
                
                StatusText.Text = $"{plugin.Name} 安装成功";
                MessageBox.Show($"{plugin.Name} 安装成功！", "安装成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安装失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "安装失败";
            }
        }
    }

    private async Task UpdateAllPluginsAsync(List<PluginUpdateInfo> updates)
    {
        foreach (var update in updates)
        {
            try
            {
                StatusText.Text = $"正在更新 {update.PluginId}...";
                await _autoUpdateService.UpdatePluginAsync(update.PluginId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新 {update.PluginId} 失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        StatusText.Text = "批量更新完成";
        MessageBox.Show("所有插件更新完成！", "更新完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task<PluginManifest?> GetPluginManifestAsync(PluginStoreEntry plugin)
    {
        try
        {
            // Try to get manifest from store or download
            var plugins = await _storeService.FetchPluginsAsync(search: plugin.Id);
            var storePlugin = plugins.FirstOrDefault(p => p.Id == plugin.Id);
            
            if (storePlugin != null)
            {
                // For now, create a basic manifest
                return new PluginManifest
                {
                    Id = storePlugin.Id,
                    Name = storePlugin.Name,
                    Version = storePlugin.Version,
                    Author = storePlugin.Author,
                    Permissions = new List<string>() // Would need to get from store
                };
            }
        }
        catch
        {
            // Ignore errors
        }
        
        return null;
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            UpdateDisplayedPlugins();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = (int)Math.Ceiling((double)_allPlugins.Count / PageSize);
        if (_currentPage < totalPages)
        {
            _currentPage++;
            UpdateDisplayedPlugins();
        }
    }
}
