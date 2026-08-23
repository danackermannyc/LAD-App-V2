using System;
using System.IO;
using System.Windows.Forms;

namespace LADApp
{
    internal static class Program
    {
        // Static reference to MainForm for crash handler access
        private static MainForm? mainFormInstance;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Set up global exception handlers BEFORE anything else
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                // Configure DPI awareness for high-DPI displays
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                mainFormInstance = new MainForm();
                Application.Run(mainFormInstance);
            }
            catch (Exception ex)
            {
                // Show error dialog if app fails to start
                HandleCrash("Startup Error", ex);
            }
        }

        /// <summary>
        /// Handles unhandled exceptions from the UI thread.
        /// </summary>
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleCrash("Unhandled Thread Exception", e.Exception);
        }

        /// <summary>
        /// Handles unhandled exceptions from any thread (non-UI).
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                HandleCrash("Unhandled Domain Exception", ex);
            }
            else
            {
                HandleCrash("Unhandled Domain Exception", new Exception($"Unknown exception: {e.ExceptionObject}"));
            }
        }

        /// <summary>
        /// Final BIOS Failsafe: Direct revert of critical settings without MainForm dependency.
        /// This ensures Lid Policy (Sleep) and Display (Extended) are restored even if
        /// MainForm hasn't been created or is in an invalid state.
        /// </summary>
        private static void FinalBiosFailsafe()
        {
            try
            {
                // Revert Lid Close Action to Sleep (critical safety setting)
                try
                {
                    PowerManager powerManager = new PowerManager();
                    powerManager.SetLidCloseSleep();
                }
                catch { /* Ignore - continue with other reverts */ }

                // Restore Display Topology to Extended mode (critical safety setting)
                try
                {
                    DisplayManager displayManager = new DisplayManager();
                    displayManager.RestoreExtendedMode(null); // No logging during crash
                }
                catch { /* Ignore - continue with other reverts */ }

                // Disarm any devices we armed for wake. Read straight from config so this
                // works even when MainForm never existed, and so an undocked machine is
                // not left wakeable by a peripheral.
                try
                {
                    AppConfig config = AppConfig.Load();
                    if (config.ArmedWakeDeviceIds.Count > 0)
                    {
                        PeripheralWakeManager wakeManager = new PeripheralWakeManager();
                        wakeManager.DisableWakeForDevices(config.ArmedWakeDeviceIds, null);
                        config.ArmedWakeDeviceIds.Clear();
                        config.Save();
                    }
                }
                catch { /* Ignore - continue with other reverts */ }
            }
            catch
            {
                // If Final BIOS Failsafe itself fails, we've done our best
            }
        }

        /// <summary>
        /// Centralized crash handler that performs emergency cleanup and logging.
        /// </summary>
        private static void HandleCrash(string crashType, Exception exception)
        {
            try
            {
                // Step 1: Final BIOS Failsafe - Direct revert of critical settings
                // This runs FIRST and doesn't depend on MainForm instance
                try
                {
                    FinalBiosFailsafe();
                }
                catch (Exception failsafeEx)
                {
                    // If failsafe fails, log it but continue
                    LogCrash($"Final BIOS Failsafe failed: {failsafeEx.Message}", crashType, exception);
                }

                // Step 2: Attempt full emergency cleanup via MainForm (if available)
                try
                {
                    if (mainFormInstance != null)
                    {
                        mainFormInstance.EmergencyRevertAllSettings();
                    }
                }
                catch (Exception cleanupEx)
                {
                    // If cleanup fails, log it but continue
                    LogCrash($"Emergency cleanup failed: {cleanupEx.Message}", crashType, exception);
                }

                // Step 2: Write crash log to file
                LogCrash($"Crash Type: {crashType}", crashType, exception);

                // Step 3: Show user alert (non-blocking, but visible)
                try
                {
                    MessageBox.Show(
                        "LAD App has encountered an error. System defaults have been restored for safety.\n\n" +
                        $"A crash log has been saved to:\n{Path.Combine(AppConfig.AppDataDirectory, "crash_log.txt")}",
                        "LAD App - Critical Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch
                {
                    // If message box fails, continue anyway
                }
            }
            catch
            {
                // If everything fails, at least try to write a minimal log
                try
                {
                    string minimalLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CRITICAL: Crash handler itself failed!\n" +
                                       $"Crash Type: {crashType}\n" +
                                       $"Exception: {exception?.Message ?? "Unknown"}\n";
                    File.AppendAllText(Path.Combine(AppConfig.AppDataDirectory, "crash_log.txt"), minimalLog);
                }
                catch
                {
                    // Absolute last resort - do nothing
                }
            }
        }

        /// <summary>
        /// Writes crash information to crash_log.txt file.
        /// </summary>
        private static void LogCrash(string additionalInfo, string crashType, Exception exception)
        {
            try
            {
                // Write beside config.json in LocalAppData. The install directory is
                // read-only under Program Files and MSIX, which would silently discard
                // the log exactly when it matters.
                Directory.CreateDirectory(AppConfig.AppDataDirectory);
                string logPath = Path.Combine(AppConfig.AppDataDirectory, "crash_log.txt");
                string separator = new string('=', 80);
                string logEntry = $"\n{separator}\n" +
                                 $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] CRASH DETECTED\n" +
                                 $"{separator}\n" +
                                 $"Crash Type: {crashType}\n" +
                                 $"Additional Info: {additionalInfo}\n\n" +
                                 $"Exception Type: {exception?.GetType().FullName ?? "Unknown"}\n" +
                                 $"Exception Message: {exception?.Message ?? "No message"}\n\n" +
                                 $"Stack Trace:\n{exception?.StackTrace ?? "No stack trace"}\n\n";

                if (exception?.InnerException != null)
                {
                    logEntry += $"Inner Exception:\n" +
                               $"  Type: {exception.InnerException.GetType().FullName}\n" +
                               $"  Message: {exception.InnerException.Message}\n" +
                               $"  Stack Trace: {exception.InnerException.StackTrace}\n\n";
                }

                logEntry += $"{separator}\n\n";

                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // If logging fails, silently continue
            }
        }
    }
}
