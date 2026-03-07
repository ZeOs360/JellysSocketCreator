using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace JellysSocketsConfig
{
    public partial class Form1 : Form
    {
        // Clean gray-blue theme
        private static readonly Color BgDark = Color.FromArgb(30, 32, 38);
        private static readonly Color BgCard = Color.FromArgb(42, 45, 52);
        private static readonly Color BgInput = Color.FromArgb(55, 58, 66);
        private static readonly Color BgHeader = Color.FromArgb(36, 39, 46);
        private static readonly Color AccentBlue = Color.FromArgb(88, 140, 190);
        private static readonly Color AccentGreen = Color.FromArgb(92, 160, 108);
        private static readonly Color AccentRed = Color.FromArgb(180, 90, 90);
        private static readonly Color TextPrimary = Color.FromArgb(225, 228, 232);
        private static readonly Color TextSecondary = Color.FromArgb(140, 145, 155);
        private static readonly Color BorderColor = Color.FromArgb(65, 70, 80);

        private DataGridView dgvSockets = null!;
        private TextBox txtGamePath = null!;
        private Button btnBrowse = null!;
        private Button btnAdd = null!;
        private Button btnRemove = null!;
        private Button btnSave = null!;
        private Button btnDeploy = null!;
        private Label lblStatus = null!;
        private PictureBox picLogo = null!;

        private List<SocketEntry> sockets = new List<SocketEntry>();
        private string configPath = "JellysSockets.json";
        private Image? logoImage = null;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
            LoadConfig();
            AutoDetectGamePath();
        }

        private void SetupUI()
        {
            // Load icon and logo
            LoadResources();

            // Form settings
            this.Text = "Jelly's Socket Creator";
            this.Size = new Size(680, 580);
            this.MinimumSize = new Size(550, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgDark;
            this.ForeColor = TextPrimary;
            this.Font = new Font("Segoe UI", 9);

            // Main layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = BgDark,
                Padding = new Padding(0)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));   // Game path
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Sockets grid
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));   // Footer
            this.Controls.Add(mainLayout);

            // ===== HEADER =====
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgHeader,
                Padding = new Padding(15, 10, 15, 10)
            };

            // Logo
            picLogo = new PictureBox
            {
                Size = new Size(48, 48),
                Location = new Point(15, 11),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            if (logoImage != null) picLogo.Image = logoImage;
            pnlHeader.Controls.Add(picLogo);

            // Title
            var lblTitle = new Label
            {
                Text = "Jelly's Socket Creator",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(75, 12),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblTitle);

            // Subtitle
            var lblSubtitle = new Label
            {
                Text = "Custom CPU Sockets for PC Building Simulator 2",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextSecondary,
                AutoSize = true,
                Location = new Point(77, 42),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblSubtitle);

            mainLayout.Controls.Add(pnlHeader, 0, 0);

            // ===== GAME PATH SECTION =====
            var pnlPath = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(15, 8, 15, 8)
            };

            var lblPath = new Label
            {
                Text = "Game Folder:",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextSecondary,
                Location = new Point(15, 12),
                AutoSize = true
            };
            pnlPath.Controls.Add(lblPath);

            txtGamePath = new TextBox
            {
                Location = new Point(15, 35),
                Size = new Size(520, 26),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlPath.Controls.Add(txtGamePath);

            btnBrowse = CreateButton("Browse", BgInput, 90, 26);
            btnBrowse.Location = new Point(545, 35);
            btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowse.Click += BtnBrowse_Click;
            pnlPath.Controls.Add(btnBrowse);

            mainLayout.Controls.Add(pnlPath, 0, 1);

            // ===== SOCKETS SECTION =====
            var pnlSockets = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(15, 5, 15, 10),
                ColumnCount = 1,
                RowCount = 3
            };
            pnlSockets.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));   // Label
            pnlSockets.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Grid
            pnlSockets.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));   // Buttons

            var lblSockets = new Label
            {
                Text = "Custom Sockets (ID: 100-999)",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextSecondary,
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            pnlSockets.Controls.Add(lblSockets, 0, 0);

            // DataGridView
            dgvSockets = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = BgCard,
                ForeColor = TextPrimary,
                GridColor = BorderColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 },
                ScrollBars = ScrollBars.Vertical
            };

            dgvSockets.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "Socket ID", Width = 120 });
            dgvSockets.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Socket Name" });

            // Styling
            dgvSockets.DefaultCellStyle.BackColor = BgCard;
            dgvSockets.DefaultCellStyle.ForeColor = TextPrimary;
            dgvSockets.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 70, 85);
            dgvSockets.DefaultCellStyle.SelectionForeColor = TextPrimary;
            dgvSockets.DefaultCellStyle.Font = new Font("Segoe UI", 10);

            dgvSockets.ColumnHeadersDefaultCellStyle.BackColor = BgHeader;
            dgvSockets.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            dgvSockets.ColumnHeadersDefaultCellStyle.SelectionBackColor = BgHeader;
            dgvSockets.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9);
            dgvSockets.ColumnHeadersHeight = 36;
            dgvSockets.EnableHeadersVisualStyles = false;

            dgvSockets.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(48, 52, 60);

            pnlSockets.Controls.Add(dgvSockets, 0, 1);

            // Buttons panel - use TableLayoutPanel for proper left/right alignment
            var pnlButtons = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 2,
                RowCount = 1
            };
            pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Left buttons container
            var pnlLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnAdd = CreateButton("+ Add", AccentGreen, 85, 34);
            btnAdd.Click += BtnAdd_Click;
            pnlLeft.Controls.Add(btnAdd);

            btnRemove = CreateButton("Remove", AccentRed, 85, 34);
            btnRemove.Click += BtnRemove_Click;
            pnlLeft.Controls.Add(btnRemove);

            btnSave = CreateButton("Save", BgInput, 75, 34);
            btnSave.Click += BtnSave_Click;
            pnlLeft.Controls.Add(btnSave);

            pnlButtons.Controls.Add(pnlLeft, 0, 0);

            // Right - Deploy button
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            btnDeploy = CreateButton("Deploy to Game", AccentBlue, 145, 36);
            btnDeploy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDeploy.Location = new Point(pnlRight.Width - 145, 2);
            btnDeploy.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnDeploy.Click += BtnDeploy_Click;
            pnlRight.Controls.Add(btnDeploy);

            // Handle resize to keep Deploy button on right
            pnlRight.Resize += (s, e) => btnDeploy.Location = new Point(pnlRight.Width - 145, 2);

            pnlButtons.Controls.Add(pnlRight, 1, 0);

            pnlSockets.Controls.Add(pnlButtons, 0, 2);

            mainLayout.Controls.Add(pnlSockets, 0, 2);

            // ===== FOOTER =====
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgHeader,
                Padding = new Padding(15, 0, 15, 0)
            };

            lblStatus = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextSecondary,
                Location = new Point(15, 18),
                AutoSize = true
            };
            pnlFooter.Controls.Add(lblStatus);

            var lblGitHub = new LinkLabel
            {
                Text = "GitHub",
                Font = new Font("Segoe UI", 9),
                LinkColor = AccentBlue,
                Location = new Point(580, 18),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblGitHub.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/ZeOs360/JellysSocketCreator",
                UseShellExecute = true
            });
            pnlFooter.Controls.Add(lblGitHub);

            mainLayout.Controls.Add(pnlFooter, 0, 3);
        }

        private Button CreateButton(string text, Color bgColor, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bgColor, 0.1f);
            return btn;
        }

        private void LoadResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Load logo
            try
            {
                using var stream = assembly.GetManifestResourceStream("JellysSocketsConfig.jelly.png");
                if (stream != null) logoImage = Image.FromStream(stream);
            }
            catch { }
            if (logoImage == null && File.Exists("jelly.png"))
            {
                try { logoImage = Image.FromFile("jelly.png"); } catch { }
            }

            // Load icon
            try
            {
                using var stream = assembly.GetManifestResourceStream("JellysSocketsConfig.jelly.ico");
                if (stream != null) this.Icon = new Icon(stream);
            }
            catch { }
            if (this.Icon == null && File.Exists("jelly.ico"))
            {
                try { this.Icon = new Icon("jelly.ico"); } catch { }
            }
        }

        private void AutoDetectGamePath()
        {
            // Try to load saved path first
            if (File.Exists("JellysSocketsConfig.settings"))
            {
                try
                {
                    string saved = File.ReadAllText("JellysSocketsConfig.settings").Trim();
                    if (Directory.Exists(saved))
                    {
                        txtGamePath.Text = saved;
                        return;
                    }
                }
                catch { }
            }

            // Common paths to check
            string[] commonPaths = new[]
            {
                @"C:\Program Files\Epic Games\PCBuildingSimulator2",
                @"D:\Program Files\Epic Games\PCBuildingSimulator2",
                @"E:\Program Files\Epic Games\PCBuildingSimulator2",
                @"C:\Games\PCBuildingSimulator2",
                @"D:\Games\PCBuildingSimulator2",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "PCBuildingSimulator2"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Epic Games", "PCBuildingSimulator2")
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "PCBS2.exe")))
                {
                    txtGamePath.Text = path;
                    SetStatus($"Auto-detected: {path}", AccentGreen);
                    return;
                }
            }

            // Try registry (Epic Games Launcher)
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Epic Games\EOS");
                if (key != null)
                {
                    var installPath = key.GetValue("ModSdkMetadataDir") as string;
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        var epicRoot = Path.GetDirectoryName(Path.GetDirectoryName(installPath));
                        if (epicRoot != null)
                        {
                            var gamePath = Path.Combine(epicRoot, "PCBuildingSimulator2");
                            if (Directory.Exists(gamePath))
                            {
                                txtGamePath.Text = gamePath;
                                return;
                            }
                        }
                    }
                }
            }
            catch { }

            // Default fallback
            txtGamePath.Text = @"C:\Program Files\Epic Games\PCBuildingSimulator2";
        }

        private void LoadConfig()
        {
            sockets.Clear();
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<SocketConfig>(json);
                    if (config?.sockets != null)
                        sockets.AddRange(config.sockets);
                }
                catch (Exception ex)
                {
                    SetStatus($"Config error: {ex.Message}", AccentRed);
                }
            }
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgvSockets.Rows.Clear();
            foreach (var s in sockets)
                dgvSockets.Rows.Add(s.id, s.name);
        }

        private void SaveConfig()
        {
            sockets.Clear();
            foreach (DataGridViewRow row in dgvSockets.Rows)
            {
                if (row.Cells["ID"].Value != null && row.Cells["Name"].Value != null)
                {
                    if (int.TryParse(row.Cells["ID"].Value.ToString(), out int id))
                        sockets.Add(new SocketEntry { id = id, name = row.Cells["Name"].Value.ToString() ?? "" });
                }
            }

            var config = new SocketConfig { sockets = sockets };
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, options));
        }

        private void SaveSettings()
        {
            File.WriteAllText("JellysSocketsConfig.settings", txtGamePath.Text);
        }

        private void SetStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select PC Building Simulator 2 folder",
                SelectedPath = txtGamePath.Text,
                ShowNewFolderButton = false
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtGamePath.Text = dialog.SelectedPath;
                SaveSettings();
                SetStatus("Path updated", AccentGreen);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            int nextId = 100;
            foreach (var s in sockets)
                if (s.id >= nextId) nextId = s.id + 1;

            using var form = new AddSocketForm(nextId, this.Icon);
            if (form.ShowDialog() == DialogResult.OK)
            {
                sockets.Add(new SocketEntry { id = form.SocketId, name = form.SocketName });
                RefreshGrid();
                SetStatus($"Added: {form.SocketId} = {form.SocketName}", AccentGreen);
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            if (dgvSockets.SelectedRows.Count > 0)
            {
                int idx = dgvSockets.SelectedRows[0].Index;
                if (idx >= 0 && idx < sockets.Count)
                {
                    string name = sockets[idx].name;
                    sockets.RemoveAt(idx);
                    RefreshGrid();
                    SetStatus($"Removed: {name}", AccentRed);
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            SaveConfig();
            SaveSettings();
            SetStatus("Config saved", AccentGreen);
        }

        private void BtnDeploy_Click(object? sender, EventArgs e)
        {
            SaveConfig();
            SaveSettings();

            string gamePath = txtGamePath.Text;
            if (!Directory.Exists(gamePath))
            {
                SetStatus("Game folder not found!", AccentRed);
                return;
            }

            try
            {
                // Copy JSON config
                string destConfig = Path.Combine(gamePath, "JellysSockets.json");
                File.Copy(configPath, destConfig, true);

                // Copy version.dll
                string srcDll = Path.Combine(AppContext.BaseDirectory, "version.dll");
                if (!File.Exists(srcDll)) srcDll = "version.dll";
                
                if (File.Exists(srcDll))
                {
                    string destDll = Path.Combine(gamePath, "version.dll");
                    File.Copy(srcDll, destDll, true);
                    SetStatus($"Deployed! Launch the game.", AccentGreen);
                }
                else
                {
                    SetStatus("Config deployed. version.dll not found - copy manually.", AccentBlue);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", AccentRed);
            }
        }
    }

    public class AddSocketForm : Form
    {
        private TextBox txtId = null!;
        private TextBox txtName = null!;

        public int SocketId { get; private set; }
        public string SocketName { get; private set; } = "";

        private static readonly Color BgDark = Color.FromArgb(30, 32, 38);
        private static readonly Color BgInput = Color.FromArgb(55, 58, 66);
        private static readonly Color AccentGreen = Color.FromArgb(92, 160, 108);
        private static readonly Color TextPrimary = Color.FromArgb(225, 228, 232);
        private static readonly Color TextSecondary = Color.FromArgb(140, 145, 155);
        private static readonly Color BorderColor = Color.FromArgb(65, 70, 80);

        public AddSocketForm(int suggestedId, Icon? parentIcon = null)
        {
            this.Text = "Add Socket";
            this.Size = new Size(350, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = BgDark;
            this.ForeColor = TextPrimary;
            this.Font = new Font("Segoe UI", 9);
            if (parentIcon != null) this.Icon = parentIcon;

            var lblId = new Label { Text = "Socket ID (100-999):", Location = new Point(20, 20), AutoSize = true, ForeColor = TextSecondary };
            txtId = new TextBox
            {
                Text = suggestedId.ToString(),
                Location = new Point(20, 45),
                Size = new Size(295, 26),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblName = new Label { Text = "Socket Name:", Location = new Point(20, 80), AutoSize = true, ForeColor = TextSecondary };
            txtName = new TextBox
            {
                Location = new Point(20, 105),
                Size = new Size(295, 26),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnAdd = new Button
            {
                Text = "Add",
                Location = new Point(130, 145),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentGreen,
                ForeColor = TextPrimary
            };
            btnAdd.FlatAppearance.BorderColor = BorderColor;
            btnAdd.Click += (s, e) =>
            {
                if (int.TryParse(txtId.Text, out int id) && id >= 100 && id < 1000 && !string.IsNullOrWhiteSpace(txtName.Text))
                {
                    SocketId = id;
                    SocketName = txtName.Text.Trim();
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Invalid ID (100-999) or empty name!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(220, 145),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = BgInput,
                ForeColor = TextPrimary
            };
            btnCancel.FlatAppearance.BorderColor = BorderColor;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            this.Controls.AddRange(new Control[] { lblId, txtId, lblName, txtName, btnAdd, btnCancel });
            this.AcceptButton = btnAdd;
            this.CancelButton = btnCancel;
        }
    }

    public class SocketEntry
    {
        public int id { get; set; }
        public string name { get; set; } = "";
    }

    public class SocketConfig
    {
        public List<SocketEntry> sockets { get; set; } = new List<SocketEntry>();
    }
}
