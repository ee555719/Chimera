// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Chimera.PluginLoader;

/// <summary>
/// Manages Python plugin processes and JSON-RPC communication
/// </summary>
public class PythonPluginManager : IDisposable
{
    private readonly Dictionary<string, Process> _processes = new();
    private readonly Dictionary<string, Dictionary<int, TaskCompletionSource<JsonElement>>> _pendingRequests = new();
    private readonly Dictionary<string, StringBuilder> _errorBuffers = new();
    private readonly PluginPermissionManager? _permissionManager;
    private readonly SecurityAuditLogger? _auditLogger;
    private readonly object _lock = new();
    
    public event EventHandler<PythonLogEventArgs>? LogReceived;
    public event EventHandler<PythonErrorEventArgs>? ErrorReceived;
    public event EventHandler<string>? PluginCrashed;

    public PythonPluginManager(PluginPermissionManager? permissionManager = null, SecurityAuditLogger? auditLogger = null)
    {
        _permissionManager = permissionManager;
        _auditLogger = auditLogger;
    }

    /// <summary>
    /// Start a Python plugin process
    /// </summary>
    public async Task StartPluginAsync(string pluginId, string pluginDirectory, string pythonPath)
    {
        var mainPy = Path.Combine(pluginDirectory, "main", "main.py");
        if (!File.Exists(mainPy))
        {
            throw new FileNotFoundException($"Python entry point not found: {mainPy}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{mainPy}\" --plugin-id \"{pluginId}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Python process");
        }

        lock (_lock)
        {
            _processes[pluginId] = process;
            _pendingRequests[pluginId] = new Dictionary<int, TaskCompletionSource<JsonElement>>();
            _errorBuffers[pluginId] = new StringBuilder();
        }

        // Set up output handlers
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                HandleOutput(pluginId, e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                _errorBuffers[pluginId].AppendLine(e.Data);
                ErrorReceived?.Invoke(this, new PythonErrorEventArgs(pluginId, e.Data));
            }
        };

        process.Exited += (sender, e) =>
        {
            lock (_lock)
            {
                _processes.Remove(pluginId);
            }
            PluginCrashed?.Invoke(this, pluginId);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    /// <summary>
    /// Check if a plugin has the required permission before sending a request
    /// </summary>
    public async Task<bool> CheckPermissionAsync(string pluginId, string permission)
    {
        if (_permissionManager == null)
            return true; // No permission manager, allow by default

        var hasPermission = await _permissionManager.HasPermissionAsync(pluginId, permission);
        if (!hasPermission)
        {
            _auditLogger?.LogSecurityViolation(pluginId, permission, 
                $"Plugin {pluginId} attempted to use {permission} without permission");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Send a JSON-RPC request to a Python plugin
    /// </summary>
    public async Task<JsonElement> SendRequestAsync(string pluginId, string method, object? parameters = null, int timeoutMs = 30000)
    {
        Process process;
        int requestId;
        TaskCompletionSource<JsonElement> tcs;

        lock (_lock)
        {
            if (!_processes.TryGetValue(pluginId, out process!))
            {
                throw new InvalidOperationException($"Plugin {pluginId} is not running");
            }

            requestId = _pendingRequests[pluginId].Count + 1;
            tcs = new TaskCompletionSource<JsonElement>();
            _pendingRequests[pluginId][requestId] = tcs;
        }

        var request = new
        {
            jsonrpc = "2.0",
            method,
            @params = parameters,
            id = requestId
        };

        var json = JsonSerializer.Serialize(request);
        await process.StandardInput.WriteLineAsync(json);
        await process.StandardInput.FlushAsync();

        // Wait for response with timeout
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await using var registration = cts.Token.Register(() => tcs.TrySetCanceled());
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Request {method} timed out after {timeoutMs}ms");
        }
    }

    /// <summary>
    /// Stop a Python plugin process
    /// </summary>
    public async Task StopPluginAsync(string pluginId)
    {
        Process? process;
        lock (_lock)
        {
            if (!_processes.TryGetValue(pluginId, out process))
            {
                return;
            }
        }

        try
        {
            // Try graceful shutdown first
            await SendRequestAsync(pluginId, "host.process.request_shutdown", new { }, 5000);
        }
        catch
        {
            // If graceful shutdown fails, force kill
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch { }

        lock (_lock)
        {
            _processes.Remove(pluginId);
            _pendingRequests.Remove(pluginId);
            _errorBuffers.Remove(pluginId);
        }
    }

    /// <summary>
    /// Stop all running Python plugins
    /// </summary>
    public async Task StopAllAsync()
    {
        List<string> pluginIds;
        lock (_lock)
        {
            pluginIds = _processes.Keys.ToList();
        }

        foreach (var pluginId in pluginIds)
        {
            await StopPluginAsync(pluginId);
        }
    }

    /// <summary>
    /// Check if a Python plugin is running
    /// </summary>
    public bool IsPluginRunning(string pluginId)
    {
        lock (_lock)
        {
            return _processes.ContainsKey(pluginId) && !_processes[pluginId].HasExited;
        }
    }

    private void HandleOutput(string pluginId, string data)
    {
        // Try to parse as JSON-RPC response
        try
        {
            var json = JsonDocument.Parse(data);
            var root = json.RootElement;
            
            if (root.TryGetProperty("id", out var idElement))
            {
                var id = idElement.GetInt32();
                
                lock (_lock)
                {
                    if (_pendingRequests.TryGetValue(pluginId, out var requests) &&
                        requests.TryGetValue(id, out var tcs))
                    {
                        requests.Remove(id);
                        
                        if (root.TryGetProperty("error", out var error))
                        {
                            tcs.SetException(new InvalidOperationException(error.ToString()));
                        }
                        else if (root.TryGetProperty("result", out var result))
                        {
                            tcs.SetResult(result);
                        }
                        else
                        {
                            tcs.SetException(new InvalidOperationException("Invalid JSON-RPC response"));
                        }
                    }
                }
            }
        }
        catch
        {
            // Not JSON, treat as log output
            LogReceived?.Invoke(this, new PythonLogEventArgs(pluginId, data));
        }
    }

    public void Dispose()
    {
        StopAllAsync().GetAwaiter().GetResult();
        
        foreach (var process in _processes.Values)
        {
            try { process.Dispose(); } catch { }
        }
        
        GC.SuppressFinalize(this);
    }
}

public class PythonLogEventArgs : EventArgs
{
    public string PluginId { get; }
    public string Message { get; }

    public PythonLogEventArgs(string pluginId, string message)
    {
        PluginId = pluginId;
        Message = message;
    }
}

public class PythonErrorEventArgs : EventArgs
{
    public string PluginId { get; }
    public string Error { get; }

    public PythonErrorEventArgs(string pluginId, string error)
    {
        PluginId = pluginId;
        Error = error;
    }
}
