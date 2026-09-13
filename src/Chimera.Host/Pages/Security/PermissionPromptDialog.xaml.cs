// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using Chimera.Abstractions.Models;

namespace Chimera.Host.Pages.Security;

public partial class PermissionPromptDialog : Window
{
    public bool IsGranted { get; private set; }
    public bool RememberChoice { get; private set; }

    public PermissionPromptDialog(string pluginId, string permission)
    {
        InitializeComponent();

        PluginInfo.Text = $"插件 ID: {pluginId}";
        PermissionName.Text = GetPermissionDisplayName(permission);
        PermissionDescription.Text = GetPermissionDescription(permission);
    }

    private void AllowButton_Click(object sender, RoutedEventArgs e)
    {
        IsGranted = true;
        RememberChoice = RememberChoiceCheckBox.IsChecked == true;
        DialogResult = true;
    }

    private void DenyButton_Click(object sender, RoutedEventArgs e)
    {
        IsGranted = false;
        RememberChoice = RememberChoiceCheckBox.IsChecked == true;
        DialogResult = false;
    }

    private static string GetPermissionDisplayName(string permission)
    {
        return permission switch
        {
            PermissionConstants.FilesystemRead => "文件系统读取",
            PermissionConstants.FilesystemWrite => "文件系统写入",
            PermissionConstants.Network => "网络访问",
            PermissionConstants.ProcessSpawn => "进程启动",
            PermissionConstants.RegistryRead => "注册表读取",
            PermissionConstants.RegistryWrite => "注册表写入",
            PermissionConstants.Clipboard => "剪贴板访问",
            PermissionConstants.Notifications => "通知",
            _ => permission
        };
    }

    private static string GetPermissionDescription(string permission)
    {
        return permission switch
        {
            PermissionConstants.FilesystemRead => "允许插件读取文件和目录。请确保您信任该插件的开发者。",
            PermissionConstants.FilesystemWrite => "允许插件创建、修改或删除文件和目录。此权限具有较高风险。",
            PermissionConstants.Network => "允许插件访问互联网。插件可能发送或接收数据。",
            PermissionConstants.ProcessSpawn => "允许插件启动外部进程。此权限具有较高安全风险。",
            PermissionConstants.RegistryRead => "允许插件读取 Windows 注册表。",
            PermissionConstants.RegistryWrite => "允许插件修改 Windows 注册表。此权限具有较高风险。",
            PermissionConstants.Clipboard => "允许插件访问剪贴板内容，包括复制的文本和图像。",
            PermissionConstants.Notifications => "允许插件显示系统通知。",
            _ => $"请求权限: {permission}"
        };
    }
}
