// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace Chimera.PluginLoader;

/// <summary>
/// Manages Embeddable Python deployment for Python plugins
/// </summary>
public class EmbeddablePythonManager : IDisposable
{
    private readonly string _pythonDirectory;
    private readonly HttpClient _httpClient;
    
    private const string PythonVersion = "3.12.4";
    private const string PythonUrlTemplate = "https://www.python.org/ftp/python/{0}/python-{0}-embed-amd64.zip";
    private const string GetPipUrl = "https://bootstrap.pypa.io/get-pip.py";

    public EmbeddablePythonManager(string? baseDirectory = null)
    {
        _pythonDirectory = Path.Combine(baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory, "python");
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Get the path to the Python executable
    /// </summary>
    public string? GetPythonPath()
    {
        var pythonExe = Path.Combine(_pythonDirectory, "python.exe");
        return File.Exists(pythonExe) ? pythonExe : null;
    }

    /// <summary>
    /// Check if Python is installed
    /// </summary>
    public bool IsPythonInstalled()
    {
        return GetPythonPath() != null;
    }

    /// <summary>
    /// Install Embeddable Python
    /// </summary>
    public async Task InstallPythonAsync(IProgress<double>? progress = null)
    {
        if (IsPythonInstalled())
        {
            return;
        }

        // Create Python directory
        Directory.CreateDirectory(_pythonDirectory);

        // Download Python
        var url = string.Format(PythonUrlTemplate, PythonVersion);
        var zipPath = Path.Combine(Path.GetTempPath(), $"python-{PythonVersion}.zip");

        try
        {
            // Download the zip file
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var totalBytesRead = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progressPercentage = (double)totalBytesRead / totalBytes * 100;
                            progress?.Report(progressPercentage);
                        }
                    }
                }
            }

            // Extract the zip file
            ZipFile.ExtractToDirectory(zipPath, _pythonDirectory, true);

            // Enable pip by modifying python3x._pth file
            EnablePip();

            // Install pip
            await InstallPipAsync();

            // Cleanup
            File.Delete(zipPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to install Python: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Install Python package dependencies from a requirements file
    /// </summary>
    public async Task InstallDependenciesAsync(string requirementsFile, IProgress<string>? progress = null)
    {
        var pythonPath = GetPythonPath();
        if (pythonPath == null)
        {
            throw new InvalidOperationException("Python is not installed");
        }

        if (!File.Exists(requirementsFile))
        {
            throw new FileNotFoundException($"Requirements file not found: {requirementsFile}");
        }

        var pipPath = Path.Combine(_pythonDirectory, "Scripts", "pip.exe");
        if (!File.Exists(pipPath))
        {
            pipPath = pythonPath; // Try using python -m pip
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = pipPath,
            Arguments = $"install -r \"{requirementsFile}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start pip process");
        }

        // Read output asynchronously
        var outputTask = Task.Run(async () =>
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line != null)
                {
                    progress?.Report(line);
                }
            }
        });

        var errorTask = Task.Run(async () =>
        {
            while (!process.StandardError.EndOfStream)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (line != null)
                {
                    progress?.Report($"ERROR: {line}");
                }
            }
        });

        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"pip install failed with exit code {process.ExitCode}");
        }
    }

    /// <summary>
    /// Uninstall Python
    /// </summary>
    public void UninstallPython()
    {
        if (Directory.Exists(_pythonDirectory))
        {
            try
            {
                Directory.Delete(_pythonDirectory, true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to uninstall Python: {ex.Message}", ex);
            }
        }
    }

    private void EnablePip()
    {
        // Find the python3x._pth file
        var pthFiles = Directory.GetFiles(_pythonDirectory, "python3*._pth");
        foreach (var pthFile in pthFiles)
        {
            var content = File.ReadAllText(pthFile);
            
            // Uncomment import site
            content = content.Replace("#import site", "import site");
            
            File.WriteAllText(pthFile, content);
        }
    }

    private async Task InstallPipAsync()
    {
        var pythonPath = GetPythonPath();
        if (pythonPath == null)
        {
            throw new InvalidOperationException("Python is not installed");
        }

        // Download get-pip.py
        var getPipPath = Path.Combine(_pythonDirectory, "get-pip.py");
        var getPipContent = await _httpClient.GetStringAsync(GetPipUrl);
        await File.WriteAllTextAsync(getPipPath, getPipContent);

        // Run get-pip.py
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{getPipPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }

        // Cleanup
        File.Delete(getPipPath);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
