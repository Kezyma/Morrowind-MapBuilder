using Morroweb.Net.Services;
using System.Drawing;
using System.Security.Policy;

namespace Morroweb.Net.MapGenerator
{
    public partial class MapBuilder : Form
    {
        public MapBuilder()
        {
            InitializeComponent();
        }

        public string MwPath { get; set; }
        public string OutPath { get; set; }
        public string MapPath { get; set; }
        public bool BigMap { get; set; } = false;
        public bool Markers { get; set; } = false;
        public bool Compress { get; set; } = false;

        private void btnMwPath_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                MwPath = fbd.SelectedPath;
                txtMwPath.Text = fbd.SelectedPath;
                var mapPath = Path.Combine(MwPath, "Maps");
                if (Directory.Exists(mapPath))
                {
                    MapPath = mapPath;
                    txtMapPath.Text = mapPath;
                }
            }
        }

        private void btnOutPath_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                OutPath = fbd.SelectedPath;
                txtOutPath.Text = fbd.SelectedPath;
            }
        }

        private void btnMapPath_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                MapPath = fbd.SelectedPath;
                txtMapPath.Text = fbd.SelectedPath;
            }
        }

        private void chkBigMap_CheckedChanged(object sender, EventArgs e)
        {
            BigMap = chkBigMap.Checked;
        }

        private void chkMarker_CheckedChanged(object sender, EventArgs e)
        {
            Markers = chkMarker.Checked;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            btnRun.Enabled = false;

            txtProg.Text = "Generating Tiles";
            var espExt = new EspExtractorService();
            var mapGen = new TileGeneratorService(espExt);
            mapGen.GenerateTiles(MapPath, OutPath, Compress);

            if (BigMap == true)
            {
                txtProg.Text = "Generating Full Size Map";
                mapGen.StitchTiles(MapPath, OutPath, Compress);
            }

            if (Markers == true)
            {
                txtProg.Text = "Exporting Coordinates";
                mapGen.GenerateCoordinates(MwPath, OutPath);
            }

            txtProg.Text = "Generating Demo";
            mapGen.GenerateDemo(OutPath);

            txtProg.Text = "Complete";
            btnRun.Enabled = true;
        }

        private void chkCompress_CheckedChanged(object sender, EventArgs e)
        {
            Compress = chkCompress.Checked;
        }
    }
}
