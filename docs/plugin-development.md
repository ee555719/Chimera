# Plugin Development Guide

This guide explains how to develop plugins for Chimera.

## Plugin Structure

A plugin consists of:

1. A `.dll` assembly containing the plugin code (for .NET plugins)
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
  "dependencies": [],
  "runtime": "dotnet"
}
```

### Runtime Types

- `"runtime": "dotnet"` (default) - .NET plugin loaded via AssemblyLoadContext
- `"runtime": "python"` - Python plugin loaded via subprocess with JSON-RPC communication

## .NET Plugins

### Extension Points

#### IStartupTask

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

#### IMenuContribution

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

#### IPageContribution

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

#### IThemeContribution

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

#### ISettingsSection

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

## Python Plugins

Python plugins use process isolation with JSON-RPC 2.0 communication over stdio.

### Plugin Structure

```
my-python-plugin/
├── main/
│   └── main.py          # Plugin entry point
├── library.txt           # Python dependencies (optional)
├── plugin.json           # Plugin manifest
└── README.md            # Documentation
```

### Plugin Manifest for Python

```json
{
  "id": "com.example.python",
  "name": "My Python Plugin",
  "version": "1.0.0",
  "author": "Your Name",
  "runtime": "python",
  "entryAssembly": "main/main.py",
  "minHostVersion": "1.0.0",
  "permissions": ["notifications"],
  "dependencies": []
}
```

### Python SDK Usage

```python
from chimera_sdk import ChimeraPlugin, ui, logger

plugin = ChimeraPlugin()

@plugin.on_startup
def on_startup():
    logger.info("Python plugin loaded!")
    
    # Register a custom tab
    ui.register_tab(
        title="My Panel",
        elements=[
            {"type": "text", "content": "Hello from Python!"},
            {"type": "button", "label": "Click Me", "action": "my_plugin.button_click"}
        ]
    )

@plugin.on_shutdown
def on_shutdown():
    logger.info("Python plugin unloaded!")

def handle_button_click(params):
    logger.info("Button clicked!")
    return {"success": True}

plugin.register_handler("my_plugin.button_click", handle_button_click)

if __name__ == "__main__":
    plugin.run()
```

### Dependencies

Add Python package dependencies to `library.txt`:

```
requests>=2.28.0
flask>=2.0.0
```

Dependencies are automatically installed when the plugin is installed.

### Available SDK Modules

- `ui` - UI operations (register tabs, inject content, customize styles)
- `fs` - File system operations (request write/delete with user confirmation)
- `process` - Process operations (request shutdown, launch external programs)
- `logger` - Logging operations (debug, info, warning, error)

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

### .NET Plugins

1. Create a new class library project
2. Reference `Chimera.Abstractions`
3. Implement the desired interfaces
4. Build and place the output in the plugins directory
5. Restart the host or use hot-reload in development mode

### Python Plugins

1. Create a new directory in the plugins folder
2. Add `main/main.py` with your plugin code
3. Add `plugin.json` with `"runtime": "python"`
4. Optionally add `library.txt` with Python dependencies
5. Restart the host

## Best Practices

- Keep plugins small and focused
- Declare only necessary permissions
- Handle errors gracefully
- Use the plugin logger instead of console output
- Test plugins in isolation before packaging
