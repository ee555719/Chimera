// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json.Serialization;

namespace Chimera.Abstractions.Models;

public class DistributionManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("splash")]
    public string? Splash { get; set; }

    [JsonPropertyName("window")]
    public WindowSettings? Window { get; set; }

    [JsonPropertyName("theme")]
    public string? Theme { get; set; }

    [JsonPropertyName("plugins")]
    public List<string> Plugins { get; set; } = new();

    [JsonPropertyName("settings")]
    public Dictionary<string, object> Settings { get; set; } = new();

    [JsonPropertyName("licenseText")]
    public string? LicenseText { get; set; }
}

public class WindowSettings
{
    [JsonPropertyName("width")]
    public int Width { get; set; } = 1280;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 800;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}
