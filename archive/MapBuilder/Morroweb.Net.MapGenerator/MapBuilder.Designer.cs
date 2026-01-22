namespace Morroweb.Net.MapGenerator
{
    partial class MapBuilder
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            btnMwPath = new Button();
            txtMwPath = new Label();
            txtMapPath = new Label();
            btnMapPath = new Button();
            label3 = new Label();
            txtOutPath = new Label();
            btnOutPath = new Button();
            label7 = new Label();
            btnRun = new Button();
            chkBigMap = new CheckBox();
            chkMarker = new CheckBox();
            txtProg = new Label();
            chkCompress = new CheckBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 16);
            label1.Name = "label1";
            label1.Size = new Size(93, 15);
            label1.TabIndex = 0;
            label1.Text = "Morrowind Path";
            // 
            // btnMwPath
            // 
            btnMwPath.Location = new Point(111, 12);
            btnMwPath.Name = "btnMwPath";
            btnMwPath.Size = new Size(75, 23);
            btnMwPath.TabIndex = 1;
            btnMwPath.Text = "Browse";
            btnMwPath.UseVisualStyleBackColor = true;
            btnMwPath.Click += btnMwPath_Click;
            // 
            // txtMwPath
            // 
            txtMwPath.AutoSize = true;
            txtMwPath.Location = new Point(192, 16);
            txtMwPath.Name = "txtMwPath";
            txtMwPath.Size = new Size(16, 15);
            txtMwPath.TabIndex = 2;
            txtMwPath.Text = "...";
            // 
            // txtMapPath
            // 
            txtMapPath.AutoSize = true;
            txtMapPath.Location = new Point(192, 74);
            txtMapPath.Name = "txtMapPath";
            txtMapPath.Size = new Size(16, 15);
            txtMapPath.TabIndex = 5;
            txtMapPath.Text = "...";
            // 
            // btnMapPath
            // 
            btnMapPath.Location = new Point(111, 70);
            btnMapPath.Name = "btnMapPath";
            btnMapPath.Size = new Size(75, 23);
            btnMapPath.TabIndex = 4;
            btnMapPath.Text = "Browse";
            btnMapPath.UseVisualStyleBackColor = true;
            btnMapPath.Click += btnMapPath_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 74);
            label3.Name = "label3";
            label3.Size = new Size(63, 15);
            label3.TabIndex = 3;
            label3.Text = "Maps Path";
            // 
            // txtOutPath
            // 
            txtOutPath.AutoSize = true;
            txtOutPath.Location = new Point(192, 45);
            txtOutPath.Name = "txtOutPath";
            txtOutPath.Size = new Size(16, 15);
            txtOutPath.TabIndex = 11;
            txtOutPath.Text = "...";
            // 
            // btnOutPath
            // 
            btnOutPath.Location = new Point(111, 41);
            btnOutPath.Name = "btnOutPath";
            btnOutPath.Size = new Size(75, 23);
            btnOutPath.TabIndex = 10;
            btnOutPath.Text = "Browse";
            btnOutPath.UseVisualStyleBackColor = true;
            btnOutPath.Click += btnOutPath_Click;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(12, 45);
            label7.Name = "label7";
            label7.Size = new Size(72, 15);
            label7.TabIndex = 9;
            label7.Text = "Output Path";
            // 
            // btnRun
            // 
            btnRun.Location = new Point(14, 149);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(116, 23);
            btnRun.TabIndex = 12;
            btnRun.Text = "Generate Map";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // chkBigMap
            // 
            chkBigMap.AutoSize = true;
            chkBigMap.Location = new Point(14, 99);
            chkBigMap.Name = "chkBigMap";
            chkBigMap.Size = new Size(172, 19);
            chkBigMap.TabIndex = 13;
            chkBigMap.Text = "Generate Full Size PNG Map";
            chkBigMap.UseVisualStyleBackColor = true;
            chkBigMap.CheckedChanged += chkBigMap_CheckedChanged;
            // 
            // chkMarker
            // 
            chkMarker.AutoSize = true;
            chkMarker.Location = new Point(14, 124);
            chkMarker.Name = "chkMarker";
            chkMarker.Size = new Size(145, 19);
            chkMarker.TabIndex = 14;
            chkMarker.Text = "Generate Map Markers";
            chkMarker.UseVisualStyleBackColor = true;
            chkMarker.CheckedChanged += chkMarker_CheckedChanged;
            // 
            // txtProg
            // 
            txtProg.AutoSize = true;
            txtProg.Location = new Point(14, 184);
            txtProg.Name = "txtProg";
            txtProg.Size = new Size(16, 15);
            txtProg.TabIndex = 16;
            txtProg.Text = "...";
            // 
            // chkCompress
            // 
            chkCompress.AutoSize = true;
            chkCompress.Location = new Point(136, 152);
            chkCompress.Name = "chkCompress";
            chkCompress.Size = new Size(120, 19);
            chkCompress.TabIndex = 17;
            chkCompress.Text = "Compress Images";
            chkCompress.UseVisualStyleBackColor = true;
            chkCompress.CheckedChanged += chkCompress_CheckedChanged;
            // 
            // MapBuilder
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(676, 209);
            Controls.Add(chkCompress);
            Controls.Add(txtProg);
            Controls.Add(chkMarker);
            Controls.Add(chkBigMap);
            Controls.Add(btnRun);
            Controls.Add(txtOutPath);
            Controls.Add(btnOutPath);
            Controls.Add(label7);
            Controls.Add(txtMapPath);
            Controls.Add(btnMapPath);
            Controls.Add(label3);
            Controls.Add(txtMwPath);
            Controls.Add(btnMwPath);
            Controls.Add(label1);
            MaximumSize = new Size(692, 248);
            MinimumSize = new Size(692, 248);
            Name = "MapBuilder";
            Text = "Map Builder for Morrowind";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Button btnMwPath;
        private Label txtMwPath;
        private Label txtMapPath;
        private Button btnMapPath;
        private Label label3;
        private Label txtOutPath;
        private Button btnOutPath;
        private Label label7;
        private Button btnRun;
        private CheckBox chkBigMap;
        private CheckBox chkMarker;
        private Label txtProg;
        private CheckBox chkCompress;
    }
}
