namespace MorrowindMapGen.Gui;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        // Main layout
        this.tableLayoutMain = new TableLayoutPanel();
        this.groupConfig = new GroupBox();
        this.groupSteps = new GroupBox();
        this.groupLayers = new GroupBox();
        this.groupProgress = new GroupBox();

        // Config controls
        this.radioOpenMW = new RadioButton();
        this.radioMorrowind = new RadioButton();
        this.lblOpenMWConfig = new Label();
        this.txtOpenMWConfig = new TextBox();
        this.btnBrowseOpenMW = new Button();
        this.lblMorrowindIni = new Label();
        this.txtMorrowindIni = new TextBox();
        this.btnBrowseMorrowind = new Button();
        this.lblOutputDir = new Label();
        this.txtOutputDir = new TextBox();
        this.btnBrowseOutput = new Button();

        // Steps controls
        this.chkGenerateTiles = new CheckBox();
        this.chkGenerateMarkers = new CheckBox();
        this.chkGenerateBigMap = new CheckBox();
        this.chkGenerateWebMap = new CheckBox();
        this.chk512pxMode = new CheckBox();

        // Layers controls
        this.listLayers = new ListView();
        this.colName = new ColumnHeader();
        this.colType = new ColumnHeader();
        this.colSource = new ColumnHeader();
        this.colEnabled = new ColumnHeader();
        this.panelLayerButtons = new FlowLayoutPanel();
        this.btnAddLayer = new Button();
        this.btnRemoveLayer = new Button();
        this.btnMoveUp = new Button();
        this.btnMoveDown = new Button();

        // Progress controls
        this.progressBar = new ProgressBar();
        this.lblStatus = new Label();
        this.panelButtons = new FlowLayoutPanel();
        this.btnGenerate = new Button();
        this.btnCancel = new Button();

        this.SuspendLayout();
        this.tableLayoutMain.SuspendLayout();
        this.groupConfig.SuspendLayout();
        this.groupSteps.SuspendLayout();
        this.groupLayers.SuspendLayout();
        this.groupProgress.SuspendLayout();

        // === Main Layout ===
        this.tableLayoutMain.ColumnCount = 1;
        this.tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this.tableLayoutMain.RowCount = 4;
        this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
        this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
        this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this.tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
        this.tableLayoutMain.Dock = DockStyle.Fill;
        this.tableLayoutMain.Padding = new Padding(8);
        this.tableLayoutMain.Controls.Add(this.groupConfig, 0, 0);
        this.tableLayoutMain.Controls.Add(this.groupSteps, 0, 1);
        this.tableLayoutMain.Controls.Add(this.groupLayers, 0, 2);
        this.tableLayoutMain.Controls.Add(this.groupProgress, 0, 3);

        // === Config Group ===
        this.groupConfig.Text = "Configuration";
        this.groupConfig.Dock = DockStyle.Fill;
        this.groupConfig.Padding = new Padding(10);

        // Radio buttons
        this.radioOpenMW.Text = "OpenMW";
        this.radioOpenMW.AutoSize = true;
        this.radioOpenMW.Location = new Point(15, 25);
        this.radioOpenMW.Checked = true;
        this.radioOpenMW.CheckedChanged += radioOpenMW_CheckedChanged;

        this.radioMorrowind.Text = "Morrowind";
        this.radioMorrowind.AutoSize = true;
        this.radioMorrowind.Location = new Point(100, 25);
        this.radioMorrowind.CheckedChanged += radioMorrowind_CheckedChanged;

        // OpenMW config
        this.lblOpenMWConfig.Text = "OpenMW Config:";
        this.lblOpenMWConfig.AutoSize = true;
        this.lblOpenMWConfig.Location = new Point(15, 55);

        this.txtOpenMWConfig.Location = new Point(120, 52);
        this.txtOpenMWConfig.Size = new Size(400, 23);
        this.txtOpenMWConfig.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.txtOpenMWConfig.ReadOnly = true;
        this.txtOpenMWConfig.Cursor = Cursors.Hand;
        this.txtOpenMWConfig.Click += txtOpenMWConfig_Click;

        this.btnBrowseOpenMW.Text = "...";
        this.btnBrowseOpenMW.Size = new Size(30, 23);
        this.btnBrowseOpenMW.Location = new Point(525, 52);
        this.btnBrowseOpenMW.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.btnBrowseOpenMW.Click += btnBrowseOpenMW_Click;

        // Morrowind config
        this.lblMorrowindIni.Text = "Morrowind.ini:";
        this.lblMorrowindIni.AutoSize = true;
        this.lblMorrowindIni.Location = new Point(15, 85);

        this.txtMorrowindIni.Location = new Point(120, 82);
        this.txtMorrowindIni.Size = new Size(400, 23);
        this.txtMorrowindIni.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.txtMorrowindIni.ReadOnly = true;
        this.txtMorrowindIni.Cursor = Cursors.Hand;
        this.txtMorrowindIni.Click += txtMorrowindIni_Click;

        this.btnBrowseMorrowind.Text = "...";
        this.btnBrowseMorrowind.Size = new Size(30, 23);
        this.btnBrowseMorrowind.Location = new Point(525, 82);
        this.btnBrowseMorrowind.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.btnBrowseMorrowind.Click += btnBrowseMorrowind_Click;

        // Output directory
        this.lblOutputDir.Text = "Output Directory:";
        this.lblOutputDir.AutoSize = true;
        this.lblOutputDir.Location = new Point(15, 115);

        this.txtOutputDir.Location = new Point(120, 112);
        this.txtOutputDir.Size = new Size(400, 23);
        this.txtOutputDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.txtOutputDir.ReadOnly = true;
        this.txtOutputDir.Cursor = Cursors.Hand;
        this.txtOutputDir.Click += txtOutputDir_Click;

        this.btnBrowseOutput.Text = "...";
        this.btnBrowseOutput.Size = new Size(30, 23);
        this.btnBrowseOutput.Location = new Point(525, 112);
        this.btnBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.btnBrowseOutput.Click += btnBrowseOutput_Click;

        this.groupConfig.Controls.Add(this.radioOpenMW);
        this.groupConfig.Controls.Add(this.radioMorrowind);
        this.groupConfig.Controls.Add(this.lblOpenMWConfig);
        this.groupConfig.Controls.Add(this.txtOpenMWConfig);
        this.groupConfig.Controls.Add(this.btnBrowseOpenMW);
        this.groupConfig.Controls.Add(this.lblMorrowindIni);
        this.groupConfig.Controls.Add(this.txtMorrowindIni);
        this.groupConfig.Controls.Add(this.btnBrowseMorrowind);
        this.groupConfig.Controls.Add(this.lblOutputDir);
        this.groupConfig.Controls.Add(this.txtOutputDir);
        this.groupConfig.Controls.Add(this.btnBrowseOutput);

        // === Steps Group ===
        this.groupSteps.Text = "Generation Steps";
        this.groupSteps.Dock = DockStyle.Fill;
        this.groupSteps.Padding = new Padding(10);

        this.chkGenerateTiles.Text = "Generate Tiles (run OpenMW Map Generator)";
        this.chkGenerateTiles.AutoSize = true;
        this.chkGenerateTiles.Location = new Point(15, 25);
        this.chkGenerateTiles.Checked = true;

        this.chkGenerateMarkers.Text = "Generate Markers (extract cells and doors)";
        this.chkGenerateMarkers.AutoSize = true;
        this.chkGenerateMarkers.Location = new Point(15, 50);
        this.chkGenerateMarkers.Checked = true;

        this.chkGenerateBigMap.Text = "Generate Big Map (full-size stitched PNG)";
        this.chkGenerateBigMap.AutoSize = true;
        this.chkGenerateBigMap.Location = new Point(15, 75);

        this.chkGenerateWebMap.Text = "Generate Web Map (tiles + HTML viewer)";
        this.chkGenerateWebMap.AutoSize = true;
        this.chkGenerateWebMap.Location = new Point(15, 100);
        this.chkGenerateWebMap.Checked = true;

        this.chk512pxMode.Text = "512px Mode (4x fewer requests, larger files)";
        this.chk512pxMode.AutoSize = true;
        this.chk512pxMode.Location = new Point(300, 25);

        this.groupSteps.Controls.Add(this.chkGenerateTiles);
        this.groupSteps.Controls.Add(this.chkGenerateMarkers);
        this.groupSteps.Controls.Add(this.chkGenerateBigMap);
        this.groupSteps.Controls.Add(this.chkGenerateWebMap);
        this.groupSteps.Controls.Add(this.chk512pxMode);

        // === Layers Group ===
        this.groupLayers.Text = "Layers";
        this.groupLayers.Dock = DockStyle.Fill;
        this.groupLayers.Padding = new Padding(10);

        this.listLayers.View = View.Details;
        this.listLayers.FullRowSelect = true;
        this.listLayers.MultiSelect = false;
        this.listLayers.Dock = DockStyle.Fill;
        this.listLayers.Columns.AddRange(new ColumnHeader[] { this.colName, this.colType, this.colSource, this.colEnabled });
        this.listLayers.SelectedIndexChanged += listLayers_SelectedIndexChanged;

        this.colName.Text = "Layer Name";
        this.colName.Width = 150;
        this.colType.Text = "Type";
        this.colType.Width = 80;
        this.colSource.Text = "Source Folder";
        this.colSource.Width = 200;
        this.colEnabled.Text = "Enabled";
        this.colEnabled.Width = 60;

        this.panelLayerButtons.FlowDirection = FlowDirection.LeftToRight;
        this.panelLayerButtons.Dock = DockStyle.Bottom;
        this.panelLayerButtons.AutoSize = true;
        this.panelLayerButtons.Padding = new Padding(0, 5, 0, 0);

        this.btnAddLayer.Text = "Add Layer...";
        this.btnAddLayer.AutoSize = true;
        this.btnAddLayer.Click += btnAddLayer_Click;

        this.btnRemoveLayer.Text = "Remove";
        this.btnRemoveLayer.AutoSize = true;
        this.btnRemoveLayer.Click += btnRemoveLayer_Click;

        this.btnMoveUp.Text = "Move Up";
        this.btnMoveUp.AutoSize = true;
        this.btnMoveUp.Click += btnMoveUp_Click;

        this.btnMoveDown.Text = "Move Down";
        this.btnMoveDown.AutoSize = true;
        this.btnMoveDown.Click += btnMoveDown_Click;

        this.btnToggleType = new Button();
        this.btnToggleType.Text = "Toggle Type";
        this.btnToggleType.AutoSize = true;
        this.btnToggleType.Click += btnToggleType_Click;

        this.btnRenameLayer = new Button();
        this.btnRenameLayer.Text = "Rename";
        this.btnRenameLayer.AutoSize = true;
        this.btnRenameLayer.Click += btnRenameLayer_Click;

        this.btnToggleEnabled = new Button();
        this.btnToggleEnabled.Text = "Toggle Enabled";
        this.btnToggleEnabled.AutoSize = true;
        this.btnToggleEnabled.Click += btnToggleEnabled_Click;

        this.panelLayerButtons.Controls.Add(this.btnAddLayer);
        this.panelLayerButtons.Controls.Add(this.btnRemoveLayer);
        this.panelLayerButtons.Controls.Add(this.btnMoveUp);
        this.panelLayerButtons.Controls.Add(this.btnMoveDown);
        this.panelLayerButtons.Controls.Add(this.btnToggleType);
        this.panelLayerButtons.Controls.Add(this.btnToggleEnabled);
        this.panelLayerButtons.Controls.Add(this.btnRenameLayer);

        this.groupLayers.Controls.Add(this.listLayers);
        this.groupLayers.Controls.Add(this.panelLayerButtons);

        // === Progress Group ===
        this.groupProgress.Text = "Progress";
        this.groupProgress.Dock = DockStyle.Fill;
        this.groupProgress.Padding = new Padding(10);

        this.progressBar.Dock = DockStyle.Top;
        this.progressBar.Height = 23;
        this.progressBar.Style = ProgressBarStyle.Blocks;

        this.lblStatus.Text = "Ready";
        this.lblStatus.AutoSize = true;
        this.lblStatus.Location = new Point(15, 50);

        this.panelButtons.FlowDirection = FlowDirection.RightToLeft;
        this.panelButtons.Dock = DockStyle.Bottom;
        this.panelButtons.AutoSize = true;

        this.btnGenerate.Text = "Generate Map";
        this.btnGenerate.AutoSize = true;
        this.btnGenerate.MinimumSize = new Size(100, 28);
        this.btnGenerate.Click += btnGenerate_Click;

        this.btnCancel.Text = "Cancel";
        this.btnCancel.AutoSize = true;
        this.btnCancel.MinimumSize = new Size(80, 28);
        this.btnCancel.Click += btnCancel_Click;

        this.panelButtons.Controls.Add(this.btnGenerate);
        this.panelButtons.Controls.Add(this.btnCancel);

        this.groupProgress.Controls.Add(this.progressBar);
        this.groupProgress.Controls.Add(this.lblStatus);
        this.groupProgress.Controls.Add(this.panelButtons);

        // === Form ===
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(584, 661);
        this.Controls.Add(this.tableLayoutMain);
        this.MinimumSize = new Size(600, 700);
        this.Name = "MainForm";
        this.Text = "Morrowind Map Generator";
        this.FormClosing += MainForm_FormClosing;

        this.tableLayoutMain.ResumeLayout(false);
        this.groupConfig.ResumeLayout(false);
        this.groupConfig.PerformLayout();
        this.groupSteps.ResumeLayout(false);
        this.groupSteps.PerformLayout();
        this.groupLayers.ResumeLayout(false);
        this.groupLayers.PerformLayout();
        this.groupProgress.ResumeLayout(false);
        this.groupProgress.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel tableLayoutMain;
    private GroupBox groupConfig;
    private GroupBox groupSteps;
    private GroupBox groupLayers;
    private GroupBox groupProgress;

    // Config controls
    private RadioButton radioOpenMW;
    private RadioButton radioMorrowind;
    private Label lblOpenMWConfig;
    private TextBox txtOpenMWConfig;
    private Button btnBrowseOpenMW;
    private Label lblMorrowindIni;
    private TextBox txtMorrowindIni;
    private Button btnBrowseMorrowind;
    private Label lblOutputDir;
    private TextBox txtOutputDir;
    private Button btnBrowseOutput;

    // Steps controls
    private CheckBox chkGenerateTiles;
    private CheckBox chkGenerateMarkers;
    private CheckBox chkGenerateBigMap;
    private CheckBox chkGenerateWebMap;
    private CheckBox chk512pxMode;

    // Layers controls
    private ListView listLayers;
    private ColumnHeader colName;
    private ColumnHeader colType;
    private ColumnHeader colSource;
    private ColumnHeader colEnabled;
    private FlowLayoutPanel panelLayerButtons;
    private Button btnAddLayer;
    private Button btnRemoveLayer;
    private Button btnMoveUp;
    private Button btnMoveDown;
    private Button btnToggleType;
    private Button btnToggleEnabled;
    private Button btnRenameLayer;

    // Progress controls
    private ProgressBar progressBar;
    private Label lblStatus;
    private FlowLayoutPanel panelButtons;
    private Button btnGenerate;
    private Button btnCancel;
}
