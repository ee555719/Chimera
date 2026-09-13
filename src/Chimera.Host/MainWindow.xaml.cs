// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using System.Windows.Controls;
using Chimera.Host.Pages;

namespace Chimera.Host;

public partial class MainWindow : Window
{
    private readonly PluginsPage _pluginsPage;
    private readonly AboutPage _aboutPage;

    public MainWindow()
    {
        InitializeComponent();
        
        _pluginsPage = new PluginsPage();
        _aboutPage = new AboutPage();
        
        // Default to Plugins page
        NavigationList.SelectedIndex = 0;
        ContentArea.Content = _pluginsPage;
        StatusText.Text = "就绪";
        VersionText.Text = $"版本 {typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";
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
                    StatusText.Text = "安装插件";
                    break;
                case "About":
                    ContentArea.Content = _aboutPage;
                    StatusText.Text = "关于";
                    break;
            }
        }
    }
}
