# ADR-001: Use Cooperative Sandboxing Instead of AppContainer

## Status

Accepted

## Context

Chimera needs a security model for plugins that balances security with practicality.

## Decision

We will use cooperative sandboxing instead of AppContainer isolation.

## Consequences

### Positive
- Plugins can integrate with WPF UI
- Better performance (no IPC overhead)
- Simpler implementation
- Plugins can share types through Chimera.Abstractions

### Negative
- No process isolation
- Plugins can access host memory
- Potential for privilege escalation
- Requires trust in plugin authors

### Mitigations
- Permission declaration in plugin.json
- Audit logging of permission violations
- Host services as the only access point to system resources
- Documentation of security limitations

## Alternatives Considered

1. **AppContainer**: Full isolation but prevents UI integration
2. **Separate Processes**: Better isolation but complex IPC
3. **Code Access Security**: Deprecated in .NET Core

## References

- [AppContainer Isolation](https://docs.microsoft.com/en-us/windows/win32/secauthz/appcontainer-isolation)
- [Cooperative Security](https://docs.microsoft.com/en-us/dotnet/standard/security/cooperative-security)
