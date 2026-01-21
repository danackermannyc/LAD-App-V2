using System;
using System.Runtime.InteropServices;

namespace LADApp
{
    /// <summary>
    /// Manages Windows power settings, specifically the Lid Close Action.
    /// Uses PowerWriteACValueIndex and PowerSetActiveScheme as per project requirements.
    /// </summary>
    public class PowerManager
    {
        // Windows Power Management API imports
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerWriteACValueIndex(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            uint AcValueIndex);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadACValueIndex(
            IntPtr RootPowerKey,
            IntPtr SchemeGuid,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            out uint AcValueIndex);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, IntPtr SchemeGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint LocalFree(IntPtr hMem);

        // Power setting GUIDs
        private static readonly Guid SUBGROUP_BUTTONS = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347");
        private static readonly Guid LID_CLOSE_ACTION = new Guid("5ca83367-6e45-459f-a27b-476b1d01c936");
        private static readonly Guid SUBGROUP_SLEEP = new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");
        private static readonly Guid HIBERNATE_TIMEOUT = new Guid("9d7815a6-7ee4-497e-8888-515a05f02364");

        // Lid Close Action values
        private const uint LID_CLOSE_DO_NOTHING = 0;
        private const uint LID_CLOSE_SLEEP = 1;
        private const uint LID_CLOSE_HIBERNATE = 2;
        private const uint LID_CLOSE_SHUTDOWN = 3;

        // Success code for Power Management APIs
        private const uint ERROR_SUCCESS = 0;

        // Hibernate timeout storage (original value to restore)
        private uint? originalHibernateTimeout = null;

        // Power scheme GUIDs
        private static readonly Guid HIGH_PERFORMANCE_SCHEME = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

        // Original power scheme storage (to restore custom manufacturer schemes)
        private Guid? originalPowerSchemeGuid = null;

