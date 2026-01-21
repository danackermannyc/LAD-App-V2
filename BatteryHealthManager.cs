using System;
using System.Management;
using System.Runtime.InteropServices;

namespace LADApp
{
    /// <summary>
    /// Manages battery health guard features, including 80% charge limiting.
    /// Supports manufacturer-specific WMI implementations for Lenovo, ASUS, Dell, and HP.
    /// </summary>
    public class BatteryHealthManager
    {
        private string? detectedManufacturer = null;
        private bool? wmiSupportAvailable = null;
        private string? wmiClassName = null;

        // Manufacturer names
        private const string MANUFACTURER_LENOVO = "LENOVO";
        private const string MANUFACTURER_ASUS = "ASUS";
        private const string MANUFACTURER_DELL = "DELL";
        private const string MANUFACTURER_HP = "HP";
        private const string MANUFACTURER_ACER = "ACER";
        private const string MANUFACTURER_MSI = "MSI";

        // WMI class names for battery charge control
        private const string WMI_LENOVO_BIOS_SETTING = "Lenovo_BiosSetting";
        private const string WMI_ASUS_ATK = "AsusAtkWmi_WMNB";
        private const string WMI_DELL_BIOS = "DellSmbiosBattery";
        private const string WMI_HP_BIOS = "HP_BIOSSetting";

