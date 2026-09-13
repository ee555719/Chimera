[**English**](README.md) | [简体中文](README.zh.md)

# Chimera

A modular Windows desktop host application with a powerful plugin system. Customize menus, pages, themes, shortcuts, and settings through plugins, then package everything into a single standalone `.exe`.

## Core Features

- **Plugin System** - Dynamic plugin loading with isolation and hot-reload
- **Customizable UI** - Build your own application by combining plugins
- **Standalone Packaging** - Package plugins and host into a single `.exe`
- **Permission Model** - Cooperative sandboxing for plugin security
- **Theme Support** - Light, dark, and system themes with plugin extensions

## Quick Start

### Prerequisites

- Windows 10 (1809+) or Windows 11
- .NET 8 SDK

### Build from Source

```bash
git clone https://github.com/ee555719/Chimera.git
cd Chimera
dotnet build
```

### Run the Host

```bash
dotnet run --project src/Chimera.Host
```

## Plugin Development

Plugins implement interfaces from `Chimera.Abstractions`:

```csharp
public class MyPlugin : IStartupTask, IMenuContribution
{
    public string Id => "com.example.myplugin";
    public string Header => "My Plugin";
    
    public Task ExecuteAsync(IPluginContext context)
    {
        context.Logger.Info("Plugin loaded!");
        return Task.CompletedTask;
    }
}
```

See [docs/plugin-development.md](docs/plugin-development.md) for details.

## Packaging

Create a standalone executable with the CLI:

```bash
dotnet run --project src/Chimera.Cli -- pack -c distribution.json -o Chimera.exe
```

See [docs/packaging.md](docs/packaging.md) for the packaging format.

## License

This project is licensed under the [GNU Affero General Public License v3.0](LICENSE).

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.
