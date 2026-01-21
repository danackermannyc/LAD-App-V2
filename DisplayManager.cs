using System;
using System.Runtime.InteropServices;

namespace LADApp
{
    /// <summary>
    /// Manages display topology using SetDisplayConfig API.
    /// Forces External-Only mode when LAD Ready to prevent internal screen from waking.
    /// </summary>
    public class DisplayManager
    {
        // Windows Display Configuration API
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SetDisplayConfig(
            uint numPathArrayElements,
            IntPtr pathArray,
            uint numModeInfoArrayElements,
            IntPtr modeInfoArray,
            SetDisplayConfigFlags flags);

        // SetDisplayConfig flags
        [Flags]
        private enum SetDisplayConfigFlags : uint
        {
            SDC_TOPOLOGY_INTERNAL = 0x00000001,
            SDC_TOPOLOGY_CLONE = 0x00000002,
            SDC_TOPOLOGY_EXTEND = 0x00000004,
            SDC_TOPOLOGY_EXTERNAL = 0x00000008,
            SDC_APPLY = 0x00000080,
            // SDC_USE_DATABASE_CURRENT is a combination of all topology flags
            // Value: 0x00000001 | 0x00000002 | 0x00000004 | 0x00000008 = 0x0000000F
            SDC_USE_DATABASE_CURRENT = 0x0000000F
        }

        // HRESULT success code (S_OK = 0)
        private const int S_OK = 0;

        /// <summary>
        /// Forces the display to External-Only mode (kills internal panel signal).
        /// This is called when LAD Ready state is detected.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool ForceExternalOnlyMode(Action<string>? logCallback = null)
        {
            try
            {
                int result = SetDisplayConfig(
                    0,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    SetDisplayConfigFlags.SDC_TOPOLOGY_EXTERNAL | SetDisplayConfigFlags.SDC_APPLY);

                if (result == S_OK)
                {
                    logCallback?.Invoke("DISPLAY: Forcing External-Only Mode");
                    return true;
                }
                else
                {
                    // SetDisplayConfig returns HRESULT, not Win32 error
                    // Format as HRESULT: 0xHHHHHHHH
                    string hresultHex = $"0x{result:X8}";
                    logCallback?.Invoke($"DISPLAY: Failed to set External-Only mode (HRESULT: {hresultHex})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"DISPLAY: Exception setting External-Only mode - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores the display to Extended mode (internal + external).
        /// This is called when Quick Eject is triggered or monitor is unplugged.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool RestoreExtendedMode(Action<string>? logCallback = null)
        {
            try
            {
                // Try EXTEND mode first (most common for multi-monitor setups)
                int result = SetDisplayConfig(
                    0,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    SetDisplayConfigFlags.SDC_TOPOLOGY_EXTEND | SetDisplayConfigFlags.SDC_APPLY);

                if (result == S_OK)
                {
                    logCallback?.Invoke("DISPLAY: Restored Extended Mode (internal + external)");
                    return true;
                }
                else
                {
                    // Fallback: Use database current (restores to last known good configuration)
                    result = SetDisplayConfig(
                        0,
                        IntPtr.Zero,
                        0,
                        IntPtr.Zero,
                        SetDisplayConfigFlags.SDC_USE_DATABASE_CURRENT | SetDisplayConfigFlags.SDC_APPLY);

                    if (result == S_OK)
                    {
                        logCallback?.Invoke("DISPLAY: Restored to previous configuration");
                        return true;
                    }
                    else
                    {
                        // SetDisplayConfig returns HRESULT, not Win32 error
                        // Format as HRESULT: 0xHHHHHHHH
                        string hresultHex = $"0x{result:X8}";
                        logCallback?.Invoke($"DISPLAY: Failed to restore display mode (HRESULT: {hresultHex})");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"DISPLAY: Exception restoring display mode - {ex.Message}");
                return false;
            }
        }
    }
}
