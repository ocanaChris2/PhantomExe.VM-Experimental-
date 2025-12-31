using PhantomExe.Protector;
using PhantomExe.Protector.Detection;
using PhantomExe.Core;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PhantomExe.WinForms
{
    public partial class MainForm : Form
    {
        private readonly BackgroundWorker _worker = new();
        private string? _currentInputPath;

        public MainForm()
        {
            InitializeComponent();
            InitializeDragDrop();
            InitializeWorker();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // Set default values
            cmbTargetFramework.SelectedIndex = 5; // .NET 9
            txtLicenseKey.Text = "HWID_AUTO_DETECT";
            UpdateToolchainStatus("Auto");
            UpdateStatus("Ready. Drag & drop an assembly or use File → Open");
        }

        private void InitializeDragDrop()
        {
            var targets = new Control[] { this, panelDrop };
            foreach (var ctrl in targets)
            {
                ctrl.AllowDrop = true;
                ctrl.DragEnter += OnDragEnter;
                ctrl.DragDrop += OnDragDrop;
                ctrl.DragLeave += OnDragLeave;
            }
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length == 1 && IsSupportedAssembly(files[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                    panelDrop.BackColor = Color.FromArgb(220, 240, 255);
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object? sender, EventArgs e)
        {
            panelDrop.BackColor = Color.FromArgb(245, 248, 255);
        }

        private void OnDragDrop(object? sender, DragEventArgs e)
        {
            panelDrop.BackColor = Color.FromArgb(245, 248, 255);
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length == 1 && IsSupportedAssembly(files[0]))
                {
                    _currentInputPath = files[0];
                    txtInputPath.Text = _currentInputPath;
                    DetectAndUpdateToolchain();
                    ProcessFile(_currentInputPath);
                }
            }
        }

        private bool IsSupportedAssembly(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            return (ext == ".dll" || ext == ".exe") && File.Exists(path);
        }

        private void InitializeWorker()
        {
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += Worker_DoWork;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void openToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _currentInputPath = openFileDialog.FileName;
                txtInputPath.Text = _currentInputPath;
                DetectAndUpdateToolchain();
                ProcessFile(_currentInputPath);
            }
        }

        private void DetectAndUpdateToolchain()
        {
            if (string.IsNullOrEmpty(_currentInputPath)) return;

            try
            {
                var type = ProjectDetector.Detect(_currentInputPath);
                var isModern = type == ProjectType.ModernNetCore;

                // Update UI on main thread
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateToolchainUI(type, isModern)));
                }
                else
                {
                    UpdateToolchainUI(type, isModern);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"⚠️ Detection failed: {ex.Message}");
            }
        }

        private void UpdateToolchainUI(ProjectType type, bool isModern)
        {
            radAuto.Checked = true;
            UpdateToolchainStatus(type.ToString());

            // Suggest appropriate toolchain
            if (!radAuto.Checked)
            {
                radDnlib.Checked = !isModern;
                radAsmResolver.Checked = isModern;
            }
        }

        private void radAuto_CheckedChanged(object? sender, EventArgs e)
        {
            if (radAuto.Checked)
            {
                DetectAndUpdateToolchain();
            }
        }

        private void radDnlib_CheckedChanged(object? sender, EventArgs e)
        {
            if (radDnlib.Checked)
            {
                UpdateToolchainStatus("dnlib (Legacy .NET Framework)");
            }
        }

        private void radAsmResolver_CheckedChanged(object? sender, EventArgs e)
        {
            if (radAsmResolver.Checked)
            {
                UpdateToolchainStatus("AsmResolver (Modern .NET 5+)");
            }
        }

        private void UpdateToolchainStatus(string status)
        {
            lblToolchain.Text = $"Toolchain: {status}";
            lblToolchain.ForeColor = status.Contains("Modern") || status.Contains("ModernNetCore")
                ? Color.ForestGreen
                : status.Contains("Legacy") || status.Contains("LegacyNetFramework")
                ? Color.OrangeRed
                : Color.DimGray;
        }

        private void ProcessFile(string filePath)
        {
            if (_worker.IsBusy)
            {
                MessageBox.Show("A protection job is already running. Please wait.", "Busy",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"File not found: {filePath}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Determine toolchain
            var toolchain = radAuto.Checked
                ? ProjectDetector.Detect(filePath).ToString()
                : radDnlib.Checked ? "dnlib" : "AsmResolver";

            var config = new ProtectionConfig
            {
                EnableJIT = chkEnableJIT.Checked,
                EnableAntiDebug = chkEnableAntiDebug.Checked,
                EncryptStrings = chkEncryptStringConstants.Checked,
                ObfuscateControlFlow = chkObfuscateFlow.Checked,
                VirtualizeMethods = chkVirtualizeMethods.Checked,
                TargetFramework = ParseFramework(cmbTargetFramework.SelectedItem?.ToString()),
                LicenseKey = txtLicenseKey.Text == "HWID_AUTO_DETECT"
                    ? GetHardwareIdSafely()
                    : txtLicenseKey.Text
            };

            UpdateStatus($"🔄 Loading {Path.GetFileName(filePath)}...");
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            // Disable controls during processing
            SetControlsEnabled(false);

            _worker.RunWorkerAsync(new WorkerArgs(filePath, config, toolchain));
        }

        private void SetControlsEnabled(bool enabled)
        {
            panelOptions.Enabled = enabled;
            menuStrip.Enabled = enabled;
        }

        private TargetFramework ParseFramework(string? name)
        {
            if (string.IsNullOrEmpty(name)) return TargetFramework.Net9;

            return name switch
            {
                string s when s.Contains("4.5") => TargetFramework.Net45,
                string s when s.Contains("5") && !s.Contains("4.5") => TargetFramework.Net5,
                string s when s.Contains("6") => TargetFramework.Net6,
                string s when s.Contains("7") => TargetFramework.Net7,
                string s when s.Contains("8") => TargetFramework.Net8,
                string s when s.Contains("9") => TargetFramework.Net9,
                _ => TargetFramework.Net9
            };
        }

        private string GetHardwareIdSafely()
        {
            try
            {
#if NET6_0_OR_GREATER
                if (!OperatingSystem.IsWindows())
                    return Environment.MachineName ?? "NonWindows";
#endif
                // HardwareFingerprint is only available in net6.0-windows build
                // For net8.0-windows, just use machine name
                return Environment.MachineName ?? "DefaultHWID";
            }
            catch
            {
                return Environment.MachineName ?? "FallbackHWID";
            }
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            var args = e.Argument as WorkerArgs;
            if (args == null)
            {
                e.Result = new InvalidOperationException("Invalid worker arguments");
                return;
            }

            var reporter = new WinFormsReporter(_worker);

            try
            {
                var protector = new PhantomExe.Protector.Protector(reporter);
                var outputPath = protector.Protect(args.InputPath, args.Config);
                e.Result = outputPath;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is string message)
                UpdateStatus(message);
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visible = false;
            progressBar.Style = ProgressBarStyle.Continuous;
            SetControlsEnabled(true);

            if (e.Result is Exception ex)
            {
                var errorMsg = $"Protection failed:\n\n{ex.Message}";
                if (ex.InnerException != null)
                    errorMsg += $"\n\nDetails: {ex.InnerException.Message}";

                MessageBox.Show(errorMsg, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("❌ Protection failed");
            }
            else if (e.Result is string outputPath)
            {
                UpdateStatus($"✅ Success! Protected: {Path.GetFileName(outputPath)}");

                var result = MessageBox.Show(
                    $"Assembly protected successfully!\n\nOutput: {outputPath}\n\nOpen containing folder?",
                    "Success",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{outputPath}\"",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception folderEx)
                    {
                        MessageBox.Show($"Could not open folder: {folderEx.Message}", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => toolStripStatusLabel.Text = message));
            }
            else
            {
                toolStripStatusLabel.Text = message;
            }
        }

        private void exitToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_worker.IsBusy)
            {
                var result = MessageBox.Show(
                    "A protection job is running. Are you sure you want to exit?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                _worker.CancelAsync();
            }

            Close();
        }

        private void aboutToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "PhantomExe VM Protector v1.0\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                "© 2025 PhantomExe Project\n" +
                "Advanced IL Virtualization Engine for .NET\n\n" +
                "Features:\n" +
                "  • Drag & Drop Protection\n" +
                "  • Hardware-Bound Licensing\n" +
                "  • Anti-Tamper & Anti-Debug\n" +
                "  • Method Virtualization\n" +
                "  • String Encryption\n" +
                "  • Control Flow Obfuscation\n\n" +
                "Toolchains:\n" +
                "  • dnlib (Legacy .NET Framework)\n" +
                "  • AsmResolver (Modern .NET 5+)\n\n" +
                "Auto-detection supported for optimal protection.",
                "About PhantomExe Protector",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _worker?.Dispose();
            base.OnFormClosed(e);
        }

        // Reporter bridge
        private class WinFormsReporter : IProgressReporter
        {
            private readonly BackgroundWorker _worker;
            public WinFormsReporter(BackgroundWorker worker) => _worker = worker;
            public void Report(string value) => _worker.ReportProgress(0, value);
        }

        private record WorkerArgs(string InputPath, ProtectionConfig Config, string Toolchain);
    }
}