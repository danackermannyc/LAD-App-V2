using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LADApp
{
    public class SystemMonitor
    {
        // Windows API for monitor detection
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;
        
        // State flags for DISPLAY_DEVICE
        private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        private const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;

        // Power status tracking
        private PowerLineStatus lastPowerStatus;
        private int lastExternalMonitorCount;

        public event EventHandler<PowerStatusChangedEventArgs>? PowerStatusChanged;
        public event EventHandler<MonitorCountChangedEventArgs>? MonitorCountChanged;

        public SystemMonitor()
        {
            // Initialize with current state
            lastPowerStatus = SystemInformation.PowerStatus.PowerLineStatus;
            lastExternalMonitorCount = GetExternalMonitorCount();
        }

        public PowerLineStatus GetCurrentPowerStatus()
        {
            return SystemInformation.PowerStatus.PowerLineStatus;
        }

        public bool IsOnACPower()
        {
            return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
        }

        /// <summary>
        /// Gets the battery charge percentage (0-100).
        /// Returns null if battery is not present or status is unavailable.
        /// </summary>
        public float? GetBatteryPercentage()
        {
            try
            {
                var powerStatus = SystemInformation.PowerStatus;
                float batteryLifePercent = powerStatus.BatteryLifePercent;
                
                // BatteryLifePercent returns -1 if battery is not present or status unavailable
                if (batteryLifePercent >= 0 && batteryLifePercent <= 1.0f)
                {
                    return batteryLifePercent * 100.0f; // Convert from 0-1 to 0-100
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the battery charge status (e.g., "Charging", "Discharging", "Not Present").
        /// </summary>
        public string GetBatteryStatus()
        {
            try
            {
                var powerStatus = SystemInformation.PowerStatus;
                BatteryChargeStatus chargeStatus = powerStatus.BatteryChargeStatus;
                
                if (chargeStatus.HasFlag(BatteryChargeStatus.NoSystemBattery))
                {
                    return "Not Present";
                }
                else if (chargeStatus.HasFlag(BatteryChargeStatus.Charging))
                {
                    return "Charging";
                }
                else if (chargeStatus.HasFlag(BatteryChargeStatus.High))
                {
                    return "High";
                }
                else if (chargeStatus.HasFlag(BatteryChargeStatus.Low))
                {
                    return "Low";
                }
                else if (chargeStatus.HasFlag(BatteryChargeStatus.Critical))
                {
                    return "Critical";
                }
                else
                {
                    return "Discharging";
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets the estimated battery life remaining in seconds.
        /// Returns null if not available or battery is charging.
        /// </summary>
        public int? GetBatteryLifeRemaining()
        {
            try
            {
                var powerStatus = SystemInformation.PowerStatus;
                int remaining = powerStatus.BatteryLifeRemaining;
                
                // BatteryLifeRemaining returns -1 if not available
                if (remaining >= 0)
                {
                    return remaining;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a formatted string for battery life remaining (e.g., "2h 30m" or "N/A").
        /// </summary>
        public string GetFormattedBatteryLifeRemaining()
        {
            int? remainingSeconds = GetBatteryLifeRemaining();
            if (remainingSeconds.HasValue)
            {
                int totalSeconds = remainingSeconds.Value;
                int hours = totalSeconds / 3600;
                int minutes = (totalSeconds % 3600) / 60;
                
                if (hours > 0)
                {
                    return $"{hours}h {minutes}m";
                }
                else
                {
                    return $"{minutes}m";
                }
            }
            return "N/A";
        }

        /// <summary>
        /// Gets a formatted battery status string with percentage and status.
        /// </summary>
        public string GetFormattedBatteryStatus()
        {
            float? percentage = GetBatteryPercentage();
            string status = GetBatteryStatus();
            
            if (percentage.HasValue)
            {
                return $"{percentage.Value:F0}% ({status})";
            }
            else
            {
                return status;
            }
        }

        /// <summary>
        /// Gets a color indicator for the battery status (for potential icon color changes).
        /// </summary>
        public System.Drawing.Color GetBatteryColor()
        {
            float? percentage = GetBatteryPercentage();
            string status = GetBatteryStatus();
            
            if (!percentage.HasValue || status == "Not Present")
            {
                return System.Drawing.Color.Gray; // Unknown/No battery
            }
            
            if (status == "Charging")
            {
                return System.Drawing.Color.Blue; // Charging
            }
            else if (status == "Critical")
            {
                return System.Drawing.Color.Red; // Critical
            }
            else if (percentage.Value < 20)
            {
                return System.Drawing.Color.OrangeRed; // Low
            }
            else if (percentage.Value < 50)
            {
                return System.Drawing.Color.Orange; // Medium
            }
            else
            {
                return System.Drawing.Color.Green; // Good
            }
        }

        private bool IsInternalMonitor(DISPLAY_DEVICE monitorDevice, DISPLAY_DEVICE adapterDevice)
        {
            // Be very conservative - only mark as internal if we're CERTAIN
            // External monitors are more common, so when in doubt, assume external
            string deviceID = monitorDevice.DeviceID.ToUpperInvariant();
            string deviceString = monitorDevice.DeviceString.ToUpperInvariant();
            string adapterString = adapterDevice.DeviceString.ToUpperInvariant();
            
            // Only mark as internal if we have STRONG indicators:
            // 1. Explicitly says "Built-in" or "Internal" in DeviceString
            // 2. DeviceID contains "MONITOR\DEFAULT" (Windows default/internal monitor)
            // 3. Very short DeviceID (< 15 chars) AND on integrated graphics AND DeviceString is generic
            
            bool hasExplicitInternalMarker = deviceString.Contains("BUILT-IN") || 
                                            deviceString.Contains("INTERNAL");
            
            bool hasDefaultMonitorID = deviceID.Contains("MONITOR\\DEFAULT");
            
            bool isGenericOnIntegrated = (deviceID.Length < 15) && 
                                        (adapterString.Contains("INTEL") && adapterString.Contains("HD")) &&
                                        (deviceString.Length < 30 || deviceString.Contains("GENERIC"));
            
            return hasExplicitInternalMarker || hasDefaultMonitorID || isGenericOnIntegrated;
        }

        public int GetExternalMonitorCount()
        {
            // Use Windows API to enumerate all physical display devices
            // Identify internal vs external by hardware characteristics
            int externalCount = 0;
            int totalActiveMonitors = 0;
            
            DISPLAY_DEVICE adapterDevice = new DISPLAY_DEVICE();
            adapterDevice.cb = Marshal.SizeOf(adapterDevice);
            uint adapterIndex = 0;
            
            while (EnumDisplayDevices(null, adapterIndex, ref adapterDevice, 0))
            {
                // Enumerate monitors for this adapter
                DISPLAY_DEVICE monitorDevice = new DISPLAY_DEVICE();
                monitorDevice.cb = Marshal.SizeOf(monitorDevice);
                uint monitorIndex = 0;
                
                while (EnumDisplayDevices(adapterDevice.DeviceName, monitorIndex, ref monitorDevice, 0))
                {
                    // Check if this monitor is attached to desktop (active)
                    if ((monitorDevice.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
                    {
                        totalActiveMonitors++;
                        
                        // Check if this is an external monitor
                        if (!IsInternalMonitor(monitorDevice, adapterDevice))
                        {
                            externalCount++;
                        }
                    }
                    
                    monitorIndex++;
                    monitorDevice.cb = Marshal.SizeOf(monitorDevice);
                }
                
                adapterIndex++;
                adapterDevice.cb = Marshal.SizeOf(adapterDevice);
            }
            
            // Fallback: If we couldn't identify any as internal and there's only 1 monitor,
            // and we're on a laptop (which always has an internal display), then this must be external
            if (totalActiveMonitors == 1 && externalCount == 0)
            {
                // Check if this is likely a laptop (has battery)
                try
                {
                    var powerStatus = SystemInformation.PowerStatus;
                    // If we can detect battery status, it's likely a laptop
                    // In this case, if only 1 monitor is active and it's not clearly internal, assume external
                    externalCount = 1;
                }
                catch
                {
                    // Can't determine, assume it might be external
                    externalCount = 1;
                }
            }
            
            return externalCount;
        }

        public string GetScreenInfo()
        {
            // Debug method to get detailed screen information
            System.Text.StringBuilder info = new System.Text.StringBuilder();
            
            // Show logical screens (Windows Forms view)
            info.AppendLine($"Logical Screens (Windows Forms): {Screen.AllScreens.Length}");
            Screen? primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                info.AppendLine($"Primary Logical Screen: {primaryScreen.DeviceName} ({primaryScreen.Bounds.Width}x{primaryScreen.Bounds.Height})");
            }
            
            // Show physical monitors (Windows API view)
            info.AppendLine("");
            info.AppendLine("Physical Monitors (Windows API):");
            
            DISPLAY_DEVICE adapterDevice = new DISPLAY_DEVICE();
            adapterDevice.cb = Marshal.SizeOf(adapterDevice);
            uint adapterIndex = 0;
            int physicalMonitorCount = 0;
            int externalCount = 0;
            
            while (EnumDisplayDevices(null, adapterIndex, ref adapterDevice, 0))
            {
                bool isPrimaryAdapter = (adapterDevice.StateFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0;
                info.AppendLine($"  Adapter {adapterIndex}: {adapterDevice.DeviceString} {(isPrimaryAdapter ? "(PRIMARY)" : "")}");
                
                // Enumerate monitors for this adapter
                DISPLAY_DEVICE monitorDevice = new DISPLAY_DEVICE();
                monitorDevice.cb = Marshal.SizeOf(monitorDevice);
                uint monitorIndex = 0;
                
                while (EnumDisplayDevices(adapterDevice.DeviceName, monitorIndex, ref monitorDevice, 0))
                {
                    if ((monitorDevice.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
                    {
                        physicalMonitorCount++;
                        bool isInternal = IsInternalMonitor(monitorDevice, adapterDevice);
                        if (!isInternal) externalCount++;
                        
                        info.AppendLine($"    Monitor {monitorIndex}: {monitorDevice.DeviceString}");
                        info.AppendLine($"      DeviceID: {monitorDevice.DeviceID}");
                        info.AppendLine($"      Type: {(isInternal ? "INTERNAL" : "EXTERNAL")}");
                    }
                    monitorIndex++;
                    monitorDevice.cb = Marshal.SizeOf(monitorDevice);
                }
                
                adapterIndex++;
                adapterDevice.cb = Marshal.SizeOf(adapterDevice);
            }
            
            // Apply the same fallback logic as GetExternalMonitorCount()
            if (physicalMonitorCount == 1 && externalCount == 0)
            {
                // If only 1 monitor is active and we classified it as internal,
                // but we're on a laptop (which always has an internal display),
                // then the internal must be off and this is external
                try
                {
                    var powerStatus = SystemInformation.PowerStatus;
                    externalCount = 1; // Assume external if we can't clearly identify as internal
                }
                catch
                {
                    externalCount = 1;
                }
            }
            
            info.AppendLine($"Total Active Monitors: {physicalMonitorCount}");
            info.AppendLine($"External Monitors Detected: {externalCount}");
            
            return info.ToString();
        }

        public void CheckForChanges()
        {
            // Check power status
            PowerLineStatus currentPowerStatus = GetCurrentPowerStatus();
            if (currentPowerStatus != lastPowerStatus)
            {
                lastPowerStatus = currentPowerStatus;
                PowerStatusChanged?.Invoke(this, new PowerStatusChangedEventArgs(currentPowerStatus));
            }

            // Check monitor count
            int currentMonitorCount = GetExternalMonitorCount();
            if (currentMonitorCount != lastExternalMonitorCount)
            {
                int oldCount = lastExternalMonitorCount;
                lastExternalMonitorCount = currentMonitorCount;
                MonitorCountChanged?.Invoke(this, new MonitorCountChangedEventArgs(oldCount, currentMonitorCount));
            }
        }

        /// <summary>
        /// Attempts to read fan speeds using WMI.
        /// Note: WMI fan data is often not available on many systems.
        /// Returns null if fan data is not available.
        /// </summary>
        /// <returns>Dictionary of fan names and their speeds in RPM, or null if unavailable</returns>
        public System.Collections.Generic.Dictionary<string, uint>? GetFanSpeeds()
        {
            try
            {
                using (System.Management.ManagementObjectSearcher searcher = 
                    new System.Management.ManagementObjectSearcher("SELECT Name, DesiredSpeed FROM Win32_Fan"))
                {
                    var fanSpeeds = new System.Collections.Generic.Dictionary<string, uint>();
                    bool foundAny = false;

                    foreach (System.Management.ManagementObject fan in searcher.Get())
                    {
                        string? name = fan["Name"]?.ToString();
                        object? speedObj = fan["DesiredSpeed"];
                        
                        if (!string.IsNullOrEmpty(name) && speedObj != null)
                        {
                            try
                            {
                                uint speed = Convert.ToUInt32(speedObj);
                                if (speed > 0) // Only include non-zero speeds
                                {
                                    fanSpeeds[name] = speed;
                                    foundAny = true;
                                }
                            }
                            catch
                            {
                                // Skip invalid speed values
                            }
                        }
                    }

                    return foundAny ? fanSpeeds : null;
                }
            }
            catch
            {
                // WMI fan data not available - this is common on many systems
                return null;
            }
        }

        /// <summary>
        /// Gets a formatted string of fan speeds for display.
        /// Returns "N/A" if fan data is not available.
        /// </summary>
        public string GetFormattedFanSpeeds()
        {
            var fanSpeeds = GetFanSpeeds();
            if (fanSpeeds == null || fanSpeeds.Count == 0)
            {
                return "N/A (WMI not available)";
            }

            System.Text.StringBuilder result = new System.Text.StringBuilder();
            bool first = true;
            foreach (var kvp in fanSpeeds)
            {
                if (!first) result.Append(", ");
                result.Append($"{kvp.Key}: {kvp.Value} RPM");
                first = false;
            }

            return result.ToString();
        }
    }

    public class PowerStatusChangedEventArgs : EventArgs
    {
        public PowerLineStatus PowerStatus { get; }

        public PowerStatusChangedEventArgs(PowerLineStatus powerStatus)
        {
            PowerStatus = powerStatus;
        }
    }

    public class MonitorCountChangedEventArgs : EventArgs
    {
        public int OldCount { get; }
        public int NewCount { get; }

        public MonitorCountChangedEventArgs(int oldCount, int newCount)
        {
            OldCount = oldCount;
            NewCount = newCount;
        }
    }
}
