using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace JellysSocketsConfig
{
    public partial class Form1 : Form
    {
        // Modern color scheme - Purple/Blue gradient theme
        private static readonly Color BgDark = Color.FromArgb(13, 17, 23);
        private static readonly Color BgPanel = Color.FromArgb(22, 27, 34);
        private static readonly Color BgCard = Color.FromArgb(30, 37, 46);
        private static readonly Color BgInput = Color.FromArgb(33, 38, 45);
        private static readonly Color AccentPurple = Color.FromArgb(130, 80, 223);
        private static readonly Color AccentBlue = Color.FromArgb(56, 139, 253);
        private static readonly Color AccentGreen = Color.FromArgb(63, 185, 80);
        private static readonly Color AccentRed = Color.FromArgb(248, 81, 73);
        private static readonly Color AccentOrange = Color.FromArgb(210, 153, 34);
        private static readonly Color TextPrimary = Color.FromArgb(230, 237, 243);
        private static readonly Color TextSecondary = Color.FromArgb(139, 148, 158);
        private static readonly Color BorderColor = Color.FromArgb(48, 54, 61);

        private Panel pnlHeader = null!;
        private Panel pnlContent = null!;
        private Panel pnlFooter = null!;
        private DataGridView dgvSockets = null!;
        private TextBox txtGamePath = null!;
        private GradientButton btnBrowse = null!;
        private GradientButton btnAdd = null!;
        private GradientButton btnRemove = null!;
        private GradientButton btnSave = null!;
        private GradientButton btnDeploy = null!;
        private Label lblStatus = null!;
        private PictureBox picLogo = null!;

        private List<SocketEntry> sockets = new List<SocketEntry>();
        private string configPath = "JellysSockets.json";
        private Image? logoImage = null;

        public Form1()
        {
            InitializeComponent();
            LoadLogoImage();
            SetupModernUI();
            LoadConfig();
            LoadSettings();
        }

        private void LoadLogoImage()
        {
            try
            {
                // Try to load from embedded resource
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "JellysSocketsConfig.jelly.png";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    logoImage = Image.FromStream(stream);
                }
            }
            catch
            {
                // Try to load from file
                try
                {
                    if (File.Exists("jelly.png"))
                        logoImage = Image.FromFile("jelly.png");
                }
                catch { }
            }
        }

        private void SetupModernUI()
        {
            // Form settings
            this.Text = "Jelly's Socket Creator";
            this.Size = new Size(750, 620);
            this.MinimumSize = new Size(650, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgDark;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.DoubleBuffered = true;

            // Set form icon
            try
            {
                if (File.Exists("jelly.ico"))
                    this.Icon = new Icon("jelly.ico");
            }
            catch { }

            // ========== HEADER ==========
            pnlHeader = new GradientPanel(BgPanel, Color.FromArgb(35, 39, 47))
            {
                Dock = DockStyle.Top,
                Height = 90
            };

            // Logo
            picLogo = new PictureBox
            {
                Size = new Size(60, 60),
                Location = new Point(25, 15),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            if (logoImage != null)
                picLogo.Image = logoImage;
            pnlHeader.Controls.Add(picLogo);

            // Title
            var lblTitle = new Label
            {
                Text = "Jelly's Socket Creator",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(95, 18),
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
                Location = new Point(97, 52),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblSubtitle);

            // Version badge
            var lblVersion = new Label
            {
                Text = "v1.0.0",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = AccentPurple,
                BackColor = Color.FromArgb(130, 80, 223, 30),
                AutoSize = false,
                Size = new Size(50, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(this.Width - 80, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlHeader.Controls.Add(lblVersion);

            this.Controls.Add(pnlHeader);

            // ========== FOOTER ==========
            pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = BgPanel
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            lblStatus = new Label
            {
                Text = "✓ Ready - Add your custom sockets and deploy!",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextSecondary,
                AutoSize = true,
                Location = new Point(25, 18)
            };
            pnlFooter.Controls.Add(lblStatus);

            // GitHub link
            var lblGitHub = new LinkLabel
            {
                Text = "GitHub",
                Font = new Font("Segoe UI", 9),
                LinkColor = AccentBlue,
                ActiveLinkColor = AccentPurple,
                AutoSize = true,
                Location = new Point(this.Width - 80, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblGitHub.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/ZeOs360/JellysSocketCreator",
                UseShellExecute = true
            });
            pnlFooter.Controls.Add(lblGitHub);

            this.Controls.Add(pnlFooter);

            // ========== CONTENT ==========
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(25, 15, 25, 15)
            };
            this.Controls.Add(pnlContent);

            // Game Path Card
            var cardPath = new RoundedPanel
            {
                Location = new Point(25, 15),
                Size = new Size(680, 85),
                BackColor = BgCard,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblPathTitle = new Label
            {
                Text = "📁 Game Installation",
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(15, 12),
                BackColor = Color.Transparent
            };
            cardPath.Controls.Add(lblPathTitle);

            txtGamePath = new TextBox
            {
                Location = new Point(15, 45),
                Size = new Size(560, 28),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = @"C:\Program Files\Epic Games\PCBuildingSimulator2",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cardPath.Controls.Add(txtGamePath);

            btnBrowse = new GradientButton("Browse", AccentBlue, Color.FromArgb(36, 119, 233))
            {
                Location = new Point(585, 43),
                Size = new Size(80, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowse.Click += BtnBrowse_Click;
            cardPath.Controls.Add(btnBrowse);

            pnlContent.Controls.Add(cardPath);

            // Sockets Card
            var cardSockets = new RoundedPanel
            {
                Location = new Point(25, 110),
                Size = new Size(680, 290),
                BackColor = BgCard,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblSocketsTitle = new Label
            {
                Text = "🔧 Custom Sockets",
                Font = new Font("Segoe UI Semibold", 11),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(15, 12),
                BackColor = Color.Transparent
            };
            cardSockets.Controls.Add(lblSocketsTitle);

            var lblSocketsHint = new Label
            {
                Text = "Socket IDs must be 100-999",
                Font = new Font("Segoe UI", 8),
                ForeColor = TextSecondary,
                AutoSize = true,
                Location = new Point(160, 16),
                BackColor = Color.Transparent
            };
            cardSockets.Controls.Add(lblSocketsHint);

            // DataGridView
            dgvSockets = new DataGridView
            {
                Location = new Point(15, 45),
                Size = new Size(650, 190),
                BackgroundColor = BgInput,
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
                RowTemplate = { Height = 38 },
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ScrollBars = ScrollBars.Vertical
            };

            dgvSockets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ID",
                HeaderText = "  Socket ID",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });
            dgvSockets.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "  Socket Name"
            });

            dgvSockets.DefaultCellStyle.BackColor = BgInput;
            dgvSockets.DefaultCellStyle.ForeColor = TextPrimary;
            dgvSockets.DefaultCellStyle.SelectionBackColor = AccentPurple;
            dgvSockets.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvSockets.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvSockets.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            dgvSockets.ColumnHeadersDefaultCellStyle.BackColor = BgCard;
            dgvSockets.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            dgvSockets.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9);
            dgvSockets.ColumnHeadersHeight = 42;
            dgvSockets.EnableHeadersVisualStyles = false;

            dgvSockets.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(38, 43, 52);

            cardSockets.Controls.Add(dgvSockets);

            // Socket buttons
            btnAdd = new GradientButton("+ Add Socket", AccentGreen, Color.FromArgb(43, 165, 60))
            {
                Location = new Point(15, 245),
                Size = new Size(120, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnAdd.Click += BtnAdd_Click;
            cardSockets.Controls.Add(btnAdd);

            btnRemove = new GradientButton("Remove", AccentRed, Color.FromArgb(228, 61, 53))
            {
                Location = new Point(145, 245),
                Size = new Size(100, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnRemove.Click += BtnRemove_Click;
            cardSockets.Controls.Add(btnRemove);

            pnlContent.Controls.Add(cardSockets);

            // Action Buttons Panel
            var pnlActions = new Panel
            {
                Location = new Point(25, 410),
                Size = new Size(680, 50),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            btnSave = new GradientButton("💾 Save Config", BorderColor, Color.FromArgb(58, 64, 71))
            {
                Location = new Point(0, 5),
                Size = new Size(130, 40)
            };
            btnSave.Click += BtnSave_Click;
            pnlActions.Controls.Add(btnSave);

            btnDeploy = new GradientButton("🚀 Deploy to Game", AccentPurple, Color.FromArgb(100, 60, 193))
            {
                Location = new Point(530, 5),
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnDeploy.Click += BtnDeploy_Click;
            pnlActions.Controls.Add(btnDeploy);

            pnlContent.Controls.Add(pnlActions);

            // Resize handler
            this.Resize += (s, e) =>
            {
                cardPath.Width = pnlContent.Width - 50;
                txtGamePath.Width = cardPath.Width - 120;
                cardSockets.Width = pnlContent.Width - 50;
                cardSockets.Height = pnlContent.Height - 180;
                dgvSockets.Width = cardSockets.Width - 30;
                dgvSockets.Height = cardSockets.Height - 100;
                btnAdd.Top = cardSockets.Height - 45;
                btnRemove.Top = cardSockets.Height - 45;
                pnlActions.Width = pnlContent.Width - 50;
                pnlActions.Top = pnlContent.Height - 70;
            };
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
                    {
                        sockets.AddRange(config.sockets);
                    }
                }
                catch (Exception ex)
                {
                    SetStatus($"⚠ Config error: {ex.Message}", AccentOrange);
                }
            }
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgvSockets.Rows.Clear();
            foreach (var s in sockets)
            {
                dgvSockets.Rows.Add(s.id, s.name);
            }
        }

        private void SaveConfig()
        {
            sockets.Clear();
            foreach (DataGridViewRow row in dgvSockets.Rows)
            {
                if (row.Cells["ID"].Value != null && row.Cells["Name"].Value != null)
                {
                    if (int.TryParse(row.Cells["ID"].Value.ToString(), out int id))
                    {
                        sockets.Add(new SocketEntry { id = id, name = row.Cells["Name"].Value.ToString() ?? "" });
                    }
                }
            }

            var config = new SocketConfig { sockets = sockets };
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, json);
        }

        private void LoadSettings()
        {
            string settingsPath = "JellysSocketsConfig.settings";
            if (File.Exists(settingsPath))
            {
                string gamePath = File.ReadAllText(settingsPath).Trim();
                if (!string.IsNullOrEmpty(gamePath))
                    txtGamePath.Text = gamePath;
            }
        }

        private void SaveSettings()
        {
            File.WriteAllText("JellysSocketsConfig.settings", txtGamePath.Text);
        }

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select PC Building Simulator 2 folder",
                SelectedPath = txtGamePath.Text
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtGamePath.Text = dialog.SelectedPath;
                SaveSettings();
                SetStatus("✓ Game path updated!", AccentGreen);
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            int nextId = 100;
            foreach (var s in sockets)
            {
                if (s.id >= nextId) nextId = s.id + 1;
            }

            using var form = new AddSocketForm(nextId);
            if (form.ShowDialog() == DialogResult.OK)
            {
                sockets.Add(new SocketEntry { id = form.SocketId, name = form.SocketName });
                RefreshGrid();
                SetStatus($"✓ Added: {form.SocketId} = {form.SocketName}", AccentGreen);
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
                    SetStatus($"✓ Removed: {name}", TextSecondary);
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            SaveConfig();
            SaveSettings();
            SetStatus($"✓ Saved {sockets.Count} socket(s) to config", AccentGreen);
        }

        private void BtnDeploy_Click(object? sender, EventArgs e)
        {
            SaveConfig();
            SaveSettings();

            string gamePath = txtGamePath.Text;
            if (!Directory.Exists(gamePath))
            {
                SetStatus("✗ Game folder not found! Check the path.", AccentRed);
                return;
            }

            try
            {
                // Copy config file
                string destConfig = Path.Combine(gamePath, "JellysSockets.json");
                File.Copy(configPath, destConfig, true);

                // Copy DLL if exists
                string sourceDll = "version.dll";
                string destDll = Path.Combine(gamePath, "version.dll");
                if (File.Exists(sourceDll))
                    File.Copy(sourceDll, destDll, true);

                SetStatus("✓ Deployed! Launch the game to apply changes.", AccentGreen);
                
                MessageBox.Show(
                    "Deployment successful!\n\nYour custom sockets are now installed.\nLaunch PC Building Simulator 2 to use them.",
                    "Jelly's Socket Creator",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                SetStatus($"✗ Deploy failed: {ex.Message}", AccentRed);
            }
        }
    }

    // ==================== HELPER CLASSES ====================

    public class SocketEntry
    {
        public int id { get; set; }
        public string name { get; set; } = "";
    }

    public class SocketConfig
    {
        public List<SocketEntry> sockets { get; set; } = new();
    }

    public class GradientPanel : Panel
    {
        private Color color1, color2;

        public GradientPanel(Color c1, Color c2)
        {
            color1 = c1;
            color2 = c2;
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var brush = new LinearGradientBrush(
                this.ClientRectangle,
                color1, color2,
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, this.ClientRectangle);
        }
    }

    public class RoundedPanel : Panel
    {
        public RoundedPanel()
        {
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = GetRoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8);
            using var brush = new SolidBrush(this.BackColor);
            e.Graphics.FillPath(brush, path);

            using var pen = new Pen(Color.FromArgb(48, 54, 61), 1);
            e.Graphics.DrawPath(pen, path);
        }

        private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class GradientButton : Button
    {
        private Color color1, color2;
        private bool isHovered = false;

        public GradientButton(string text, Color c1, Color c2)
        {
            this.Text = text;
            this.color1 = c1;
            this.color2 = c2;
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            this.Cursor = Cursors.Hand;
            this.DoubleBuffered = true;

            this.MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
            this.MouseLeave += (s, e) => { isHovered = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = GetRoundedRect(rect, 6);

            // Gradient fill
            Color c1 = isHovered ? ControlPaint.Light(color1, 0.1f) : color1;
            Color c2 = isHovered ? ControlPaint.Light(color2, 0.1f) : color2;

            using var brush = new LinearGradientBrush(rect, c1, c2, LinearGradientMode.Vertical);
            e.Graphics.FillPath(brush, path);

            // Border
            using var pen = new Pen(Color.FromArgb(60, 255, 255, 255), 1);
            e.Graphics.DrawPath(pen, path);

            // Text
            TextRenderer.DrawText(e.Graphics, this.Text, this.Font, rect, this.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            this.Region = new Region(path);
        }

        private GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class AddSocketForm : Form
    {
        private TextBox txtId = null!;
        private TextBox txtName = null!;

        public int SocketId { get; private set; }
        public string SocketName { get; private set; } = "";

        private static readonly Color BgDark = Color.FromArgb(22, 27, 34);
        private static readonly Color BgInput = Color.FromArgb(33, 38, 45);
        private static readonly Color AccentPurple = Color.FromArgb(130, 80, 223);
        private static readonly Color AccentGreen = Color.FromArgb(63, 185, 80);
        private static readonly Color TextPrimary = Color.FromArgb(230, 237, 243);
        private static readonly Color TextSecondary = Color.FromArgb(139, 148, 158);
        private static readonly Color BorderColor = Color.FromArgb(48, 54, 61);

        public AddSocketForm(int suggestedId)
        {
            this.Text = "Add Custom Socket";
            this.Size = new Size(420, 240);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = BgDark;
            this.ForeColor = TextPrimary;

            var lblTitle = new Label
            {
                Text = "🔧 Add New Socket",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(20, 15),
                AutoSize = true
            };

            var lblId = new Label
            {
                Text = "Socket ID:",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextSecondary,
                Location = new Point(20, 65),
                AutoSize = true
            };

            txtId = new TextBox
            {
                Text = suggestedId.ToString(),
                Location = new Point(180, 62),
                Size = new Size(200, 28),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };

            var lblIdHint = new Label
            {
                Text = "(100-999)",
                Font = new Font("Segoe UI", 8),
                ForeColor = TextSecondary,
                Location = new Point(115, 68),
                AutoSize = true
            };

            var lblName = new Label
            {
                Text = "Socket Name:",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextSecondary,
                Location = new Point(20, 105),
                AutoSize = true
            };

            txtName = new TextBox
            {
                Text = "",
                Location = new Point(180, 102),
                Size = new Size(200, 28),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };

            var btnAdd = new GradientButton("Add Socket", AccentGreen, Color.FromArgb(43, 165, 60))
            {
                Location = new Point(115, 155),
                Size = new Size(110, 38)
            };
            btnAdd.Click += (s, e) =>
            {
                if (int.TryParse(txtId.Text, out int id) && id >= 100 && id < 1000 && !string.IsNullOrWhiteSpace(txtName.Text))
                {
                    SocketId = id;
                    SocketName = txtName.Text.Trim();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid ID (must be 100-999) or empty name!", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            var btnCancel = new GradientButton("Cancel", BorderColor, Color.FromArgb(58, 64, 71))
            {
                Location = new Point(235, 155),
                Size = new Size(110, 38)
            };
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lblTitle, lblId, lblIdHint, txtId, lblName, txtName, btnAdd, btnCancel });
            this.AcceptButton = btnAdd;
            this.CancelButton = btnCancel;
        }
    }
}
