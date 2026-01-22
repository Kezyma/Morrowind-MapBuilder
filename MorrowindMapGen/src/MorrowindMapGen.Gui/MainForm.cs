using Microsoft.Extensions.Logging;
using MorrowindMapGen.Core;
using MorrowindMapGen.Core.MapGeneration;
using MorrowindMapGen.Gui.Models;
using MorrowindMapGen.Gui.Services;

namespace MorrowindMapGen.Gui;

public partial class MainForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly ConfigDetectionService _configDetectionService;
    private GuiSettings _settings;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isGenerating;

    public MainForm()
    {
        InitializeComponent();

        _settingsService = new SettingsService();
        _configDetectionService = new ConfigDetectionService();
        _settings = _settingsService.Load();

        LoadSettingsToUI();
        UpdateUIState();
    }

    private void LoadSettingsToUI()
    {
        // Game type
        if (_settings.GameType == GameType.OpenMW)
        {
            radioOpenMW.Checked = true;
        }
        else
        {
            radioMorrowind.Checked = true;
        }

        // Config paths
        txtOpenMWConfig.Text = _settings.OpenMWConfigPath ?? _configDetectionService.GetDefaultOpenMWConfigPath();
        txtMorrowindIni.Text = _settings.MorrowindIniPath ?? _configDetectionService.GetDefaultMorrowindIniPath() ?? string.Empty;
        txtOutputDir.Text = _settings.OutputDirectory ?? string.Empty;

        // Steps
        chkGenerateTiles.Checked = _settings.GenerateTiles;
        chkGenerateMarkers.Checked = _settings.GenerateMarkers;
        chkGenerateBigMap.Checked = _settings.GenerateBigMap;
        chkGenerateWebMap.Checked = _settings.GenerateWebMap;
        chk512pxMode.Checked = _settings.Use512pxTiles;

        // Layers
        RefreshLayersList();
    }

    private void SaveSettingsFromUI()
    {
        _settings.GameType = radioOpenMW.Checked ? GameType.OpenMW : GameType.Morrowind;
        _settings.OpenMWConfigPath = txtOpenMWConfig.Text;
        _settings.MorrowindIniPath = txtMorrowindIni.Text;
        _settings.OutputDirectory = txtOutputDir.Text;
        _settings.GenerateTiles = chkGenerateTiles.Checked;
        _settings.GenerateMarkers = chkGenerateMarkers.Checked;
        _settings.GenerateBigMap = chkGenerateBigMap.Checked;
        _settings.GenerateWebMap = chkGenerateWebMap.Checked;
        _settings.Use512pxTiles = chk512pxMode.Checked;

        _settingsService.Save(_settings);
    }

    private void RefreshLayersList()
    {
        listLayers.Items.Clear();
        var allLayers = _settings.GetAllLayers();

        foreach (var layer in allLayers)
        {
            var item = new ListViewItem(layer.Name)
            {
                Tag = layer
            };
            item.SubItems.Add(layer.IsOverlay ? "Overlay" : "Base Layer");
            item.SubItems.Add(layer.SourcePath ?? "(Built-in)");
            item.SubItems.Add(layer.EnabledByDefault ? "Yes" : "No");
            listLayers.Items.Add(item);
        }
    }

    private void UpdateUIState()
    {
        // Config paths
        txtOpenMWConfig.Enabled = radioOpenMW.Checked;
        btnBrowseOpenMW.Enabled = radioOpenMW.Checked;
        txtMorrowindIni.Enabled = radioMorrowind.Checked;
        btnBrowseMorrowind.Enabled = radioMorrowind.Checked;

        // Generation controls
        var canGenerate = !_isGenerating && !string.IsNullOrWhiteSpace(txtOutputDir.Text);
        btnGenerate.Enabled = canGenerate;
        btnCancel.Enabled = _isGenerating;

        // Layer controls
        var selectedLayer = GetSelectedLayer();
        var isCustomLayer = selectedLayer != null && !selectedLayer.IsBuiltIn;
        btnRemoveLayer.Enabled = isCustomLayer;
        btnRenameLayer.Enabled = isCustomLayer;

        // For move buttons, check position within custom layers only
        var customLayerCount = _settings.CustomLayers.Count;
        var customLayerIndex = selectedLayer != null ? _settings.CustomLayers.OrderBy(l => l.SortOrder).ToList().FindIndex(l => l.Id == selectedLayer.Id) : -1;
        btnMoveUp.Enabled = isCustomLayer && customLayerIndex > 0;
        btnMoveDown.Enabled = isCustomLayer && customLayerIndex >= 0 && customLayerIndex < customLayerCount - 1;

        // Toggle type button - enabled for Generated Map and custom layers, not for Cells/Doors
        var canToggleType = selectedLayer != null &&
            (selectedLayer.Name == "Generated Map" || !selectedLayer.IsBuiltIn);
        btnToggleType.Enabled = canToggleType;

        // Update button text to show what action it will take
        if (selectedLayer != null && canToggleType)
        {
            btnToggleType.Text = selectedLayer.IsOverlay ? "Set as Base" : "Set as Overlay";
        }
        else
        {
            btnToggleType.Text = "Toggle Type";
        }

        // Toggle Enabled button - enabled for custom layers and Cells/Doors, not Generated Map
        var canToggleEnabled = selectedLayer != null &&
            (selectedLayer.Name != "Generated Map");
        btnToggleEnabled.Enabled = canToggleEnabled;

        // Update Toggle Enabled button text based on current state
        if (selectedLayer != null && canToggleEnabled)
        {
            bool isEnabled;
            if (selectedLayer.Name == "Cells")
                isEnabled = _settings.CellMarkersEnabled;
            else if (selectedLayer.Name == "Doors")
                isEnabled = _settings.DoorMarkersEnabled;
            else if (selectedLayer.Name == "Fast Travel")
                isEnabled = _settings.FastTravelEnabled;
            else
                isEnabled = selectedLayer.EnabledByDefault;

            btnToggleEnabled.Text = isEnabled ? "Disable" : "Enable";
        }
        else
        {
            btnToggleEnabled.Text = "Toggle Enabled";
        }
    }

    private GuiLayerInfo? GetSelectedLayer()
    {
        if (listLayers.SelectedItems.Count == 0)
            return null;
        return listLayers.SelectedItems[0].Tag as GuiLayerInfo;
    }

    #region Event Handlers

    private void radioOpenMW_CheckedChanged(object sender, EventArgs e)
    {
        UpdateUIState();
    }

    private void radioMorrowind_CheckedChanged(object sender, EventArgs e)
    {
        UpdateUIState();
    }

    private void btnBrowseOpenMW_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select OpenMW Configuration File",
            Filter = "OpenMW Config|openmw.cfg|All Files|*.*",
            FileName = "openmw.cfg"
        };

        if (!string.IsNullOrEmpty(txtOpenMWConfig.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(txtOpenMWConfig.Text);
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtOpenMWConfig.Text = dialog.FileName;
        }
    }

    private void btnBrowseMorrowind_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Morrowind.ini File",
            Filter = "Morrowind Config|Morrowind.ini|All Files|*.*",
            FileName = "Morrowind.ini"
        };

        if (!string.IsNullOrEmpty(txtMorrowindIni.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(txtMorrowindIni.Text);
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtMorrowindIni.Text = dialog.FileName;
        }
    }

    private void btnBrowseOutput_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select Output Directory",
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrEmpty(txtOutputDir.Text) && Directory.Exists(txtOutputDir.Text))
        {
            dialog.InitialDirectory = txtOutputDir.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtOutputDir.Text = dialog.SelectedPath;
            UpdateUIState();
        }
    }

    private void btnAddLayer_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select Layer Source Folder",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var folderName = Path.GetFileName(dialog.SelectedPath);
            var layer = GuiLayerInfo.CreateCustomLayer(folderName, true, dialog.SelectedPath);
            layer.SortOrder = _settings.CustomLayers.Count + 1;
            _settings.CustomLayers.Add(layer);
            RefreshLayersList();
        }
    }

    private void btnRemoveLayer_Click(object sender, EventArgs e)
    {
        var selectedLayer = GetSelectedLayer();
        if (selectedLayer == null || selectedLayer.IsBuiltIn)
            return;

        _settings.CustomLayers.RemoveAll(l => l.Id == selectedLayer.Id);
        RefreshLayersList();
        UpdateUIState();
    }

    private void btnMoveUp_Click(object sender, EventArgs e)
    {
        var selectedLayer = GetSelectedLayer();
        if (selectedLayer == null || selectedLayer.IsBuiltIn)
            return;

        // Sort custom layers by SortOrder to find neighbors
        var sortedLayers = _settings.CustomLayers.OrderBy(l => l.SortOrder).ToList();
        var index = sortedLayers.FindIndex(l => l.Id == selectedLayer.Id);

        if (index > 0)
        {
            // Swap SortOrder values with the layer above
            var layerAbove = sortedLayers[index - 1];
            var currentLayer = sortedLayers[index];
            (currentLayer.SortOrder, layerAbove.SortOrder) = (layerAbove.SortOrder, currentLayer.SortOrder);

            RefreshLayersList();
            // Re-select the moved layer
            SelectLayerById(selectedLayer.Id);
        }
    }

    private void btnMoveDown_Click(object sender, EventArgs e)
    {
        var selectedLayer = GetSelectedLayer();
        if (selectedLayer == null || selectedLayer.IsBuiltIn)
            return;

        // Sort custom layers by SortOrder to find neighbors
        var sortedLayers = _settings.CustomLayers.OrderBy(l => l.SortOrder).ToList();
        var index = sortedLayers.FindIndex(l => l.Id == selectedLayer.Id);

        if (index >= 0 && index < sortedLayers.Count - 1)
        {
            // Swap SortOrder values with the layer below
            var layerBelow = sortedLayers[index + 1];
            var currentLayer = sortedLayers[index];
            (currentLayer.SortOrder, layerBelow.SortOrder) = (layerBelow.SortOrder, currentLayer.SortOrder);

            RefreshLayersList();
            // Re-select the moved layer
            SelectLayerById(selectedLayer.Id);
        }
    }

    private void SelectLayerById(Guid id)
    {
        foreach (ListViewItem item in listLayers.Items)
        {
            if (item.Tag is GuiLayerInfo layer && layer.Id == id)
            {
                item.Selected = true;
                item.EnsureVisible();
                break;
            }
        }
    }

    private void listLayers_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateUIState();
    }

    private void btnRenameLayer_Click(object sender, EventArgs e)
    {
        var selectedLayer = GetSelectedLayer();
        if (selectedLayer == null || selectedLayer.IsBuiltIn)
            return;

        // Show input dialog for new name
        var newName = ShowInputDialog("Rename Layer", "Enter new layer name:", selectedLayer.Name);
        if (!string.IsNullOrWhiteSpace(newName) && newName != selectedLayer.Name)
        {
            // Find and update the layer in CustomLayers
            var customLayer = _settings.CustomLayers.FirstOrDefault(l => l.Id == selectedLayer.Id);
            if (customLayer != null)
            {
                customLayer.Name = newName;
                RefreshLayersList();
                SelectLayerById(selectedLayer.Id);
            }
        }
    }

    private static string? ShowInputDialog(string title, string prompt, string defaultValue)
    {
        using var form = new Form
        {
            Text = title,
            Width = 350,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var label = new Label { Left = 15, Top = 15, Text = prompt, AutoSize = true };
        var textBox = new TextBox { Left = 15, Top = 40, Width = 300, Text = defaultValue };
        var okButton = new Button { Text = "OK", Left = 155, Top = 75, Width = 75, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", Left = 240, Top = 75, Width = 75, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        textBox.SelectAll();

        return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
    }

    private void btnToggleType_Click(object sender, EventArgs e)
    {
        var selectedLayer = GetSelectedLayer();
        if (selectedLayer == null)
            return;

        // Check if this is the Generated Map layer
        if (selectedLayer.IsBuiltIn && selectedLayer.Name == "Generated Map")
        {
            // Check if we can make it an overlay (need at least one other base layer)
            if (!_settings.GeneratedMapIsOverlay)
            {
                // Trying to change from base to overlay
                var otherBaseLayers = _settings.CustomLayers.Count(l => !l.IsOverlay);
                if (otherBaseLayers == 0)
                {
                    MessageBox.Show("Cannot change Generated Map to overlay. There must be at least one base layer.\n\nAdd another layer and set it as base layer first.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            _settings.GeneratedMapIsOverlay = !_settings.GeneratedMapIsOverlay;
        }
        else if (selectedLayer.IsBuiltIn)
        {
            // Cells and Doors are always overlays
            MessageBox.Show("Marker layers (Cells, Doors) are always overlays.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        else
        {
            // Custom layer - find it and toggle
            var customLayer = _settings.CustomLayers.FirstOrDefault(l => l.Id == selectedLayer.Id);
            if (customLayer != null)
            {
                // Check if we can make it an overlay
                if (!customLayer.IsOverlay)
                {
                    // Trying to change from base to overlay - check if it's the only base layer
                    if (_settings.GetBaseLayerCount() <= 1)
                    {
                        MessageBox.Show("Cannot change to overlay. There must be at least one base layer.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                customLayer.IsOverlay = !customLayer.IsOverlay;
            }
        }

        RefreshLayersList();
        UpdateUIState();
    }

    private void btnToggleEnabled_Click(object sender, EventArgs e)
    {
        var selectedLayer = GetSelectedLayer();
        if (selectedLayer == null)
            return;

        // Check if this is a built-in marker layer (Cells/Doors/Fast Travel)
        if (selectedLayer.IsBuiltIn && selectedLayer.Name == "Cells")
        {
            _settings.CellMarkersEnabled = !_settings.CellMarkersEnabled;
        }
        else if (selectedLayer.IsBuiltIn && selectedLayer.Name == "Doors")
        {
            _settings.DoorMarkersEnabled = !_settings.DoorMarkersEnabled;
        }
        else if (selectedLayer.IsBuiltIn && selectedLayer.Name == "Fast Travel")
        {
            _settings.FastTravelEnabled = !_settings.FastTravelEnabled;
        }
        else if (selectedLayer.IsBuiltIn && selectedLayer.Name == "Generated Map")
        {
            // Generated Map is always enabled as a base or overlay, can't be disabled
            MessageBox.Show("The Generated Map layer is always enabled.", "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        else
        {
            // Custom layer - find it and toggle
            var customLayer = _settings.CustomLayers.FirstOrDefault(l => l.Id == selectedLayer.Id);
            if (customLayer != null)
            {
                customLayer.EnabledByDefault = !customLayer.EnabledByDefault;
            }
        }

        RefreshLayersList();
        UpdateUIState();
    }

    private void txtOpenMWConfig_Click(object sender, EventArgs e)
    {
        if (radioOpenMW.Checked)
        {
            btnBrowseOpenMW_Click(sender, e);
        }
    }

    private void txtMorrowindIni_Click(object sender, EventArgs e)
    {
        if (radioMorrowind.Checked)
        {
            btnBrowseMorrowind_Click(sender, e);
        }
    }

    private void txtOutputDir_Click(object sender, EventArgs e)
    {
        btnBrowseOutput_Click(sender, e);
    }

    private async void btnGenerate_Click(object sender, EventArgs e)
    {
        if (_isGenerating)
            return;

        // Validate inputs
        var configPath = radioOpenMW.Checked ? txtOpenMWConfig.Text : txtMorrowindIni.Text;
        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            MessageBox.Show("Please select a valid configuration file.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtOutputDir.Text))
        {
            MessageBox.Show("Please select an output directory.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Save settings before generation
        SaveSettingsFromUI();

        // Start generation
        _isGenerating = true;
        _cancellationTokenSource = new CancellationTokenSource();
        UpdateUIState();
        progressBar.Style = ProgressBarStyle.Marquee;
        lblStatus.Text = "Starting generation...";

        try
        {
            var progress = new Progress<GenerationProgress>(OnProgressUpdate);

            var options = new MapGeneratorOptions
            {
                ConfigPath = configPath,
                OutputDirectory = txtOutputDir.Text,
                GenerateTiles = chkGenerateTiles.Checked,
                GenerateMarkers = chkGenerateMarkers.Checked,
                GenerateBigMap = chkGenerateBigMap.Checked,
                GenerateWebMap = chkGenerateWebMap.Checked,
                Use512pxTiles = chk512pxMode.Checked,
                OutputFormat = TileOutputFormat.WebP,
                CellMarkersEnabled = _settings.CellMarkersEnabled,
                DoorMarkersEnabled = _settings.DoorMarkersEnabled,
                FastTravelEnabled = _settings.FastTravelEnabled,
                GeneratedMapIsOverlay = _settings.GeneratedMapIsOverlay,
                Progress = progress,
                Layers = _settings.CustomLayers.Select(l => l.ToLayerInfo()).ToList()
            };

            // Create logger factory
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var service = new MapGeneratorService(loggerFactory);
            await service.GenerateAsync(options, _cancellationTokenSource.Token);

            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 100;
            lblStatus.Text = "Generation complete!";

            MessageBox.Show("Map generation completed successfully!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Generation cancelled.";
        }
        catch (Exception ex)
        {
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 0;
            lblStatus.Text = "Generation failed.";

            MessageBox.Show($"Generation failed:\n\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isGenerating = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            UpdateUIState();
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        lblStatus.Text = "Cancelling...";
    }

    private void OnProgressUpdate(GenerationProgress progress)
    {
        if (InvokeRequired)
        {
            Invoke(() => OnProgressUpdate(progress));
            return;
        }

        lblStatus.Text = $"[{progress.CurrentStep}/{progress.TotalSteps}] {progress.Step}";
        if (!string.IsNullOrEmpty(progress.Message))
        {
            lblStatus.Text += $" - {progress.Message}";
        }

        if (progress.StepProgress >= 0)
        {
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = progress.StepProgress;
        }
        else
        {
            progressBar.Style = ProgressBarStyle.Marquee;
        }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (_isGenerating)
        {
            var result = MessageBox.Show(
                "Generation is in progress. Are you sure you want to close?",
                "Confirm Close",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            _cancellationTokenSource?.Cancel();
        }

        SaveSettingsFromUI();
    }

    #endregion
}
