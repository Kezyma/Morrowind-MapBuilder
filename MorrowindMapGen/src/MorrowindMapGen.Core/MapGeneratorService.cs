using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core.Configuration;
using MorrowindMapGen.Core.MapGeneration;
using MorrowindMapGen.Core.Markers;
using MorrowindMapGen.Core.Markers.Models;
using MorrowindMapGen.Core.Output;
using MorrowindMapGen.Core.Tools;

namespace MorrowindMapGen.Core;

/// <summary>
/// Progress information for map generation.
/// </summary>
public class GenerationProgress
{
    /// <summary>
    /// Current step name.
    /// </summary>
    public string Step { get; set; } = string.Empty;

    /// <summary>
    /// Current step number (1-based).
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Total number of steps.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Progress within the current step (0-100), or -1 for indeterminate.
    /// </summary>
    public int StepProgress { get; set; } = -1;

    /// <summary>
    /// Detailed status message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Whether this is the final completion message.
    /// </summary>
    public bool IsComplete { get; set; }
}

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
    /// Whether to run the OpenMW Map Generator tool to create raw tiles.
    /// When false, uses existing tiles from the tiles folder.
    /// </summary>
    public bool GenerateTiles { get; set; } = true;

    /// <summary>
    /// Whether to generate the web map (tiles and HTML viewer).
    /// </summary>
    public bool GenerateWebMap { get; set; } = true;

    /// <summary>
    /// Whether to generate a full-size stitched map image.
    /// </summary>
    public bool GenerateBigMap { get; set; }

    /// <summary>
    /// Whether to extract and include map markers.
    /// </summary>
    public bool GenerateMarkers { get; set; }

    /// <summary>
    /// Whether to use 512px tiles instead of 256px (reduces HTTP requests by 4x).
    /// </summary>
    public bool Use512pxTiles { get; set; }

    /// <summary>
    /// Output format for generated tiles.
    /// </summary>
    public TileOutputFormat OutputFormat { get; set; } = TileOutputFormat.WebP;

    /// <summary>
    /// Custom directory for external tools (optional).
    /// </summary>
    public string? ToolsDirectory { get; set; }

    /// <summary>
    /// Directory containing raw tiles (when GenerateTiles is false).
    /// If null, uses 'tiles' folder next to output directory.
    /// </summary>
    public string? TilesDirectory { get; set; }

    /// <summary>
    /// Additional tile layers to include (underlayers and overlayers).
    /// </summary>
    public List<LayerInfo> Layers { get; set; } = new();

    /// <summary>
    /// Progress reporter for UI updates.
    /// </summary>
    public IProgress<GenerationProgress>? Progress { get; set; }

    /// <summary>
    /// Whether cell markers are enabled by default in the viewer.
    /// </summary>
    public bool CellMarkersEnabled { get; set; } = true;

    /// <summary>
    /// Whether door markers are enabled by default in the viewer.
    /// </summary>
    public bool DoorMarkersEnabled { get; set; } = false;

    /// <summary>
    /// Whether the generated map layer is an overlay instead of a base layer.
    /// When true, there must be at least one other base layer in Layers.
    /// </summary>
    public bool GeneratedMapIsOverlay { get; set; } = false;
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

        // Calculate total steps based on options
        var totalSteps = CalculateTotalSteps(options);
        var currentStep = 0;

        void ReportProgress(string step, string? message = null, int stepProgress = -1, bool isComplete = false)
        {
            currentStep++;
            var progress = new GenerationProgress
            {
                Step = step,
                CurrentStep = currentStep,
                TotalSteps = totalSteps,
                StepProgress = stepProgress,
                Message = message,
                IsComplete = isComplete
            };
            options.Progress?.Report(progress);
        }

        // Parse configuration
        ReportProgress("Parsing configuration");
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

        // Create tile processing options
        var tileOptions = new TileProcessingOptions
        {
            OutputFormat = options.OutputFormat,
            Use512pxTiles = options.Use512pxTiles
        };

        // Determine tiles directory - next to executable for portability
        var exeDir = AppContext.BaseDirectory;
        var defaultTilesDir = Path.Combine(exeDir, "tiles");

        try
        {
            string rawTilesDir;

            // Step 1: Generate map tiles using OpenMW Map Generator (if requested)
            if (options.GenerateTiles)
            {
                ReportProgress("Generating map tiles", "Running OpenMW Map Generator...");
                _logger.LogInformation("=== Generating map tiles ===");
                var mapGenRunner = new OpenMWMapGenRunner(
                    _loggerFactory.CreateLogger<OpenMWMapGenRunner>(),
                    toolManager);

                // Generate tiles next to executable
                rawTilesDir = options.TilesDirectory ?? defaultTilesDir;
                Directory.CreateDirectory(rawTilesDir);

                rawTilesDir = await mapGenRunner.GenerateMapAsync(
                    gameConfig,
                    rawTilesDir,
                    new MapGenOptions { OverwriteMaps = true },
                    cancellationToken);
            }
            else
            {
                // Use existing tiles from specified or default location
                rawTilesDir = options.TilesDirectory ?? defaultTilesDir;

                if (!Directory.Exists(rawTilesDir))
                {
                    throw new InvalidOperationException($"Tiles directory not found: {rawTilesDir}");
                }
                _logger.LogInformation("Using existing tiles from: {Path}", rawTilesDir);
            }

            MapMetadata? metadata = null;

            // Step 2: Process tiles into zoom pyramid (if generating web map)
            if (options.GenerateWebMap)
            {
                ReportProgress("Processing tiles", "Creating zoom pyramid...");
                _logger.LogInformation("=== Processing tiles ===");
                var tileProcessor = new TileProcessor(_loggerFactory.CreateLogger<TileProcessor>(), _loggerFactory, tileOptions);
                metadata = await tileProcessor.ProcessTilesAsync(
                    rawTilesDir,
                    options.OutputDirectory,
                    options.Layers,
                    cancellationToken);
            }

            // Step 3: Extract or load markers
            MarkerCollection? markers = null;
            var markersPath = Path.Combine(options.OutputDirectory, "markers.json");
            var markersNeeded = options.GenerateWebMap && (options.CellMarkersEnabled || options.DoorMarkersEnabled);

            if (options.GenerateMarkers)
            {
                // Generate fresh markers
                ReportProgress("Extracting markers", "Merging plugins and extracting data...");
                _logger.LogInformation("=== Extracting markers ===");

                // We need metadata for marker extraction - if we didn't generate web map, create minimal metadata
                if (metadata == null)
                {
                    var tileProcessor = new TileProcessor(_loggerFactory.CreateLogger<TileProcessor>(), _loggerFactory, tileOptions);
                    metadata = tileProcessor.AnalyzeTiles(rawTilesDir);
                }

                markers = await ExtractMarkersAsync(
                    gameConfig,
                    toolManager,
                    workingDir,
                    metadata,
                    cancellationToken);

                // Save markers.json for future use
                var markerExtractor = new MarkerExtractor(_loggerFactory.CreateLogger<MarkerExtractor>());
                markerExtractor.SaveToJson(markers, markersPath);
            }
            else if (markersNeeded)
            {
                // Try to load existing markers if we need them for the web map
                _logger.LogInformation("Loading existing markers...");
                var markerExtractor = new MarkerExtractor(_loggerFactory.CreateLogger<MarkerExtractor>());
                markers = markerExtractor.LoadFromJson(markersPath);

                if (markers == null)
                {
                    _logger.LogWarning("No saved markers found at {Path}. Web map will have no markers.", markersPath);
                }
            }

            // Step 4: Generate HTML viewer (if generating web map)
            if (options.GenerateWebMap)
            {
                ReportProgress("Generating HTML viewer");
                _logger.LogInformation("=== Generating HTML viewer ===");

                var htmlOptions = new HtmlGeneratorOptions
                {
                    TileExtension = tileOptions.FileExtension,
                    CellMarkersEnabled = options.CellMarkersEnabled,
                    DoorMarkersEnabled = options.DoorMarkersEnabled,
                    GeneratedMapIsOverlay = options.GeneratedMapIsOverlay,
                    BaseMapHasFallback = metadata?.HasFallback ?? false
                };

                var htmlGenerator = new HtmlGenerator(_loggerFactory.CreateLogger<HtmlGenerator>());
                var htmlPath = Path.Combine(options.OutputDirectory, "map.html");
                htmlGenerator.GenerateViewer(htmlPath, metadata!, options.Layers, markers, htmlOptions);
            }

            // Step 5: Generate big map (if requested)
            if (options.GenerateBigMap)
            {
                ReportProgress("Generating full-size map", "Stitching tiles...");
                _logger.LogInformation("=== Generating full-size map ===");

                if (metadata == null)
                {
                    var tileProcessor = new TileProcessor(_loggerFactory.CreateLogger<TileProcessor>(), _loggerFactory, tileOptions);
                    metadata = tileProcessor.AnalyzeTiles(rawTilesDir);
                }

                var mapStitcher = new MapStitcher(_loggerFactory.CreateLogger<MapStitcher>());
                var mapPath = Path.Combine(options.OutputDirectory, "map.png");
                await mapStitcher.StitchMapAsync(options.OutputDirectory, metadata, mapPath, cancellationToken);
            }

            // Report completion
            ReportProgress("Complete", "Map generation complete!", 100, isComplete: true);
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
    /// Calculates the total number of steps based on options.
    /// </summary>
    private static int CalculateTotalSteps(MapGeneratorOptions options)
    {
        var steps = 1; // Configuration parsing always happens

        if (options.GenerateTiles) steps++;
        if (options.GenerateWebMap) steps += 2; // Process tiles + HTML
        if (options.GenerateMarkers) steps++;
        if (options.GenerateBigMap) steps++;

        steps++; // Final completion step

        return steps;
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
            toolManager,
            _loggerFactory);

        var mergedPath = Path.Combine(mergeDir, "merged.esm");
        await mergeRunner.MergePluginsAsync(gameConfig, mergedPath, cancellationToken: cancellationToken);

        // Convert to JSON
        _logger.LogInformation("Converting to JSON...");
        var tes3convRunner = new Tes3ConvRunner(
            _loggerFactory.CreateLogger<Tes3ConvRunner>(),
            toolManager);

        var jsonPath = Path.Combine(mergeDir, "merged.json");
        await tes3convRunner.ConvertToJsonAsync(mergedPath, jsonPath, cancellationToken);

        // Extract markers (uses raw game grid coordinates)
        _logger.LogInformation("Extracting markers from JSON...");
        var markerExtractor = new MarkerExtractor(_loggerFactory.CreateLogger<MarkerExtractor>());
        return markerExtractor.ExtractFromJson(jsonPath);
    }
}
