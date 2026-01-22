using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MorrowindMapGen.Core.Tools;

/// <summary>
/// Information about a plugin file and its master dependencies.
/// </summary>
public class PluginInfo
{
    /// <summary>
    /// The filename of the plugin (e.g., "Morrowind.esm").
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Full path to the plugin file.
    /// </summary>
    public required string FullPath { get; set; }

    /// <summary>
    /// Full path to the converted JSON file.
    /// </summary>
    public string? JsonPath { get; set; }

    /// <summary>
    /// List of master files this plugin depends on, in order.
    /// </summary>
    public List<string> Masters { get; set; } = new();

    /// <summary>
    /// The last (most dependent) master file this plugin should merge into.
    /// </summary>
    public string? MergeTarget => Masters.Count > 0 ? Masters[^1] : null;
}

/// <summary>
/// Represents a merge operation to perform.
/// </summary>
public class MergeOperation
{
    /// <summary>
    /// The plugin file to merge.
    /// </summary>
    public required PluginInfo Source { get; set; }

    /// <summary>
    /// The target file to merge into.
    /// </summary>
    public required string TargetFileName { get; set; }
}


/// <summary>
/// Calculates the correct order for merging plugins based on their master dependencies.
/// </summary>
public class PluginMergeCalculator
{
    private readonly ILogger<PluginMergeCalculator> _logger;

    public PluginMergeCalculator(ILogger<PluginMergeCalculator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads master dependencies from a tes3conv JSON file.
    /// </summary>
    /// <param name="jsonPath">Path to the JSON file.</param>
    /// <returns>List of master file names this plugin depends on.</returns>
    public List<string> ReadMasterDependencies(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"JSON file not found: {jsonPath}");
        }

