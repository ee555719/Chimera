# Permission Model

Chimera uses a cooperative sandbox model for plugin security.

## Why Cooperative Sandboxing?

Chimera uses cooperative sandboxing instead of AppContainer isolation because:

1. **UI Integration**: Plugins need to integrate with the WPF UI, which requires access to UI threads and resources
2. **Performance**: AppContainer adds overhead for inter-process communication
3. **Complexity**: Full sandboxing requires separate processes and complex IPC mechanisms
4. **Practicality**: Most desktop plugins need legitimate access to system resources

## Available Permissions

### File System Access

```json
{
  "permissions": [
    "filesystem:read:documents",
    "filesystem:write:temp",
    "filesystem:read:C:\\CustomPath"
  ]
}
```

Scopes:
- `documents` - User's Documents folder
- `appdata` - Application data folder
- `temp` - Temporary directory
- Absolute paths - Specific directories

### Network Access

```json
{
  "permissions": ["network"]
}
```

Allows HTTP/HTTPS requests through `IHostServices.SendHttpRequestAsync()`.

### Process Spawning

```json
{
  "permissions": ["process:spawn"]
}
```

Allows launching external processes.

### Registry Access

```json
{
  "permissions": ["registry:read", "registry:write"]
}
```

Allows reading and writing Windows registry keys.

### Clipboard Access

```json
{
  "permissions": ["clipboard"]
}
```

Allows reading and writing clipboard content.

### Notifications

```json
{
  "permissions": ["notifications"]
}
```

Allows showing system notifications.

## Permission Enforcement

1. Plugins declare required permissions in `plugin.json`
2. At runtime, plugins access resources only through `IHostServices`
3. `IHostServices` checks permissions before each operation
4. Undeclared permissions throw `PluginPermissionException`
5. Violations are logged to the audit log

## Audit Logging

All permission violations are logged with:
- Plugin ID
- Requested permission
- Timestamp
- Stack trace

Logs are stored in the plugin's log directory.

## Known Limitations

1. **No Process Isolation**: Plugins run in the same process as the host
2. **UI Thread Access**: Plugins can access the UI thread directly
3. **Memory Access**: Plugins can access the host's memory space
4. **No Network Filtering**: Network permission allows all outbound connections

## Recommendations

1. Only install plugins from trusted sources
2. Declare minimal required permissions
3. Review plugin permissions before installation
4. Monitor audit logs for suspicious activity
5. Consider using separate user accounts for sensitive operations