        /// <summary>
        /// Detects the laptop manufacturer using WMI.
        /// </summary>
        /// <returns>Manufacturer name (e.g., "LENOVO", "ASUS") or null if detection fails</returns>
        public string? DetectManufacturer()
        {
            if (detectedManufacturer != null)
            {
                return detectedManufacturer;
            }

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Manufacturer FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string? manufacturer = obj["Manufacturer"]?.ToString();
                        if (!string.IsNullOrEmpty(manufacturer))
                        {
                            detectedManufacturer = manufacturer.ToUpperInvariant().Trim();
                            return detectedManufacturer;
                        }
                    }
                }
            }
            catch
            {
                // Silently fail - manufacturer detection is not critical
            }

            return null;
        }

        /// <summary>
        /// Checks if WMI support is available for battery charge threshold control.
        /// </summary>
        /// <returns>True if WMI support is available, false otherwise</returns>
        public bool IsWmiSupportAvailable()
        {
            if (wmiSupportAvailable.HasValue)
            {
                return wmiSupportAvailable.Value;
            }

            string? manufacturer = DetectManufacturer();
            if (string.IsNullOrEmpty(manufacturer))
            {
                wmiSupportAvailable = false;
                return false;
            }

            try
            {
                // Check for manufacturer-specific WMI classes
                string? className = GetWmiClassName(manufacturer);
                if (string.IsNullOrEmpty(className))
                {
                    wmiSupportAvailable = false;
                    return false;
                }

                // Try to access the WMI class to verify it exists
                using (ManagementClass mgmtClass = new ManagementClass($"root\\WMI", className, null))
                {
                    // If we can get the class, it exists
                    wmiClassName = className;
                    wmiSupportAvailable = true;
                    return true;
                }
            }
            catch
            {
                wmiSupportAvailable = false;
                return false;
            }
        }

        /// <summary>
        /// Gets the WMI class name for the detected manufacturer.
        /// </summary>
        private string? GetWmiClassName(string manufacturer)
        {
            if (manufacturer.Contains(MANUFACTURER_LENOVO))
            {
                return WMI_LENOVO_BIOS_SETTING;
            }
            else if (manufacturer.Contains(MANUFACTURER_ASUS))
            {
                return WMI_ASUS_ATK;
            }
            else if (manufacturer.Contains(MANUFACTURER_DELL))
            {
                return WMI_DELL_BIOS;
            }
            else if (manufacturer.Contains(MANUFACTURER_HP))
            {
                return WMI_HP_BIOS;
            }

            return null;
        }

        /// <summary>
        /// Enables 80% charge limit using manufacturer-specific WMI methods.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool Enable80PercentLimit()
        {
            if (!IsWmiSupportAvailable())
            {
                return false;
            }

            string? manufacturer = DetectManufacturer();
            if (string.IsNullOrEmpty(manufacturer) || string.IsNullOrEmpty(wmiClassName))
            {
                return false;
            }

            try
            {
                if (manufacturer.Contains(MANUFACTURER_LENOVO))
                {
                    return SetLenovoConservationMode(true);
                }
                else if (manufacturer.Contains(MANUFACTURER_ASUS))
                {
                    return SetAsusBatteryHealthMode(80); // 80% limit
                }
                else if (manufacturer.Contains(MANUFACTURER_DELL))
                {
                    return SetDellBatteryThreshold(80);
                }
                else if (manufacturer.Contains(MANUFACTURER_HP))
                {
                    return SetHpBatteryHealthMode(true);
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Disables charge limit (restores to 100%).
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool DisableChargeLimit()
        {
            if (!IsWmiSupportAvailable())
            {
                return false;
            }

            string? manufacturer = DetectManufacturer();
            if (string.IsNullOrEmpty(manufacturer) || string.IsNullOrEmpty(wmiClassName))
            {
                return false;
            }

            try
            {
                if (manufacturer.Contains(MANUFACTURER_LENOVO))
                {
                    return SetLenovoConservationMode(false);
                }
                else if (manufacturer.Contains(MANUFACTURER_ASUS))
                {
                    return SetAsusBatteryHealthMode(100); // Full capacity
                }
                else if (manufacturer.Contains(MANUFACTURER_DELL))
                {
                    return SetDellBatteryThreshold(100);
                }
                else if (manufacturer.Contains(MANUFACTURER_HP))
                {
                    return SetHpBatteryHealthMode(false);
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Sets Lenovo Conservation Mode via WMI.
        /// </summary>
        private bool SetLenovoConservationMode(bool enable)
        {
            try
            {
                using (ManagementClass biosClass = new ManagementClass("root\\WMI", "Lenovo_SetBiosSetting", null))
                {
                    ManagementObject instance = biosClass.CreateInstance();
                    instance["CurrentSetting"] = enable ? "ConservationMode,Enabled" : "ConservationMode,Disabled";
                    
                    ManagementBaseObject result = instance.InvokeMethod("SetBiosSetting", instance, null);
                    uint returnValue = (uint)result["Return"];
                    
                    return returnValue == 0; // 0 = success
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sets ASUS Battery Health Charging mode via WMI.
        /// </summary>
        private bool SetAsusBatteryHealthMode(int percentLimit)
        {
            try
            {
                using (ManagementClass atkClass = new ManagementClass("root\\WMI", "AsusAtkWmi_WMNB", null))
                {
                    ManagementObjectCollection instances = atkClass.GetInstances();
                    foreach (ManagementObject instance in instances)
                    {
                        // ASUS uses DEVS method with specific codes
                        // 0x00120057 is the code for battery charge limit
                        // Value: 0 = Full, 1 = Balanced (80%), 2 = Maximum Lifespan (60%)
                        int mode = percentLimit >= 100 ? 0 : (percentLimit >= 80 ? 1 : 2);
                        object? resultObj = instance.InvokeMethod("DEVS", new object[] { 0x00120057, mode });
                        ManagementBaseObject? result = resultObj as ManagementBaseObject;
                        
                        uint returnValue = result != null ? (uint)(result["returnValue"] ?? 1) : 1;
                        return returnValue == 0;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Sets Dell battery charge threshold via WMI.
        /// </summary>
        private bool SetDellBatteryThreshold(int percentLimit)
        {
            try
            {
                using (ManagementClass dellClass = new ManagementClass("root\\WMI", "DellSmbiosBattery", null))
                {
                    ManagementObjectCollection instances = dellClass.GetInstances();
                    foreach (ManagementObject instance in instances)
                    {
                        // Dell uses SetBatteryChargeThreshold method
                        ManagementBaseObject inParams = instance.GetMethodParameters("SetBatteryChargeThreshold");
                        inParams["StartThreshold"] = (uint)Math.Max(50, percentLimit - 5); // Start charging at 5% below limit
                        inParams["StopThreshold"] = (uint)percentLimit;
                        
                        ManagementBaseObject result = instance.InvokeMethod("SetBatteryChargeThreshold", inParams, null);
                        uint returnValue = result != null ? (uint)(result["ReturnValue"] ?? 1) : 1;
                        return returnValue == 0;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Sets HP Battery Health Manager mode via WMI.
        /// Note: HP WMI implementation varies by model. This is a best-effort attempt.
        /// </summary>
        private bool SetHpBatteryHealthMode(bool enable80Percent)
        {
            try
            {
                // Try root\HP\InstrumentedBIOS namespace first
                try
                {
                    using (ManagementClass hpClass = new ManagementClass("root\\HP\\InstrumentedBIOS", "HP_BIOSSetting", null))
                    {
                        ManagementObjectCollection instances = hpClass.GetInstances();
                        foreach (ManagementObject instance in instances)
                        {
                            string? name = instance["Name"]?.ToString();
                            if (name != null && name.Contains("Battery", StringComparison.OrdinalIgnoreCase))
                            {
                                // HP uses SetBiosSetting method
                                ManagementBaseObject inParams = instance.GetMethodParameters("SetBiosSetting");
                                inParams["Name"] = name;
                                inParams["Value"] = enable80Percent ? "Maximize My Battery Health" : "Minimize Battery Health Management";
                                
                                ManagementBaseObject result = instance.InvokeMethod("SetBiosSetting", inParams, null);
                                uint returnValue = result != null ? (uint)(result["ReturnValue"] ?? 1) : 1;
                                return returnValue == 0;
                            }
                        }
                    }
                }
                catch
                {
                    // Try alternative namespace or method
                }

                // Fallback: Try root\WMI namespace
                using (ManagementClass hpClass = new ManagementClass("root\\WMI", "HP_BIOSSetting", null))
                {
                    ManagementObjectCollection instances = hpClass.GetInstances();
                    foreach (ManagementObject instance in instances)
                    {
                        string? name = instance["Name"]?.ToString();
                        if (name != null && name.Contains("Battery", StringComparison.OrdinalIgnoreCase))
                        {
                            ManagementBaseObject inParams = instance.GetMethodParameters("SetBiosSetting");
                            inParams["Name"] = name;
                            inParams["Value"] = enable80Percent ? "Maximize My Battery Health" : "Minimize Battery Health Management";
                            
                            ManagementBaseObject result = instance.InvokeMethod("SetBiosSetting", inParams, null);
                            uint returnValue = result != null ? (uint)(result["ReturnValue"] ?? 1) : 1;
                            return returnValue == 0;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Gets instructions for manually setting battery charge limit in BIOS or manufacturer app.
        /// </summary>
        public string GetManualInstructions()
        {
            string? manufacturer = DetectManufacturer();
            
            if (manufacturer != null && manufacturer.Contains(MANUFACTURER_LENOVO))
            {
                return "Lenovo: Open Lenovo Vantage → Device → Power → Battery → Enable Conservation Mode (limits charge to ~75-80%).\n\n" +
                       "Alternatively, check BIOS → Config → Power → Conservation Mode.";
            }
            else if (manufacturer != null && manufacturer.Contains(MANUFACTURER_ASUS))
            {
                return "ASUS: Open MyASUS app → Customization → Battery Health Charging → Select 'Balanced Mode' (80% limit).\n\n" +
                       "Alternatively, check BIOS → Advanced → Power → Battery Health Charging.";
            }
            else if (manufacturer != null && manufacturer.Contains(MANUFACTURER_DELL))
            {
                return "Dell: Open Dell Power Manager → Battery Settings → Custom → Set Stop Charging to 80%.\n\n" +
                       "Alternatively, check BIOS → Power Management → Battery Charge Threshold.";
            }
            else if (manufacturer != null && manufacturer.Contains(MANUFACTURER_HP))
            {
                return "HP: Open HP Support Assistant → Battery → Battery Health Manager → Select 'Maximize My Battery Health' (80% limit).\n\n" +
                       "Alternatively, check BIOS → Advanced → Power Options → Battery Health Manager.";
            }
            else
            {
                return "Battery Health Guard (80% Limit) is not automatically supported on your laptop model.\n\n" +
                       "To manually enable 80% charge limiting:\n" +
                       "1. Check your laptop manufacturer's app (e.g., Lenovo Vantage, MyASUS, Dell Power Manager, HP Support Assistant)\n" +
                       "2. Look for Battery Health, Conservation Mode, or Charge Threshold settings\n" +
                       "3. Enable the 80% limit option\n" +
                       "4. Alternatively, check your BIOS settings for battery charge threshold options";
            }
        }

        /// <summary>
        /// Gets the detected manufacturer name.
        /// </summary>
        public string? GetDetectedManufacturer()
        {
            return detectedManufacturer ?? DetectManufacturer();
        }
    }
}
