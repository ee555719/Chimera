# Plugin Development Guide

This guide explains how to develop plugins for Chimera.

## Plugin Structure

A plugin consists of:

1. A `.dll` assembly containing the plugin code
2. A `plugin.json` manifest file
3. Any additional resources

## Plugin Manifest

Each plugin must have a `plugin.json` file:

```json
{
  "id": "com.example.hello",
  "name": "Hello World",
  "version": "1.0.0",
  "author": "Your Name",
  "license": "AGPL-3.0-or-later",
  "entryAssembly": "HelloWorld.dll",
  "entryType": "HelloWorld.Plugin",
  "minHostVersion": "1.0.0",
  "permissions": ["filesystem:read:documents", "network"],
  "dependencies": []
}
```

## Extension Points

### IStartupTask

Executes when the plugin is loaded:

```csharp
public class MyStartupTask : IStartupTask
{
    public Task ExecuteAsync(IPluginContext context)
    {
        context.Logger.Info("Plugin initialized!");
        return Task.CompletedTask;
    }
}
```

### IMenuContribution

Adds items to the application menu:

```csharp
public class MyMenu : IMenuContribution
{
    public string Id => "my.menu.item";
    public string Header => "My Menu Item";
    public string? Icon => null;
    public int Order => 100;
    public ICommand Command => new RelayCommand(Execute);
    
    private void Execute()
    {
        // Handle menu click
    }
}
```

### IPageContribution

Registers a navigation page:

```csharp
public class MyPage : IPageContribution
{
    public string Id => "my.page";
    public string Title => "My Page";
    public string? Icon => null;
    public int Order => 100;
    public Type PageType => typeof(MyPageControl);
}
```

### IThemeContribution

Defines a custom theme:

```csharp
public class MyTheme : IThemeContribution
{
    public string Id => "my.theme";
    public string Name => "My Theme";
    public bool IsDark => true;
    public IDictionary<string, string> Colors => new Dictionary<string, string>
    {
        ["Primary"] = "#FF0000",
        ["Background"] = "#000000"
    };
    public IDictionary<string, string> Fonts => new Dictionary<string, string>
    {
        ["Default"] = "Segoe UI"
    };
    public double CornerRadius => 8;
}
```

### ISettingsSection

Adds a settings page:

```csharp
public class MySettings : ISettingsSection
{
    public string Id => "my.settings";
    public string Title => "My Settings";
    public string? Icon => null;
    public int Order => 100;
    public Type SettingsType => typeof(MySettingsControl);
}
```

## Permission Model

Plugins must declare required permissions in `plugin.json`. The host intercepts undeclared permission requests:

```json
{
  "permissions": [
    "filesystem:read:documents",
    "filesystem:write:temp",
    "network",
    "clipboard"
  ]
}
```

Available permissions:
- `filesystem:read:<scope>` / `filesystem:write:<scope>` (scopes: `documents`, `appdata`, `temp`, or absolute paths)
- `network`
- `process:spawn`
- `registry:read` / `registry:write`
- `clipboard`
- `notifications`

## Host Services

Access system resources through `IHostServices`:

```csharp
public class MyPlugin : IStartupTask
{
    public Task ExecuteAsync(IPluginContext context)
    {
        var hostServices = context.HostServices;
        
        // File operations
        var stream = hostServices.OpenFile("data.txt", FileMode.Open, FileAccess.Read, FileShare.Read);
        
        // Network
        var response = await hostServices.SendHttpRequestAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://api.example.com"));
        
        // Notifications
        hostServices.ShowNotification("Title", "Message", NotificationType.Information);
        
        return Task.CompletedTask;
    }
}
```

## Building and Testing

1. Create a new class library project
2. Reference `Chimera.Abstractions`
3. Implement the desired interfaces
4. Build and place the output in the plugins directory
5. Restart the host or use hot-reload in development mode

## Best Practices

- Keep plugins small and focused
- Declare only necessary permissions
- Handle errors gracefully
- Use the plugin logger instead of console output
- Test plugins in isolation before packaging
