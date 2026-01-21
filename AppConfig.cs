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
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LADApp",
            "config.json");

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
        /// </summary>
        public void Save()
        {
            try
            {
                // Ensure directory exists
                string? directory = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize and save
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail - configuration is not critical for app operation
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
