// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json.Serialization;

namespace Chimera.Abstractions.Models;

public class AiChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AiChatRequest
{
    [JsonPropertyName("messages")]
    public List<AiChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("context")]
    public string? Context { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1000;
}

public class AiChatResponse
{
    [JsonPropertyName("message")]
    public AiChatMessage Message { get; set; } = new();

    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<AiAction> Actions { get; set; } = new();
}

public class AiAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class AiPluginRecommendation
{
    [JsonPropertyName("plugin_id")]
    public string PluginId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}

public class AiContext
{
    [JsonPropertyName("installed_plugins")]
    public List<string> InstalledPlugins { get; set; } = new();

    [JsonPropertyName("recent_actions")]
    public List<string> RecentActions { get; set; } = new();

    [JsonPropertyName("user_preferences")]
    public Dictionary<string, string> UserPreferences { get; set; } = new();

    [JsonPropertyName("system_info")]
    public Dictionary<string, string> SystemInfo { get; set; } = new();
}
