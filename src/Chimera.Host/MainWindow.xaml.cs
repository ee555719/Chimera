// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using System.Windows.Controls;
using Chimera.Host.Pages;
using Chimera.Host.Pages.DevTools;

namespace Chimera.Host;

public partial class MainWindow : Window
{
    private readonly PluginsPage _pluginsPage;
    private readonly AboutPage _aboutPage;
    private readonly ShortcutEditorPage _shortcutEditorPage;
    private readonly DevToolsPage _devToolsPage;
    private readonly SecurityPage _securityPage;
    private readonly PluginMarketplacePage _marketplacePage;
    private readonly AiAssistantPage _aiAssistantPage;

    public MainWindow()
    {
        InitializeComponent();
        
        _pluginsPage = new PluginsPage();
        _aboutPage = new AboutPage();
        _shortcutEditorPage = new ShortcutEditorPage();
        _devToolsPage = new DevToolsPage();
        _securityPage = new SecurityPage();
        _marketplacePage = new PluginMarketplacePage();
        _aiAssistantPage = new AiAssistantPage();
        
        // Default to Plugins page
        NavigationList.SelectedIndex = 0;
        ContentArea.Content = _pluginsPage;
        StatusText.Text = "就绪";
        VersionText.Text = $"版本 {typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";
        
        // Register keyboard shortcut for DevTools (F12)
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F12)
        {
            // Open DevTools
            NavigationList.SelectedItem = NavigationList.Items
                .OfType<ListViewItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == "DevTools");
        }
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavigationList.SelectedItem is ListViewItem item)
        {
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "Plugins":
                    ContentArea.Content = _pluginsPage;
                    StatusText.Text = "已安装插件";
                    break;
                case "Marketplace":
                    ContentArea.Content = _marketplacePage;
                    StatusText.Text = "插件市场";
                    break;
                case "AiAssistant":
                    ContentArea.Content = _aiAssistantPage;
                    StatusText.Text = "AI 助手";
                    break;
                case "Shortcuts":
                    ContentArea.Content = _shortcutEditorPage;
                    StatusText.Text = "快捷键编辑器";
                    break;
                case "DevTools":
                    ContentArea.Content = _devToolsPage;
                    StatusText.Text = "插件调试器";
                    break;
                case "Security":
                    ContentArea.Content = _securityPage;
                    StatusText.Text = "安全管理";
                    break;
                case "About":
                    ContentArea.Content = _aboutPage;
                    StatusText.Text = "关于";
                    break;
            }
        }
    }
}
