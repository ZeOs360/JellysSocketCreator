using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace JellysSocketsConfig
{
    public partial class Form1 : Form
    {
        // Modern color scheme
        private static readonly Color BgDark = Color.FromArgb(18, 18, 24);
        private static readonly Color BgPanel = Color.FromArgb(28, 28, 36);
        private static readonly Color BgInput = Color.FromArgb(38, 38, 48);
        private static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
        private static readonly Color AccentGreen = Color.FromArgb(34, 197, 94);
        private static readonly Color AccentRed = Color.FromArgb(239, 68, 68);
        private static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);
        private static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);
        private static readonly Color BorderColor = Color.FromArgb(55, 65, 81);

        private Panel pnlHeader = null!;
        private Panel pnlContent = null!;
        private Panel pnlFooter = null!;
        private DataGridView dgvSockets = null!;
        private TextBox txtGamePath = null!;
        private ModernButton btnBrowse = null!;
        private ModernButton btnAdd = null!;
        private ModernButton btnRemove = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnDeploy = null!;
        private Label lblStatus = null!;
        private Label lblVersion = null!;

        private List<SocketEntry> sockets = new List<SocketEntry>();
        private string configPath = "JellysSockets.json";

        // Enable window dragging
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public Form1()
        {
            InitializeComponent();
            SetupModernUI();
            LoadConfig();
            LoadSettings();
        }

        private void SetupModernUI()
        {
            // Form settings
            this.Text = "JellysSockets";
            this.Size = new Size(700, 580);
            this.MinimumSize = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgDark;
            this.ForeColor = TextPrimary;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.DoubleBuffered = true;

            // Header Panel
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = BgPanel,
                Padding = new Padding(20, 15, 20, 15)
            };
            pnlHeader.Paint += (s, e) => DrawPanelBorder(e.Graphics, pnlHeader, false, true);

            var lblTitle = new Label
            {
                Text = "⚡ JellysSockets",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(20, 12)
            };

            var lblSubtitle = new Label
            {
                Text = "Custom CPU Socket Manager for PC Building Simulator 2",
                Font = new Font("Segoe UI", 9),
                ForeColor = TextSecondary,
                AutoSize = true,
                Location = new Point(22, 48)
            };

            lblVersion = new Label
            {
                Text = "v1.0.0",
                Font = new Font("Segoe UI", 9),
                ForeColor = AccentBlue,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, lblVersion });
            this.Controls.Add(pnlHeader);

            // Footer Panel
            pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = BgPanel,
                Padding = new Padding(20, 10, 20, 10)
            };
            pnlFooter.Paint += (s, e) => DrawPanelBorder(e.Graphics, pnlFooter, true, false);

            lblStatus = new Label
            {
                Text = "✓ Ready",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextSecondary,
                AutoSize = true,
                Location = new Point(20, 20)
            };
            pnlFooter.Controls.Add(lblStatus);
            this.Controls.Add(pnlFooter);

            // Content Panel
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(25, 20, 25, 20)
            };
            this.Controls.Add(pnlContent);

            // Game Path Section
            var lblGamePath = new Label
            {
                Text = "📁 Game Installation Path",
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(25, 20)
            };
            pnlContent.Controls.Add(lblGamePath);

            var pnlPathContainer = new Panel
            {
                Location = new Point(25, 45),
                Size = new Size(630, 40),
                BackColor = BgInput,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlPathContainer.Paint += (s, e) => DrawRoundedBorder(e.Graphics, pnlPathContainer);

            txtGamePath = new TextBox
            {
                Location = new Point(12, 9),
                Size = new Size(540, 22),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                Text = @"C:\Program Files\Epic Games\PCBuildingSimulator2",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlPathContainer.Controls.Add(txtGamePath);

            btnBrowse = new ModernButton("Browse", AccentBlue)
            {
                Location = new Point(558, 5),
                Size = new Size(65, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnBrowse.Click += BtnBrowse_Click;
            pnlPathContainer.Controls.Add(btnBrowse);
            pnlContent.Controls.Add(pnlPathContainer);

            // Sockets Section
            var lblSockets = new Label
            {
                Text = "🔧 Custom Sockets",
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(25, 100)
            };
            pnlContent.Controls.Add(lblSockets);

            // DataGridView
            dgvSockets = new DataGridView
            {
                Location = new Point(25, 125),
                Size = new Size(630, 230),
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
                RowTemplate = { Height = 35 },
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            dgvSockets.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "ID", 
                HeaderText = "Socket ID", 
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgvSockets.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "Name", 
                HeaderText = "Socket Name"
            });

            dgvSockets.DefaultCellStyle.BackColor = BgInput;
            dgvSockets.DefaultCellStyle.ForeColor = TextPrimary;
            dgvSockets.DefaultCellStyle.SelectionBackColor = AccentBlue;
            dgvSockets.DefaultCellStyle.SelectionForeColor = TextPrimary;
            dgvSockets.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dgvSockets.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

            dgvSockets.ColumnHeadersDefaultCellStyle.BackColor = BgPanel;
            dgvSockets.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            dgvSockets.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9);
            dgvSockets.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            dgvSockets.ColumnHeadersHeight = 40;
            dgvSockets.EnableHeadersVisualStyles = false;

            dgvSockets.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(32, 32, 42);
            pnlContent.Controls.Add(dgvSockets);

            // Button Panel
            var pnlButtons = new Panel
            {
                Location = new Point(25, 365),
                Size = new Size(630, 45),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            btnAdd = new ModernButton("+ Add Socket", AccentGreen)
            {
                Location = new Point(0, 5),
                Size = new Size(120, 36)
            };
            btnAdd.Click += BtnAdd_Click;

            btnRemove = new ModernButton("Remove", AccentRed)
            {
                Location = new Point(130, 5),
                Size = new Size(100, 36)
            };
            btnRemove.Click += BtnRemove_Click;

            btnSave = new ModernButton("Save", BorderColor)
            {
                Location = new Point(400, 5),
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSave.Click += BtnSave_Click;

            btnDeploy = new ModernButton("🚀 Deploy", AccentBlue)
            {
                Location = new Point(510, 5),
                Size = new Size(120, 36),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnDeploy.Click += BtnDeploy_Click;

            pnlButtons.Controls.AddRange(new Control[] { btnAdd, btnRemove, btnSave, btnDeploy });
            pnlContent.Controls.Add(pnlButtons);

            // Resize handler
            this.Resize += (s, e) =>
            {
                lblVersion.Location = new Point(pnlHeader.Width - 70, 14);
                pnlPathContainer.Width = pnlContent.Width - 50;
                txtGamePath.Width = pnlPathContainer.Width - 90;
                dgvSockets.Width = pnlContent.Width - 50;
                pnlButtons.Width = pnlContent.Width - 50;
                btnSave.Left = pnlButtons.Width - 230;
                btnDeploy.Left = pnlButtons.Width - 120;
            };

            // Initial position
            lblVersion.Location = new Point(pnlHeader.Width - 70, 14);
        }

        private void DrawPanelBorder(Graphics g, Panel p, bool top, bool bottom)
        {
            using (var pen = new Pen(BorderColor, 1))
            {
                if (top) g.DrawLine(pen, 0, 0, p.Width, 0);
                if (bottom) g.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
            }
        }

        private void DrawRoundedBorder(Graphics g, Panel p)
        {
            using (var pen = new Pen(BorderColor, 1))
            {
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            }
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
                    SetStatus("⚠ Error loading config: " + ex.Message, AccentRed);
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
                {
                    txtGamePath.Text = gamePath;
                }
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
                SetStatus("✓ Game path updated", AccentGreen);
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
                SetStatus($"✓ Added socket: {form.SocketId} = {form.SocketName}", AccentGreen);
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
                    SetStatus($"✓ Removed socket: {name}", TextSecondary);
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            SaveConfig();
            SaveSettings();
            SetStatus($"✓ Saved {sockets.Count} sockets", AccentGreen);
        }

        private void BtnDeploy_Click(object? sender, EventArgs e)
        {
            SaveConfig();
            SaveSettings();

            string gamePath = txtGamePath.Text;
            if (!Directory.Exists(gamePath))
            {
                SetStatus("✗ Game folder not found!", AccentRed);
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
                {
                    File.Copy(sourceDll, destDll, true);
                }

                SetStatus("✓ Deployed successfully! Launch the game to apply changes.", AccentGreen);
            }
            catch (Exception ex)
            {
                SetStatus("✗ Deploy error: " + ex.Message, AccentRed);
            }
        }
    }

    public class SocketEntry
    {
        public int id { get; set; }
        public string name { get; set; } = "";
    }

    public class SocketConfig
    {
        public List<SocketEntry> sockets { get; set; } = new();
    }

    public class ModernButton : Button
    {
        private Color baseColor;
        private bool isHovered = false;

        public ModernButton(string text, Color color)
        {
            this.Text = text;
            this.baseColor = color;
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = color;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            this.Cursor = Cursors.Hand;

            this.MouseEnter += (s, e) => { isHovered = true; UpdateColor(); };
            this.MouseLeave += (s, e) => { isHovered = false; UpdateColor(); };
        }

        private void UpdateColor()
        {
            if (isHovered)
            {
                this.BackColor = ControlPaint.Light(baseColor, 0.15f);
            }
            else
            {
                this.BackColor = baseColor;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw rounded corners effect
            using var path = GetRoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 6);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
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

        private static readonly Color BgDark = Color.FromArgb(28, 28, 36);
        private static readonly Color BgInput = Color.FromArgb(38, 38, 48);
        private static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
        private static readonly Color AccentGreen = Color.FromArgb(34, 197, 94);
        private static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);
        private static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);
        private static readonly Color BorderColor = Color.FromArgb(55, 65, 81);

        public AddSocketForm(int suggestedId)
        {
            this.Text = "Add Custom Socket";
            this.Size = new Size(400, 220);
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
                Text = "Socket ID (100-999):",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextSecondary,
                Location = new Point(20, 60),
                AutoSize = true
            };

            txtId = new TextBox
            {
                Text = suggestedId.ToString(),
                Location = new Point(180, 57),
                Size = new Size(180, 28),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };

            var lblName = new Label
            {
                Text = "Socket Name:",
                Font = new Font("Segoe UI", 10),
                ForeColor = TextSecondary,
                Location = new Point(20, 100),
                AutoSize = true
            };

            txtName = new TextBox
            {
                Text = "",
                Location = new Point(180, 97),
                Size = new Size(180, 28),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11)
            };

            var btnAdd = new ModernButton("Add Socket", AccentGreen)
            {
                Location = new Point(100, 145),
                Size = new Size(100, 35)
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

            var btnCancel = new ModernButton("Cancel", BorderColor)
            {
                Location = new Point(210, 145),
                Size = new Size(100, 35)
            };
            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] { lblTitle, lblId, txtId, lblName, txtName, btnAdd, btnCancel });
            this.AcceptButton = btnAdd;
            this.CancelButton = btnCancel;
        }
    }
}
