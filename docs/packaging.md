# Packaging Guide

This guide explains how to package Chimera plugins into a standalone executable.

## Distribution Manifest

Create a `distribution.json` file:

```json
{
  "id": "com.example.myapp",
  "name": "MyApp",
  "version": "1.0.0",
  "icon": "assets/icon.ico",
  "splash": "assets/splash.png",
  "window": {
    "width": 1280,
    "height": 800,
    "title": "MyApp"
  },
  "theme": "dark",
  "plugins": ["com.example.hello@1.0.0"],
  "settings": {},
  "licenseText": "..."
}
```

## Packaging Process

1. Build the host in release mode:
   ```bash
   dotnet publish src/Chimera.Host -c Release -r win-x64 --self-contained true
   ```

2. Collect plugins and resources

3. Create a payload archive

4. Append payload to the Chimera.Bootstrapper executable

5. The output is a single `Chimera.exe` file (70-90 MB)

## Payload Format

The final executable contains:
```
[bootstrapper code][payload][uint64 length][magic "CHIMBOOT1"]
```

## Bootstrapper Behavior

1. Reads the payload from the end of the executable
2. Calculates SHA-256 hash
3. Extracts to `%LOCALAPPDATA%\Chimera\apps\<distId>\<hash>\`
4. If directory exists and hash matches, skips extraction
5. Sets environment variables and launches `Chimera.Host.exe`

## CLI Usage

```bash
# Initialize a new distribution project
chimera init

# Run in development mode
chimera dev

# Install a plugin
chimera plugin install path/to/plugin

# List installed plugins
chimera plugin list

# Package the distribution
chimera pack -c distribution.json -o Chimera.exe
```

## Host Modes

### Development Mode
- Loads plugins from `%APPDATA%\Chimera\plugins\`
- Shows full management interface
- Enables hot-reload

### Distribution Mode
- Loads only plugins from the packaged distribution
- Uses distribution-specific window settings
- Hides packaging tools

## Troubleshooting

### Plugin Not Loading
- Verify `plugin.json` exists and is valid JSON
- Check that the entry assembly exists
- Ensure permissions are correctly declared

### Packaging Fails
- Verify all plugin paths exist
- Check that the bootstrapper can find all required files
- Ensure sufficient disk space for the payload

### Hot-Reload Not Working
- Check that the FileSystemWatcher is set up correctly
- Verify the plugin directory is not on a network share
- Restart the host if ALC unload fails
