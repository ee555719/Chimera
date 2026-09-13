// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

public class SecurityAuditLogger
{
    private readonly string _auditDirectory;
    private readonly object _lock = new();
    private readonly List<SecurityAuditEntry> _recentEntries = new();
    private const int MaxRecentEntries = 1000;

    public event Action<SecurityAuditEntry>? OnAuditEvent;

    public IReadOnlyList<SecurityAuditEntry> RecentEntries => _recentEntries.AsReadOnly();

    public SecurityAuditLogger(string? auditDirectory = null)
    {
        _auditDirectory = auditDirectory ?? Path.Combine(ChimeraPaths.ConfigDirectory, "audit");
        Directory.CreateDirectory(_auditDirectory);
    }

    public void LogPermissionCheck(string pluginId, string permission, bool granted, string? details = null)
    {
        var entry = new SecurityAuditEntry
        {
            PluginId = pluginId,
            Event = "permission_check",
            Permission = permission,
            Result = granted ? "granted" : "denied",
            Level = granted ? "info" : "warning",
            Details = details
        };
        WriteEntry(entry);
    }

    public void LogPermissionGrant(string pluginId, string permission, string grantedBy = "user")
    {
        var entry = new SecurityAuditEntry
        {
            PluginId = pluginId,
            Event = "permission_granted",
            Permission = permission,
            Result = "granted",
            Level = "info",
            Details = $"Granted by {grantedBy}"
        };
        WriteEntry(entry);
    }

    public void LogPermissionRevoke(string pluginId, string permission)
    {
        var entry = new SecurityAuditEntry
        {
            PluginId = pluginId,
            Event = "permission_revoked",
            Permission = permission,
            Result = "revoked",
            Level = "warning"
        };
        WriteEntry(entry);
    }

    public void LogSecurityViolation(string pluginId, string permission, string details)
    {
        var entry = new SecurityAuditEntry
        {
            PluginId = pluginId,
            Event = "security_violation",
            Permission = permission,
            Result = "blocked",
            Level = "error",
            Details = details
        };
        WriteEntry(entry);
    }

    public void LogPluginLoad(string pluginId, bool success, string? details = null)
    {
        var entry = new SecurityAuditEntry
        {
            PluginId = pluginId,
            Event = "plugin_load",
            Result = success ? "success" : "failed",
            Level = success ? "info" : "error",
            Details = details
        };
        WriteEntry(entry);
    }

    public void LogPluginUnload(string pluginId)
    {
        var entry = new SecurityAuditEntry
        {
            PluginId = pluginId,
            Event = "plugin_unload",
            Result = "success",
            Level = "info"
        };
        WriteEntry(entry);
    }

    public void LogSuspiciousActivity(string pluginId, string activity, string details)
    {
        var entry = new SecurityAuditEntry
        {
            PluginId = pluginId,
            Event = "suspicious_activity",
            Result = "flagged",
            Level = "critical",
            Details = $"{activity}: {details}"
        };
        WriteEntry(entry);
    }

    private void WriteEntry(SecurityAuditEntry entry)
    {
        lock (_lock)
        {
            _recentEntries.Insert(0, entry);
            while (_recentEntries.Count > MaxRecentEntries)
            {
                _recentEntries.RemoveAt(_recentEntries.Count - 1);
            }

            OnAuditEvent?.Invoke(entry);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var fileName = $"audit-{entry.Timestamp:yyyy-MM-dd}.jsonl";
                var filePath = Path.Combine(_auditDirectory, fileName);
                var json = JsonSerializer.Serialize(entry);
                lock (_lock)
                {
                    File.AppendAllText(filePath, json + Environment.NewLine);
                }
            }
            catch
            {
                // Silently ignore write errors for audit log
            }
        });
    }

    public async Task<List<SecurityAuditEntry>> LoadAuditLogAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var entries = new List<SecurityAuditEntry>();
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        var files = Directory.GetFiles(_auditDirectory, "audit-*.jsonl")
            .Where(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (DateTime.TryParse(name.Replace("audit-", ""), out var date))
                {
                    return date >= start.Date && date <= end.Date;
                }
                return false;
            })
            .OrderByDescending(f => f);

        foreach (var file in files)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(file);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var entry = JsonSerializer.Deserialize<SecurityAuditEntry>(line);
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                }
            }
            catch
            {
                // Skip corrupted entries
            }
        }

        return entries;
    }
}
