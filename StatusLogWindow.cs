using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LADApp
{
    public partial class StatusLogWindow : Form
    {
        private Panel systemCard = null!;
        private Panel healthCard = null!;
        private Panel peripheralsCard = null!;
        private Label systemStatusLabel = null!;
        private Label healthStatusLabel = null!;
        private Label peripheralsStatusLabel = null!;
        private Button viewLogButton = null!;
        private TextBox? logTextBox = null;
        private bool logVisible = false;
        
        private static readonly object fileLock = new object();
        private static string? sessionLogPath = null;
        
        // Status data (updated from MainForm)
        public string SystemStatus { get; set; } = "Initializing...";
        public string HealthStatus { get; set; } = "Initializing...";
        public string PeripheralsStatus { get; set; } = "Initializing...";
        public bool IsLADReady { get; set; } = false;

        public StatusLogWindow()
        {
            InitializeComponent();
            InitializeSessionLog();
        }

        private void InitializeSessionLog()
        {
            try
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                sessionLogPath = Path.Combine(appDirectory, "session_log.txt");

                string separator = new string('=', 80);
                string sessionStart = $"\n{separator}\n" +
                                     $"SESSION START: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n" +
                                     $"Application Directory: {appDirectory}\n" +
                                     $"{separator}\n\n";

                lock (fileLock)
                {
                    File.AppendAllText(sessionLogPath, sessionStart);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize session log: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            // Modern dark acrylic theme
            this.Text = "LAD Dashboard";
            this.Size = new Size(800, 500);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 400);
            this.BackColor = Color.FromArgb(32, 32, 32); // Dark background
            this.ForeColor = Color.White;

            // Main container with padding
            Panel mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };

            // Title label
            Label titleLabel = new Label
            {
                Text = "LAD Dashboard",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Status cards container
            FlowLayoutPanel cardsContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 50, 0, 0),
                AutoSize = false
            };

            // System Status Card
            systemCard = CreateStatusCard("System", "LAD Ready", SystemIcons.Information);
            systemStatusLabel = new Label
            {
                Text = SystemStatus,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Location = new Point(15, 60),
                Size = new Size(190, 110),
                TextAlign = ContentAlignment.TopLeft
            };
            systemCard.Controls.Add(systemStatusLabel);

            // Health Status Card
            healthCard = CreateStatusCard("Health", "Battery & Thermal", SystemIcons.Shield);
            healthStatusLabel = new Label
            {
                Text = HealthStatus,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Location = new Point(15, 60),
                Size = new Size(190, 110),
                TextAlign = ContentAlignment.TopLeft
            };
            healthCard.Controls.Add(healthStatusLabel);

            // Peripherals Status Card
            peripheralsCard = CreateStatusCard("Peripherals", "Wake Devices", SystemIcons.Application);
            peripheralsStatusLabel = new Label
            {
                Text = PeripheralsStatus,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Location = new Point(15, 60),
                Size = new Size(190, 110),
                TextAlign = ContentAlignment.TopLeft
            };
            peripheralsCard.Controls.Add(peripheralsStatusLabel);

            // Add cards to container
            cardsContainer.Controls.Add(systemCard);
            cardsContainer.Controls.Add(healthCard);
            cardsContainer.Controls.Add(peripheralsCard);

            // View Log button (bottom)
            viewLogButton = new Button
            {
                Text = "View Log",
                Size = new Size(120, 35),
                Location = new Point(20, 0),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            viewLogButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            viewLogButton.FlatAppearance.BorderSize = 1;
            viewLogButton.Click += ViewLogButton_Click;

            // Layout
            mainContainer.Controls.Add(titleLabel);
            mainContainer.Controls.Add(cardsContainer);
            mainContainer.Controls.Add(viewLogButton);
            this.Controls.Add(mainContainer);

            // Update status display
            UpdateStatusDisplay();
        }

        private Panel CreateStatusCard(string title, string subtitle, Icon icon)
        {
            Panel card = new Panel
            {
                Size = new Size(220, 180),
                Margin = new Padding(10),
                BackColor = Color.FromArgb(45, 45, 45), // Slightly lighter dark
                Padding = new Padding(15)
            };

            // Rounded corners effect (using Paint event)
            card.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                using (GraphicsPath path = new GraphicsPath())
                {
                    int radius = 12;
                    Rectangle rect = card.ClientRectangle;
                    rect.Width--;
                    rect.Height--;
                    
                    path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    
                    card.Region = new Region(path);
                }
                
                // Draw border
                using (Pen pen = new Pen(Color.FromArgb(80, 80, 80), 1))
                {
                    g.DrawPath(pen, new GraphicsPath());
                }
            };

            // Card header
            Label titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            Label subtitleLabel = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(15, 38)
            };

            card.Controls.Add(titleLabel);
            card.Controls.Add(subtitleLabel);

            return card;
        }

        public void UpdateStatusDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateStatusDisplay));
                return;
            }

            // Update System card
            systemStatusLabel.Text = SystemStatus;
            systemCard.BackColor = IsLADReady 
                ? Color.FromArgb(45, 80, 45) // Green tint when ready
                : Color.FromArgb(45, 45, 45);

            // Update Health card
            healthStatusLabel.Text = HealthStatus;

            // Update Peripherals card
            peripheralsStatusLabel.Text = PeripheralsStatus;
        }

        public void SetSystemStatus(string status)
        {
            SystemStatus = status;
            UpdateStatusDisplay();
        }

        public void SetHealthStatus(string status)
        {
            HealthStatus = status;
            UpdateStatusDisplay();
        }

        public void SetPeripheralsStatus(string status)
        {
            PeripheralsStatus = status;
            UpdateStatusDisplay();
        }

        public void SetLADReadyState(bool isReady)
        {
            IsLADReady = isReady;
            UpdateStatusDisplay();
        }

        private void ViewLogButton_Click(object? sender, EventArgs e)
        {
            if (!logVisible)
            {
                // Show log
                logTextBox = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 9),
                    BackColor = Color.FromArgb(20, 20, 20),
                    ForeColor = Color.LimeGreen,
                    Margin = new Padding(20)
                };

                Panel logPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20),
                    BackColor = Color.FromArgb(32, 32, 32)
                };
                logPanel.Controls.Add(logTextBox);

                // Add close button
                Button closeLogButton = new Button
                {
                    Text = "Close Log",
                    Size = new Size(120, 35),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                    BackColor = Color.FromArgb(64, 64, 64),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9)
                };
                closeLogButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
                closeLogButton.FlatAppearance.BorderSize = 1;
                closeLogButton.Click += (s, e) =>
                {
                    logPanel.Visible = false;
                    logPanel.Dispose();
                    logTextBox = null;
                    logVisible = false;
                    viewLogButton.Text = "View Log";
                };

                logPanel.Controls.Add(closeLogButton);
                this.Controls.Add(logPanel);
                logPanel.BringToFront();
                logVisible = true;
                viewLogButton.Text = "Hide Log";
            }
            else
            {
                // Hide log
                if (logTextBox != null)
                {
                    Control? logPanel = logTextBox.Parent;
                    if (logPanel != null)
                    {
                        logPanel.Visible = false;
                        logPanel.Dispose();
                    }
                    logTextBox = null;
                    logVisible = false;
                    viewLogButton.Text = "View Log";
                }
            }
        }

        // Legacy support - maintain AddLogEntry for backward compatibility
        public void AddLogEntry(string message)
        {
            if (logTextBox != null && logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action<string>(AddLogEntry), message);
                return;
            }

            if (logTextBox != null)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"{timestamp} - {message}\r\n";
                logTextBox.AppendText(logEntry);
                logTextBox.SelectionStart = logTextBox.Text.Length;
                logTextBox.ScrollToCaret();
            }

            // Always write to session log file
            WriteToSessionLog(message);
        }

        private static void WriteToSessionLog(string message)
        {
            if (string.IsNullOrEmpty(sessionLogPath))
            {
                try
                {
                    string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    sessionLogPath = Path.Combine(appDirectory, "session_log.txt");
                }
                catch
                {
                    return;
                }
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"{timestamp} - {message}\r\n";

                lock (fileLock)
                {
                    File.AppendAllText(sessionLogPath, logEntry);
                }
            }
            catch
            {
                // Silently handle errors
            }
        }

        public static void WriteDirectToSessionLog(string message)
        {
            if (string.IsNullOrEmpty(sessionLogPath))
            {
                try
                {
                    string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    sessionLogPath = Path.Combine(appDirectory, "session_log.txt");
                }
                catch
                {
                    return;
                }
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"{timestamp} - {message}\r\n";

                lock (fileLock)
                {
                    File.AppendAllText(sessionLogPath, logEntry);
                }
            }
            catch
            {
                // Silently handle errors
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
