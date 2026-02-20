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
            DpiHelper.ConfigureForm(this);
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
            // Modern dark acrylic theme with DPI-aware sizing
            this.Text = "LAD Dashboard";
            this.Size = DpiHelper.Scale(new Size(800, 500));
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = DpiHelper.Scale(new Size(600, 400));
            this.BackColor = Color.FromArgb(32, 32, 32); // Dark background
            this.ForeColor = Color.White;

            // Use TableLayoutPanel for responsive layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = DpiHelper.Scale(new Padding(20)),
                BackColor = Color.Transparent
            };

            // Configure rows: Title (auto), Cards (fill), Button (auto)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiHelper.Scale(50)));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiHelper.Scale(50)));

            // Title label
            Label titleLabel = new Label
            {
                Text = "LAD Dashboard",
                Font = DpiHelper.CreateFont("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Status cards container using FlowLayoutPanel for wrapping
            FlowLayoutPanel cardsContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = false,
                AutoScroll = true
            };

            // System Status Card
            systemCard = CreateStatusCard("System", "LAD Ready", SystemIcons.Information);
            systemStatusLabel = new Label
            {
                Text = SystemStatus,
                Font = DpiHelper.CreateFont("Segoe UI", 9),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = DpiHelper.Scale(new Padding(15, 60, 15, 15)),
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };
            systemCard.Controls.Add(systemStatusLabel);

            // Health Status Card
            healthCard = CreateStatusCard("Health", "Battery & Thermal", SystemIcons.Shield);
            healthStatusLabel = new Label
            {
                Text = HealthStatus,
                Font = DpiHelper.CreateFont("Segoe UI", 9),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = DpiHelper.Scale(new Padding(15, 60, 15, 15)),
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };
            healthCard.Controls.Add(healthStatusLabel);

            // Peripherals Status Card
            peripheralsCard = CreateStatusCard("Peripherals", "Wake Devices", SystemIcons.Application);
            peripheralsStatusLabel = new Label
            {
                Text = PeripheralsStatus,
                Font = DpiHelper.CreateFont("Segoe UI", 9),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = DpiHelper.Scale(new Padding(15, 60, 15, 15)),
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent
            };
            peripheralsCard.Controls.Add(peripheralsStatusLabel);

            // Add cards to container
            cardsContainer.Controls.Add(systemCard);
            cardsContainer.Controls.Add(healthCard);
            cardsContainer.Controls.Add(peripheralsCard);

            // View Log button container
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            viewLogButton = new Button
            {
                Text = "View Log",
                Size = DpiHelper.Scale(new Size(120, 35)),
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = DpiHelper.CreateFont("Segoe UI", 9)
            };
            viewLogButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            viewLogButton.FlatAppearance.BorderSize = 1;
            viewLogButton.Click += ViewLogButton_Click;

            buttonPanel.Controls.Add(viewLogButton);

            // Add controls to main layout
            mainLayout.Controls.Add(titleLabel, 0, 0);
            mainLayout.Controls.Add(cardsContainer, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainLayout);

            // Update status display
            UpdateStatusDisplay();
        }

        private Panel CreateStatusCard(string title, string subtitle, Icon icon)
        {
            Panel card = new Panel
            {
                Size = DpiHelper.Scale(new Size(220, 180)),
                Margin = DpiHelper.Scale(new Padding(10)),
                BackColor = Color.FromArgb(45, 45, 45), // Slightly lighter dark
                Padding = DpiHelper.Scale(new Padding(15)),
                BorderStyle = BorderStyle.FixedSingle // Simple border instead of rounded corners
            };

            // Card header using TableLayoutPanel for proper positioning
            TableLayoutPanel headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label titleLabel = new Label
            {
                Text = title,
                Font = DpiHelper.CreateFont("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            Label subtitleLabel = new Label
            {
                Text = subtitle,
                Font = DpiHelper.CreateFont("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Padding = DpiHelper.Scale(new Padding(0, 3, 0, 0)),
                BackColor = Color.Transparent
            };

            headerLayout.Controls.Add(titleLabel, 0, 0);
            headerLayout.Controls.Add(subtitleLabel, 0, 1);
            card.Controls.Add(headerLayout);

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
                    Font = DpiHelper.CreateFont("Consolas", 9),
                    BackColor = Color.FromArgb(20, 20, 20),
                    ForeColor = Color.LimeGreen
                };

                // Use TableLayoutPanel for log panel layout
                TableLayoutPanel logLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = Color.FromArgb(32, 32, 32),
                    Padding = DpiHelper.Scale(new Padding(20))
                };

                logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiHelper.Scale(50)));

                // Add close button
                Button closeLogButton = new Button
                {
                    Text = "Close Log",
                    Size = DpiHelper.Scale(new Size(120, 35)),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                    BackColor = Color.FromArgb(64, 64, 64),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = DpiHelper.CreateFont("Segoe UI", 9)
                };
                closeLogButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
                closeLogButton.FlatAppearance.BorderSize = 1;
                closeLogButton.Click += (s, e) =>
                {
                    logLayout.Visible = false;
                    logLayout.Dispose();
                    logTextBox = null;
                    logVisible = false;
                    viewLogButton.Text = "View Log";
                };

                logLayout.Controls.Add(logTextBox, 0, 0);
                logLayout.Controls.Add(closeLogButton, 0, 1);

                this.Controls.Add(logLayout);
                logLayout.BringToFront();
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
