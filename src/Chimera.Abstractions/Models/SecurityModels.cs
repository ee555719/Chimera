// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json.Serialization;

namespace Chimera.Abstractions.Models;

public class PluginPermissionGrant
{
    [JsonPropertyName("pluginId")]
    public string PluginId { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = new();

    [JsonPropertyName("grantedAt")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("grantedBy")]
    public string GrantedBy { get; set; } = "user";
}

public class SecurityAuditEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("level")]
    public string Level { get; set; } = "info";

    [JsonPropertyName("pluginId")]
    public string PluginId { get; set; } = string.Empty;

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("permission")]
    public string? Permission { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

public class PermissionGrantConfig
{
    [JsonPropertyName("grants")]
    public List<PluginPermissionGrant> Grants { get; set; } = new();
}
