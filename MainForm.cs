using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LADApp
{
    public partial class MainForm : Form
    {
        // Windows API for power management
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_AWAYMODE_REQUIRED = 0x00000040;

        // Windows API for global hotkeys
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Windows messages
        private const int WM_POWERBROADCAST = 0x0218;
        private const int WM_HOTKEY = 0x0312;
        private const int PBT_APMSUSPEND = 0x0004;
        private const int PBT_APMRESUMESUSPEND = 0x0007;
        private const int PBT_APMRESUMEAUTOMATIC = 0x0012;

        // Hotkey constants
        // Using Ctrl+Shift+Alt+D for "Display Revert" - less likely to conflict
        private const int HOTKEY_ID_SAFETY_REVERT = 1;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_D = 0x44; // Changed from VK_R to VK_D

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private StatusLogWindow? statusLogWindow;
        private SystemMonitor systemMonitor;
        private PowerManager powerManager;
        private PeripheralWakeManager peripheralWakeManager;
        private DisplayManager displayManager;
        private BatteryHealthManager batteryHealthManager;
        private System.Windows.Forms.Timer detectionTimer;
        private System.Windows.Forms.Timer heartbeatTimer;
        private System.Windows.Forms.Timer performanceHeartbeatTimer;
        private System.Windows.Forms.Timer? wizardTimer;
        private System.Windows.Forms.Timer batteryTimer;
        private bool lastLADReadyState = false;
        private List<string> logBuffer = new List<string>();
        private AppConfig appConfig;
        
        // Battery status tracking
        private float? currentBatteryPercentage;
        private string currentBatteryStatus = "Unknown";
        private string currentBatteryLifeRemaining = "N/A";
        
        // Sleep/Wake tracking for stress testing
        private DateTime? sleepStartTime = null;
        private DateTime? wakeDetectedTime = null;

        public MainForm()
        {
            // Hide the form window
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;

            // Load application configuration
            appConfig = AppConfig.Load();
            if (appConfig.FirstRun)
            {
                LogToStatusWindow($"CONFIG: First Run detected - Configuration file: {AppConfig.GetConfigFilePath()}");
                LogToStatusWindow("CONFIG: Calibration wizard will appear in 1 second...");
            }
            else
            {
                LogToStatusWindow("CONFIG: Configuration loaded (not first run)");
                LogToStatusWindow($"CONFIG: Config file exists at: {AppConfig.GetConfigFilePath()}");
            }

            // Initialize system monitor
            systemMonitor = new SystemMonitor();
            systemMonitor.PowerStatusChanged += SystemMonitor_PowerStatusChanged;
            systemMonitor.MonitorCountChanged += SystemMonitor_MonitorCountChanged;

            // Initialize power manager
            powerManager = new PowerManager();

            // Load or read original hibernate timeout value
            if (appConfig.OriginalHibernateTimeout.HasValue)
            {
                powerManager.SetOriginalHibernateTimeout(appConfig.OriginalHibernateTimeout);
                LogToStatusWindow($"CONFIG: Loaded original hibernate timeout from config: {appConfig.OriginalHibernateTimeout.Value} seconds");
            }
            else
            {
                // Read current hibernate timeout from system and store it
                uint? currentTimeout = powerManager.ReadHibernateTimeout();
                if (currentTimeout.HasValue)
                {
                    powerManager.SetOriginalHibernateTimeout(currentTimeout);
                    appConfig.OriginalHibernateTimeout = currentTimeout;
                    appConfig.Save();
                    LogToStatusWindow($"CONFIG: Read and stored original hibernate timeout: {currentTimeout.Value} seconds");
                    StatusLogWindow.WriteDirectToSessionLog($"CONFIG: Read and stored original hibernate timeout: {currentTimeout.Value} seconds");
                }
                else
                {
                    LogToStatusWindow("CONFIG: Could not read hibernate timeout (may not be available on this system)");
                    StatusLogWindow.WriteDirectToSessionLog("CONFIG: WARNING - Could not read hibernate timeout (may not be available on this system)");
                }
            }

            // Load or read original power scheme GUID
            if (!string.IsNullOrEmpty(appConfig.OriginalPowerSchemeGuid))
            {
                try
                {
                    Guid originalSchemeGuid = Guid.Parse(appConfig.OriginalPowerSchemeGuid);
                    powerManager.SetOriginalPowerScheme(originalSchemeGuid);
                    LogToStatusWindow($"CONFIG: Loaded original power scheme from config: {appConfig.OriginalPowerSchemeGuid}");
                }
                catch (Exception ex)
                {
                    LogToStatusWindow($"CONFIG: Failed to parse stored power scheme GUID: {ex.Message}");
                    // Fall through to read from system
                }
            }
            
            // If we don't have a stored scheme, read it from the system
            if (!powerManager.GetOriginalPowerScheme().HasValue)
            {
                Guid? currentScheme = powerManager.GetCurrentPowerScheme();
                if (currentScheme.HasValue)
                {
                    powerManager.SetOriginalPowerScheme(currentScheme);
                    appConfig.OriginalPowerSchemeGuid = currentScheme.Value.ToString();
                    appConfig.Save();
                    LogToStatusWindow($"CONFIG: Read and stored original power scheme: {currentScheme.Value}");
                    StatusLogWindow.WriteDirectToSessionLog($"CONFIG: Read and stored original power scheme: {currentScheme.Value}");
                }
                else
                {
                    LogToStatusWindow("CONFIG: Could not read current power scheme (may require Administrator)");
                    StatusLogWindow.WriteDirectToSessionLog("CONFIG: WARNING - Could not read current power scheme (may require Administrator)");
                }
            }

            // Initialize peripheral wake manager
            peripheralWakeManager = new PeripheralWakeManager();

            // Initialize display manager
            displayManager = new DisplayManager();

            // Initialize battery health manager
            batteryHealthManager = new BatteryHealthManager();
            string? manufacturer = batteryHealthManager.DetectManufacturer();
            if (!string.IsNullOrEmpty(manufacturer))
            {
                LogToStatusWindow($"BATTERY: Detected manufacturer: {manufacturer}");
                bool wmiSupported = batteryHealthManager.IsWmiSupportAvailable();
                if (wmiSupported)
                {
                    LogToStatusWindow("BATTERY: WMI support available for battery charge threshold control");
                    StatusLogWindow.WriteDirectToSessionLog($"BATTERY: WMI support available for {manufacturer}");
                }
                else
                {
                    LogToStatusWindow("BATTERY: WMI support not available - manual configuration required");
                    StatusLogWindow.WriteDirectToSessionLog($"BATTERY: WMI support not available for {manufacturer} - manual configuration required");
                }
            }

            // Create the tray icon
            trayIcon = new NotifyIcon();
            UpdateTrayIcon(false); // Initial state
            trayIcon.Visible = true;

            // Create the context menu with modern styling
            trayMenu = new ContextMenuStrip();
            trayMenu.BackColor = Color.FromArgb(45, 45, 45);
            trayMenu.ForeColor = Color.White;
            trayMenu.Renderer = new ModernMenuRenderer();
            
            // Dashboard
            ToolStripMenuItem dashboardItem = new ToolStripMenuItem("Dashboard", SystemIcons.Application.ToBitmap());
            dashboardItem.Click += OnShowStatusLog;
            trayMenu.Items.Add(dashboardItem);
            
            // Refresh Status
            ToolStripMenuItem refreshItem = new ToolStripMenuItem("Refresh Status", SystemIcons.Asterisk.ToBitmap());
            refreshItem.Click += OnRefreshStatus;
            trayMenu.Items.Add(refreshItem);
            
            trayMenu.Items.Add(new ToolStripSeparator());
            
            // Battery Info submenu
            ToolStripMenuItem batteryMenu = new ToolStripMenuItem("Battery Info", SystemIcons.Shield.ToBitmap());
            batteryMenu.DropDownOpening += BatteryMenu_DropDownOpening;
            trayMenu.Items.Add(batteryMenu);
            
            // Battery Health Guard toggle
            ToolStripMenuItem batteryHealthGuardItem = new ToolStripMenuItem("Battery Health Guard (80% Limit)", SystemIcons.Shield.ToBitmap());
            batteryHealthGuardItem.CheckOnClick = true;
            batteryHealthGuardItem.Checked = appConfig.BatteryHealthGuardEnabled;
            batteryHealthGuardItem.Click += BatteryHealthGuardItem_Click;
            trayMenu.Items.Add(batteryHealthGuardItem);
            
            trayMenu.Items.Add(new ToolStripSeparator());
            
            // Calibration Wizard
            ToolStripMenuItem wizardItem = new ToolStripMenuItem("Calibration Wizard...", SystemIcons.Question.ToBitmap());
            wizardItem.Click += OnCalibrationWizard;
            trayMenu.Items.Add(wizardItem);
            
            trayMenu.Items.Add(new ToolStripSeparator());
            
            // Quick Eject (clearly separated)
            ToolStripMenuItem ejectItem = new ToolStripMenuItem("Quick Eject", SystemIcons.Warning.ToBitmap());
            ejectItem.Click += OnQuickEject;
            trayMenu.Items.Add(ejectItem);
            
            trayMenu.Items.Add(new ToolStripSeparator());
            
            // Exit
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", SystemIcons.Error.ToBitmap());
            exitItem.Click += OnExit;
            trayMenu.Items.Add(exitItem);

            // Assign the menu to the tray icon
            trayIcon.ContextMenuStrip = trayMenu;

            // Handle double-click to show status log
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            // Initialize detection timer (checks every 2 seconds)
            detectionTimer = new System.Windows.Forms.Timer();
            detectionTimer.Interval = 2000; // 2 seconds
            detectionTimer.Tick += DetectionTimer_Tick;
            detectionTimer.Start();

            // Initialize heartbeat timer (30 seconds)
            heartbeatTimer = new System.Windows.Forms.Timer();
            heartbeatTimer.Interval = 30000; // 30 seconds
            heartbeatTimer.Tick += HeartbeatTimer_Tick;
            heartbeatTimer.Start();

            // Initialize performance heartbeat timer (5 minutes)
            performanceHeartbeatTimer = new System.Windows.Forms.Timer();
            performanceHeartbeatTimer.Interval = 300000; // 5 minutes = 300,000 ms
            performanceHeartbeatTimer.Tick += PerformanceHeartbeatTimer_Tick;
            performanceHeartbeatTimer.Start();

            // Initialize battery polling timer (45 seconds - lightweight polling)
            batteryTimer = new System.Windows.Forms.Timer();
            batteryTimer.Interval = 45000; // 45 seconds
            batteryTimer.Tick += BatteryTimer_Tick;
            batteryTimer.Start();
            
            // Initial battery status update
            UpdateBatteryStatus();

            // Set power request to prevent sleep and keep app running
            // This helps bypass Bonjour/LSA security hangs
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);

            // Initial status check and log entry
            string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            LogToStatusWindow($"LAD App started at {startTime}");
            LogToStatusWindow("Detection system initialized");
            StatusLogWindow.WriteDirectToSessionLog($"LAD App started at {startTime}");
            StatusLogWindow.WriteDirectToSessionLog("Detection system initialized");
            
            // Initial LAD Ready check (before setting last state)
            bool isOnAC = systemMonitor.IsOnACPower();
            int monitorCount = systemMonitor.GetExternalMonitorCount();
            bool initialLADReady = isOnAC && monitorCount > 0;
            lastLADReadyState = initialLADReady;
            
            // Set initial tray icon state
            UpdateTrayIcon(initialLADReady);
            
            // Log initial LAD Ready status and set power policy
            if (initialLADReady)
            {
                LogToStatusWindow("System State: LAD Ready - Safe to Close Lid");
                ApplyLidClosePolicy(initialLADReady);
            }
            else
            {
                var missingItems = new List<string>();
                if (!isOnAC) missingItems.Add("AC Power");
                if (monitorCount == 0) missingItems.Add("External Monitor");
                string missingText = string.Join(" or ", missingItems);
                LogToStatusWindow($"System State: LAD Not Ready - {missingText} Required");
                ApplyLidClosePolicy(initialLADReady);
            }
            
            RefreshStatus();
            
            // Initial heartbeat
            LogToStatusWindow("Heartbeat: Power Request Sent");

            // Initialize performance tracking
            lastPerformanceCheck = DateTime.UtcNow;
            lastTotalProcessorTime = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;

            // Register hotkey when form handle is created (HandleCreated event)
            this.HandleCreated += MainForm_HandleCreated;

            // Show calibration wizard on first run using a timer (more reliable for hidden forms)
            if (appConfig.FirstRun)
            {
                wizardTimer = new System.Windows.Forms.Timer();
                wizardTimer.Interval = 1000; // 1 second delay to ensure everything is initialized
                wizardTimer.Tick += WizardTimer_Tick;
                wizardTimer.Start();
            }
        }

        private void WizardTimer_Tick(object? sender, EventArgs e)
        {
            // Stop the timer - only run once
            wizardTimer?.Stop();
            wizardTimer?.Dispose();
            wizardTimer = null!;

            // Show calibration wizard on first run
            if (appConfig.FirstRun)
            {
                try
                {
                    // Show wizard without making MainForm visible (use null as owner)
                    CalibrationWizard wizard = new CalibrationWizard(peripheralWakeManager, appConfig);
                    DialogResult result = wizard.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        // Configuration saved by wizard
                        LogToStatusWindow("CONFIG: Calibration wizard completed - configuration saved");
                        
                        // Reload config
                        appConfig = AppConfig.Load();
                        
                        // Enable wake for all devices (app does this automatically, but ensure it's done)
                        LogToStatusWindow("PERIPHERAL: Enabling wake for all keyboards and mice");
                        peripheralWakeManager.EnableWakeForKeyboardsAndMice(LogToStatusWindow);
                    }
                    else
                    {
                        LogToStatusWindow("CONFIG: Calibration wizard cancelled - using default settings");
                    }
                }
                catch (Exception ex)
                {
                    LogToStatusWindow($"CONFIG: Error showing calibration wizard - {ex.Message}");
                }
            }
        }

        private void MainForm_HandleCreated(object? sender, EventArgs e)
        {
            // Register Ctrl+Shift+Alt+D safety hotkey for display topology revert
            // This must be done after the form handle is created
            // Using Ctrl+Shift+Alt+D to avoid conflicts with common hotkeys
            bool hotkeyRegistered = RegisterHotKey(
                this.Handle,
                HOTKEY_ID_SAFETY_REVERT,
                MOD_CONTROL | MOD_SHIFT | MOD_ALT,
                VK_D);

            if (hotkeyRegistered)
            {
                LogToStatusWindow("SAFETY: Ctrl+Shift+Alt+D hotkey registered for display topology revert");
            }
            else
            {
                int lastError = Marshal.GetLastWin32Error();
                LogToStatusWindow($"SAFETY: Failed to register Ctrl+Shift+Alt+D hotkey (Error: {lastError})");
                
                // Common error codes:
                // 1409 = ERROR_HOTKEY_ALREADY_REGISTERED (hotkey already in use by another app)
                // 5 = ERROR_ACCESS_DENIED (insufficient privileges)
                if (lastError == 1409)
                {
                    LogToStatusWindow("SAFETY: Hotkey already registered by another application");
                    LogToStatusWindow("SAFETY: Try closing other applications or use Quick Eject from tray menu");
                }
                else if (lastError == 5)
                {
                    LogToStatusWindow("SAFETY: Access denied - may need to run as Administrator");
                }
            }
        }

        private void DetectionTimer_Tick(object? sender, EventArgs e)
        {
            systemMonitor.CheckForChanges();
            RefreshStatus();
        }

        private void HeartbeatTimer_Tick(object? sender, EventArgs e)
        {
            // Send power request heartbeat to prevent Bonjour/LSA security hangs
            SetThreadExecutionState(ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
            LogToStatusWindow("Heartbeat: Power Request Sent");
        }

        // Performance tracking for CPU usage calculation
        private DateTime lastPerformanceCheck = DateTime.UtcNow;
        private TimeSpan lastTotalProcessorTime = TimeSpan.Zero;

        private void PerformanceHeartbeatTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Get current process
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

                // Calculate CPU usage percentage
                DateTime currentTime = DateTime.UtcNow;
                TimeSpan currentTotalProcessorTime = currentProcess.TotalProcessorTime;
                TimeSpan elapsedTime = currentTime - lastPerformanceCheck;
                
                double cpuUsagePercent = 0.0;
                if (elapsedTime.TotalSeconds > 0 && lastTotalProcessorTime != TimeSpan.Zero)
                {
                    TimeSpan cpuTimeUsed = currentTotalProcessorTime - lastTotalProcessorTime;
                    // CPU usage = (CPU time used / elapsed wall-clock time) * 100
                    // Divide by processor count to get percentage of total system CPU
                    cpuUsagePercent = (cpuTimeUsed.TotalMilliseconds / elapsedTime.TotalMilliseconds) * 100.0 / Environment.ProcessorCount;
                }
                
                // Update tracking variables for next calculation
                lastPerformanceCheck = currentTime;
                lastTotalProcessorTime = currentTotalProcessorTime;
                
                // Get memory usage (Working Set = physical RAM used)
                long memoryBytes = currentProcess.WorkingSet64;
                double memoryMB = memoryBytes / (1024.0 * 1024.0);

                // Log performance metrics
                // Target: < 1% CPU, < 50MB RAM
                string status = "OK";
                if (cpuUsagePercent >= 1.0 || memoryMB >= 50.0)
                {
                    status = "WARNING";
                }

                LogToStatusWindow($"PERFORMANCE [{status}]: CPU: {cpuUsagePercent:F2}%, RAM: {memoryMB:F2} MB (Target: <1% CPU, <50MB RAM)");
            }
            catch (Exception ex)
            {
                LogToStatusWindow($"PERFORMANCE: Error collecting metrics - {ex.Message}");
            }
        }

        private void BatteryTimer_Tick(object? sender, EventArgs e)
        {
            UpdateBatteryStatus();
        }

        private void UpdateBatteryStatus()
        {
            try
            {
                // Update battery information
                currentBatteryPercentage = systemMonitor.GetBatteryPercentage();
                currentBatteryStatus = systemMonitor.GetBatteryStatus();
                currentBatteryLifeRemaining = systemMonitor.GetFormattedBatteryLifeRemaining();
                
                // Update tray icon tooltip with new information
                RefreshStatus();
            }
            catch
            {
                // Silently handle errors - don't spam logs for battery reading failures
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Handle global hotkey messages
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_SAFETY_REVERT)
                {
                    // Ctrl+Shift+Alt+D pressed - safety revert display topology
                    SafetyRevertDisplay();
                    return; // Don't pass to base - we handled it
                }
            }

            // Handle power broadcast messages to detect sleep/wake
            if (m.Msg == WM_POWERBROADCAST)
            {
                int wParam = m.WParam.ToInt32();
                
                if (wParam == PBT_APMSUSPEND)
                {
                    // System is about to suspend
                    sleepStartTime = DateTime.Now;
                    string sleepTimestamp = sleepStartTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    LogToStatusWindow($"SLEEP EVENT: Entering sleep mode at {sleepTimestamp}");
                    StatusLogWindow.WriteDirectToSessionLog($"SLEEP EVENT: Entering sleep mode at {sleepTimestamp}");
                }
                else if (wParam == PBT_APMRESUMESUSPEND || wParam == PBT_APMRESUMEAUTOMATIC)
                {
                    // System is resuming from sleep
                    wakeDetectedTime = DateTime.Now;
                    string wakeTimestamp = wakeDetectedTime.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string resumeType = (wParam == PBT_APMRESUMESUSPEND) ? "Manual Resume" : "Automatic Resume";
                    
                    // Calculate sleep duration if we have sleep start time
                    string sleepDuration = "Unknown";
                    if (sleepStartTime.HasValue)
                    {
                        TimeSpan duration = wakeDetectedTime.Value - sleepStartTime.Value;
                        sleepDuration = $"{duration.TotalSeconds:F2} seconds ({duration.TotalMinutes:F2} minutes)";
                    }
                    
                    LogToStatusWindow($"WAKE EVENT: {resumeType} detected at {wakeTimestamp}");
                    LogToStatusWindow($"WAKE EVENT: Sleep duration was {sleepDuration}");
                    StatusLogWindow.WriteDirectToSessionLog($"WAKE EVENT: {resumeType} detected at {wakeTimestamp}");
                    StatusLogWindow.WriteDirectToSessionLog($"WAKE EVENT: Sleep duration was {sleepDuration}");
                    
                    // Re-apply wake settings after a short delay to ensure system is ready
                    System.Windows.Forms.Timer resumeTimer = new System.Windows.Forms.Timer();
                    resumeTimer.Interval = 2000; // 2 seconds delay
                    DateTime resumeStartTime = DateTime.Now;
                    resumeTimer.Tick += (s, e) =>
                    {
                        resumeTimer.Stop();
                        resumeTimer.Dispose();
                        OnSystemResume(resumeStartTime);
                    };
                    resumeTimer.Start();
                }
            }

            base.WndProc(ref m);
        }

        private void OnSystemResume(DateTime resumeStartTime)
        {
            try
            {
                DateTime resumeOperationStart = DateTime.Now;
                TimeSpan delayBeforeResume = resumeOperationStart - resumeStartTime;
                
                LogToStatusWindow($"RESUME OPERATION: Starting LAD settings re-application (delay: {delayBeforeResume.TotalMilliseconds:F0}ms)");
                StatusLogWindow.WriteDirectToSessionLog($"RESUME OPERATION: Starting LAD settings re-application (delay: {delayBeforeResume.TotalMilliseconds:F0}ms)");
                
                // Re-check LAD Ready state
                bool isOnAC = systemMonitor.IsOnACPower();
                int monitorCount = systemMonitor.GetExternalMonitorCount();
                bool isLADReady = isOnAC && monitorCount > 0;
                
                LogToStatusWindow($"RESUME OPERATION: LAD Ready check - AC: {isOnAC}, Monitors: {monitorCount}, LAD Ready: {isLADReady}");
                StatusLogWindow.WriteDirectToSessionLog($"RESUME OPERATION: LAD Ready check - AC: {isOnAC}, Monitors: {monitorCount}, LAD Ready: {isLADReady}");
                
                // Force re-application of settings
                lastLADReadyState = !isLADReady; // Force state change detection
                CheckLADReadyStatus();
                
                DateTime resumeOperationEnd = DateTime.Now;
                TimeSpan resumeOperationDuration = resumeOperationEnd - resumeOperationStart;
                TimeSpan totalTimeSinceWake = resumeOperationEnd - (wakeDetectedTime ?? resumeOperationStart);
                
                LogToStatusWindow($"RESUME OPERATION: Completed in {resumeOperationDuration.TotalMilliseconds:F0}ms");
                LogToStatusWindow($"RESUME OPERATION: Total time since wake: {totalTimeSinceWake.TotalMilliseconds:F0}ms ({totalTimeSinceWake.TotalSeconds:F2}s)");
                StatusLogWindow.WriteDirectToSessionLog($"RESUME OPERATION: Completed in {resumeOperationDuration.TotalMilliseconds:F0}ms");
                StatusLogWindow.WriteDirectToSessionLog($"RESUME OPERATION: Total time since wake: {totalTimeSinceWake.TotalMilliseconds:F0}ms ({totalTimeSinceWake.TotalSeconds:F2}s)");
                
                // Reset sleep tracking
                sleepStartTime = null;
                wakeDetectedTime = null;
            }
            catch (Exception ex)
            {
                string errorMsg = $"RESUME OPERATION: ERROR - {ex.Message}";
                LogToStatusWindow(errorMsg);
                StatusLogWindow.WriteDirectToSessionLog(errorMsg);
                StatusLogWindow.WriteDirectToSessionLog($"RESUME OPERATION: Stack trace: {ex.StackTrace}");
            }
        }

        private void SystemMonitor_PowerStatusChanged(object? sender, PowerStatusChangedEventArgs e)
        {
            string powerStatus = e.PowerStatus == PowerLineStatus.Online ? "AC Power" : "Battery Power";
            string message = e.PowerStatus == PowerLineStatus.Online 
                ? "AC Power Connected" 
                : "AC Power Disconnected - Running on Battery";
            
            LogToStatusWindow(message);
            RefreshStatus();
        }

        private void SystemMonitor_MonitorCountChanged(object? sender, MonitorCountChangedEventArgs e)
        {
            if (e.NewCount > e.OldCount)
            {
                LogToStatusWindow($"External Monitor Connected ({e.NewCount} monitor(s) detected)");
            }
            else if (e.NewCount < e.OldCount)
            {
                LogToStatusWindow($"External Monitor Disconnected ({e.NewCount} monitor(s) remaining)");
                
                // If monitor was disconnected and we're no longer LAD Ready, restore display topology
                bool isOnAC = systemMonitor.IsOnACPower();
                bool isLADReady = isOnAC && e.NewCount > 0;
                
                if (!isLADReady)
                {
                    LogToStatusWindow("DISPLAY: Monitor disconnected - Restoring Extended mode");
                    displayManager?.RestoreExtendedMode(LogToStatusWindow);
                }
            }
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            try
            {
                bool isOnAC = systemMonitor.IsOnACPower();
                int monitorCount = systemMonitor.GetExternalMonitorCount();
                bool isLADReady = isOnAC && monitorCount > 0;
                
                // Build compact tooltip focused on core functionality
                // Format: LAD Ready | Power | Monitors | Battery
                System.Text.StringBuilder tooltip = new System.Text.StringBuilder();
                
                // LAD Ready status (most important - this is the core feature)
                tooltip.Append($"LAD: {(isLADReady ? "Ready" : "Not Ready")}");
                
                // Power state
                tooltip.Append($" | Power: {(isOnAC ? "AC" : "Battery")}");
                
                // Monitor status
                tooltip.Append($" | Monitors: {monitorCount}");
                
                // Battery information (useful for laptop-as-desktop usage)
                if (currentBatteryPercentage.HasValue)
                {
                    tooltip.Append($" | Battery: {currentBatteryPercentage.Value:F0}%");
                }
                else
                {
                    tooltip.Append($" | Battery: {currentBatteryStatus}");
                }
                
                // Ensure tooltip doesn't exceed 128 characters (Windows limit)
                string tooltipText = tooltip.ToString();
                if (tooltipText.Length > 127)
                {
                    // If too long, create an even more compact version
                    tooltip.Clear();
                    tooltip.Append($"LAD: {(isLADReady ? "Ready" : "Not")}");
                    tooltip.Append($" | {(isOnAC ? "AC" : "Bat")}");
                    tooltip.Append($" | M:{monitorCount}");
                    if (currentBatteryPercentage.HasValue)
                    {
                        tooltip.Append($" | {currentBatteryPercentage.Value:F0}%");
                    }
                    tooltipText = tooltip.ToString();
                    
                    // Final safety check - truncate if still too long
                    if (tooltipText.Length > 127)
                    {
                        tooltipText = tooltipText.Substring(0, 127);
                    }
                }
                
                // Update tray icon tooltip (with error handling for length)
                try
                {
                    trayIcon.Text = tooltipText;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Fallback to minimal tooltip if still too long
                    trayIcon.Text = $"LAD: {(isLADReady ? "Ready" : "Not")} | {(isOnAC ? "AC" : "Bat")} | M:{monitorCount}";
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash - use minimal tooltip as fallback
                try
                {
                    LogToStatusWindow($"STATUS: Error refreshing status - {ex.Message}");
                    trayIcon.Text = "LAD App - Error refreshing status";
                }
                catch
                {
                    // If even logging fails, silently continue
                }
            }

            // Check LAD Ready status (only logs on state change)
            try
            {
                CheckLADReadyStatus();
                
                // Update dashboard if visible
                if (statusLogWindow != null && !statusLogWindow.IsDisposed && statusLogWindow.Visible)
                {
                    UpdateDashboardStatus();
                }
            }
            catch
            {
                // Don't let CheckLADReadyStatus crash the app
            }
        }

        private void CheckLADReadyStatus()
        {
            bool isOnAC = systemMonitor.IsOnACPower();
            int monitorCount = systemMonitor.GetExternalMonitorCount();
            bool isLADReady = isOnAC && monitorCount > 0;

            // Only log and apply policy when state changes
            if (isLADReady && !lastLADReadyState)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                LogToStatusWindow($"System State: LAD Ready - Safe to Close Lid (detected at {timestamp})");
                StatusLogWindow.WriteDirectToSessionLog($"System State: LAD Ready - Safe to Close Lid (detected at {timestamp})");
                ApplyLidClosePolicy(true);
            }
            else if (!isLADReady && lastLADReadyState)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                LogToStatusWindow($"System State: LAD Not Ready - AC Power or External Monitor Required (detected at {timestamp})");
                StatusLogWindow.WriteDirectToSessionLog($"System State: LAD Not Ready - AC Power or External Monitor Required (detected at {timestamp})");
                ApplyLidClosePolicy(false);
            }

            lastLADReadyState = isLADReady;
            
            // Update tray icon based on LAD Ready state
            UpdateTrayIcon(isLADReady);
        }

        /// <summary>
        /// Updates the tray icon based on LAD Ready state.
        /// Creates a simple monochrome icon that changes color when LAD Ready.
        /// </summary>
        private void UpdateTrayIcon(bool isLADReady)
        {
            try
            {
                // Create a simple monochrome icon
                // Green when LAD Ready, Gray when not ready
                Color iconColor = isLADReady ? Color.FromArgb(76, 175, 80) : Color.Gray;
                
                using (Bitmap bitmap = new Bitmap(16, 16))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    
                    // Draw a simple laptop icon shape
                    using (SolidBrush brush = new SolidBrush(iconColor))
                    using (Pen pen = new Pen(iconColor, 2))
                    {
                        // Laptop base
                        g.FillRectangle(brush, 2, 8, 12, 6);
                        g.DrawRectangle(pen, 2, 8, 12, 6);
                        
                        // Laptop screen
                        g.FillRectangle(brush, 3, 2, 10, 7);
                        g.DrawRectangle(pen, 3, 2, 10, 7);
                        
                        // Screen indicator (dot when LAD Ready)
                        if (isLADReady)
                        {
                            g.FillEllipse(new SolidBrush(Color.White), 7, 5, 2, 2);
                        }
                    }
                    
                    // Convert to icon
                    IntPtr hIcon = bitmap.GetHicon();
                    Icon icon = Icon.FromHandle(hIcon);
                    
                    // Update tray icon
                    Icon? oldIcon = trayIcon.Icon;
                    trayIcon.Icon = icon;
                    
                    // Clean up old icon
                    if (oldIcon != null && oldIcon.Handle != IntPtr.Zero)
                    {
                        try
                        {
                            DestroyIcon(oldIcon.Handle);
                            oldIcon.Dispose();
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
            }
            catch
            {
                // Fallback to system icon if custom icon creation fails
                if (trayIcon.Icon == null || trayIcon.Icon.Handle == IntPtr.Zero)
                {
                    trayIcon.Icon = SystemIcons.Application;
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        /// <summary>
        /// Applies the appropriate Lid Close Action policy based on LAD Ready state.
        /// Sets to "Do Nothing" (0) when LAD Ready, "Sleep" (1) when not ready.
        /// Also enables peripheral wake and disables USB Selective Suspend when LAD Ready.
        /// </summary>
        private void ApplyLidClosePolicy(bool isLADReady)
        {
            DateTime policyStartTime = DateTime.Now;
            string timestamp = policyStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            try
            {
                bool success;
                if (isLADReady)
                {
                    LogToStatusWindow($"POLICY APPLY: Starting LAD Ready policy application at {timestamp}");
                    StatusLogWindow.WriteDirectToSessionLog($"POLICY APPLY: Starting LAD Ready policy application at {timestamp}");
                    
                    // Set Lid Close Action to "Do Nothing"
                    try
                    {
                        DateTime lidPolicyStart = DateTime.Now;
                        success = powerManager.SetLidCloseDoNothing();
                        TimeSpan lidPolicyDuration = DateTime.Now - lidPolicyStart;
                        
                        if (success)
                        {
                            LogToStatusWindow($"Power Policy: Lid Close Action set to 'Do Nothing' (took {lidPolicyDuration.TotalMilliseconds:F0}ms)");
                            StatusLogWindow.WriteDirectToSessionLog($"Power Policy: Lid Close Action set to 'Do Nothing' (took {lidPolicyDuration.TotalMilliseconds:F0}ms)");
                        }
                        else
                        {
                            LogToStatusWindow("Power Policy: Failed to set Lid Close Action to 'Do Nothing' (may require Administrator)");
                            StatusLogWindow.WriteDirectToSessionLog("Power Policy: FAILED to set Lid Close Action to 'Do Nothing' (may require Administrator)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"Power Policy: Exception setting Lid Close Action - {ex.Message}");
                        StatusLogWindow.WriteDirectToSessionLog($"Power Policy: EXCEPTION setting Lid Close Action - {ex.Message}");
                    }

                    // Disable Hibernate Timeout (set to Never)
                    try
                    {
                        DateTime hibernateTimeoutStart = DateTime.Now;
                        success = powerManager.SetHibernateTimeoutNever();
                        TimeSpan hibernateTimeoutDuration = DateTime.Now - hibernateTimeoutStart;
                        
                        if (success)
                        {
                            uint? originalValue = powerManager.GetOriginalHibernateTimeout();
                            string originalValueStr = originalValue.HasValue ? $"{originalValue.Value} seconds" : "Unknown";
                            LogToStatusWindow($"Power Policy: Hibernate Timeout set to 'Never' (original: {originalValueStr}) (took {hibernateTimeoutDuration.TotalMilliseconds:F0}ms)");
                            StatusLogWindow.WriteDirectToSessionLog($"Power Policy: Hibernate Timeout set to 'Never' (original: {originalValueStr}) (took {hibernateTimeoutDuration.TotalMilliseconds:F0}ms)");
                            
                            // Update config with original value if we just read it
                            if (originalValue.HasValue && !appConfig.OriginalHibernateTimeout.HasValue)
                            {
                                appConfig.OriginalHibernateTimeout = originalValue;
                                appConfig.Save();
                            }
                        }
                        else
                        {
                            LogToStatusWindow("Power Policy: Failed to set Hibernate Timeout to 'Never' (may require Administrator or setting unavailable)");
                            StatusLogWindow.WriteDirectToSessionLog("Power Policy: FAILED to set Hibernate Timeout to 'Never' (may require Administrator or setting unavailable)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"Power Policy: Exception setting Hibernate Timeout - {ex.Message}");
                        StatusLogWindow.WriteDirectToSessionLog($"Power Policy: EXCEPTION setting Hibernate Timeout - {ex.Message}");
                    }

                    // Switch to High Performance power scheme
                    try
                    {
                        DateTime powerSchemeStart = DateTime.Now;
                        success = powerManager.SwitchToHighPerformance();
                        TimeSpan powerSchemeDuration = DateTime.Now - powerSchemeStart;
                        
                        if (success)
                        {
                            Guid? originalScheme = powerManager.GetOriginalPowerScheme();
                            string originalSchemeStr = originalScheme.HasValue ? originalScheme.Value.ToString() : "Unknown";
                            LogToStatusWindow($"POWER: Switched to High Performance Profile (original: {originalSchemeStr}) (took {powerSchemeDuration.TotalMilliseconds:F0}ms)");
                            StatusLogWindow.WriteDirectToSessionLog($"POWER: Switched to High Performance Profile (original: {originalSchemeStr}) (took {powerSchemeDuration.TotalMilliseconds:F0}ms)");
                            
                            // Update config with original scheme if we just read it
                            if (originalScheme.HasValue && string.IsNullOrEmpty(appConfig.OriginalPowerSchemeGuid))
                            {
                                appConfig.OriginalPowerSchemeGuid = originalScheme.Value.ToString();
                                appConfig.Save();
                            }
                        }
                        else
                        {
                            LogToStatusWindow("POWER: Failed to switch to High Performance Profile (may require Administrator)");
                            StatusLogWindow.WriteDirectToSessionLog("POWER: FAILED to switch to High Performance Profile (may require Administrator)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"POWER: Exception switching to High Performance Profile - {ex.Message}");
                        StatusLogWindow.WriteDirectToSessionLog($"POWER: EXCEPTION switching to High Performance Profile - {ex.Message}");
                    }

                    // Enable Battery Health Guard (80% limit) if enabled
                    if (appConfig.BatteryHealthGuardEnabled)
                    {
                        try
                        {
                            DateTime batteryGuardStart = DateTime.Now;
                            bool batteryGuardSuccess = false;
                            
                            if (batteryHealthManager.IsWmiSupportAvailable())
                            {
                                batteryGuardSuccess = batteryHealthManager.Enable80PercentLimit();
                                TimeSpan batteryGuardDuration = DateTime.Now - batteryGuardStart;
                                
                                if (batteryGuardSuccess)
                                {
                                    LogToStatusWindow($"BATTERY: 80% charge limit enabled (took {batteryGuardDuration.TotalMilliseconds:F0}ms)");
                                    StatusLogWindow.WriteDirectToSessionLog($"BATTERY: 80% charge limit enabled (took {batteryGuardDuration.TotalMilliseconds:F0}ms)");
                                }
                                else
                                {
                                    LogToStatusWindow("BATTERY: Failed to enable 80% charge limit (may require Administrator)");
                                    StatusLogWindow.WriteDirectToSessionLog("BATTERY: FAILED to enable 80% charge limit (may require Administrator)");
                                }
                            }
                            else
                            {
                                // WMI not available - log that manual configuration is needed
                                LogToStatusWindow("BATTERY: Battery Health Guard enabled but WMI not available - manual configuration required");
                                StatusLogWindow.WriteDirectToSessionLog("BATTERY: Battery Health Guard enabled but WMI not available - manual configuration required");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToStatusWindow($"BATTERY: Exception enabling 80% charge limit - {ex.Message}");
                            StatusLogWindow.WriteDirectToSessionLog($"BATTERY: EXCEPTION enabling 80% charge limit - {ex.Message}");
                        }
                    }

                    // Enable peripheral wake for keyboards and mice
                    try
                    {
                        DateTime wakeEnableStart = DateTime.Now;
                        LogToStatusWindow("PERIPHERAL: Enabling wake for keyboards and mice...");
                        StatusLogWindow.WriteDirectToSessionLog("PERIPHERAL: Enabling wake for keyboards and mice...");
                        var enabledDevices = peripheralWakeManager.EnableWakeForKeyboardsAndMice(LogToStatusWindow);
                        TimeSpan wakeEnableDuration = DateTime.Now - wakeEnableStart;
                        
                        if (enabledDevices.Count > 0)
                        {
                            LogToStatusWindow($"PERIPHERAL: Successfully enabled wake for {enabledDevices.Count} device(s) (took {wakeEnableDuration.TotalMilliseconds:F0}ms)");
                            StatusLogWindow.WriteDirectToSessionLog($"PERIPHERAL: Successfully enabled wake for {enabledDevices.Count} device(s) (took {wakeEnableDuration.TotalMilliseconds:F0}ms)");
                        }
                        else
                        {
                            LogToStatusWindow("PERIPHERAL: No devices were wake-enabled (may require Administrator or device may not support wake)");
                            StatusLogWindow.WriteDirectToSessionLog("PERIPHERAL: WARNING - No devices were wake-enabled (may require Administrator or device may not support wake)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"PERIPHERAL: Exception enabling wake for devices - {ex.Message}");
                        StatusLogWindow.WriteDirectToSessionLog($"PERIPHERAL: EXCEPTION enabling wake for devices - {ex.Message}");
                    }

                    // Disable USB Selective Suspend
                    try
                    {
                        DateTime usbSuspendStart = DateTime.Now;
                        success = peripheralWakeManager.DisableUsbSelectiveSuspend(LogToStatusWindow);
                        TimeSpan usbSuspendDuration = DateTime.Now - usbSuspendStart;
                        
                        if (!success)
                        {
                            LogToStatusWindow("PERIPHERAL: Failed to disable USB Selective Suspend (may require Administrator)");
                            StatusLogWindow.WriteDirectToSessionLog("PERIPHERAL: FAILED to disable USB Selective Suspend (may require Administrator)");
                        }
                        else
                        {
                            StatusLogWindow.WriteDirectToSessionLog($"PERIPHERAL: USB Selective Suspend disabled (took {usbSuspendDuration.TotalMilliseconds:F0}ms)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"PERIPHERAL: Exception disabling USB Selective Suspend - {ex.Message}");
                        StatusLogWindow.WriteDirectToSessionLog($"PERIPHERAL: EXCEPTION disabling USB Selective Suspend - {ex.Message}");
                    }

                    // Force External-Only display topology (kills internal panel signal)
                    try
                    {
                        DateTime displayStart = DateTime.Now;
                        success = displayManager.ForceExternalOnlyMode(LogToStatusWindow);
                        TimeSpan displayDuration = DateTime.Now - displayStart;
                        
                        if (!success)
                        {
                            LogToStatusWindow("DISPLAY: Failed to force External-Only mode (may require Administrator)");
                            StatusLogWindow.WriteDirectToSessionLog("DISPLAY: FAILED to force External-Only mode (may require Administrator)");
                        }
                        else
                        {
                            StatusLogWindow.WriteDirectToSessionLog($"DISPLAY: External-Only mode forced (took {displayDuration.TotalMilliseconds:F0}ms)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"DISPLAY: Exception forcing External-Only mode - {ex.Message}");
                        StatusLogWindow.WriteDirectToSessionLog($"DISPLAY: EXCEPTION forcing External-Only mode - {ex.Message}");
                    }
                    
                    // Log total policy application time
                    TimeSpan totalPolicyDuration = DateTime.Now - policyStartTime;
                    LogToStatusWindow($"POLICY APPLY: LAD Ready policy application completed in {totalPolicyDuration.TotalMilliseconds:F0}ms ({totalPolicyDuration.TotalSeconds:F2}s)");
                    StatusLogWindow.WriteDirectToSessionLog($"POLICY APPLY: LAD Ready policy application completed in {totalPolicyDuration.TotalMilliseconds:F0}ms ({totalPolicyDuration.TotalSeconds:F2}s)");
                }
                else
                {
                    // Revert Lid Close Action to "Sleep"
                    try
                    {
                        success = powerManager.SetLidCloseSleep();
                        if (success)
                        {
                            LogToStatusWindow("Power Policy: Lid Close Action reverted to 'Sleep'");
                        }
                        else
                        {
                            LogToStatusWindow("Power Policy: Failed to revert Lid Close Action to 'Sleep' (may require Administrator)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"Power Policy: Exception reverting Lid Close Action - {ex.Message}");
                    }

                    // Restore original Hibernate Timeout
                    try
                    {
                        DateTime hibernateTimeoutStart = DateTime.Now;
                        success = powerManager.RestoreHibernateTimeout();
                        TimeSpan hibernateTimeoutDuration = DateTime.Now - hibernateTimeoutStart;
                        
                        if (success)
                        {
                            uint? restoredValue = powerManager.GetOriginalHibernateTimeout();
                            string restoredValueStr = restoredValue.HasValue ? $"{restoredValue.Value} seconds" : "Unknown";
                            LogToStatusWindow($"Power Policy: Hibernate Timeout restored to '{restoredValueStr}' (took {hibernateTimeoutDuration.TotalMilliseconds:F0}ms)");
                            StatusLogWindow.WriteDirectToSessionLog($"Power Policy: Hibernate Timeout restored to '{restoredValueStr}' (took {hibernateTimeoutDuration.TotalMilliseconds:F0}ms)");
                        }
                        else
                        {
                            LogToStatusWindow("Power Policy: Failed to restore Hibernate Timeout (may require Administrator or setting unavailable)");
                            StatusLogWindow.WriteDirectToSessionLog("Power Policy: FAILED to restore Hibernate Timeout (may require Administrator or setting unavailable)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"Power Policy: Exception restoring Hibernate Timeout - {ex.Message}");
                        StatusLogWindow.WriteDirectToSessionLog($"Power Policy: EXCEPTION restoring Hibernate Timeout - {ex.Message}");
                    }

                    // Restore original power scheme
                    try
                    {
                        DateTime powerSchemeStart = DateTime.Now;
                        success = powerManager.RestoreOriginalPowerScheme();
                        TimeSpan powerSchemeDuration = DateTime.Now - powerSchemeStart;
                        
                        if (success)
                        {
                            Guid? restoredScheme = powerManager.GetOriginalPowerScheme();
                            string restoredSchemeStr = restoredScheme.HasValue ? restoredScheme.Value.ToString() : "Unknown";
                            LogToStatusWindow($"POWER: Restored original power scheme '{restoredSchemeStr}' (took {powerSchemeDuration.TotalMilliseconds:F0}ms)");
                            StatusLogWindow.WriteDirectToSessionLog($"POWER: Restored original power scheme '{restoredSchemeStr}' (took {powerSchemeDuration.TotalMilliseconds:F0}ms)");
                        }
                        else
                        {
                            LogToStatusWindow("POWER: Failed to restore original power scheme (may require Administrator)");
                            StatusLogWindow.WriteDirectToSessionLog("POWER: FAILED to restore original power scheme (may require Administrator)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"POWER: Exception restoring original power scheme - {ex.Message}");
                        StatusLogWindow.WriteDirectToSessionLog($"POWER: EXCEPTION restoring original power scheme - {ex.Message}");
                    }

                    // Disable Battery Health Guard (restore to 100%) if enabled
                    if (appConfig.BatteryHealthGuardEnabled)
                    {
                        try
                        {
                            DateTime batteryGuardStart = DateTime.Now;
                            bool batteryGuardSuccess = false;
                            
                            if (batteryHealthManager.IsWmiSupportAvailable())
                            {
                                batteryGuardSuccess = batteryHealthManager.DisableChargeLimit();
                                TimeSpan batteryGuardDuration = DateTime.Now - batteryGuardStart;
                                
                                if (batteryGuardSuccess)
                                {
                                    LogToStatusWindow($"BATTERY: Charge limit disabled - restored to 100% (took {batteryGuardDuration.TotalMilliseconds:F0}ms)");
                                    StatusLogWindow.WriteDirectToSessionLog($"BATTERY: Charge limit disabled - restored to 100% (took {batteryGuardDuration.TotalMilliseconds:F0}ms)");
                                }
                                else
                                {
                                    LogToStatusWindow("BATTERY: Failed to disable charge limit (may require Administrator)");
                                    StatusLogWindow.WriteDirectToSessionLog("BATTERY: FAILED to disable charge limit (may require Administrator)");
                                }
                            }
                            else
                            {
                                LogToStatusWindow("BATTERY: Battery Health Guard disabled - Please manually restore in manufacturer app");
                                StatusLogWindow.WriteDirectToSessionLog("BATTERY: Battery Health Guard disabled - Manual restoration required");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToStatusWindow($"BATTERY: Exception disabling charge limit - {ex.Message}");
                            StatusLogWindow.WriteDirectToSessionLog($"BATTERY: EXCEPTION disabling charge limit - {ex.Message}");
                        }
                    }

                    // Re-enable USB Selective Suspend (restore default)
                    try
                    {
                        success = peripheralWakeManager.EnableUsbSelectiveSuspend(LogToStatusWindow);
                        if (!success)
                        {
                            LogToStatusWindow("PERIPHERAL: Failed to re-enable USB Selective Suspend (may require Administrator)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"PERIPHERAL: Exception re-enabling USB Selective Suspend - {ex.Message}");
                    }

                    // Restore Extended display topology (restore internal screen)
                    try
                    {
                        success = displayManager.RestoreExtendedMode(LogToStatusWindow);
                        if (!success)
                        {
                            LogToStatusWindow("DISPLAY: Failed to restore Extended mode (may require Administrator)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToStatusWindow($"DISPLAY: Exception restoring Extended mode - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch any unexpected exceptions to prevent app crash
                LogToStatusWindow($"CRITICAL: Unexpected error in ApplyLidClosePolicy - {ex.Message}");
            }
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowStatusLog();
        }

        private void OnShowStatusLog(object? sender, EventArgs e)
        {
            ShowStatusLog();
        }

        private void OnRefreshStatus(object? sender, EventArgs e)
        {
            // Force a status refresh and log current state
            bool isOnAC = systemMonitor.IsOnACPower();
            int monitorCount = systemMonitor.GetExternalMonitorCount();
            
            string powerStatus = isOnAC ? "AC Power" : "Battery Power";
            LogToStatusWindow($"Manual Refresh: {powerStatus}, {monitorCount} external monitor(s)");
            
            // Add detailed screen info for debugging
            string screenInfo = systemMonitor.GetScreenInfo();
            foreach (string line in screenInfo.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                LogToStatusWindow($"  {line}");
            }
            
            RefreshStatus();
        }

        private void ShowStatusLog()
        {
            if (statusLogWindow == null || statusLogWindow.IsDisposed)
            {
                statusLogWindow = new StatusLogWindow();
                
                // Replay all buffered messages when window is first created
                foreach (string message in logBuffer)
                {
                    statusLogWindow.AddLogEntry(message);
                }
            }

            // Update dashboard status cards
            UpdateDashboardStatus();

            if (!statusLogWindow.Visible)
            {
                statusLogWindow.Show();
            }
            else
            {
                statusLogWindow.BringToFront();
            }
        }

        private void UpdateDashboardStatus()
        {
            if (statusLogWindow == null || statusLogWindow.IsDisposed)
                return;

            try
            {
                // System Status
                bool isOnAC = systemMonitor.IsOnACPower();
                int monitorCount = systemMonitor.GetExternalMonitorCount();
                bool isLADReady = isOnAC && monitorCount > 0;
                
                string systemStatus = $"LAD Ready: {(isLADReady ? "✓ Yes" : "✗ No")}\n";
                systemStatus += $"Power: {(isOnAC ? "AC" : "Battery")}\n";
                systemStatus += $"Monitors: {monitorCount}\n";
                systemStatus += $"Power Profile: High Performance\n";
                systemStatus += $"Hibernate: Disabled";
                
                statusLogWindow.SetSystemStatus(systemStatus);
                statusLogWindow.SetLADReadyState(isLADReady);

                // Health Status
                string healthStatus = "";
                if (currentBatteryPercentage.HasValue)
                {
                    healthStatus += $"Battery: {currentBatteryPercentage.Value:F0}%\n";
                }
                else
                {
                    healthStatus += $"Battery: {currentBatteryStatus}\n";
                }
                healthStatus += $"Status: {currentBatteryStatus}\n";
                healthStatus += $"Health Guard: {(appConfig.BatteryHealthGuardEnabled ? "Enabled (80%)" : "Disabled")}\n";
                
                string? manufacturer = batteryHealthManager.GetDetectedManufacturer();
                if (!string.IsNullOrEmpty(manufacturer))
                {
                    healthStatus += $"Manufacturer: {manufacturer}";
                }
                
                statusLogWindow.SetHealthStatus(healthStatus);

                // Peripherals Status
                string peripheralsStatus = "Wake Devices: Enabled\n";
                peripheralsStatus += "USB Selective Suspend: Disabled\n";
                peripheralsStatus += "Display: External-Only\n";
                peripheralsStatus += "Wake from Sleep: ✓ Ready";
                
                statusLogWindow.SetPeripheralsStatus(peripheralsStatus);
            }
            catch
            {
                // Silently handle errors
            }
        }

        private void LogToStatusWindow(string message)
        {
            // Always buffer the message
            logBuffer.Add(message);
            
            // If window exists and is not disposed, also add it to the window
            if (statusLogWindow != null && !statusLogWindow.IsDisposed)
            {
                statusLogWindow.AddLogEntry(message);
            }
        }

        /// <summary>
        /// Safety revert for display topology - restores Extended mode immediately.
        /// Called by Ctrl+Shift+Alt+D hotkey or Quick Eject to recover from black screen scenarios.
        /// </summary>
        private void SafetyRevertDisplay()
        {
            DateTime safetyRevertTime = DateTime.Now;
            string timestamp = safetyRevertTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            LogToStatusWindow($"SAFETY REVERT: Ctrl+Shift+Alt+D pressed at {timestamp} - Restoring display topology");
            StatusLogWindow.WriteDirectToSessionLog($"SAFETY REVERT: Ctrl+Shift+Alt+D pressed at {timestamp} - Restoring display topology");
            
            // Log if this happened after a wake event
            if (wakeDetectedTime.HasValue)
            {
                TimeSpan timeSinceWake = safetyRevertTime - wakeDetectedTime.Value;
                LogToStatusWindow($"SAFETY REVERT: Triggered {timeSinceWake.TotalSeconds:F2} seconds after wake event");
                StatusLogWindow.WriteDirectToSessionLog($"SAFETY REVERT: Triggered {timeSinceWake.TotalSeconds:F2} seconds after wake event");
            }
            
            // Immediately restore Extended display topology (restore internal screen)
            bool success = displayManager?.RestoreExtendedMode(LogToStatusWindow) ?? false;
            if (success)
            {
                LogToStatusWindow("SAFETY REVERT: Display topology restored successfully");
                StatusLogWindow.WriteDirectToSessionLog("SAFETY REVERT: Display topology restored successfully");
            }
            else
            {
                LogToStatusWindow("SAFETY REVERT: Failed to restore display topology (may require Administrator)");
                StatusLogWindow.WriteDirectToSessionLog("SAFETY REVERT: FAILED to restore display topology (may require Administrator)");
            }

            // Show a brief notification to confirm the action
            trayIcon?.ShowBalloonTip(
                2000, // 2 seconds
                "LAD App - Safety Revert",
                "Display topology restored to Extended mode",
                ToolTipIcon.Info);
        }

        private void BatteryMenu_DropDownOpening(object? sender, EventArgs e)
        {
            ToolStripMenuItem? menu = sender as ToolStripMenuItem;
            if (menu == null) return;
            
            // Clear existing items
            menu.DropDownItems.Clear();
            
            // Update battery status before showing menu
            UpdateBatteryStatus();
            
            // Battery percentage
            if (currentBatteryPercentage.HasValue)
            {
                menu.DropDownItems.Add($"Charge: {currentBatteryPercentage.Value:F0}%", null, null).Enabled = false;
            }
            else
            {
                menu.DropDownItems.Add("Charge: N/A", null, null).Enabled = false;
            }
            
            // Battery status
            menu.DropDownItems.Add($"Status: {currentBatteryStatus}", null, null).Enabled = false;
            
            // Power source
            bool isOnAC = systemMonitor.IsOnACPower();
            menu.DropDownItems.Add($"Power Source: {(isOnAC ? "AC Power" : "Battery")}", null, null).Enabled = false;
            
            // Remaining time (only if on battery)
            if (!isOnAC)
            {
                menu.DropDownItems.Add($"Time Remaining: {currentBatteryLifeRemaining}", null, null).Enabled = false;
            }
            
            // Battery health indicator
            System.Drawing.Color batteryColor = systemMonitor.GetBatteryColor();
            string healthStatus = GetBatteryHealthStatus();
            menu.DropDownItems.Add($"Health: {healthStatus}", null, null).Enabled = false;
            
            // Fan speeds (if available)
            string fanSpeeds = systemMonitor.GetFormattedFanSpeeds();
            menu.DropDownItems.Add($"Fan Speeds: {fanSpeeds}", null, null).Enabled = false;
        }

        private string GetBatteryHealthStatus()
        {
            if (!currentBatteryPercentage.HasValue)
                return "Unknown";
            
            float percentage = currentBatteryPercentage.Value;
            if (percentage >= 80)
                return "Excellent";
            else if (percentage >= 60)
                return "Good";
            else if (percentage >= 40)
                return "Fair";
            else if (percentage >= 20)
                return "Low";
            else
                return "Critical";
        }

        private void BatteryHealthGuardItem_Click(object? sender, EventArgs e)
        {
            ToolStripMenuItem? item = sender as ToolStripMenuItem;
            if (item == null) return;

            bool newState = item.Checked;
            appConfig.BatteryHealthGuardEnabled = newState;
            appConfig.Save();

            if (newState)
            {
                // Check if WMI support is available
                if (batteryHealthManager.IsWmiSupportAvailable())
                {
                    LogToStatusWindow("BATTERY: Battery Health Guard enabled - WMI support available");
                    StatusLogWindow.WriteDirectToSessionLog("BATTERY: Battery Health Guard enabled - WMI support available");
                    
                    // If LAD Ready, apply the limit immediately
                    bool isOnAC = systemMonitor.IsOnACPower();
                    int monitorCount = systemMonitor.GetExternalMonitorCount();
                    bool isLADReady = isOnAC && monitorCount > 0;
                    
                    if (isLADReady)
                    {
                        DateTime startTime = DateTime.Now;
                        bool success = batteryHealthManager.Enable80PercentLimit();
                        TimeSpan duration = DateTime.Now - startTime;
                        
                        if (success)
                        {
                            LogToStatusWindow($"BATTERY: 80% charge limit enabled (took {duration.TotalMilliseconds:F0}ms)");
                            StatusLogWindow.WriteDirectToSessionLog($"BATTERY: 80% charge limit enabled (took {duration.TotalMilliseconds:F0}ms)");
                        }
                        else
                        {
                            LogToStatusWindow("BATTERY: Failed to enable 80% charge limit (may require Administrator)");
                            StatusLogWindow.WriteDirectToSessionLog("BATTERY: FAILED to enable 80% charge limit (may require Administrator)");
                        }
                    }
                }
                else
                {
                    // Show instructions popup
                    string instructions = batteryHealthManager.GetManualInstructions();
                    MessageBox.Show(
                        instructions,
                        "Battery Health Guard - Manual Configuration Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    
                    LogToStatusWindow("BATTERY: Battery Health Guard enabled - Manual configuration required");
                    StatusLogWindow.WriteDirectToSessionLog("BATTERY: Battery Health Guard enabled - Manual configuration required (WMI not available)");
                }
            }
            else
            {
                // Disable charge limit
                if (batteryHealthManager.IsWmiSupportAvailable())
                {
                    DateTime startTime = DateTime.Now;
                    bool success = batteryHealthManager.DisableChargeLimit();
                    TimeSpan duration = DateTime.Now - startTime;
                    
                    if (success)
                    {
                        LogToStatusWindow($"BATTERY: Charge limit disabled - restored to 100% (took {duration.TotalMilliseconds:F0}ms)");
                        StatusLogWindow.WriteDirectToSessionLog($"BATTERY: Charge limit disabled - restored to 100% (took {duration.TotalMilliseconds:F0}ms)");
                    }
                    else
                    {
                        LogToStatusWindow("BATTERY: Failed to disable charge limit (may require Administrator)");
                        StatusLogWindow.WriteDirectToSessionLog("BATTERY: FAILED to disable charge limit (may require Administrator)");
                    }
                }
                else
                {
                    LogToStatusWindow("BATTERY: Battery Health Guard disabled - Please manually restore in manufacturer app");
                    StatusLogWindow.WriteDirectToSessionLog("BATTERY: Battery Health Guard disabled - Manual restoration required");
                }
            }
        }

        private void OnCalibrationWizard(object? sender, EventArgs e)
        {
            try
            {
                CalibrationWizard wizard = new CalibrationWizard(peripheralWakeManager, appConfig);
                DialogResult result = wizard.ShowDialog();

                if (result == DialogResult.OK)
                {
                    // Configuration saved by wizard
                    LogToStatusWindow("CONFIG: Calibration wizard completed - configuration saved");
                    
                    // Reload config to get selected devices
                    appConfig = AppConfig.Load();
                    
                    // If devices were selected, enable wake for them
                    if (!string.IsNullOrEmpty(appConfig.SelectedKeyboardInstancePath) ||
                        !string.IsNullOrEmpty(appConfig.SelectedMouseInstancePath))
                    {
                        LogToStatusWindow("PERIPHERAL: Enabling wake for selected devices from calibration");
                        peripheralWakeManager.EnableWakeForKeyboardsAndMice(LogToStatusWindow);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToStatusWindow($"CONFIG: Error showing calibration wizard - {ex.Message}");
                MessageBox.Show($"Error opening calibration wizard: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnQuickEject(object? sender, EventArgs e)
        {
            // Manually revert to Sleep mode before physical unplugging
            LogToStatusWindow("Quick Eject: Reverting Lid Close Action to 'Sleep'");
            bool success = powerManager.SetLidCloseSleep();
            if (success)
            {
                LogToStatusWindow("Quick Eject: Successfully reverted to 'Sleep' mode");
            }
            else
            {
                LogToStatusWindow("Quick Eject: Failed to revert (may require Administrator)");
            }

            // Restore original Hibernate Timeout
            LogToStatusWindow("Quick Eject: Restoring Hibernate Timeout");
            success = powerManager.RestoreHibernateTimeout();
            if (success)
            {
                uint? restoredValue = powerManager.GetOriginalHibernateTimeout();
                string restoredValueStr = restoredValue.HasValue ? $"{restoredValue.Value} seconds" : "Unknown";
                LogToStatusWindow($"Quick Eject: Hibernate Timeout restored to '{restoredValueStr}'");
                StatusLogWindow.WriteDirectToSessionLog($"Quick Eject: Hibernate Timeout restored to '{restoredValueStr}'");
            }
            else
            {
                LogToStatusWindow("Quick Eject: Failed to restore Hibernate Timeout (may require Administrator)");
                StatusLogWindow.WriteDirectToSessionLog("Quick Eject: FAILED to restore Hibernate Timeout (may require Administrator)");
            }

            // Restore original power scheme
            LogToStatusWindow("Quick Eject: Restoring original power scheme");
            success = powerManager.RestoreOriginalPowerScheme();
            if (success)
            {
                Guid? restoredScheme = powerManager.GetOriginalPowerScheme();
                string restoredSchemeStr = restoredScheme.HasValue ? restoredScheme.Value.ToString() : "Unknown";
                LogToStatusWindow($"Quick Eject: Power scheme restored to '{restoredSchemeStr}'");
                StatusLogWindow.WriteDirectToSessionLog($"Quick Eject: Power scheme restored to '{restoredSchemeStr}'");
            }
            else
            {
                LogToStatusWindow("Quick Eject: Failed to restore power scheme (may require Administrator)");
                StatusLogWindow.WriteDirectToSessionLog("Quick Eject: FAILED to restore power scheme (may require Administrator)");
            }

            // Restore Extended display topology (restore internal screen)
            LogToStatusWindow("Quick Eject: Restoring Extended display mode");
            success = displayManager.RestoreExtendedMode(LogToStatusWindow);
            if (!success)
            {
                LogToStatusWindow("Quick Eject: Failed to restore display mode (may require Administrator)");
            }

            // Re-enable USB Selective Suspend
            LogToStatusWindow("Quick Eject: Re-enabling USB Selective Suspend");
            peripheralWakeManager?.EnableUsbSelectiveSuspend(LogToStatusWindow);
        }

        private void OnExit(object? sender, EventArgs e)
        {
            // Revert power settings before exiting
            LogToStatusWindow("App Exit: Reverting Lid Close Action to 'Sleep'");
            powerManager?.SetLidCloseSleep();
            
            // Restore original Hibernate Timeout
            LogToStatusWindow("App Exit: Restoring Hibernate Timeout");
            bool success = powerManager?.RestoreHibernateTimeout() ?? false;
            if (success)
            {
                uint? restoredValue = powerManager?.GetOriginalHibernateTimeout();
                string restoredValueStr = restoredValue.HasValue ? $"{restoredValue.Value} seconds" : "Unknown";
                LogToStatusWindow($"App Exit: Hibernate Timeout restored to '{restoredValueStr}'");
                StatusLogWindow.WriteDirectToSessionLog($"App Exit: Hibernate Timeout restored to '{restoredValueStr}'");
            }
            else
            {
                LogToStatusWindow("App Exit: Failed to restore Hibernate Timeout (may require Administrator)");
                StatusLogWindow.WriteDirectToSessionLog("App Exit: FAILED to restore Hibernate Timeout (may require Administrator)");
            }

            // Restore original power scheme
            LogToStatusWindow("App Exit: Restoring original power scheme");
            success = powerManager?.RestoreOriginalPowerScheme() ?? false;
            if (success)
            {
                Guid? restoredScheme = powerManager?.GetOriginalPowerScheme();
                string restoredSchemeStr = restoredScheme.HasValue ? restoredScheme.Value.ToString() : "Unknown";
                LogToStatusWindow($"App Exit: Power scheme restored to '{restoredSchemeStr}'");
                StatusLogWindow.WriteDirectToSessionLog($"App Exit: Power scheme restored to '{restoredSchemeStr}'");
            }
            else
            {
                LogToStatusWindow("App Exit: Failed to restore power scheme (may require Administrator)");
                StatusLogWindow.WriteDirectToSessionLog("App Exit: FAILED to restore power scheme (may require Administrator)");
            }
            
            // Re-enable USB Selective Suspend (restore default)
            LogToStatusWindow("App Exit: Re-enabling USB Selective Suspend");
            peripheralWakeManager?.EnableUsbSelectiveSuspend(LogToStatusWindow);
            
            Application.Exit();
        }

        /// <summary>
        /// Emergency cleanup method called by crash handler.
        /// Reverts all system settings to safe defaults.
        /// This method must be exception-safe (handle all exceptions internally).
        /// </summary>
        public void EmergencyRevertAllSettings()
        {
            // This method is called during crash handling, so we must be extremely defensive
            // Each operation is wrapped in try-catch to ensure we attempt all reverts

            // 1. Revert Lid Close Action to Sleep
            try
            {
                powerManager?.SetLidCloseSleep();
            }
            catch { /* Ignore - continue with other reverts */ }

            // 2. Restore Hibernate Timeout
            try
            {
                powerManager?.RestoreHibernateTimeout();
            }
            catch { /* Ignore - continue with other reverts */ }

            // 3. Restore original power scheme
            try
            {
                powerManager?.RestoreOriginalPowerScheme();
            }
            catch { /* Ignore - continue with other reverts */ }

            // 4. Restore Display Topology to Extended mode
            try
            {
                displayManager?.RestoreExtendedMode(null); // No logging callback during crash
            }
            catch { /* Ignore - continue with other reverts */ }

            // 5. Re-enable USB Selective Suspend
            try
            {
                peripheralWakeManager?.EnableUsbSelectiveSuspend(null); // No logging callback during crash
            }
            catch { /* Ignore - continue with other reverts */ }

            // 6. Unregister hotkey (if handle is still valid)
            try
            {
                if (this.IsHandleCreated)
                {
                    UnregisterHotKey(this.Handle, HOTKEY_ID_SAFETY_REVERT);
                }
            }
            catch { /* Ignore - continue with other reverts */ }

            // 7. Clear power request to allow normal sleep behavior
            try
            {
                SetThreadExecutionState(ES_CONTINUOUS);
            }
            catch { /* Ignore - continue with other reverts */ }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unregister hotkey
            try
            {
                if (this.IsHandleCreated)
                {
                    UnregisterHotKey(this.Handle, HOTKEY_ID_SAFETY_REVERT);
                }
            }
            catch { /* Ignore */ }

            // Clear power request to allow normal sleep behavior
            SetThreadExecutionState(ES_CONTINUOUS);

            // Revert power settings before closing
            LogToStatusWindow("App Closing: Reverting Lid Close Action to 'Sleep'");
            powerManager?.SetLidCloseSleep();

            // Restore original Hibernate Timeout
            LogToStatusWindow("App Closing: Restoring Hibernate Timeout");
            bool success = powerManager?.RestoreHibernateTimeout() ?? false;
            if (success)
            {
                uint? restoredValue = powerManager?.GetOriginalHibernateTimeout();
                string restoredValueStr = restoredValue.HasValue ? $"{restoredValue.Value} seconds" : "Unknown";
                LogToStatusWindow($"App Closing: Hibernate Timeout restored to '{restoredValueStr}'");
                StatusLogWindow.WriteDirectToSessionLog($"App Closing: Hibernate Timeout restored to '{restoredValueStr}'");
            }
            else
            {
                LogToStatusWindow("App Closing: Failed to restore Hibernate Timeout (may require Administrator)");
                StatusLogWindow.WriteDirectToSessionLog("App Closing: FAILED to restore Hibernate Timeout (may require Administrator)");
            }

            // Restore original power scheme
            LogToStatusWindow("App Closing: Restoring original power scheme");
            success = powerManager?.RestoreOriginalPowerScheme() ?? false;
            if (success)
            {
                Guid? restoredScheme = powerManager?.GetOriginalPowerScheme();
                string restoredSchemeStr = restoredScheme.HasValue ? restoredScheme.Value.ToString() : "Unknown";
                LogToStatusWindow($"App Closing: Power scheme restored to '{restoredSchemeStr}'");
                StatusLogWindow.WriteDirectToSessionLog($"App Closing: Power scheme restored to '{restoredSchemeStr}'");
            }
            else
            {
                LogToStatusWindow("App Closing: Failed to restore power scheme (may require Administrator)");
                StatusLogWindow.WriteDirectToSessionLog("App Closing: FAILED to restore power scheme (may require Administrator)");
            }

            // Re-enable USB Selective Suspend (restore default)
            LogToStatusWindow("App Closing: Re-enabling USB Selective Suspend");
            peripheralWakeManager?.EnableUsbSelectiveSuspend(LogToStatusWindow);

            // Clean up timers
            detectionTimer?.Stop();
            detectionTimer?.Dispose();
            heartbeatTimer?.Stop();
            heartbeatTimer?.Dispose();
            performanceHeartbeatTimer?.Stop();
            performanceHeartbeatTimer?.Dispose();
            batteryTimer?.Stop();
            batteryTimer?.Dispose();
            wizardTimer?.Stop();
            wizardTimer?.Dispose();

            // Save configuration before closing
            try
            {
                appConfig?.Save();
            }
            catch { /* Ignore */ }

            // Clean up the tray icon when the form closes
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            // Close status log window
            statusLogWindow?.Close();

            base.OnFormClosing(e);
        }
    }
}
