using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Result of running an external tool.
/// </summary>
public class ToolRunResult
{
    public required int ExitCode { get; init; }
    public required string StandardOutput { get; init; }
    public required string StandardError { get; init; }
    public required TimeSpan Duration { get; init; }

    public bool Success => ExitCode == 0;
}

/// <summary>
/// Base class for running external tools.
/// </summary>
public abstract class ToolRunner
{
    protected readonly ILogger Logger;
    protected readonly ToolManager ToolManager;

    protected ToolRunner(ILogger logger, ToolManager toolManager)
    {
        Logger = logger;
        ToolManager = toolManager;
    }

    /// <summary>
    /// Gets the tool info for this runner.
    /// </summary>
    protected abstract ToolInfo Tool { get; }

    /// <summary>
    /// Runs the tool with the specified arguments.
    /// </summary>
    protected async Task<ToolRunResult> RunToolAsync(
        string arguments,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var toolPath = await ToolManager.GetToolPathAsync(Tool, cancellationToken);

        Logger.LogDebug("Running {Tool}: {Path} {Arguments}", Tool.Name, toolPath, arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = toolPath,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(toolPath),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var sw = Stopwatch.StartNew();

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                Logger.LogTrace("[{Tool}] {Output}", Tool.Name, e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                Logger.LogTrace("[{Tool}] [ERR] {Output}", Tool.Name, e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(30);

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new ToolException($"Tool {Tool.Name} timed out after {effectiveTimeout}");
        }

        sw.Stop();

        var result = new ToolRunResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString(),
            Duration = sw.Elapsed
        };

        if (result.Success)
        {
            Logger.LogDebug("{Tool} completed successfully in {Duration:F1}s", Tool.Name, result.Duration.TotalSeconds);
        }
        else
        {
            Logger.LogWarning("{Tool} failed with exit code {ExitCode}", Tool.Name, result.ExitCode);
        }

        return result;
    }
}
