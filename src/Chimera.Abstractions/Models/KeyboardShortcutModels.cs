// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chimera.Abstractions.Models;

/// <summary>
/// Keyboard shortcut definition
/// </summary>
public class KeyboardShortcut
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = new();

    [JsonPropertyName("pluginId")]
    public string PluginId { get; set; } = string.Empty;

    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Keyboard shortcut configuration
/// </summary>
public class KeyboardShortcutConfig
{
    [JsonPropertyName("shortcuts")]
    public List<KeyboardShortcut> Shortcuts { get; set; } = new();
}
