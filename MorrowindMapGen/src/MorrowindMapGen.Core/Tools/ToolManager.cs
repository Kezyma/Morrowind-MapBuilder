using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Manages downloading, extracting, and locating external tools.
/// </summary>
public class ToolManager
{
    private readonly ILogger<ToolManager> _logger;
    private readonly string _toolsDirectory;
    private readonly HttpClient _httpClient;

    public ToolManager(ILogger<ToolManager> logger, string? toolsDirectory = null)
    {
        _logger = logger;
        _toolsDirectory = toolsDirectory ?? Path.Combine(AppContext.BaseDirectory, "tools");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MorrowindMapGen/1.0");
    }

    /// <summary>
    /// Gets the directory where tools are stored.
    /// </summary>
    public string ToolsDirectory => _toolsDirectory;

    /// <summary>
    /// Gets the path to a tool's executable, downloading if necessary.
    /// </summary>
    /// <param name="tool">The tool to get.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the tool's executable.</returns>
    public async Task<string> GetToolPathAsync(ToolInfo tool, CancellationToken cancellationToken = default)
    {
        var toolDir = Path.Combine(_toolsDirectory, tool.Name);
        var executablePath = Path.Combine(toolDir, tool.ExecutableName);

        if (File.Exists(executablePath))
        {
            _logger.LogDebug("Tool {Tool} already exists at {Path}", tool.Name, executablePath);
            return executablePath;
        }

        _logger.LogInformation("Downloading {Tool} {Version}...", tool.Name, tool.Version);
        await DownloadAndExtractToolAsync(tool, cancellationToken);

        if (!File.Exists(executablePath))
        {
            throw new ToolException($"Tool {tool.Name} executable not found after extraction: {executablePath}");
        }

        return executablePath;
    }

    /// <summary>
    /// Checks if a tool is already downloaded.
    /// </summary>
    /// <param name="tool">The tool to check.</param>
    /// <returns>True if the tool is available.</returns>
    public bool IsToolAvailable(ToolInfo tool)
    {
        var toolDir = Path.Combine(_toolsDirectory, tool.Name);
        var executablePath = Path.Combine(toolDir, tool.ExecutableName);
        return File.Exists(executablePath);
    }

    /// <summary>
    /// Downloads and extracts all required tools.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task EnsureAllToolsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var tool in KnownTools.All)
        {
            await GetToolPathAsync(tool, cancellationToken);
        }
    }

    /// <summary>
    /// Downloads and extracts a tool from its download URL.
    /// </summary>
    private async Task DownloadAndExtractToolAsync(ToolInfo tool, CancellationToken cancellationToken)
    {
        var toolDir = Path.Combine(_toolsDirectory, tool.Name);

        // Clean up existing directory
        if (Directory.Exists(toolDir))
        {
            Directory.Delete(toolDir, recursive: true);
        }

        Directory.CreateDirectory(toolDir);

        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{tool.Name}_{Guid.NewGuid()}.zip");

        try
        {
            // Download with retry
            await DownloadWithRetryAsync(tool.DownloadUrl, tempZipPath, maxRetries: 3, cancellationToken);

            // Extract
            _logger.LogDebug("Extracting {Tool} to {Path}...", tool.Name, toolDir);
            ZipFile.ExtractToDirectory(tempZipPath, toolDir);

            // If executable is in a subdirectory, move files up
            if (!string.IsNullOrEmpty(tool.ZipSubdirectory))
            {
                var subDir = Path.Combine(toolDir, tool.ZipSubdirectory);
                if (Directory.Exists(subDir))
                {
                    foreach (var file in Directory.GetFiles(subDir))
                    {
                        var destPath = Path.Combine(toolDir, Path.GetFileName(file));
                        File.Move(file, destPath, overwrite: true);
                    }
                }
            }

            // Handle case where zip extracts to a single directory
            var subDirs = Directory.GetDirectories(toolDir);
            if (subDirs.Length == 1 && !File.Exists(Path.Combine(toolDir, tool.ExecutableName)))
            {
                var singleSubDir = subDirs[0];
                foreach (var file in Directory.GetFiles(singleSubDir))
                {
                    var destPath = Path.Combine(toolDir, Path.GetFileName(file));
                    File.Move(file, destPath, overwrite: true);
                }
                foreach (var dir in Directory.GetDirectories(singleSubDir))
                {
                    var destPath = Path.Combine(toolDir, Path.GetFileName(dir));
                    Directory.Move(dir, destPath);
                }
                Directory.Delete(singleSubDir, recursive: true);
            }

            _logger.LogInformation("Successfully installed {Tool} {Version}", tool.Name, tool.Version);
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }
        }
    }

    /// <summary>
    /// Downloads a file with retry logic.
    /// </summary>
    private async Task DownloadWithRetryAsync(string url, string destinationPath, int maxRetries, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Download attempt {Attempt}/{MaxRetries}: {Url}", attempt, maxRetries, url);

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var fileStream = File.Create(destinationPath);
                await response.Content.CopyToAsync(fileStream, cancellationToken);

                return; // Success
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                _logger.LogWarning("Download attempt {Attempt} failed: {Message}", attempt, ex.Message);

                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                    _logger.LogDebug("Waiting {Delay} before retry...", delay);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        throw new ToolException($"Failed to download {url} after {maxRetries} attempts", lastException);
    }

    /// <summary>
    /// Deletes all downloaded tools.
    /// </summary>
    public void CleanupTools()
    {
        if (Directory.Exists(_toolsDirectory))
        {
            _logger.LogInformation("Cleaning up tools directory: {Path}", _toolsDirectory);
            Directory.Delete(_toolsDirectory, recursive: true);
        }
    }
}

/// <summary>
/// Exception thrown when tool management operations fail.
/// </summary>
public class ToolException : Exception
{
    public ToolException(string message) : base(message) { }
    public ToolException(string message, Exception? innerException) : base(message, innerException) { }
}
