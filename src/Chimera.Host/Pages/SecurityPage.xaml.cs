// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using System.Windows.Controls;
using Chimera.Host.Services;

namespace Chimera.Host.Pages;

public partial class SecurityPage : UserControl
{
    private readonly PermissionManagerService _permissionService;

    public SecurityPage()
    {
        InitializeComponent();
        _permissionService = new PermissionManagerService();
        Loaded += SecurityPage_Loaded;
    }

    private async void SecurityPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPermissionsAsync();
    }

    private async Task LoadPermissionsAsync()
    {
        try
        {
            var allPermissions = await _permissionService.GetAllPermissionsAsync();
            var permissionList = allPermissions.Select(kvp => new PermissionDisplayInfo
            {
                PluginId = kvp.Key,
                Permissions = kvp.Value,
                GrantedAt = DateTime.Now // Would need to get from config
            }).ToList();

            PermissionsList.ItemsSource = permissionList;
            StatusText.Text = $"共 {permissionList.Count} 个插件有权限配置";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载权限失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadPermissionsAsync();
    }

    private async void RevokePermission_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PermissionDisplayInfo permissionInfo)
        {
            var result = MessageBox.Show($"确定要撤销插件 {permissionInfo.PluginId} 的所有权限吗？", 
                "确认撤销", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _permissionService.RevokeAllPluginPermissionsAsync(permissionInfo.PluginId);
                    await LoadPermissionsAsync();
                    StatusText.Text = $"已撤销插件 {permissionInfo.PluginId} 的所有权限";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"撤销权限失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

public class PermissionDisplayInfo
{
    public string PluginId { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public DateTime GrantedAt { get; set; }
}
