# Calibration Wizard Implementation

## Overview
The Calibration Wizard is a first-run setup tool that guides users through configuring LAD App for their specific hardware setup. It enables device selection, wake testing, and configuration persistence.

---

## Features

### 1. Administrator Privilege Check
- **Step 1**: Checks if the app is running with Administrator privileges
- Displays clear status indicator (green for admin, orange for non-admin)
- Provides instructions on why Administrator privileges are needed

### 2. Device Selection
- **Step 2**: Lists all detected HID devices (keyboards and mice)
- Shows device names and Device Instance Paths for identification
- Separate dropdowns for keyboard and mouse selection
- "Refresh Device List" button to re-scan for devices
- Validation ensures both devices are selected before proceeding

### 3. Wake Test
- **Step 3**: Tests wake functionality with selected devices
- Enables wake for selected devices
- Puts system to sleep for user testing
- Provides clear instructions and safety warnings
- Status feedback on test completion

### 4. Completion
- **Step 4**: Summary of configuration
- Displays selected devices
- Confirms settings are saved
- Provides next steps information

---

## Implementation Details

### Files Created/Modified

1. **CalibrationWizard.cs** (NEW)
   - Complete wizard form with 4-step flow
   - Windows Forms implementation matching existing UI style
   - Device enumeration and selection logic
   - Wake testing functionality

2. **AppConfig.cs** (MODIFIED)
   - Added `SelectedKeyboardInstancePath` property
   - Added `SelectedMouseInstancePath` property
   - Stores selected devices for future use

3. **PeripheralWakeManager.cs** (MODIFIED)
   - Added `DeviceInfo` class for device details
   - Added `EnumerateDevicesForSelection()` method
   - Returns devices with names, instance paths, and type information

4. **MainForm.cs** (MODIFIED)
   - Integrated wizard to show on first run
   - Added "Calibration Wizard..." menu item to tray icon
   - Handles wizard completion and config reload

---

## User Flow

1. **First Launch**
   - App detects `FirstRun = true` in config
   - Wizard automatically appears after MainForm is shown
   - User goes through 4 steps

2. **Device Selection**
   - User sees list of all keyboards and mice
   - Selects primary keyboard and mouse
   - Can refresh list if devices are connected/disconnected

3. **Wake Test** (Optional but Recommended)
   - User confirms they're ready to test
   - System is put to sleep
   - User wakes system with selected device
   - Test completion is recorded

4. **Completion**
   - Configuration is saved
   - `FirstRun` flag is set to `false`
   - Selected device instance paths are stored
   - App continues with normal operation

5. **Re-running Wizard**
   - User can access wizard from tray icon menu
   - "Calibration Wizard..." option available anytime
   - Useful for changing device selection or re-testing

---

## Configuration Storage

The wizard saves the following to `%LocalAppData%\LADApp\config.json`:

```json
{
  "firstRun": false,
  "lastVersion": null,
  "selectedKeyboardInstancePath": "USB\\VID_046D&PID_C534\\5&3afd18bd&0&5",
  "selectedMouseInstancePath": "USB\\VID_046D&PID_C52B\\6&1a2b3c4d&0&6"
}
```

---

## Device Enumeration

The `EnumerateDevicesForSelection()` method:
- Scans all HID devices using SetupAPI
- Retrieves Device Instance Paths (most reliable identifier)
- Gets device names (manufacturer + product)
- Determines device type (keyboard/mouse) using heuristics
- Returns `List<DeviceInfo>` for wizard display

### DeviceInfo Class
```csharp
public class DeviceInfo
{
    public string Name { get; set; }              // Display name
    public string InstancePath { get; set; }      // Unique identifier
    public string DeviceDescription { get; set; } // Windows description
    public bool IsKeyboard { get; set; }          // Device type
    public bool IsMouse { get; set; }             // Device type
    public ushort VendorID { get; set; }          // USB VID
    public ushort ProductID { get; set; }          // USB PID
}
```

---

## Wake Test Implementation

The wake test:
1. Validates device selection
2. Shows confirmation dialog with safety warnings
3. Enables wake for selected devices (currently enables all devices)
4. Puts system to sleep using `Application.SetSuspendState()`
5. User wakes system with selected device
6. Records test completion status

**Note**: The wake test currently enables wake for all keyboards/mice. Future enhancement could enable wake only for selected devices using their Instance Paths.

---

## Safety Features

1. **Validation**: Requires both keyboard and mouse selection before proceeding
2. **Confirmation**: Wake test requires explicit user confirmation
3. **Safety Warnings**: Clear instructions about lid position and monitor connection
4. **Error Handling**: Graceful error handling with user-friendly messages
5. **Cancel Option**: User can cancel at any time (with confirmation)

---

## Future Enhancements

1. **Selective Wake Enablement**: Enable wake only for selected devices (not all devices)
2. **Device Type Detection**: More accurate keyboard/mouse detection using HID usage pages
3. **Multiple Device Support**: Allow selection of multiple keyboards/mice
4. **Wake Test Automation**: Automated wake test that doesn't require manual intervention
5. **BIOS Check**: Detect and warn about BIOS USB wake settings
6. **Device Persistence**: Remember devices across USB port changes

---

## Testing Checklist

- [ ] First run shows wizard automatically
- [ ] Administrator check displays correct status
- [ ] Device list populates with connected devices
- [ ] Device selection works correctly
- [ ] Refresh button updates device list
- [ ] Wake test puts system to sleep
- [ ] Selected devices can wake the system
- [ ] Configuration saves correctly
- [ ] Wizard can be re-opened from tray menu
- [ ] Wizard doesn't show after first run completion
- [ ] Error handling works for edge cases

---

## Notes

- The wizard uses Windows Forms to match the existing UI style
- Device Instance Paths are used for reliable device identification
- Configuration is saved immediately upon wizard completion
- The wizard is non-blocking - app continues to run in background
- All wizard operations are logged to the Status Log window
