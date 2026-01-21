using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.Configuration;
using MorrowindMapGen.Core.MapGeneration;
using MorrowindMapGen.Core.Markers;
using MorrowindMapGen.Core.Markers.Models;
using MorrowindMapGen.Core.Output;
using MorrowindMapGen.Core.Tools;

namespace MorrowindMapGen.Core;

/// <summary>
/// Options for map generation.
/// </summary>
public class MapGeneratorOptions
{
    /// <summary>
    /// Path to the configuration file (Morrowind.ini or openmw.cfg).
    /// </summary>
    public required string ConfigPath { get; set; }

    /// <summary>
    /// Output directory for generated files.
    /// </summary>
    public required string OutputDirectory { get; set; }

    /// <summary>
    /// Whether to generate a full-size stitched map image.
    /// </summary>
    public bool GenerateBigMap { get; set; }

    /// <summary>
    /// Whether to extract and include map markers.
    /// </summary>
    public bool GenerateMarkers { get; set; }

    /// <summary>
    /// Custom directory for external tools (optional).
    /// </summary>
    public string? ToolsDirectory { get; set; }

    /// <summary>
    /// Additional tile layers to include (underlayers and overlayers).
    /// </summary>
    public List<LayerInfo> Layers { get; set; } = new();
}

/// <summary>
/// Main service that orchestrates the map generation process.
/// </summary>
public class MapGeneratorService
{
    private readonly ILogger<MapGeneratorService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public MapGeneratorService(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<MapGeneratorService>();
    }

    /// <summary>
    /// Generates a map with the specified options.
    /// </summary>
    public async Task GenerateAsync(MapGeneratorOptions options, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting map generation...");
        _logger.LogInformation("Config: {Config}", options.ConfigPath);
        _logger.LogInformation("Output: {Output}", options.OutputDirectory);

        // Parse configuration
        _logger.LogInformation("Parsing configuration...");
        var configParser = new ConfigParserFactory();
        var gameConfig = configParser.Parse(options.ConfigPath);

        var validationErrors = gameConfig.Validate();
        if (validationErrors.Count > 0)
        {
            foreach (var error in validationErrors)
            {
                _logger.LogError("Configuration error: {Error}", error);
            }
            throw new InvalidOperationException("Configuration validation failed.");
        }

        _logger.LogInformation("Found {Count} plugins in load order", gameConfig.EnabledPlugins.Count);
        foreach (var plugin in gameConfig.GetPluginsInLoadOrder())
        {
            _logger.LogDebug("  {Plugin}", plugin);
        }

        // Initialize tool manager
        var toolManager = new ToolManager(
            _loggerFactory.CreateLogger<ToolManager>(),
            options.ToolsDirectory);

        // Create working directory
        var workingDir = Path.Combine(options.OutputDirectory, ".work");
        Directory.CreateDirectory(workingDir);

        try
        {
            // Step 1: Generate map tiles using OpenMW Map Generator
            _logger.LogInformation("=== Step 1: Generating map tiles ===");
            var mapGenRunner = new OpenMWMapGenRunner(
                _loggerFactory.CreateLogger<OpenMWMapGenRunner>(),
                toolManager);

            var tilesDir = Path.Combine(workingDir, "tiles");
            var rawTilesDir = await mapGenRunner.GenerateMapAsync(
                gameConfig,
                tilesDir,
                new MapGenOptions { OverwriteMaps = true },
                cancellationToken);

            // Step 2: Process tiles into zoom pyramid
            _logger.LogInformation("=== Step 2: Processing tiles ===");
            var tileProcessor = new TileProcessor(_loggerFactory.CreateLogger<TileProcessor>());
            var metadata = await tileProcessor.ProcessTilesAsync(
                rawTilesDir,
                options.OutputDirectory,
                options.Layers,
                cancellationToken);

            // Step 3: Extract markers (if requested)
            MarkerCollection? markers = null;
            if (options.GenerateMarkers)
            {
                _logger.LogInformation("=== Step 3: Extracting markers ===");
                markers = await ExtractMarkersAsync(
                    gameConfig,
                    toolManager,
                    workingDir,
                    metadata,
                    cancellationToken);

                // Save markers.json
                var markerExtractor = new MarkerExtractor(_loggerFactory.CreateLogger<MarkerExtractor>());
                var markersPath = Path.Combine(options.OutputDirectory, "markers.json");
                markerExtractor.SaveToJson(markers, markersPath);
            }

            // Step 4: Generate HTML viewer
            _logger.LogInformation("=== Step 4: Generating HTML viewer ===");
            var htmlGenerator = new HtmlGenerator(_loggerFactory.CreateLogger<HtmlGenerator>());
            var htmlPath = Path.Combine(options.OutputDirectory, "map.html");
            htmlGenerator.GenerateViewer(htmlPath, metadata, options.Layers, markers);

            // Step 5: Generate big map (if requested)
            if (options.GenerateBigMap)
            {
                _logger.LogInformation("=== Step 5: Generating full-size map ===");
                var mapStitcher = new MapStitcher(_loggerFactory.CreateLogger<MapStitcher>());
                var mapPath = Path.Combine(options.OutputDirectory, "map.png");
                await mapStitcher.StitchMapAsync(options.OutputDirectory, metadata, mapPath, cancellationToken);
            }

            _logger.LogInformation("=== Map generation complete! ===");
            _logger.LogInformation("Output directory: {Dir}", options.OutputDirectory);
        }
        finally
        {
            // Cleanup working directory
            if (Directory.Exists(workingDir))
            {
                try
                {
                    Directory.Delete(workingDir, recursive: true);
                    _logger.LogDebug("Cleaned up working directory");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to clean up working directory: {Message}", ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// Extracts markers by merging plugins and converting to JSON.
    /// </summary>
    /// <param name="gameConfig">Game configuration.</param>
    /// <param name="toolManager">Tool manager.</param>
    /// <param name="workingDir">Working directory.</param>
    /// <param name="metadata">Map metadata with tile bounds for coordinate normalization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<MarkerCollection> ExtractMarkersAsync(
        GameConfiguration gameConfig,
        ToolManager toolManager,
        string workingDir,
        MapMetadata metadata,
        CancellationToken cancellationToken)
    {
        var mergeDir = Path.Combine(workingDir, "merge");
        Directory.CreateDirectory(mergeDir);

        // Merge all plugins
        _logger.LogInformation("Merging plugins...");
        var mergeRunner = new MergeToMasterRunner(
            _loggerFactory.CreateLogger<MergeToMasterRunner>(),
            toolManager);

        var mergedPath = Path.Combine(mergeDir, "merged.esm");
        await mergeRunner.MergePluginsAsync(gameConfig, mergedPath, cancellationToken: cancellationToken);

        // Convert to JSON
        _logger.LogInformation("Converting to JSON...");
        var tes3convRunner = new Tes3ConvRunner(
            _loggerFactory.CreateLogger<Tes3ConvRunner>(),
            toolManager);

        var jsonPath = Path.Combine(mergeDir, "merged.json");
        await tes3convRunner.ConvertToJsonAsync(mergedPath, jsonPath, cancellationToken);

        // Extract markers (pass map bounds for coordinate normalization)
        _logger.LogInformation("Extracting markers from JSON...");
        var markerExtractor = new MarkerExtractor(_loggerFactory.CreateLogger<MarkerExtractor>());
        return markerExtractor.ExtractFromJson(jsonPath, metadata.MinX, metadata.MaxY);
    }
}