        var jsonContent = File.ReadAllText(jsonPath);

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Expected JSON array at root");
        }

        // Find the header record (first record, type "Header" or "TES3")
        foreach (var element in root.EnumerateArray())
        {
            if (element.TryGetProperty("type", out var typeProp))
            {
                var type = typeProp.GetString();
                if (string.Equals(type, "Header", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, "TES3", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractMasterNames(element);
                }
            }
        }

        // No header found
        _logger.LogWarning("No header record found in {Path}", jsonPath);
        return new List<string>();
    }

    /// <summary>
    /// Extracts master file names from a header element.
    /// Handles both array format (["name", size]) and object format ({name, size}).
    /// </summary>
    private List<string> ExtractMasterNames(JsonElement headerElement)
    {
        var masters = new List<string>();

        if (!headerElement.TryGetProperty("masters", out var mastersElement))
        {
            return masters;
        }

        if (mastersElement.ValueKind != JsonValueKind.Array)
        {
            return masters;
        }

        foreach (var masterElement in mastersElement.EnumerateArray())
        {
            string? masterName = null;

            if (masterElement.ValueKind == JsonValueKind.Array)
            {
                // Array format: ["Morrowind.esm", 12345]
                if (masterElement.GetArrayLength() > 0)
                {
                    var firstElement = masterElement[0];
                    if (firstElement.ValueKind == JsonValueKind.String)
                    {
                        masterName = firstElement.GetString();
                    }
                }
            }
            else if (masterElement.ValueKind == JsonValueKind.Object)
            {
                // Object format: {"name": "Morrowind.esm", "size": 12345}
                if (masterElement.TryGetProperty("name", out var nameProp))
                {
                    masterName = nameProp.GetString();
                }
            }
            else if (masterElement.ValueKind == JsonValueKind.String)
            {
                // Simple string format: "Morrowind.esm"
                masterName = masterElement.GetString();
            }

            if (!string.IsNullOrWhiteSpace(masterName))
            {
                masters.Add(masterName);
            }
        }

        return masters;
    }

    /// <summary>
    /// Calculates the correct merge order for a set of plugins based on their dependencies.
    /// </summary>
    /// <param name="plugins">List of plugins with their master dependencies already populated.</param>
    /// <returns>Ordered list of merge operations to perform.</returns>
    public List<MergeOperation> CalculateMergeOrder(List<PluginInfo> plugins)
    {
        if (plugins.Count == 0)
        {
            return new List<MergeOperation>();
        }

        _logger.LogInformation("Calculating merge order for {Count} plugins...", plugins.Count);

        // Build a lookup by filename (case-insensitive)
        var pluginsByName = new Dictionary<string, PluginInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in plugins)
        {
            pluginsByName[plugin.FileName] = plugin;
        }

        // Build dependency graph
        // A plugin depends on all its masters
        var dependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in plugins)
        {
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var master in plugin.Masters)
            {
                // Only add dependency if the master is in our plugin list
                if (pluginsByName.ContainsKey(master))
                {
                    deps.Add(master);
                }
            }
            dependencies[plugin.FileName] = deps;
        }

        // Topological sort using Kahn's algorithm
        var sorted = new List<PluginInfo>();
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Initialize in-degrees
        foreach (var plugin in plugins)
        {
            inDegree[plugin.FileName] = 0;
        }

        // Calculate in-degrees
        foreach (var (pluginName, deps) in dependencies)
        {
            foreach (var dep in deps)
            {
                // dep -> pluginName (pluginName depends on dep)
                // So pluginName has an incoming edge from dep
                if (inDegree.ContainsKey(pluginName))
                {
                    inDegree[pluginName]++;
                }
            }
        }

        // Find all nodes with no dependencies
        var queue = new Queue<string>();
        foreach (var (name, degree) in inDegree)
        {
            if (degree == 0)
            {
                queue.Enqueue(name);
            }
        }

        // Process the queue
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(pluginsByName[current]);

            // For each plugin that depends on this one, reduce its in-degree
            foreach (var (pluginName, deps) in dependencies)
            {
                if (deps.Contains(current))
                {
                    inDegree[pluginName]--;
                    if (inDegree[pluginName] == 0)
                    {
                        queue.Enqueue(pluginName);
                    }
                }
            }
        }

        // Check for cycles
        if (sorted.Count != plugins.Count)
        {
            _logger.LogWarning("Dependency cycle detected! Using original order for remaining plugins.");
            // Add remaining plugins that weren't sorted
            foreach (var plugin in plugins)
            {
                if (!sorted.Contains(plugin))
                {
                    sorted.Add(plugin);
                }
            }
        }

        // Now create merge operations
        // Each plugin merges into its last listed master
        // We track where content ends up so subsequent merges target the right file
        var mergeOps = new List<MergeOperation>();

        // Track which file currently contains each plugin's content
        // (after merging A into B, A's content is now in B)
        var contentLocation = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in sorted)
        {
            contentLocation[plugin.FileName] = plugin.FileName;
        }

        // First plugin is our base master - no merge needed for it
        var baseMaster = sorted[0];
        _logger.LogDebug("Base master: {FileName}", baseMaster.FileName);

        // Process remaining plugins in topological order
        for (int i = 1; i < sorted.Count; i++)
        {
            var plugin = sorted[i];

            // Find the last master listed in the plugin's header that exists in our plugin list
            string targetFileName;
            if (plugin.Masters.Count > 0)
            {
                var lastMaster = plugin.Masters
                    .LastOrDefault(m => pluginsByName.ContainsKey(m));

                if (lastMaster != null)
                {
                    // Use the current location of that master's content
                    targetFileName = contentLocation[lastMaster];
                }
                else
                {
                    // No matching master found, merge into base
                    targetFileName = contentLocation[baseMaster.FileName];
                }
            }
            else
            {
                // No masters listed - merge into base master
                targetFileName = contentLocation[baseMaster.FileName];
            }

            mergeOps.Add(new MergeOperation
            {
                Source = plugin,
                TargetFileName = targetFileName
            });

            // After merge, this plugin's content is now in the target file
            contentLocation[plugin.FileName] = targetFileName;

            _logger.LogDebug("Merge: {Source} -> {Target}", plugin.FileName, targetFileName);
        }

        _logger.LogInformation("Calculated {Count} merge operations", mergeOps.Count);

        return mergeOps;
    }
}
