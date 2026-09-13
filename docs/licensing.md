# Licensing Guide

This document explains the licensing implications for Chimera and its plugins.

## Chimera License

Chimera is licensed under the [GNU Affero General Public License v3.0](../LICENSE).

### What This Means

- You can use, modify, and distribute Chimera
- You must provide source code for any modifications
- You must license derivative works under AGPL-3.0 or compatible
- If you distribute a modified version, you must make the source available

## Plugin Licensing

### Do Plugins Have to Use AGPL?

**Short answer**: No, but there are considerations.

**Long answer**: The AGPL has specific requirements for how plugins interact with the host:

1. **Plugin as a Separate Work**: Plugins loaded through Chimera's public API are generally considered separate works, not derivative works of Chimera itself.

2. **API Boundary**: The `Chimera.Abstractions` interfaces define a stable API boundary. Plugins that only interact through these interfaces are not necessarily derived works.

3. **Recommendation**: We recommend plugins use AGPL-3.0-or-later or a compatible license (GPL-3.0-or-later, Apache-2.0, MIT, BSD-2-Clause, BSD-3-Clause).

### Recommended Licenses

For maximum compatibility with Chimera:

- **AGPL-3.0-or-later**: Fully compatible, strongest copyleft
- **GPL-3.0-or-later**: Compatible, but weaker copyleft than AGPL
- **Apache-2.0**: Permissive, patent grant included
- **MIT**: Permissive, simple terms
- **BSD-2-Clause**: Permissive, minimal restrictions

### Plugin Distribution

When distributing a Chimera-based application:

1. **Include Chimera Source**: You must make Chimera's source code available
2. **Plugin Licenses**: Each plugin must have its own license
3. **Combined Works**: If plugins and host are combined into a single work, the combined work must be licensed under AGPL-3.0 or compatible

## Building a Product with Chimera

### Steps

1. Create a `distribution.json` with your plugins
2. Package using `chimera pack`
3. Distribute the resulting executable
4. Provide source code for Chimera and any modifications
5. Ensure all plugins have compatible licenses

### Example Distribution License

If you distribute a packaged application:

```
This application includes Chimera, licensed under AGPL-3.0.
Source code is available at: https://your-repo.com/chimera-source

This application includes the following plugins:
- Plugin A (MIT License)
- Plugin B (Apache-2.0 License)
```

## Commercial Use

Chimera and its plugins can be used commercially:

- No licensing fees
- No royalty requirements
- Must comply with license terms
- Must provide source code for modifications

## Contributing

By contributing to Chimera, you agree to license your contributions under AGPL-3.0-or-later.

## Further Reading

- [GNU AGPL v3.0 Full Text](https://www.gnu.org/licenses/agpl-3.0.html)
- [GPL Compatibility List](https://www.gnu.org/licenses/license-list.html)
- [Open Source Initiative](https://opensource.org/)
