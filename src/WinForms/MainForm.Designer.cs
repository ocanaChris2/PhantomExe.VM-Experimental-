namespace PhantomExe.WinForms
{
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
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.panelDrop = new System.Windows.Forms.Panel();
            this.lblDropZone = new System.Windows.Forms.Label();
            this.panelOptions = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkVirtualizeMethods = new System.Windows.Forms.CheckBox();
            this.chkObfuscateFlow = new System.Windows.Forms.CheckBox();
            this.chkEncryptStringConstants = new System.Windows.Forms.CheckBox();
            this.chkEnableAntiDebug = new System.Windows.Forms.CheckBox();
            this.chkEnableJIT = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.txtLicenseKey = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbTargetFramework = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radAuto = new System.Windows.Forms.RadioButton();
            this.radDnlib = new System.Windows.Forms.RadioButton();
            this.radAsmResolver = new System.Windows.Forms.RadioButton();
            this.lblToolchain = new System.Windows.Forms.Label();
            this.txtInputPath = new System.Windows.Forms.TextBox();
            this.menuStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.panelDrop.SuspendLayout();
            this.panelOptions.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(784, 24);
            this.menuStrip.TabIndex = 0;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(152, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.aboutToolStripMenuItem.Text = "&About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 539);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(784, 22);
            this.statusStrip.TabIndex = 1;
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(39, 17);
            this.toolStripStatusLabel.Text = "Ready";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 510);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(760, 23);
            this.progressBar.TabIndex = 2;
            this.progressBar.Visible = false;
            // 
            // panelDrop
            // 
            this.panelDrop.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelDrop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(248)))), ((int)(((byte)(255)))));
            this.panelDrop.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelDrop.Controls.Add(this.lblDropZone);
            this.panelDrop.Location = new System.Drawing.Point(12, 65);
            this.panelDrop.Name = "panelDrop";
            this.panelDrop.Size = new System.Drawing.Size(760, 200);
            this.panelDrop.TabIndex = 3;
            // 
            // lblDropZone
            // 
            this.lblDropZone.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblDropZone.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblDropZone.ForeColor = System.Drawing.Color.Gray;
            this.lblDropZone.Location = new System.Drawing.Point(0, 80);
            this.lblDropZone.Name = "lblDropZone";
            this.lblDropZone.Size = new System.Drawing.Size(760, 40);
            this.lblDropZone.TabIndex = 0;
            this.lblDropZone.Text = "📦 Drag & Drop .NET Assembly Here\r\n(.dll or .exe)";
            this.lblDropZone.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelOptions
            // 
            this.panelOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelOptions.Controls.Add(this.groupBox3);
            this.panelOptions.Controls.Add(this.groupBox1);
            this.panelOptions.Location = new System.Drawing.Point(12, 271);
            this.panelOptions.Name = "panelOptions";
            this.panelOptions.Size = new System.Drawing.Size(760, 233);
            this.panelOptions.TabIndex = 4;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.chkVirtualizeMethods);
            this.groupBox1.Controls.Add(this.chkObfuscateFlow);
            this.groupBox1.Controls.Add(this.chkEncryptStringConstants);
            this.groupBox1.Controls.Add(this.chkEnableAntiDebug);
            this.groupBox1.Controls.Add(this.chkEnableJIT);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(754, 120);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Protection Options";
            // 
            // chkVirtualizeMethods
            // 
            this.chkVirtualizeMethods.AutoSize = true;
            this.chkVirtualizeMethods.Checked = true;
            this.chkVirtualizeMethods.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkVirtualizeMethods.Location = new System.Drawing.Point(15, 90);
            this.chkVirtualizeMethods.Name = "chkVirtualizeMethods";
            this.chkVirtualizeMethods.Size = new System.Drawing.Size(227, 19);
            this.chkVirtualizeMethods.TabIndex = 4;
            this.chkVirtualizeMethods.Text = "Virtualize Methods (IL → VM Bytecode)";
            this.chkVirtualizeMethods.UseVisualStyleBackColor = true;
            // 
            // chkObfuscateFlow
            // 
            this.chkObfuscateFlow.AutoSize = true;
            this.chkObfuscateFlow.Checked = true;
            this.chkObfuscateFlow.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkObfuscateFlow.Location = new System.Drawing.Point(15, 72);
            this.chkObfuscateFlow.Name = "chkObfuscateFlow";
            this.chkObfuscateFlow.Size = new System.Drawing.Size(172, 19);
            this.chkObfuscateFlow.TabIndex = 3;
            this.chkObfuscateFlow.Text = "Obfuscate Control Flow";
            this.chkObfuscateFlow.UseVisualStyleBackColor = true;
            // 
            // chkEncryptStringConstants
            // 
            this.chkEncryptStringConstants.AutoSize = true;
            this.chkEncryptStringConstants.Checked = true;
            this.chkEncryptStringConstants.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEncryptStringConstants.Location = new System.Drawing.Point(15, 54);
            this.chkEncryptStringConstants.Name = "chkEncryptStringConstants";
            this.chkEncryptStringConstants.Size = new System.Drawing.Size(163, 19);
            this.chkEncryptStringConstants.TabIndex = 2;
            this.chkEncryptStringConstants.Text = "Encrypt String Constants";
            this.chkEncryptStringConstants.UseVisualStyleBackColor = true;
            // 
            // chkEnableAntiDebug
            // 
            this.chkEnableAntiDebug.AutoSize = true;
            this.chkEnableAntiDebug.Checked = true;
            this.chkEnableAntiDebug.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableAntiDebug.Location = new System.Drawing.Point(15, 36);
            this.chkEnableAntiDebug.Name = "chkEnableAntiDebug";
            this.chkEnableAntiDebug.Size = new System.Drawing.Size(172, 19);
            this.chkEnableAntiDebug.TabIndex = 1;
            this.chkEnableAntiDebug.Text = "Enable Anti-Debug Checks";
            this.chkEnableAntiDebug.UseVisualStyleBackColor = true;
            // 
            // chkEnableJIT
            // 
            this.chkEnableJIT.AutoSize = true;
            this.chkEnableJIT.Checked = true;
            this.chkEnableJIT.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableJIT.Location = new System.Drawing.Point(15, 18);
            this.chkEnableJIT.Name = "chkEnableJIT";
            this.chkEnableJIT.Size = new System.Drawing.Size(139, 19);
            this.chkEnableJIT.TabIndex = 0;
            this.chkEnableJIT.Text = "Enable JIT Compilation";
            this.chkEnableJIT.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.txtLicenseKey);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.cmbTargetFramework);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Location = new System.Drawing.Point(3, 129);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(754, 95);
            this.groupBox3.TabIndex = 1;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Configuration";
            // 
            // txtLicenseKey
            // 
            this.txtLicenseKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLicenseKey.Location = new System.Drawing.Point(120, 55);
            this.txtLicenseKey.Name = "txtLicenseKey";
            this.txtLicenseKey.Size = new System.Drawing.Size(620, 23);
            this.txtLicenseKey.TabIndex = 3;
            this.txtLicenseKey.Text = "HWID_AUTO_DETECT";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "License Key:";
            // 
            // cmbTargetFramework
            // 
            this.cmbTargetFramework.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbTargetFramework.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTargetFramework.FormattingEnabled = true;
            this.cmbTargetFramework.Items.AddRange(new object[] {
            ".NET Framework 4.5",
            ".NET 5",
            ".NET 6",
            ".NET 7",
            ".NET 8",
            ".NET 9"});
            this.cmbTargetFramework.Location = new System.Drawing.Point(120, 22);
            this.cmbTargetFramework.Name = "cmbTargetFramework";
            this.cmbTargetFramework.Size = new System.Drawing.Size(620, 23);
            this.cmbTargetFramework.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Target Framework:";
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = ".NET Assemblies|*.dll;*.exe|All Files|*.*";
            this.openFileDialog.Title = "Select .NET Assembly to Protect";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.radAuto);
            this.groupBox2.Controls.Add(this.radDnlib);
            this.groupBox2.Controls.Add(this.radAsmResolver);
            this.groupBox2.Location = new System.Drawing.Point(12, 27);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(545, 32);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            // 
            // radAuto
            // 
            this.radAuto.AutoSize = true;
            this.radAuto.Checked = true;
            this.radAuto.Location = new System.Drawing.Point(10, 8);
            this.radAuto.Name = "radAuto";
            this.radAuto.Size = new System.Drawing.Size(104, 19);
            this.radAuto.TabIndex = 0;
            this.radAuto.TabStop = true;
            this.radAuto.Text = "Auto-Detect";
            this.radAuto.UseVisualStyleBackColor = true;
            this.radAuto.CheckedChanged += new System.EventHandler(this.radAuto_CheckedChanged);
            // 
            // radDnlib
            // 
            this.radDnlib.AutoSize = true;
            this.radDnlib.Location = new System.Drawing.Point(135, 8);
            this.radDnlib.Name = "radDnlib";
            this.radDnlib.Size = new System.Drawing.Size(124, 19);
            this.radDnlib.TabIndex = 1;
            this.radDnlib.Text = "dnlib (Legacy)";
            this.radDnlib.UseVisualStyleBackColor = true;
            this.radDnlib.CheckedChanged += new System.EventHandler(this.radDnlib_CheckedChanged);
            // 
            // radAsmResolver
            // 
            this.radAsmResolver.AutoSize = true;
            this.radAsmResolver.Location = new System.Drawing.Point(285, 8);
            this.radAsmResolver.Name = "radAsmResolver";
            this.radAsmResolver.Size = new System.Drawing.Size(156, 19);
            this.radAsmResolver.TabIndex = 2;
            this.radAsmResolver.Text = "AsmResolver (Modern)";
            this.radAsmResolver.UseVisualStyleBackColor = true;
            this.radAsmResolver.CheckedChanged += new System.EventHandler(this.radAsmResolver_CheckedChanged);
            // 
            // lblToolchain
            // 
            this.lblToolchain.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblToolchain.ForeColor = System.Drawing.Color.DimGray;
            this.lblToolchain.Location = new System.Drawing.Point(563, 35);
            this.lblToolchain.Name = "lblToolchain";
            this.lblToolchain.Size = new System.Drawing.Size(209, 18);
            this.lblToolchain.TabIndex = 6;
            this.lblToolchain.Text = "Toolchain: Auto";
            this.lblToolchain.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtInputPath
            // 
            this.txtInputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInputPath.BackColor = System.Drawing.SystemColors.Window;
            this.txtInputPath.Location = new System.Drawing.Point(563, 7);
            this.txtInputPath.Name = "txtInputPath";
            this.txtInputPath.ReadOnly = true;
            this.txtInputPath.Size = new System.Drawing.Size(209, 23);
            this.txtInputPath.TabIndex = 7;
            this.txtInputPath.Text = "No file selected";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.txtInputPath);
            this.Controls.Add(this.lblToolchain);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.panelOptions);
            this.Controls.Add(this.panelDrop);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PhantomExe VM Protector";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.panelDrop.ResumeLayout(false);
            this.panelOptions.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Panel panelDrop;
        private System.Windows.Forms.Label lblDropZone;
        private System.Windows.Forms.Panel panelOptions;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkVirtualizeMethods;
        private System.Windows.Forms.CheckBox chkObfuscateFlow;
        private System.Windows.Forms.CheckBox chkEncryptStringConstants;
        private System.Windows.Forms.CheckBox chkEnableAntiDebug;
        private System.Windows.Forms.CheckBox chkEnableJIT;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txtLicenseKey;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbTargetFramework;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radAuto;
        private System.Windows.Forms.RadioButton radDnlib;
        private System.Windows.Forms.RadioButton radAsmResolver;
        private System.Windows.Forms.Label lblToolchain;
        private System.Windows.Forms.TextBox txtInputPath;
    }
}