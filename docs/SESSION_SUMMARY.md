# LAD App V2 - Session Summary for New Instance

## Project Overview

**LAD (Laptop As Desktop)** is a Windows system utility that enables "Zero-Touch Wake" - allowing closed laptops to wake from sleep using external USB/Bluetooth keyboards and mice, eliminating the need to open the lid.

**Tech Stack:** C# / .NET 8.0 / Windows Forms / WPF (for future UI)

**Key Requirement:** < 1% CPU, < 50MB RAM performance target

---

## Current Status: Phase 2 Complete ✅

### Phase 1 (Complete)
- ✅ Core Detection System (AC Power + External Monitor)
- ✅ Intelligent Lid Policy (Do Nothing when docked, Sleep when mobile)
- ✅ Display Topology Lock (External-Only mode)
- ✅ Safety Hotkey (Ctrl+Shift+Alt+D for display revert)
- ✅ Global Crash Handler with emergency cleanup

### Phase 2 (Complete)
- ✅ Device Instance Path upgrade for reliable wake
- ✅ Performance Heartbeat (5-minute CPU/RAM logging)
- ✅ Configuration system with First Run flag
- ✅ Simplified Calibration Wizard (3 steps: Welcome, Wake Test, Completion)

---

## Architecture Overview

### Core Components

1. **MainForm.cs** - Main application form (hidden, runs in system tray)
   - Manages all timers and system monitoring
   - Handles LAD Ready state detection
   - Integrates all managers

2. **SystemMonitor.cs** - Monitors power and display state
   - Tracks AC/Battery power status
   - Detects external monitors
   - Fires events on state changes

3. **PowerManager.cs** - Manages Windows power settings
   - Sets Lid Close Action via `PowerWriteACValueIndex`
   - Uses `PowerSetActiveScheme` to apply changes

4. **PeripheralWakeManager.cs** - Manages USB/Bluetooth wake
   - **Uses Device Instance Paths** (primary method) for reliable identification
   - Falls back to Device Description, then Device Name
   - Enables wake for all keyboards/mice automatically
   - Disables USB Selective Suspend

5. **DisplayManager.cs** - Controls display topology
   - Forces External-Only mode when LAD Ready
   - Restores Extended mode when mobile

6. **AppConfig.cs** - Configuration management
   - JSON-based config in `%LocalAppData%\LADApp\config.json`
   - Stores: `FirstRun`, `LastVersion`, `SelectedKeyboardInstancePath`, `SelectedMouseInstancePath`
   - Note: Device selection was removed from wizard, but config fields remain

7. **CalibrationWizard.cs** - First-run setup wizard
   - Step 1: Administrator privilege check
   - Step 2: Wake test (optional)
   - Step 3: Completion summary
   - Shows automatically on first run (via timer)

8. **StatusLogWindow.cs** - Debug/logging window
   - Shows all app activity
   - Accessible from tray icon

---

## Key Technical Details

### Device Instance Path Implementation
- Uses `CM_Get_Device_ID` from `cfgmgr32.dll`
- Device Instance Paths are unique, persistent identifiers (e.g., `USB\VID_046D&PID_C534\5&3afd18bd&0&5`)
- Three-tier fallback: Instance Path → Device Description → Device Name
- All methods logged for debugging

### Performance Monitoring
- CPU usage calculated via `Process.TotalProcessorTime` delta
- Accounts for multi-core systems (divides by `Environment.ProcessorCount`)
- Memory usage via `Process.WorkingSet64`
- Logs every 5 minutes with OK/WARNING status

### Configuration System
- Location: `%LocalAppData%\LADApp\config.json`
- Auto-creates directory if needed
- Graceful error handling (non-critical for app operation)
- `FirstRun` flag triggers wizard on startup

### Calibration Wizard
- Shows automatically on first run (1-second delay timer)
- Can be manually opened from tray menu
- Simplified to 3 steps (device selection removed due to enumeration issues)
- App enables wake for ALL devices automatically (no selection needed)

---

## Important Files

### Source Files
- `MainForm.cs` - Main application logic
- `SystemMonitor.cs` - Power/display monitoring
- `PowerManager.cs` - Power settings management
- `PeripheralWakeManager.cs` - Wake enablement (Device Instance Paths)
- `DisplayManager.cs` - Display topology control
- `AppConfig.cs` - Configuration management
- `CalibrationWizard.cs` - First-run wizard
- `StatusLogWindow.cs` - Logging window
- `Program.cs` - Entry point with crash handlers

### Documentation
- `docs/PRD.md` - Product Requirements Document (source of truth)
- `docs/PHASE2_UPGRADE_SUMMARY.md` - Phase 2 implementation details
- `docs/CALIBRATION_WIZARD.md` - Wizard documentation
- `.cursor/skills/lad-v2-rules/SKILL.md` - Project rules

---

## Known Issues / Design Decisions

### Device Enumeration Removed
- Original plan: User selects keyboard/mouse in wizard
- **Decision:** Removed device selection step
- **Reason:** Device enumeration had path extraction issues (garbled paths, Win32 errors)
- **Solution:** App enables wake for ALL keyboards/mice automatically
- **Result:** Simpler UX, aligns with actual app behavior

