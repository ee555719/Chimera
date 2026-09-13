// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chimera.Abstractions.Models;

/// <summary>
/// Plugin store entry
/// </summary>
public class PluginStoreEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("license")]
    public string License { get; set; } = "AGPL-3.0-or-later";

    [JsonPropertyName("runtime")]
    public string Runtime { get; set; } = "dotnet";

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Plugin store response
/// </summary>
public class PluginStoreResponse
{
    [JsonPropertyName("plugins")]
    public List<PluginStoreEntry> Plugins { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}

/// <summary>
/// Plugin store configuration
/// </summary>
public class PluginStoreConfig
{
    [JsonPropertyName("sourceUrl")]
    public string SourceUrl { get; set; } = "https://raw.githubusercontent.com/ee555719/Chimera/main/plugin-store.json";

    [JsonPropertyName("enableSignatureVerification")]
    public bool EnableSignatureVerification { get; set; } = false;

    [JsonPropertyName("trustedDevelopers")]
    public List<string> TrustedDevelopers { get; set; } = new();
}
