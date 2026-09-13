# Python Plugin Template for Chimera

This is a template for creating Python plugins for Chimera.

## Structure

```
my-plugin/
├── main/
│   └── main.py          # Plugin entry point
├── library.txt           # Python dependencies (optional)
├── README.md            # This file
└── plugin.json          # Plugin manifest
```

## Getting Started

1. Copy this template to your plugins directory
2. Edit `main/main.py` to implement your plugin logic
3. Add any dependencies to `library.txt`
4. Update `plugin.json` with your plugin information

## Plugin Manifest

```json
{
  "id": "com.example.myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "author": "Your Name",
  "runtime": "python",
  "entryAssembly": "main/main.py"
}
```

## SDK Usage

```python
from chimera_sdk import ChimeraPlugin, ui, logger

plugin = ChimeraPlugin()

@plugin.on_startup
def on_startup():
    logger.info("Plugin loaded!")
    ui.register_tab(title="My Tab", elements=[...])

if __name__ == "__main__":
    plugin.run()
```

## Dependencies

Add Python package dependencies to `library.txt`:

```
requests>=2.28.0
flask>=2.0.0
```

Dependencies will be automatically installed when the plugin is loaded.