        /// <summary>
        /// Sets the Lid Close Action to "Do Nothing" (0) when on AC power.
        /// This allows the laptop to remain active when the lid is closed.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetLidCloseDoNothing()
        {
            try
            {
                // Get the active power scheme GUID
                IntPtr activeSchemeGuidPtr;
                uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPtr);
                
                if (result != ERROR_SUCCESS)
                {
                    return false;
                }

                try
                {
                    // Create local copies of GUIDs for ref parameters
                    Guid subgroupButtons = SUBGROUP_BUTTONS;
                    Guid lidCloseAction = LID_CLOSE_ACTION;
                    
                    // Set Lid Close Action to "Do Nothing" (0) for AC power
                    result = PowerWriteACValueIndex(
                        IntPtr.Zero,
                        activeSchemeGuidPtr,
                        ref subgroupButtons,
                        ref lidCloseAction,
                        LID_CLOSE_DO_NOTHING);

                    if (result != ERROR_SUCCESS)
                    {
                        return false;
                    }

                    // Apply the changes to the active power scheme
                    result = PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuidPtr);
                    
                    return result == ERROR_SUCCESS;
                }
                finally
                {
                    // Free the memory allocated by PowerGetActiveScheme
                    if (activeSchemeGuidPtr != IntPtr.Zero)
                    {
                        LocalFree(activeSchemeGuidPtr);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the Lid Close Action to "Sleep" (1) when on AC power.
        /// This is the default behavior and should be restored when LAD mode is not active.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetLidCloseSleep()
        {
            try
            {
                // Get the active power scheme GUID
                IntPtr activeSchemeGuidPtr;
                uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPtr);
                
                if (result != ERROR_SUCCESS)
                {
                    return false;
                }

                try
                {
                    // Create local copies of GUIDs for ref parameters
                    Guid subgroupButtons = SUBGROUP_BUTTONS;
                    Guid lidCloseAction = LID_CLOSE_ACTION;
                    
                    // Set Lid Close Action to "Sleep" (1) for AC power
                    result = PowerWriteACValueIndex(
                        IntPtr.Zero,
                        activeSchemeGuidPtr,
                        ref subgroupButtons,
                        ref lidCloseAction,
                        LID_CLOSE_SLEEP);

                    if (result != ERROR_SUCCESS)
                    {
                        return false;
                    }

                    // Apply the changes to the active power scheme
                    result = PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuidPtr);
                    
                    return result == ERROR_SUCCESS;
                }
                finally
                {
                    // Free the memory allocated by PowerGetActiveScheme
                    if (activeSchemeGuidPtr != IntPtr.Zero)
                    {
                        LocalFree(activeSchemeGuidPtr);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the current Hibernate Timeout value for AC power.
        /// </summary>
        /// <returns>The timeout value in seconds, or null if read failed</returns>
        public uint? ReadHibernateTimeout()
        {
            try
            {
                // Get the active power scheme GUID
                IntPtr activeSchemeGuidPtr;
                uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPtr);
                
                if (result != ERROR_SUCCESS)
                {
                    return null;
                }

                try
                {
                    // Create local copies of GUIDs for ref parameters
                    Guid subgroupSleep = SUBGROUP_SLEEP;
                    Guid hibernateTimeout = HIBERNATE_TIMEOUT;
                    
                    // Read current Hibernate Timeout value for AC power
                    uint acValue;
                    result = PowerReadACValueIndex(
                        IntPtr.Zero,
                        activeSchemeGuidPtr,
                        ref subgroupSleep,
                        ref hibernateTimeout,
                        out acValue);

                    if (result != ERROR_SUCCESS)
                    {
                        return null;
                    }

                    return acValue;
                }
                finally
                {
                    // Free the memory allocated by PowerGetActiveScheme
                    if (activeSchemeGuidPtr != IntPtr.Zero)
                    {
                        LocalFree(activeSchemeGuidPtr);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the Hibernate Timeout to "Never" (0) for AC power.
        /// Stores the original value for later restoration.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetHibernateTimeoutNever()
        {
            try
            {
                // Read and store original value if not already stored
                if (!originalHibernateTimeout.HasValue)
                {
                    originalHibernateTimeout = ReadHibernateTimeout();
                }

                // Get the active power scheme GUID
                IntPtr activeSchemeGuidPtr;
                uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPtr);
                
                if (result != ERROR_SUCCESS)
                {
                    return false;
                }

                try
                {
                    // Create local copies of GUIDs for ref parameters
                    Guid subgroupSleep = SUBGROUP_SLEEP;
                    Guid hibernateTimeout = HIBERNATE_TIMEOUT;
                    
                    // Set Hibernate Timeout to "Never" (0) for AC power
                    result = PowerWriteACValueIndex(
                        IntPtr.Zero,
                        activeSchemeGuidPtr,
                        ref subgroupSleep,
                        ref hibernateTimeout,
                        0); // 0 = Never

                    if (result != ERROR_SUCCESS)
                    {
                        return false;
                    }

                    // Apply the changes to the active power scheme
                    result = PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuidPtr);
                    
                    return result == ERROR_SUCCESS;
                }
                finally
                {
                    // Free the memory allocated by PowerGetActiveScheme
                    if (activeSchemeGuidPtr != IntPtr.Zero)
                    {
                        LocalFree(activeSchemeGuidPtr);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restores the original Hibernate Timeout value for AC power.
        /// If no original value was stored, attempts to read current value first.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool RestoreHibernateTimeout()
        {
            try
            {
                // If we don't have the original value stored, try to read it
                // (in case app was restarted and original value wasn't persisted)
                if (!originalHibernateTimeout.HasValue)
                {
                    originalHibernateTimeout = ReadHibernateTimeout();
                }

                // If we still don't have a value, can't restore
                if (!originalHibernateTimeout.HasValue)
                {
                    return false;
                }

                // Get the active power scheme GUID
                IntPtr activeSchemeGuidPtr;
                uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPtr);
                
                if (result != ERROR_SUCCESS)
                {
                    return false;
                }

                try
                {
                    // Create local copies of GUIDs for ref parameters
                    Guid subgroupSleep = SUBGROUP_SLEEP;
                    Guid hibernateTimeout = HIBERNATE_TIMEOUT;
                    
                    // Restore original Hibernate Timeout value for AC power
                    result = PowerWriteACValueIndex(
                        IntPtr.Zero,
                        activeSchemeGuidPtr,
                        ref subgroupSleep,
                        ref hibernateTimeout,
                        originalHibernateTimeout.Value);

                    if (result != ERROR_SUCCESS)
                    {
                        return false;
                    }

                    // Apply the changes to the active power scheme
                    result = PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuidPtr);
                    
                    return result == ERROR_SUCCESS;
                }
                finally
                {
                    // Free the memory allocated by PowerGetActiveScheme
                    if (activeSchemeGuidPtr != IntPtr.Zero)
                    {
                        LocalFree(activeSchemeGuidPtr);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the stored original hibernate timeout value.
        /// </summary>
        /// <returns>The original timeout value, or null if not stored</returns>
        public uint? GetOriginalHibernateTimeout()
        {
            return originalHibernateTimeout;
        }

        /// <summary>
        /// Sets the stored original hibernate timeout value (for persistence).
        /// </summary>
        /// <param name="value">The original timeout value to store</param>
        public void SetOriginalHibernateTimeout(uint? value)
        {
            originalHibernateTimeout = value;
        }

        /// <summary>
        /// Gets the current active power scheme GUID.
        /// </summary>
        /// <returns>The current power scheme GUID, or null if read failed</returns>
        public Guid? GetCurrentPowerScheme()
        {
            try
            {
                IntPtr activeSchemeGuidPtr;
                uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPtr);
                
                if (result != ERROR_SUCCESS)
                {
                    return null;
                }

                try
                {
                    // Marshal the IntPtr to a Guid structure
                    Guid schemeGuid = Marshal.PtrToStructure<Guid>(activeSchemeGuidPtr);
                    return schemeGuid;
                }
                finally
                {
                    // Free the memory allocated by PowerGetActiveScheme
                    if (activeSchemeGuidPtr != IntPtr.Zero)
                    {
                        LocalFree(activeSchemeGuidPtr);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Switches to the High Performance power scheme.
        /// Stores the original scheme GUID for later restoration.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool SwitchToHighPerformance()
        {
            try
            {
                // Get and store original scheme if not already stored
                if (!originalPowerSchemeGuid.HasValue)
                {
                    originalPowerSchemeGuid = GetCurrentPowerScheme();
                }

                // Allocate memory for the High Performance GUID
                IntPtr highPerfGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Guid>());
                try
                {
                    // Copy the High Performance GUID to the allocated memory
                    Marshal.StructureToPtr(HIGH_PERFORMANCE_SCHEME, highPerfGuidPtr, false);

                    // Set the active power scheme to High Performance
                    uint result = PowerSetActiveScheme(IntPtr.Zero, highPerfGuidPtr);
                    
                    return result == ERROR_SUCCESS;
                }
                finally
                {
                    // Free the allocated memory
                    if (highPerfGuidPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(highPerfGuidPtr);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restores the original power scheme.
        /// If no original scheme was stored, attempts to read current scheme first.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool RestoreOriginalPowerScheme()
        {
            try
            {
                // If we don't have the original scheme stored, try to get it
                // (in case app was restarted and original scheme wasn't persisted)
                if (!originalPowerSchemeGuid.HasValue)
                {
                    originalPowerSchemeGuid = GetCurrentPowerScheme();
                }

                // If we still don't have a scheme, can't restore
                if (!originalPowerSchemeGuid.HasValue)
                {
                    return false;
                }

                // Allocate memory for the original GUID
                IntPtr originalGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Guid>());
                try
                {
                    // Copy the original GUID to the allocated memory
                    Marshal.StructureToPtr(originalPowerSchemeGuid.Value, originalGuidPtr, false);

                    // Restore the original power scheme
                    uint result = PowerSetActiveScheme(IntPtr.Zero, originalGuidPtr);
                    
                    return result == ERROR_SUCCESS;
                }
                finally
                {
                    // Free the allocated memory
                    if (originalGuidPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(originalGuidPtr);
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the stored original power scheme GUID.
        /// </summary>
        /// <returns>The original power scheme GUID, or null if not stored</returns>
        public Guid? GetOriginalPowerScheme()
        {
            return originalPowerSchemeGuid;
        }

        /// <summary>
        /// Sets the stored original power scheme GUID (for persistence).
        /// </summary>
        /// <param name="value">The original power scheme GUID to store</param>
        public void SetOriginalPowerScheme(Guid? value)
        {
            originalPowerSchemeGuid = value;
        }
    }
}
