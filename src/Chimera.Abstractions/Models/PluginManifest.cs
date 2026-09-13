// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json.Serialization;

namespace Chimera.Abstractions.Models;

public class PluginManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("license")]
    public string License { get; set; } = "AGPL-3.0-or-later";

    [JsonPropertyName("entryAssembly")]
    public string EntryAssembly { get; set; } = string.Empty;

    [JsonPropertyName("entryType")]
    public string EntryType { get; set; } = string.Empty;

    [JsonPropertyName("minHostVersion")]
    public string MinHostVersion { get; set; } = "1.0.0";

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();

    [JsonPropertyName("runtime")]
    public string? Runtime { get; set; }
}
