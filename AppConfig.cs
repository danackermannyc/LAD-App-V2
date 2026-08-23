using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LADApp
{
    /// <summary>
    /// Manages application configuration stored in a JSON file.
    /// Used for First Run flag and other persistent settings.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Per-user writable directory for all app state (config, session log, crash log).
        /// Never use the install directory: under Program Files or MSIX it is read-only,
        /// which silently discards exactly the diagnostics we need after a failure.
        /// </summary>
        public static string AppDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LADApp");

        private static readonly string ConfigFilePath = Path.Combine(AppDataDirectory, "config.json");

        [JsonPropertyName("firstRun")]
        public bool FirstRun { get; set; } = true;

        [JsonPropertyName("lastVersion")]
        public string? LastVersion { get; set; }

        [JsonPropertyName("selectedKeyboardInstancePath")]
        public string? SelectedKeyboardInstancePath { get; set; }

        [JsonPropertyName("selectedMouseInstancePath")]
        public string? SelectedMouseInstancePath { get; set; }

        [JsonPropertyName("originalHibernateTimeout")]
        public uint? OriginalHibernateTimeout { get; set; }

        [JsonPropertyName("originalPowerSchemeGuid")]
        public string? OriginalPowerSchemeGuid { get; set; }

        [JsonPropertyName("laptopOrientation")]
        public string? LaptopOrientation { get; set; }

        [JsonPropertyName("batteryHealthGuardEnabled")]
        public bool BatteryHealthGuardEnabled { get; set; } = false;

        /// <summary>
        /// True while LAD policy is applied to the system.
        /// Written to disk BEFORE the first system change and cleared only after a
        /// completed revert. If this is still true at startup, the previous session
        /// died without reverting (kill, power loss, BSOD) and the machine is sitting
        /// in a modified state - see MainForm's startup recovery.
        /// </summary>
        [JsonPropertyName("policyActive")]
        public bool PolicyActive { get; set; } = false;

        /// <summary>
        /// Identifiers of the devices this app armed for wake, so revert can disarm
        /// exactly those and nothing else. Each entry is whichever identifier
        /// DevicePowerSetDeviceState actually accepted (instance path, device
        /// description, or product name).
        /// </summary>
        [JsonPropertyName("armedWakeDeviceIds")]
        public List<string> ArmedWakeDeviceIds { get; set; } = new List<string>();

        /// <summary>
        /// Loads configuration from file, or returns default configuration if file doesn't exist.
        /// </summary>
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    AppConfig? config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch (Exception)
            {
                // If loading fails, return default config
            }

            // Return default configuration
            return new AppConfig();
        }

        /// <summary>
        /// Saves configuration to file.
        /// Writes to a temporary file and swaps it into place so that a crash or power
        /// loss mid-write cannot leave a truncated config behind - PolicyActive is a
        /// safety flag, so a corrupt config would defeat crash recovery.
        /// </summary>
        /// <returns>True if the configuration was written to disk.</returns>
        public bool Save()
        {
            try
            {
                if (!Directory.Exists(AppDataDirectory))
                {
                    Directory.CreateDirectory(AppDataDirectory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(this, options);

                string tempPath = ConfigFilePath + ".tmp";
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true); // force to disk, not just the OS cache
                }

                File.Move(tempPath, ConfigFilePath, overwrite: true);
                return true;
            }
            catch (Exception)
            {
                // Never let a config write failure take down the app; callers that care
                // about durability check the return value.
                return false;
            }
        }

        /// <summary>
        /// Gets the configuration file path (for debugging/logging purposes).
        /// </summary>
        public static string GetConfigFilePath()
        {
            return ConfigFilePath;
        }
    }
}
