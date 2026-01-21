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
            LoadStep(1);
        }

        private void InitializeComponent()
        {
            this.Text = "LAD App - First Run Calibration Wizard";
            this.Size = new Size(700, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = true;

            // Title label
            titleLabel = new Label
            {
                Text = "Welcome to LAD App",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Description label
            descriptionLabel = new Label
            {
                Text = "This wizard will help you configure LAD App for your setup.",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(20, 60)
            };

            // Content panel (where step-specific content goes)
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 100, 20, 60)
            };

            // Buttons
            backButton = new Button
            {
                Text = "< Back",
                Size = new Size(100, 30),
                Location = new Point(420, 420),
                Enabled = false
            };
            backButton.Click += BackButton_Click;

            nextButton = new Button
            {
                Text = "Next >",
                Size = new Size(100, 30),
                Location = new Point(530, 420)
            };
            nextButton.Click += NextButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 30),
                Location = new Point(20, 420)
            };
            cancelButton.Click += CancelButton_Click;

            // Add controls
            this.Controls.Add(titleLabel);
            this.Controls.Add(descriptionLabel);
            this.Controls.Add(contentPanel);
            this.Controls.Add(backButton);
            this.Controls.Add(nextButton);
            this.Controls.Add(cancelButton);
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

            // Check admin status
            bool isAdmin = IsRunningAsAdministrator();
            
            adminStatusLabel = new Label
            {
                Text = isAdmin 
                    ? "✓ Running with Administrator privileges" 
                    : "⚠ Not running as Administrator - some features may not work",
                Font = new Font("Segoe UI", 10),
                ForeColor = isAdmin ? Color.Green : Color.Orange,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Label infoLabel = new Label
            {
                Text = "LAD App requires Administrator privileges to:\n" +
                       "• Modify power settings (Lid Close Action)\n" +
                       "• Enable wake from USB/Bluetooth devices\n" +
                       "• Control display topology\n\n" +
                       "If you're not running as Administrator, please restart the app with elevated privileges.",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(20, 60)
            };

            contentPanel.Controls.Add(adminStatusLabel);
            contentPanel.Controls.Add(infoLabel);
        }


        private void LoadWakeTestStep()
        {
            titleLabel.Text = "Test Wake Functionality";
            descriptionLabel.Text = "Verify that your keyboard and mouse can wake the system from sleep.";

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
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            testWakeButton = new Button
            {
                Text = "Start Wake Test",
                Size = new Size(200, 40),
                Location = new Point(20, 150),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            testWakeButton.Click += TestWakeButton_Click;

            wakeTestStatusLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(20, 210),
                ForeColor = Color.Blue
            };

            contentPanel.Controls.Add(wakeTestLabel);
            contentPanel.Controls.Add(testWakeButton);
            contentPanel.Controls.Add(wakeTestStatusLabel);

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
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            contentPanel.Controls.Add(completionLabel);
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
                    peripheralWakeManager.EnableWakeForKeyboardsAndMice(null);

                    wakeTestStatusLabel.Text = "Enabling wake for devices...";
                    wakeTestStatusLabel.ForeColor = Color.Blue;
                    Application.DoEvents();

                    // Put system to sleep
                    wakeTestStatusLabel.Text = "Putting system to sleep in 3 seconds...\n" +
                                              "Wake the system using your keyboard or mouse.";
                    wakeTestStatusLabel.ForeColor = Color.Orange;
                    Application.DoEvents();

                    System.Threading.Thread.Sleep(3000);

                    // Use SetSuspendState to put system to sleep
                    Application.SetSuspendState(PowerState.Suspend, true, true);

                    // If we get here, the system was woken
                    wakeTestCompleted = true;
                    wakeTestStatusLabel.Text = "✓ Wake test completed successfully!\n" +
                                              "The system was woken using your keyboard or mouse.";
                    wakeTestStatusLabel.ForeColor = Color.Green;
                    testWakeButton.Enabled = false;
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
