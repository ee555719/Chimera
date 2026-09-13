[**English**](README.md) | [简体中文](README.zh.md)

# Chimera

一个模块化的 Windows 桌面宿主应用程序，具有强大的插件系统。通过插件自定义菜单、页面、主题、快捷键和设置项，然后将所有内容打包成一个独立的 `.exe` 文件。

## 核心特性

- **插件系统** - 支持动态加载、隔离和热重载
- **可定制 UI** - 通过组合插件构建您自己的应用程序
- **独立打包** - 将插件和宿主打包成单个 `.exe`
- **权限模型** - 插件安全协作沙箱
- **主题支持** - 浅色、深色和系统主题，支持插件扩展

## 快速开始

### 环境要求

- Windows 10 (1809+) 或 Windows 11
- .NET 8 SDK

### 从源码构建

```bash
git clone https://github.com/ee555719/Chimera.git
cd Chimera
dotnet build
```

### 运行宿主

```bash
dotnet run --project src/Chimera.Host
```

## 插件开发

插件实现 `Chimera.Abstractions` 中的接口：

```csharp
public class MyPlugin : IStartupTask, IMenuContribution
{
    public string Id => "com.example.myplugin";
    public string Header => "我的插件";
    
    public Task ExecuteAsync(IPluginContext context)
    {
        context.Logger.Info("插件已加载！");
        return Task.CompletedTask;
    }
}
```

详见 [docs/plugin-development.md](docs/plugin-development.md)。

## 打包

使用 CLI 创建独立可执行文件：

```bash
dotnet run --project src/Chimera.Cli -- pack -c distribution.json -o Chimera.exe
```

详见 [docs/packaging.md](docs/packaging.md)。

## 许可证

本项目采用 [GNU Affero 通用公共许可证 v3.0](LICENSE) 许可。

## 贡献

欢迎贡献！请参阅 [CONTRIBUTING.md](CONTRIBUTING.md)。
