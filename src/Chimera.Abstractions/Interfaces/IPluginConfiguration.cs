// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface IPluginConfiguration
{
    T? Get<T>(string key);
    void Set<T>(string key, T value);
    bool ContainsKey(string key);
    IReadOnlyDictionary<string, object> GetAll();
}
