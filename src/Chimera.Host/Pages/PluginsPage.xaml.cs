// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Chimera.Host.Pages;

public partial class PluginsPage : UserControl
{
    public PluginsPage()
    {
        InitializeComponent();
        Loaded += PluginsPage_Loaded;
    }

    private void PluginsPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPlugins();
    }

    private void LoadPlugins()
    {
        // TODO: Load plugins from the plugin directory and display them
        var plugins = new List<PluginInfo>();
        
        // Placeholder for now
        PluginListView.ItemsSource = plugins;
        FooterInfo.Text = $"共 {plugins.Count} 个插件";
    }

    private void InstallPlugin_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ZIP 文件 (*.zip)|*.zip|插件目录|目录",
            Title = "选择要安装的插件"
        };

        if (dialog.ShowDialog() == true)
        {
            // TODO: Install the plugin
            MessageBox.Show($"准备安装: {dialog.FileName}", "安装插件", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPlugins();
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
                // TODO: Unload the plugin and remove files
                LoadPlugins();
            }
        }
    }

    private void TogglePlugin_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox && checkbox.Tag is string pluginId)
        {
            // TODO: Enable the plugin
        }
    }

    private void TogglePlugin_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox && checkbox.Tag is string pluginId)
        {
            // TODO: Disable the plugin
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
}
