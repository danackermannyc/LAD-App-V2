using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace LADApp
{
    /// <summary>
    /// Manages peripheral wake enablement for HID devices (keyboards and mice).
    /// Uses DevicePowerSetDeviceState to enable wake capability and disables USB Selective Suspend.
    /// </summary>
    public class PeripheralWakeManager
    {
        // Windows API imports for HID enumeration
        [DllImport("hid.dll", SetLastError = true)]
        private static extern void HidD_GetHidGuid(out Guid HidGuid);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            IntPtr Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr DeviceInfoSet,
            IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid,
            uint MemberIndex,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData,
            uint DeviceInterfaceDetailDataSize,
            ref uint RequiredSize,
            IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            out uint PropertyRegDataType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData,
            uint DeviceInterfaceDetailDataSize,
            ref uint RequiredSize,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetAttributes(
            SafeFileHandle HidDeviceObject,
            ref HIDD_ATTRIBUTES Attributes);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool HidD_GetManufacturerString(
            SafeFileHandle HidDeviceObject,
            StringBuilder Buffer,
            uint BufferLength);

        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool HidD_GetProductString(
            SafeFileHandle HidDeviceObject,
            StringBuilder Buffer,
            uint BufferLength);

        // Windows API for device power management
        [DllImport("powrprof.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint DevicePowerSetDeviceState(
            string DeviceDescription,
            uint SetFlags,
            IntPtr SetData);

        // Windows API for power scheme management (for USB Selective Suspend)
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
        private static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, IntPtr SchemeGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint LocalFree(IntPtr hMem);

        // Constants
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint DEVICEPOWER_SET_WAKEENABLED = 0x00000001;
        private const uint ERROR_SUCCESS = 0;
        
        // Device Registry Property constants
        private const uint SPDRP_DEVICEDESC = 0x00000000; // Device Description
        private const uint SPDRP_FRIENDLYNAME = 0x0000000C; // Friendly Name
        private const uint SPDRP_HARDWAREID = 0x00000001; // Hardware ID
        
        // Configuration Manager API for Device Instance Paths (more reliable than device descriptions)
        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint CM_Get_Device_ID(
            uint dnDevInst,
            StringBuilder Buffer,
            uint BufferLen,
            uint ulFlags);
        
        private const uint CM_GET_DEVICE_ID_FLAGS = 0; // Standard flags

        // USB Selective Suspend GUIDs
        // SUB_USB: 2a737441-1930-4402-8d77-b2eebf27e6b4
        // USBSELECTIVESUSPEND: 48e6b7a6-50f5-4782-a5d4-53bb8f07e226
        private static readonly Guid SUB_USB = new Guid("2a737441-1930-4402-8d77-b2eebf27e6b4");
        private static readonly Guid USB_SELECTIVE_SUSPEND = new Guid("48e6b7a6-50f5-4782-a5d4-53bb8f07e226");

        // HID usage page constants for keyboard and mouse
        private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
        private const ushort HID_USAGE_KEYBOARD = 0x06;
        private const ushort HID_USAGE_MOUSE = 0x02;

        // Device information class for wizard selection
        public class DeviceInfo
        {
            public string Name { get; set; } = string.Empty;
            public string InstancePath { get; set; } = string.Empty;
            public string DeviceDescription { get; set; } = string.Empty;
            public bool IsKeyboard { get; set; }
            public bool IsMouse { get; set; }
            public ushort VendorID { get; set; }
            public ushort ProductID { get; set; }
        }

        // Structs
        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public uint cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public uint Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        /// <summary>
        /// Enables wake capability for all connected keyboards and mice.
        /// </summary>
        /// <returns>List of device names that were successfully enabled</returns>
        public List<string> EnableWakeForKeyboardsAndMice(Action<string>? logCallback = null)
        {
            List<string> enabledDevices = new List<string>();
            
            try
            {
                // Get HID GUID
                Guid hidGuid;
                try
                {
                    HidD_GetHidGuid(out hidGuid);
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"PERIPHERAL: Failed to get HID GUID - {ex.Message}");
                    return enabledDevices;
                }

                // Get device information set
                IntPtr deviceInfoSet = SetupDiGetClassDevs(
                    ref hidGuid,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(-1))
                {
                    logCallback?.Invoke("PERIPHERAL: Failed to get HID device information set");
                    return enabledDevices;
                }

                try
                {
                    uint index = 0;
                    SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                    deviceInterfaceData.cbSize = (uint)Marshal.SizeOf(deviceInterfaceData);

                    while (SetupDiEnumDeviceInterfaces(
                        deviceInfoSet,
                        IntPtr.Zero,
                        ref hidGuid,
                        index,
                        ref deviceInterfaceData))
                    {
                        // Get required size for device interface detail
                        uint requiredSize = 0;
                        SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet,
                            ref deviceInterfaceData,
                            IntPtr.Zero,
                            0,
                            ref requiredSize,
                            IntPtr.Zero);

                        if (requiredSize > 0)
                        {
                            // Allocate buffer for device interface detail
                            IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);
                            try
                            {
                                // Set up the structure - cbSize is the size of the structure header
                                // For 64-bit: 8 bytes (pointer size), for 32-bit: 4 bytes + char size
                                uint cbSize = (uint)(IntPtr.Size == 8 ? 8 : 4 + Marshal.SystemDefaultCharSize);
                                Marshal.WriteInt32(detailDataBuffer, (int)cbSize);

                                // Get device info data
                                SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                                deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                                if (SetupDiGetDeviceInterfaceDetail(
                                    deviceInfoSet,
                                    ref deviceInterfaceData,
                                    detailDataBuffer,
                                    requiredSize,
                                    ref requiredSize,
                                    ref deviceInfoData))
                                {
                                    // Extract device path (skip the cbSize field which is at the start)
                                    IntPtr devicePathPtr = (IntPtr)((long)detailDataBuffer + IntPtr.Size);
                                    string? devicePath = Marshal.PtrToStringAuto(devicePathPtr);

                                                // Get Device Instance Path (most reliable identifier)
                                                string? deviceInstancePath = GetDeviceInstancePath(deviceInfoData);
                                                
                                                // Get Windows device description (fallback for DevicePowerSetDeviceState)
                                                string? windowsDeviceDescription = GetDeviceDescription(deviceInfoSet, deviceInfoData);
                                                
                                                if (!string.IsNullOrEmpty(devicePath))
                                                {
                                                    // Try to open the device
                                                    SafeFileHandle deviceHandle = CreateFile(
                                                        devicePath,
                                                        GENERIC_READ | GENERIC_WRITE,
                                                        FILE_SHARE_READ | FILE_SHARE_WRITE,
                                                        IntPtr.Zero,
                                                        OPEN_EXISTING,
                                                        0,
                                                        IntPtr.Zero);

                                                    if (!deviceHandle.IsInvalid)
                                                    {
                                                        try
                                                        {
                                                            // Get device attributes for logging
                                                            HIDD_ATTRIBUTES attributes = new HIDD_ATTRIBUTES();
                                                            attributes.Size = (uint)Marshal.SizeOf(attributes);

                                                            string deviceName = "Unknown Device";
                                                            if (HidD_GetAttributes(deviceHandle, ref attributes))
                                                            {
                                                                // Get device name strings for logging
                                                                StringBuilder manufacturer = new StringBuilder(256);
                                                                StringBuilder product = new StringBuilder(256);
                                                                
                                                                HidD_GetManufacturerString(deviceHandle, manufacturer, 256);
                                                                HidD_GetProductString(deviceHandle, product, 256);

                                                                deviceName = $"{manufacturer} {product}".Trim();
                                                                if (string.IsNullOrEmpty(deviceName))
                                                                {
                                                                    deviceName = $"HID Device (VID:{attributes.VendorID:X4}, PID:{attributes.ProductID:X4})";
                                                                }
                                                            }

                                                            // PRIMARY METHOD: Try to enable wake using Device Instance Path (most reliable)
                                                            // Device Instance Paths are unique and persistent across reboots
                                                            bool wakeEnabled = false;
                                                            if (!string.IsNullOrEmpty(deviceInstancePath))
                                                            {
                                                                uint result = DevicePowerSetDeviceState(
                                                                    deviceInstancePath,
                                                                    DEVICEPOWER_SET_WAKEENABLED,
                                                                    IntPtr.Zero);

                                                                if (result == ERROR_SUCCESS)
                                                                {
                                                                    wakeEnabled = true;
                                                                    enabledDevices.Add(deviceName);
                                                                    logCallback?.Invoke($"PERIPHERAL: Enabled wake for {deviceName} (Instance Path: {deviceInstancePath})");
                                                                }
                                                                else
                                                                {
                                                                    int lastError = Marshal.GetLastWin32Error();
                                                                    logCallback?.Invoke($"PERIPHERAL: Instance Path method failed for {deviceName} (Path: {deviceInstancePath}, Error: {result}, Win32: {lastError}) - trying fallback");
                                                                }
                                                            }

                                                            // FALLBACK METHOD 1: Try Windows device description if Instance Path failed
                                                            if (!wakeEnabled && !string.IsNullOrEmpty(windowsDeviceDescription))
                                                            {
                                                                uint result = DevicePowerSetDeviceState(
                                                                    windowsDeviceDescription,
                                                                    DEVICEPOWER_SET_WAKEENABLED,
                                                                    IntPtr.Zero);

                                                                if (result == ERROR_SUCCESS)
                                                                {
                                                                    wakeEnabled = true;
                                                                    enabledDevices.Add(deviceName);
                                                                    logCallback?.Invoke($"PERIPHERAL: Enabled wake for {deviceName} using Device Description fallback (Windows: {windowsDeviceDescription})");
                                                                }
                                                                else
                                                                {
                                                                    logCallback?.Invoke($"PERIPHERAL: Device Description fallback failed for {deviceName} (Error: {result})");
                                                                }
                                                            }

                                                            // FALLBACK METHOD 2: Try the device name as last resort
                                                            if (!wakeEnabled && !string.IsNullOrEmpty(deviceName))
                                                            {
                                                                uint result = DevicePowerSetDeviceState(
                                                                    deviceName,
                                                                    DEVICEPOWER_SET_WAKEENABLED,
                                                                    IntPtr.Zero);

                                                                if (result == ERROR_SUCCESS)
                                                                {
                                                                    wakeEnabled = true;
                                                                    if (!enabledDevices.Contains(deviceName))
                                                                    {
                                                                        enabledDevices.Add(deviceName);
                                                                    }
                                                                    logCallback?.Invoke($"PERIPHERAL: Enabled wake for {deviceName} using device name fallback");
                                                                }
                                                                else
                                                                {
                                                                    logCallback?.Invoke($"PERIPHERAL: All wake enablement methods failed for {deviceName}. Instance Path: {deviceInstancePath ?? "N/A"}, Description: {windowsDeviceDescription ?? "N/A"}");
                                                                }
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            deviceHandle.Close();
                                                        }
                                                    }
                                                }
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(detailDataBuffer);
                            }
                        }

                        index++;
                        deviceInterfaceData.cbSize = (uint)Marshal.SizeOf(deviceInterfaceData);
                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"PERIPHERAL: Error enumerating devices - {ex.Message}");
            }

            return enabledDevices;
        }

        /// <summary>
        /// Gets the Device Instance Path (e.g., "USB\VID_046D&PID_C534\5&3afd18bd&0&5") for a device.
        /// This is the most reliable identifier for device power management.
        /// </summary>
        private string? GetDeviceInstancePath(SP_DEVINFO_DATA deviceInfoData)
        {
            try
            {
                StringBuilder buffer = new StringBuilder(260); // MAX_DEVICE_ID_LEN = 260
                uint result = CM_Get_Device_ID(
                    deviceInfoData.DevInst,
                    buffer,
                    (uint)buffer.Capacity,
                    CM_GET_DEVICE_ID_FLAGS);

                if (result == 0) // CR_SUCCESS = 0
                {
                    string instancePath = buffer.ToString();
                    if (!string.IsNullOrEmpty(instancePath))
                    {
                        return instancePath;
                    }
                }
            }
            catch
            {
                // Ignore errors, return null
            }

            return null;
        }

        /// <summary>
        /// Gets the Windows device description for a device, which is what DevicePowerSetDeviceState needs.
        /// </summary>
        private string? GetDeviceDescription(IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
        {
            try
            {
                uint propertyRegDataType;
                uint requiredSize;
                byte[] propertyBuffer = new byte[1024];

                // Try to get device description first
                bool success = SetupDiGetDeviceRegistryProperty(
                    deviceInfoSet,
                    ref deviceInfoData,
                    SPDRP_DEVICEDESC,
                    out propertyRegDataType,
                    propertyBuffer,
                    (uint)propertyBuffer.Length,
                    out requiredSize);

                if (success && requiredSize > 0)
                {
                    string description = Encoding.Unicode.GetString(propertyBuffer, 0, (int)requiredSize).TrimEnd('\0');
                    if (!string.IsNullOrEmpty(description))
                    {
                        return description;
                    }
                }

                // Fallback to friendly name
                success = SetupDiGetDeviceRegistryProperty(
                    deviceInfoSet,
                    ref deviceInfoData,
                    SPDRP_FRIENDLYNAME,
                    out propertyRegDataType,
                    propertyBuffer,
                    (uint)propertyBuffer.Length,
                    out requiredSize);

                if (success && requiredSize > 0)
                {
                    string friendlyName = Encoding.Unicode.GetString(propertyBuffer, 0, (int)requiredSize).TrimEnd('\0');
                    if (!string.IsNullOrEmpty(friendlyName))
                    {
                        return friendlyName;
                    }
                }
            }
            catch
            {
                // Ignore errors, return null
            }

            return null;
        }

        /// <summary>
        /// Enumerates all HID devices (keyboards and mice) with their details for wizard selection.
        /// Returns devices with Instance Paths, names, and type information.
        /// </summary>
        public List<DeviceInfo> EnumerateDevicesForSelection(Action<string>? logCallback = null)
        {
            List<DeviceInfo> devices = new List<DeviceInfo>();

            try
            {
                // Get HID GUID
                Guid hidGuid;
                try
                {
                    HidD_GetHidGuid(out hidGuid);
                    logCallback?.Invoke($"DEVICE_ENUM: HID GUID retrieved: {hidGuid}");
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"DEVICE_ENUM: Failed to get HID GUID - {ex.Message}");
                    return devices;
                }

                // Get device information set
                IntPtr deviceInfoSet = SetupDiGetClassDevs(
                    ref hidGuid,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(-1))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    logCallback?.Invoke($"DEVICE_ENUM: Failed to get device information set. Win32 Error: {lastError}");
                    return devices;
                }

                logCallback?.Invoke($"DEVICE_ENUM: Device information set obtained successfully");

                try
                {
                    uint index = 0;
                    SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                    deviceInterfaceData.cbSize = (uint)Marshal.SizeOf(deviceInterfaceData);

                    while (SetupDiEnumDeviceInterfaces(
                        deviceInfoSet,
                        IntPtr.Zero,
                        ref hidGuid,
                        index,
                        ref deviceInterfaceData))
                    {
                        // Get required size for device interface detail
                        uint requiredSize = 0;
                        SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet,
                            ref deviceInterfaceData,
                            IntPtr.Zero,
                            0,
                            ref requiredSize,
                            IntPtr.Zero);

                        if (requiredSize > 0)
                        {
                            IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);
                            try
                            {
                                // Write cbSize - must match the structure alignment
                                // On 64-bit: cbSize is 8 bytes (ULONG_PTR), on 32-bit: 4 bytes (DWORD)
                                if (IntPtr.Size == 8)
                                {
                                    Marshal.WriteInt64(detailDataBuffer, 8); // 64-bit: 8 bytes
                                }
                                else
                                {
                                    Marshal.WriteInt32(detailDataBuffer, 4 + Marshal.SystemDefaultCharSize); // 32-bit: 4 + char size
                                }

                                SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                                deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                                if (SetupDiGetDeviceInterfaceDetail(
                                    deviceInfoSet,
                                    ref deviceInterfaceData,
                                    detailDataBuffer,
                                    requiredSize,
                                    ref requiredSize,
                                    ref deviceInfoData))
                                {
                                    // Extract device path - skip cbSize field
                                    // On 64-bit: skip 8 bytes, on 32-bit: skip 4 bytes
                                    IntPtr devicePathPtr = (IntPtr)((long)detailDataBuffer + (IntPtr.Size == 8 ? 8 : 4));
                                    string? devicePath = Marshal.PtrToStringUni(devicePathPtr);
                                    
                                    logCallback?.Invoke($"DEVICE_ENUM: Device path extracted at index {index}: {devicePath ?? "NULL"}");

                                    // Get Device Instance Path
                                    string? deviceInstancePath = GetDeviceInstancePath(deviceInfoData);
                                    
                                    // Get Windows device description
                                    string? windowsDeviceDescription = GetDeviceDescription(deviceInfoSet, deviceInfoData);

                                    if (!string.IsNullOrEmpty(devicePath))
                                    {
                                        // Try with read/write first, fall back to read-only if that fails
                                        SafeFileHandle deviceHandle = CreateFile(
                                            devicePath,
                                            GENERIC_READ | GENERIC_WRITE,
                                            FILE_SHARE_READ | FILE_SHARE_WRITE,
                                            IntPtr.Zero,
                                            OPEN_EXISTING,
                                            0,
                                            IntPtr.Zero);

                                        // If read/write fails, try read-only (works without admin)
                                        if (deviceHandle.IsInvalid)
                                        {
                                            deviceHandle = CreateFile(
                                                devicePath,
                                                GENERIC_READ,
                                                FILE_SHARE_READ | FILE_SHARE_WRITE,
                                                IntPtr.Zero,
                                                OPEN_EXISTING,
                                                0,
                                                IntPtr.Zero);
                                        }

                                        if (!deviceHandle.IsInvalid)
                                        {
                                            try
                                            {
                                                HIDD_ATTRIBUTES attributes = new HIDD_ATTRIBUTES();
                                                attributes.Size = (uint)Marshal.SizeOf(attributes);

                                                if (HidD_GetAttributes(deviceHandle, ref attributes))
                                                {
                                                    // Get device name strings
                                                    StringBuilder manufacturer = new StringBuilder(256);
                                                    StringBuilder product = new StringBuilder(256);
                                                    
                                                    HidD_GetManufacturerString(deviceHandle, manufacturer, 256);
                                                    HidD_GetProductString(deviceHandle, product, 256);

                                                    string deviceName = $"{manufacturer} {product}".Trim();
                                                    if (string.IsNullOrEmpty(deviceName))
                                                    {
                                                        deviceName = $"HID Device (VID:{attributes.VendorID:X4}, PID:{attributes.ProductID:X4})";
                                                    }

                                                    // Determine device type (keyboard or mouse)
                                                    // We'll use a simple heuristic: check if it's a keyboard or mouse
                                                    // For a more accurate method, we'd need to read HID usage pages
                                                    // For now, we'll include all HID devices and let the user choose
                                                    bool isKeyboard = false;
                                                    bool isMouse = false;

                                                    // Simple heuristic based on device name
                                                    string deviceNameUpper = deviceName.ToUpperInvariant();
                                                    if (deviceNameUpper.Contains("KEYBOARD") || deviceNameUpper.Contains("KB"))
                                                    {
                                                        isKeyboard = true;
                                                    }
                                                    else if (deviceNameUpper.Contains("MOUSE") || deviceNameUpper.Contains("POINTING"))
                                                    {
                                                        isMouse = true;
                                                    }
                                                    else
                                                    {
                                                        // If we can't determine, include as both (user can decide)
                                                        isKeyboard = true;
                                                        isMouse = true;
                                                    }

                                                    DeviceInfo deviceInfo = new DeviceInfo
                                                    {
                                                        Name = deviceName,
                                                        InstancePath = deviceInstancePath ?? string.Empty,
                                                        DeviceDescription = windowsDeviceDescription ?? string.Empty,
                                                        IsKeyboard = isKeyboard,
                                                        IsMouse = isMouse,
                                                        VendorID = attributes.VendorID,
                                                        ProductID = attributes.ProductID
                                                    };

                                                    devices.Add(deviceInfo);
                                                    logCallback?.Invoke($"DEVICE_ENUM: Added device: {deviceName} (Instance: {deviceInstancePath ?? "N/A"})");
                                                }
                                                else
                                                {
                                                    logCallback?.Invoke($"DEVICE_ENUM: Failed to get device attributes for device at index {index}");
                                                }
                                            }
                                            finally
                                            {
                                                deviceHandle.Close();
                                            }
                                        }
                                        else
                                        {
                                            int lastError = Marshal.GetLastWin32Error();
                                            logCallback?.Invoke($"DEVICE_ENUM: Failed to open device handle at index {index}. Win32 Error: {lastError}");
                                        }
                                    }
                                    else
                                    {
                                        logCallback?.Invoke($"DEVICE_ENUM: Device path is null or empty at index {index}");
                                    }
                                }
                                else
                                {
                                    int lastError = Marshal.GetLastWin32Error();
                                    logCallback?.Invoke($"DEVICE_ENUM: Failed to get device interface detail at index {index}. Win32 Error: {lastError}");
                                }
                            }
                            finally
                            {
                                Marshal.FreeHGlobal(detailDataBuffer);
                            }
                        }
                        else
                        {
                            logCallback?.Invoke($"DEVICE_ENUM: Required size is 0 for device at index {index}");
                        }

                        index++;
                        deviceInterfaceData.cbSize = (uint)Marshal.SizeOf(deviceInterfaceData);
                    }

                    logCallback?.Invoke($"DEVICE_ENUM: Enumeration complete. Found {devices.Count} device(s)");
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"DEVICE_ENUM: Exception during enumeration - {ex.Message}\nStack: {ex.StackTrace}");
            }

            return devices;
        }

        /// <summary>
        /// Disables USB Selective Suspend for the current power scheme.
        /// This prevents Windows from powering down USB ports.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool DisableUsbSelectiveSuspend(Action<string>? logCallback = null)
        {
            try
            {
                // Get the active power scheme GUID
                IntPtr activeSchemeGuidPtr;
                uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPtr);

                if (result != ERROR_SUCCESS)
                {
                    logCallback?.Invoke("PERIPHERAL: Failed to get active power scheme for USB Selective Suspend");
                    return false;
                }

                try
                {
                    // Create local copies of GUIDs for ref parameters
                    Guid subUsb = SUB_USB;
                    Guid usbSelectiveSuspend = USB_SELECTIVE_SUSPEND;

                    // Set USB Selective Suspend to 0 (Disabled) for AC power
                    result = PowerWriteACValueIndex(
                        IntPtr.Zero,
                        activeSchemeGuidPtr,
                        ref subUsb,
                        ref usbSelectiveSuspend,
                        0); // 0 = Disabled, 1 = Enabled

                    if (result != ERROR_SUCCESS)
                    {
                        logCallback?.Invoke($"PERIPHERAL: Failed to disable USB Selective Suspend (Error: {result})");
                        return false;
                    }

                    // Apply the changes to the active power scheme
                    result = PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuidPtr);

                    if (result == ERROR_SUCCESS)
                    {
                        logCallback?.Invoke("PERIPHERAL: USB Selective Suspend disabled");
                        return true;
                    }
                    else
                    {
                        logCallback?.Invoke($"PERIPHERAL: Failed to apply USB Selective Suspend changes (Error: {result})");
                        return false;
                    }
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
            catch (Exception ex)
            {
                logCallback?.Invoke($"PERIPHERAL: Error disabling USB Selective Suspend - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enables USB Selective Suspend (restores default behavior).
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool EnableUsbSelectiveSuspend(Action<string>? logCallback = null)
        {
            try
            {
                // Get the active power scheme GUID
                IntPtr activeSchemeGuidPtr;
                uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPtr);

                if (result != ERROR_SUCCESS)
                {
                    logCallback?.Invoke("PERIPHERAL: Failed to get active power scheme for USB Selective Suspend");
                    return false;
                }

                try
                {
                    // Create local copies of GUIDs for ref parameters
                    Guid subUsb = SUB_USB;
                    Guid usbSelectiveSuspend = USB_SELECTIVE_SUSPEND;

                    // Set USB Selective Suspend to 1 (Enabled) for AC power
                    result = PowerWriteACValueIndex(
                        IntPtr.Zero,
                        activeSchemeGuidPtr,
                        ref subUsb,
                        ref usbSelectiveSuspend,
                        1); // 0 = Disabled, 1 = Enabled

                    if (result != ERROR_SUCCESS)
                    {
                        logCallback?.Invoke($"PERIPHERAL: Failed to enable USB Selective Suspend (Error: {result})");
                        return false;
                    }

                    // Apply the changes to the active power scheme
                    result = PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuidPtr);

                    if (result == ERROR_SUCCESS)
                    {
                        logCallback?.Invoke("PERIPHERAL: USB Selective Suspend enabled (restored)");
                        return true;
                    }
                    else
                    {
                        logCallback?.Invoke($"PERIPHERAL: Failed to apply USB Selective Suspend changes (Error: {result})");
                        return false;
                    }
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
            catch (Exception ex)
            {
                logCallback?.Invoke($"PERIPHERAL: Error enabling USB Selective Suspend - {ex.Message}");
                return false;
            }
        }
    }
}
