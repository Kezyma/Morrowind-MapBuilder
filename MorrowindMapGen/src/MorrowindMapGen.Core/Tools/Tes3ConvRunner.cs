using Microsoft.Extensions.Logging;

namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Runner for the tes3conv tool that converts ESM/ESP files to JSON.
/// </summary>
public class Tes3ConvRunner : ToolRunner
{
    public Tes3ConvRunner(ILogger<Tes3ConvRunner> logger, ToolManager toolManager)
        : base(logger, toolManager)
    {
    }

    protected override ToolInfo Tool => KnownTools.Tes3Conv;

    /// <summary>
    /// Converts an ESM/ESP file to JSON format.
    /// </summary>
    /// <param name="inputPath">Path to the ESM/ESP file.</param>
    /// <param name="outputPath">Path for the output JSON file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the generated JSON file.</returns>
    public async Task<string> ConvertToJsonAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            throw new ToolException($"Input file not found: {inputPath}");
        }

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var arguments = $"\"{inputPath}\" \"{outputPath}\"";
        var result = await RunToolAsync(arguments, cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new ToolException($"tes3conv failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        if (!File.Exists(outputPath))
        {
            throw new ToolException($"tes3conv did not produce expected output file: {outputPath}");
        }

        Logger.LogInformation("Converted {Input} to JSON ({Duration:F1}s)",
            Path.GetFileName(inputPath), result.Duration.TotalSeconds);

        return outputPath;
    }
}