### Device Instance Path Extraction
- Fixed structure layout issue (64-bit: 8-byte cbSize, 32-bit: 4-byte)
- Uses `Marshal.PtrToStringUni` for Unicode device paths
- Still has issues in enumeration method, but works in `EnableWakeForKeyboardsAndMice`

### Performance Heartbeat
- Uses lightweight `TotalProcessorTime` method
- No external dependencies (no PerformanceCounter)
- Accurate enough for background service monitoring

---

## Next Steps: Phase 3

**Goal:** Build System Tray "Command Center" showing internal thermals and battery health

### Proposed Features
1. Enhanced tray icon tooltip with:
   - CPU temperature
   - Battery percentage/status
   - LAD Ready status
   - Current power state

2. Expanded context menu with:
   - Battery info submenu
   - Thermal status
   - Quick stats display

3. Optional visual indicators:
   - Icon color changes based on battery/temp
   - Status badges

### Implementation Notes
- Use WMI for CPU temperature (may require admin)
- Use `SystemInformation.PowerStatus` for battery (already available)
- Keep polling lightweight (30-60 second intervals)
- Maintain < 1% CPU, < 50MB RAM target

---

## Testing Status

### Verified Working
- ✅ App starts and runs in system tray
- ✅ LAD Ready detection (AC + Monitor)
- ✅ Lid Policy changes automatically
- ✅ Display topology switching
- ✅ Calibration Wizard appears on first run
- ✅ Performance Heartbeat logging
- ✅ Configuration persistence
- ✅ Safety hotkey (Ctrl+Shift+Alt+D)
- ✅ Crash handler emergency cleanup

### Needs Testing
- ⚠️ Wake functionality (requires sleep test)
- ⚠️ Device Instance Path reliability (works in EnableWake, but enumeration has issues)
- ⚠️ Performance targets under extended load

---

## Code Patterns & Conventions

### Error Handling
- All operations wrapped in try-catch
- Logging via `LogToStatusWindow()` callback pattern
- Graceful degradation (app continues if non-critical operations fail)

### Logging
- All managers accept optional `Action<string>? logCallback`
- MainForm provides `LogToStatusWindow` method
- Status Log window buffers all messages

### Safety Features
- Emergency cleanup in crash handler
- Safety hotkey for display revert
- Quick Eject menu option
- All settings revert on app exit

### Timer Management
- `detectionTimer`: 2 seconds (LAD Ready state)
- `heartbeatTimer`: 30 seconds (power request)
- `performanceHeartbeatTimer`: 5 minutes (CPU/RAM logging)
- `wizardTimer`: 1 second (first-run wizard trigger)

---

## Configuration File Structure

```json
{
  "firstRun": false,
  "lastVersion": null,
  "selectedKeyboardInstancePath": null,
  "selectedMouseInstancePath": null
}
```

**Note:** Device selection fields exist but aren't used (device selection removed from wizard).

---

## Windows APIs Used

### Power Management
- `PowerWriteACValueIndex` - Set Lid Close Action
- `PowerSetActiveScheme` - Apply power scheme changes
- `PowerGetActiveScheme` - Get current power scheme
- `DevicePowerSetDeviceState` - Enable wake for devices

### Device Management
- `SetupDiGetClassDevs` - Get device information set
- `SetupDiEnumDeviceInterfaces` - Enumerate HID devices
- `SetupDiGetDeviceInterfaceDetail` - Get device path
- `CM_Get_Device_ID` - Get Device Instance Path
- `CreateFile` - Open device handle
- `HidD_GetAttributes` - Get HID device attributes

### Display Management
- `SetDisplayConfig` - Control display topology
- `EnumDisplayDevices` - Enumerate monitors

### System
- `SetThreadExecutionState` - Power request heartbeats
- `RegisterHotKey` / `UnregisterHotKey` - Global hotkeys
- `WM_POWERBROADCAST` - Power state change messages

---

## Important Notes for Next Session

1. **Device Selection Removed:** Don't try to fix device enumeration - it's intentionally removed. App works with all devices.

2. **Performance Target:** Always verify < 1% CPU, < 50MB RAM in Performance Heartbeat logs.

3. **Admin Privileges:** Many features require Administrator. Wizard checks this.

4. **Safety First:** All system changes must be reversible. Crash handler reverts everything.

5. **PRD is Source of Truth:** Refer to `docs/PRD.md` for requirements.

6. **Phase 3 Focus:** System Tray enhancements - keep it lightweight and performant.

---

## Quick Start for New Instance

1. Read `docs/PRD.md` for project vision
2. Review `MainForm.cs` to understand app flow
3. Check `PeripheralWakeManager.cs` for wake implementation
4. See `CalibrationWizard.cs` for wizard flow
5. Review `AppConfig.cs` for configuration structure

**Current Priority:** Phase 3 - System Tray Command Center (thermals + battery)

---

## Build & Run

- **Project:** `LADApp.csproj`
- **Target:** .NET 8.0 Windows
- **Output:** `bin\Debug\net8.0-windows\LADApp.exe`
- **Run as:** Administrator (required for full functionality)
- **Config:** `%LocalAppData%\LADApp\config.json`

---

*Last Updated: End of Phase 2 - Calibration Wizard Complete*
