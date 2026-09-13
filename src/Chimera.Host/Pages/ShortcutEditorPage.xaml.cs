// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Chimera.Abstractions.Models;
using Chimera.PluginLoader;

namespace Chimera.Host.Pages;

public partial class ShortcutEditorPage : UserControl
{
    private readonly KeyboardShortcutManager _shortcutManager;

    public ShortcutEditorPage()
    {
        InitializeComponent();
        _shortcutManager = new KeyboardShortcutManager();
        Loaded += ShortcutEditorPage_Loaded;
    }

    private async void ShortcutEditorPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadShortcutsAsync();
    }

    private async Task LoadShortcutsAsync()
    {
        try
        {
            var shortcuts = await _shortcutManager.GetShortcutsAsync();
            var displayList = shortcuts.Select(s => new ShortcutDisplayInfo
            {
                Id = s.Id,
                Name = s.Name,
                Key = s.Key,
                Modifiers = s.Modifiers,
                ModifiersText = string.Join("+", s.Modifiers),
                PluginId = s.PluginId,
                CommandId = s.CommandId,
                IsEnabled = s.IsEnabled
            }).ToList();
            
            ShortcutsList.ItemsSource = displayList;
            FooterInfo.Text = $"共 {displayList.Count} 个快捷键";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载快捷键失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddShortcut_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ShortcutEditDialog();
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _shortcutManager.RegisterShortcutAsync(dialog.Shortcut);
                await LoadShortcutsAsync();
                FooterInfo.Text = "快捷键添加成功";
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "快捷键冲突", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void EditShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string shortcutId)
        {
            var shortcuts = await _shortcutManager.GetShortcutsAsync();
            var shortcut = shortcuts.FirstOrDefault(s => s.Id == shortcutId);
            
            if (shortcut != null)
            {
                var dialog = new ShortcutEditDialog(shortcut);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        await _shortcutManager.RegisterShortcutAsync(dialog.Shortcut);
                        await LoadShortcutsAsync();
                        FooterInfo.Text = "快捷键更新成功";
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(ex.Message, "快捷键冲突", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    private async void DeleteShortcut_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string shortcutId)
        {
            var result = MessageBox.Show("确定要删除这个快捷键吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _shortcutManager.UnregisterShortcutAsync(shortcutId);
                    await LoadShortcutsAsync();
                    FooterInfo.Text = "快捷键已删除";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void ShortcutEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox && checkbox.Tag is string shortcutId)
        {
            try
            {
                await _shortcutManager.SetEnabledAsync(shortcutId, checkbox.IsChecked == true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新状态失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON 文件 (*.json)|*.json",
            FileName = "keyboard-shortcuts.json",
            Title = "导出快捷键配置"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var config = await _shortcutManager.GetConfigAsync();
                var json = System.Text.Json.JsonSerializer.Serialize(config, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(dialog.FileName, json);
                FooterInfo.Text = "配置已导出";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON 文件 (*.json)|*.json",
            Title = "导入快捷键配置"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(dialog.FileName);
                var config = System.Text.Json.JsonSerializer.Deserialize<KeyboardShortcutConfig>(json);
                if (config != null)
                {
                    await _shortcutManager.SaveConfigAsync(config);
                    await LoadShortcutsAsync();
                    FooterInfo.Text = "配置已导入";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

public class ShortcutDisplayInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public List<string> Modifiers { get; set; } = new();
    public string ModifiersText { get; set; } = string.Empty;
    public string PluginId { get; set; } = string.Empty;
    public string CommandId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Dialog for editing keyboard shortcuts
/// </summary>
public class ShortcutEditDialog : Window
{
    private readonly TextBox _nameBox;
    private readonly TextBox _keyBox;
    private readonly CheckBox _ctrlCheck;
    private readonly CheckBox _altCheck;
    private readonly CheckBox _shiftCheck;
    private readonly CheckBox _winCheck;
    
    public KeyboardShortcut Shortcut { get; private set; }

    public ShortcutEditDialog(KeyboardShortcut? existing = null)
    {
        Title = existing == null ? "添加快捷键" : "编辑快捷键";
        Width = 350;
        Height = 250;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        
        Shortcut = existing ?? new KeyboardShortcut();
        
        var panel = new StackPanel { Margin = new Thickness(16) };
        
        // Name
        panel.Children.Add(new TextBlock { Text = "名称:" });
        _nameBox = new TextBox { Text = Shortcut.Name, Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(_nameBox);
        
        // Key
        panel.Children.Add(new TextBlock { Text = "按键:" });
        _keyBox = new TextBox { Text = Shortcut.Key, Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(_keyBox);
        
        // Modifiers
        panel.Children.Add(new TextBlock { Text = "修饰键:" });
        var modifierPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        
        _ctrlCheck = new CheckBox { Content = "Ctrl", IsChecked = Shortcut.Modifiers.Contains("Ctrl"), Margin = new Thickness(0, 0, 8, 0) };
        _altCheck = new CheckBox { Content = "Alt", IsChecked = Shortcut.Modifiers.Contains("Alt"), Margin = new Thickness(0, 0, 8, 0) };
        _shiftCheck = new CheckBox { Content = "Shift", IsChecked = Shortcut.Modifiers.Contains("Shift"), Margin = new Thickness(0, 0, 8, 0) };
        _winCheck = new CheckBox { Content = "Win", IsChecked = Shortcut.Modifiers.Contains("Win"), Margin = new Thickness(0, 0, 8, 0) };
        
        modifierPanel.Children.Add(_ctrlCheck);
        modifierPanel.Children.Add(_altCheck);
        modifierPanel.Children.Add(_shiftCheck);
        modifierPanel.Children.Add(_winCheck);
        panel.Children.Add(modifierPanel);
        
        // Buttons
        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        
        var okButton = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
        okButton.Click += OkButton_Click;
        
        var cancelButton = new Button { Content = "取消", Width = 80 };
        cancelButton.Click += (s, e) => DialogResult = false;
        
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        panel.Children.Add(buttonPanel);
        
        Content = panel;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            MessageBox.Show("请输入名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(_keyBox.Text))
        {
            MessageBox.Show("请输入按键", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var modifiers = new List<string>();
        if (_ctrlCheck.IsChecked == true) modifiers.Add("Ctrl");
        if (_altCheck.IsChecked == true) modifiers.Add("Alt");
        if (_shiftCheck.IsChecked == true) modifiers.Add("Shift");
        if (_winCheck.IsChecked == true) modifiers.Add("Win");
        
        Shortcut.Name = _nameBox.Text;
        Shortcut.Key = _keyBox.Text;
        Shortcut.Modifiers = modifiers;
        
        DialogResult = true;
    }
}
