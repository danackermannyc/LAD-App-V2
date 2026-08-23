using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace LADApp
{
    /// <summary>
    /// First Run Calibration Wizard for LAD App.
    /// Guides users through device selection and wake testing.
    /// </summary>
    public partial class CalibrationWizard : Form
    {
        private PeripheralWakeManager peripheralWakeManager;
        private AppConfig appConfig;
        
        // UI Controls
        private Panel contentPanel = null!;
        private Button nextButton = null!;
        private Button backButton = null!;
        private Button cancelButton = null!;
        private Label titleLabel = null!;
        private Label descriptionLabel = null!;
        
        // Step 1: Welcome/Admin Check
        private Label adminStatusLabel = null!;
        
        // Step 2: Wake Test
        private Label wakeTestLabel = null!;
        private Button testWakeButton = null!;
        private Label wakeTestStatusLabel = null!;
        
        // Step 4: Completion
        private Label completionLabel = null!;
        
        private int currentStep = 1;
        private const int TotalSteps = 3; // Simplified: Welcome, Wake Test, Completion
        
        private bool wakeTestCompleted = false;

        public CalibrationWizard(PeripheralWakeManager peripheralWakeManager, AppConfig appConfig)
        {
            this.peripheralWakeManager = peripheralWakeManager;
            this.appConfig = appConfig;
            InitializeComponent();
            DpiHelper.ConfigureForm(this);
            LoadStep(1);
        }

        private void InitializeComponent()
        {
            this.Text = "LAD App - First Run Calibration Wizard";
            this.Size = DpiHelper.Scale(new Size(700, 500));
            this.FormBorderStyle = FormBorderStyle.Sizable; // Changed from FixedDialog to allow DPI scaling
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = DpiHelper.Scale(new Size(600, 450));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;

            // Use TableLayoutPanel for wizard layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = DpiHelper.Scale(new Padding(20)),
                BackColor = Color.Transparent
            };

            // Configure rows: Header (auto), Content (fill), Buttons (auto)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiHelper.Scale(80)));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiHelper.Scale(50)));

            // Header panel for title and description
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            titleLabel = new Label
            {
                Text = "Welcome to LAD App",
                Font = DpiHelper.CreateFont("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            descriptionLabel = new Label
            {
                Text = "This wizard will help you configure LAD App for your setup.",
                Font = DpiHelper.CreateFont("Segoe UI", 10),
                AutoSize = true,
                Location = DpiHelper.Scale(new Point(0, 40))
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(descriptionLabel);

            // Content panel (where step-specific content goes)
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            // Button panel using TableLayoutPanel for responsive button layout
            TableLayoutPanel buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };

            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = DpiHelper.Scale(new Size(100, 30)),
                Anchor = AnchorStyles.Left,
                Font = DpiHelper.CreateFont("Segoe UI", 9)
            };
            cancelButton.Click += CancelButton_Click;

            backButton = new Button
            {
                Text = "< Back",
                Size = DpiHelper.Scale(new Size(100, 30)),
                Anchor = AnchorStyles.Right,
                Enabled = false,
                Font = DpiHelper.CreateFont("Segoe UI", 9)
            };
            backButton.Click += BackButton_Click;

            nextButton = new Button
            {
                Text = "Next >",
                Size = DpiHelper.Scale(new Size(100, 30)),
                Anchor = AnchorStyles.Right,
                Font = DpiHelper.CreateFont("Segoe UI", 9)
            };
            nextButton.Click += NextButton_Click;

            buttonPanel.Controls.Add(cancelButton, 0, 0);
            buttonPanel.Controls.Add(backButton, 1, 0);
            buttonPanel.Controls.Add(nextButton, 2, 0);

            // Add panels to main layout
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(contentPanel, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private void LoadStep(int step)
        {
            currentStep = step;
            contentPanel.Controls.Clear();

            // Update button states
            backButton.Enabled = step > 1;
            nextButton.Text = step == TotalSteps ? "Finish" : "Next >";
            nextButton.Enabled = true;

            switch (step)
            {
                case 1:
                    LoadWelcomeStep();
                    break;
                case 2:
                    LoadWakeTestStep();
                    break;
                case 3:
                    LoadCompletionStep();
                    break;
            }
        }

        private void LoadWelcomeStep()
        {
            titleLabel.Text = "Welcome to LAD App";
            descriptionLabel.Text = "This wizard will help you configure LAD App for your setup.";

            // Use FlowLayoutPanel for vertical stacking
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = DpiHelper.Scale(new Padding(20))
            };

            // Check admin status
            bool isAdmin = IsRunningAsAdministrator();

            adminStatusLabel = new Label
            {
                Text = isAdmin
                    ? "✓ Running with Administrator privileges"
                    : "⚠ Not running as Administrator - some features may not work",
                Font = DpiHelper.CreateFont("Segoe UI", 10),
                ForeColor = isAdmin ? Color.Green : Color.Orange,
                AutoSize = true,
                MaximumSize = new Size(contentPanel.Width - DpiHelper.Scale(80), 0),
                Margin = DpiHelper.Scale(new Padding(0, 0, 0, 20))
            };

            Label infoLabel = new Label
            {
                Text = "LAD App requires Administrator privileges to:\n" +
                       "• Modify power settings (Lid Close Action)\n" +
                       "• Enable wake from USB/Bluetooth devices\n" +
                       "• Control display topology\n\n" +
                       "If you're not running as Administrator, please restart the app with elevated privileges.",
                Font = DpiHelper.CreateFont("Segoe UI", 9),
                AutoSize = true,
                MaximumSize = new Size(contentPanel.Width - DpiHelper.Scale(80), 0)
            };

            flowPanel.Controls.Add(adminStatusLabel);
            flowPanel.Controls.Add(infoLabel);

            contentPanel.Controls.Add(flowPanel);
        }


        private void LoadWakeTestStep()
        {
            titleLabel.Text = "Test Wake Functionality";
            descriptionLabel.Text = "Verify that your keyboard and mouse can wake the system from sleep.";

            // Use FlowLayoutPanel for vertical stacking with proper spacing
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = DpiHelper.Scale(new Padding(20))
            };

            wakeTestLabel = new Label
            {
                Text = "This test will:\n" +
                       "1. Enable wake for all connected keyboards and mice\n" +
                       "2. Put the system to sleep\n" +
                       "3. You can wake it using any keyboard or mouse\n\n" +
                       "Important: Make sure your laptop lid is open or you have an external monitor connected\n" +
                       "before starting the test, as you'll need to see the screen to verify wake worked.\n\n" +
                       "Note: LAD App automatically enables wake for all HID devices. You don't need to\n" +
                       "select specific devices - it works with any keyboard or mouse you connect.",
                Font = DpiHelper.CreateFont("Segoe UI", 9),
                AutoSize = true,
                MaximumSize = new Size(contentPanel.Width - DpiHelper.Scale(80), 0),
                Margin = DpiHelper.Scale(new Padding(0, 0, 0, 20))
            };

            testWakeButton = new Button
            {
                Text = "Start Wake Test",
                Size = DpiHelper.Scale(new Size(200, 40)),
                Margin = DpiHelper.Scale(new Padding(0, 10, 0, 10)),
                Font = DpiHelper.CreateFont("Segoe UI", 10, FontStyle.Bold)
            };
            testWakeButton.Click += TestWakeButton_Click;

            wakeTestStatusLabel = new Label
            {
                Text = "",
                Font = DpiHelper.CreateFont("Segoe UI", 9),
                AutoSize = true,
                MaximumSize = new Size(contentPanel.Width - DpiHelper.Scale(80), 0),
                ForeColor = Color.Blue,
                Margin = DpiHelper.Scale(new Padding(0, 10, 0, 0))
            };

            flowPanel.Controls.Add(wakeTestLabel);
            flowPanel.Controls.Add(testWakeButton);
            flowPanel.Controls.Add(wakeTestStatusLabel);

            contentPanel.Controls.Add(flowPanel);

            // If wake test already completed, show status
            if (wakeTestCompleted)
            {
                wakeTestStatusLabel.Text = "✓ Wake test completed successfully!";
                wakeTestStatusLabel.ForeColor = Color.Green;
                testWakeButton.Enabled = false;
            }
        }

        private void LoadCompletionStep()
        {
            titleLabel.Text = "Calibration Complete!";
            descriptionLabel.Text = "Your LAD App is now configured and ready to use.";

            // Use FlowLayoutPanel for vertical stacking
            FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = DpiHelper.Scale(new Padding(20))
            };

            completionLabel = new Label
            {
                Text = "Configuration Summary:\n\n" +
                       $"Wake Test: {(wakeTestCompleted ? "Passed" : "Skipped")}\n\n" +
                       "Your settings have been saved. LAD App will now:\n" +
                       "• Automatically enable wake for all connected keyboards and mice\n" +
                       "• Manage lid close behavior when docked\n" +
                       "• Control display topology for optimal setup\n" +
                       "• Disable USB Selective Suspend for reliable wake\n\n" +
                       "Note: LAD App enables wake for all HID devices automatically.\n" +
                       "If you need to configure specific devices, use Windows Device Manager.\n\n" +
                       "You can access the Status Log from the system tray icon.",
                Font = DpiHelper.CreateFont("Segoe UI", 9),
                AutoSize = true,
                MaximumSize = new Size(contentPanel.Width - DpiHelper.Scale(80), 0)
            };

            flowPanel.Controls.Add(completionLabel);
            contentPanel.Controls.Add(flowPanel);
        }




        private void TestWakeButton_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "This will put your system to sleep. Make sure:\n\n" +
                "• Your laptop lid is open OR an external monitor is connected\n" +
                "• You're ready to wake the system using your keyboard or mouse\n" +
                "• You have Administrator privileges\n\n" +
                "Do you want to continue?",
                "Wake Test Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Enable wake for selected devices
                    // Note: We'll use the existing method which enables all devices
                    // In a future enhancement, we could enable only selected devices
                    var armed = peripheralWakeManager.EnableWakeForKeyboardsAndMice(null);

                    // Record what we armed so the app can disarm it later. Without this,
                    // devices armed by the wake test would stay wake-enabled forever.
                    foreach (string identifier in armed.DeviceIdentifiers)
                    {
                        if (!appConfig.ArmedWakeDeviceIds.Contains(identifier))
                        {
                            appConfig.ArmedWakeDeviceIds.Add(identifier);
                        }
                    }
                    appConfig.Save();

                    wakeTestStatusLabel.Text = "Enabling wake for devices...";
                    wakeTestStatusLabel.ForeColor = Color.Blue;
                    Application.DoEvents();

                    // Put system to sleep
                    wakeTestStatusLabel.Text = "Putting system to sleep in 3 seconds...\n" +
                                              "Wake the system using your keyboard or mouse.";
                    wakeTestStatusLabel.ForeColor = Color.Orange;
                    Application.DoEvents();

                    System.Threading.Thread.Sleep(3000);

                    // Put the system to sleep WITH wake events left enabled.
                    //
                    // The third argument is disableWakeEvent. It used to be passed as
                    // true, which asks Windows to disable all wake events for this
                    // suspend - so the wake test could never be woken by a keyboard or
                    // mouse, no matter how correctly the devices were armed. It must be
                    // false for the test to mean anything.
                    //
                    // (The second argument, force, has no effect on Vista and later.)
                    Application.SetSuspendState(PowerState.Suspend, true, false);

                    // SetSuspendState returns once the system resumes, but it cannot tell
                    // us WHY it resumed - a power button press looks identical to a
                    // keyboard wake from here. This used to claim outright that the
                    // peripherals had woken the machine, which made a failed test report
                    // itself as passed. Ask the user instead.
                    DialogResult wokeByPeripheral = MessageBox.Show(
                        "The system has resumed.\n\n" +
                        "Did you wake it using your external keyboard or mouse?\n\n" +
                        "Choose No if you had to use the power button or open the lid.",
                        "Wake Test Result",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (wokeByPeripheral == DialogResult.Yes)
                    {
                        wakeTestCompleted = true;
                        wakeTestStatusLabel.Text = "✓ Wake test passed.\n" +
                                                  "The system was woken using your keyboard or mouse.";
                        wakeTestStatusLabel.ForeColor = Color.Green;
                        testWakeButton.Enabled = false;
                    }
                    else
                    {
                        wakeTestCompleted = false;
                        wakeTestStatusLabel.Text = "✗ Wake test failed - the peripherals did not wake the system.\n" +
                                                  "You can retry the test.";
                        wakeTestStatusLabel.ForeColor = Color.Red;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during wake test: {ex.Message}",
                        "Wake Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    wakeTestStatusLabel.Text = "✗ Wake test failed. Please check Administrator privileges.";
                    wakeTestStatusLabel.ForeColor = Color.Red;
                }
            }
        }


        private void NextButton_Click(object? sender, EventArgs e)
        {
            if (currentStep < TotalSteps)
            {
                LoadStep(currentStep + 1);
            }
            else
            {
                // Finish - save configuration
                SaveConfiguration();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void BackButton_Click(object? sender, EventArgs e)
        {
            if (currentStep > 1)
            {
                LoadStep(currentStep - 1);
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to cancel the calibration wizard?\n\n" +
                "LAD App will continue to work, but wake functionality may not be optimized for your devices.",
                "Cancel Calibration",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void SaveConfiguration()
        {
            // No device selection needed - app enables wake for all devices automatically
            appConfig.FirstRun = false;
            appConfig.Save();
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
