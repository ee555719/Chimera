// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chimera.Host.Pages.DevTools;

public partial class DevToolsPage : UserControl
{
    private readonly ObservableCollection<RpcMessageInfo> _rpcMessages = new();
    private readonly ObservableCollection<LogEntry> _logs = new();
    private readonly ObservableCollection<PermissionEntry> _permissions = new();
    
    public DevToolsPage()
    {
        InitializeComponent();
        
        RpcMessagesList.ItemsSource = _rpcMessages;
        LogsList.ItemsSource = _logs;
        PermissionsList.ItemsSource = _permissions;
        
        LogPluginFilter.Items.Add("全部");
        LogPluginFilter.SelectedIndex = 0;
        
        Loaded += DevToolsPage_Loaded;
    }

    private void DevToolsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Load existing logs
        LoadLogsFromFiles();
    }

    public void AddRpcMessage(string pluginId, string direction, string method, string data)
    {
        var message = new RpcMessageInfo
        {
            Timestamp = DateTime.Now,
            PluginId = pluginId,
            Direction = direction,
            DirectionColor = direction == "发送" ? 
                new SolidColorBrush(Colors.Blue) : 
                new SolidColorBrush(Colors.Green),
            Method = method,
            Data = data
        };
        
        _rpcMessages.Insert(0, message);
        
        // Keep only last 1000 messages
        while (_rpcMessages.Count > 1000)
        {
            _rpcMessages.RemoveAt(_rpcMessages.Count - 1);
        }
    }

    public void AddLog(string pluginId, string level, string message)
    {
        var log = new LogEntry
        {
            Timestamp = DateTime.Now,
            PluginId = pluginId,
            Level = level,
            LevelColor = level switch
            {
                "Debug" => new SolidColorBrush(Colors.Gray),
                "Info" => new SolidColorBrush(Colors.Black),
                "Warn" => new SolidColorBrush(Colors.Orange),
                "Error" => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Black)
            },
            Message = message
        };
        
        _logs.Insert(0, log);
        
        // Update filter if needed
        if (!LogPluginFilter.Items.Contains(pluginId))
        {
            LogPluginFilter.Items.Add(pluginId);
        }
        
        // Keep only last 5000 logs
        while (_logs.Count > 5000)
        {
            _logs.RemoveAt(_logs.Count - 1);
        }
    }

    public void AddPermission(string pluginId, string permission, string result, string details = "")
    {
        var entry = new PermissionEntry
        {
            Timestamp = DateTime.Now,
            PluginId = pluginId,
            Permission = permission,
            Result = result,
            ResultColor = result == "允许" ? 
                new SolidColorBrush(Colors.Green) : 
                new SolidColorBrush(Colors.Red),
            Details = details
        };
        
        _permissions.Insert(0, entry);
    }

    private void LoadLogsFromFiles()
    {
        // Load logs from plugin directories
        var pluginsDir = @"D:\ChimeraPlugin";
        if (System.IO.Directory.Exists(pluginsDir))
        {
            foreach (var pluginDir in System.IO.Directory.GetDirectories(pluginsDir))
            {
                var pluginId = System.IO.Path.GetFileName(pluginDir);
                var logDir = System.IO.Path.Combine(pluginDir, "logs");
                
                if (System.IO.Directory.Exists(logDir))
                {
                    foreach (var logFile in System.IO.Directory.GetFiles(logDir, "*.log"))
                    {
                        try
                        {
                            var lines = System.IO.File.ReadAllLines(logFile);
                            foreach (var line in lines.TakeLast(100)) // Load last 100 lines
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    AddLog(pluginId, "Info", line);
                                }
                            }
                        }
                        catch
                        {
                            // Ignore errors
                        }
                    }
                }
            }
        }
    }

    private void LogPluginFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        FilterLogs();
    }

    private void LogLevelFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        FilterLogs();
    }

    private void FilterLogs()
    {
        // This is a simple filter - in production, you'd want a more sophisticated approach
        LogsList.Items.Refresh();
    }

    private void CopyLogs_Click(object sender, RoutedEventArgs e)
    {
        var logText = string.Join(Environment.NewLine, 
            _logs.Select(l => $"[{l.Timestamp:HH:mm:ss.fff}] [{l.Level}] [{l.PluginId}] {l.Message}"));
        
        Clipboard.SetText(logText);
        MessageBox.Show("日志已复制到剪贴板", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        _rpcMessages.Clear();
        _logs.Clear();
        _permissions.Clear();
    }
}

public class RpcMessageInfo
{
    public DateTime Timestamp { get; set; }
    public string PluginId { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public Brush DirectionColor { get; set; } = new SolidColorBrush(Colors.Black);
    public string Method { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string PluginId { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public Brush LevelColor { get; set; } = new SolidColorBrush(Colors.Black);
    public string Message { get; set; } = string.Empty;
}

public class PermissionEntry
{
    public DateTime Timestamp { get; set; }
    public string PluginId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public Brush ResultColor { get; set; } = new SolidColorBrush(Colors.Black);
    public string Details { get; set; } = string.Empty;
}
