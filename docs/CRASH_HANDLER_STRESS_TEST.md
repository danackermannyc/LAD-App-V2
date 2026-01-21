# Crash Handler Stress Test Guide

## Overview
This guide provides methods to test the crash handler to ensure it properly reverts system settings when the app encounters an error.

## ⚠️ IMPORTANT SAFETY NOTES
- **Test in a safe environment** - Ensure you can recover if something goes wrong
- **Have external monitor connected** - So you can see what's happening if internal display fails
- **Run as Administrator** - Required for system setting changes
- **Keep Status Log window open** - To monitor what's happening
- **Know your safety hotkey**: `Ctrl+Shift+Alt+D` - Use this to manually revert display if needed

---

## Method 1: Simple Null Reference Exception (Safest Test)

### Steps:
1. **Add a test menu item** to the tray menu (temporarily):
   - Open `MainForm.cs`
   - In the constructor, add after line 85:
     ```csharp
     trayMenu.Items.Add("TEST: Trigger Crash", null, OnTestCrash);
     ```
   - Add this method:
     ```csharp
     private void OnTestCrash(object? sender, EventArgs e)
     {
         throw new NullReferenceException("TEST: Intentional crash for crash handler testing");
     }
     ```

2. **Run the app** (as Administrator)

3. **Click "TEST: Trigger Crash"** from the tray menu

4. **Expected Results:**
   - ✅ Error message box appears: "LAD App has encountered an error..."
   - ✅ `crash_log.txt` file is created in the app folder
   - ✅ Status Log shows emergency cleanup messages (if window is open)
   - ✅ System settings are reverted:
     - Lid Close Action → Sleep
     - Display Topology → Extended mode
     - USB Selective Suspend → Enabled

5. **Verify Settings:**
   - Open Windows Power Settings → Additional Power Settings → Choose what closing the lid does
   - Verify "When I close the lid" is set to "Sleep" (not "Do Nothing")
   - Check that internal display is active (Extended mode)

6. **Check crash_log.txt:**
   - File should be in: `bin\Debug\net8.0-windows\crash_log.txt`
   - Should contain: Exception type, message, stack trace, timestamp

---

## Method 2: Timer-Based Crash (Tests Background Thread)

### Steps:
1. **Modify the heartbeat timer** (temporarily):
   - In `MainForm.cs`, find `HeartbeatTimer_Tick` method
   - Add this at the start:
     ```csharp
     private static int crashCounter = 0;
     private void HeartbeatTimer_Tick(object? sender, EventArgs e)
     {
         crashCounter++;
         if (crashCounter >= 3) // Crash after 3 heartbeats (~90 seconds)
         {
             throw new InvalidOperationException("TEST: Timer-based crash test");
         }
         // ... rest of method
     }
     ```

2. **Run the app** and wait ~90 seconds

3. **Expected Results:** Same as Method 1

---

## Method 3: Access Violation Simulation (Advanced)

### Steps:
1. **Add unsafe code** (requires `unsafe` keyword in project):
   - Add to `MainForm.cs`:
     ```csharp
     private void OnTestCrash(object? sender, EventArgs e)
     {
         IntPtr badPointer = new IntPtr(0x00000000);
         Marshal.ReadInt32(badPointer); // This will cause AccessViolationException
     }
     ```

2. **Note:** This may not be caught by managed exception handlers - Windows may terminate the process immediately

---

## Method 4: Stack Overflow (Tests Deep Recursion)

### Steps:
1. **Add recursive method**:
   ```csharp
   private void OnTestCrash(object? sender, EventArgs e)
   {
       CrashRecursive(0);
   }
   
   private void CrashRecursive(int depth)
   {
       if (depth > 10000) return; // Never reached
       CrashRecursive(depth + 1);
   }
   ```

2. **Note:** Stack overflow may terminate the process before the handler can run

---

## Method 5: Out of Memory (Tests Resource Exhaustion)

### Steps:
1. **Add memory allocation**:
   ```csharp
   private void OnTestCrash(object? sender, EventArgs e)
   {
       List<byte[]> memoryHog = new List<byte[]>();
       while (true)
       {
           memoryHog.Add(new byte[100000000]); // 100MB chunks
       }
   }
   ```

2. **Warning:** This will consume system memory - close other apps first

---

## Recommended Testing Sequence

### Phase 1: Basic Test (Method 1)
1. ✅ Test simple exception handling
2. ✅ Verify crash log creation
3. ✅ Verify settings reversion
4. ✅ Verify user alert

### Phase 2: Background Thread Test (Method 2)
1. ✅ Test exception handling from timer thread
2. ✅ Verify cleanup works from non-UI thread

### Phase 3: Stress Test (Optional)
- Only if Phase 1 and 2 pass
- Test with Methods 3-5 to see edge cases

---

## Verification Checklist

After each crash test, verify:

- [ ] **Error message box appeared** with correct text
- [ ] **crash_log.txt file exists** in app folder
- [ ] **crash_log.txt contains:**
  - [ ] Timestamp
  - [ ] Crash type
  - [ ] Exception message
  - [ ] Stack trace
- [ ] **Lid Close Action reverted** to "Sleep" (check Power Settings)
- [ ] **Display topology restored** (internal screen should be active)
- [ ] **USB Selective Suspend re-enabled** (check Power Settings → USB settings)
- [ ] **App terminated** (not hanging)

---

## What to Look For in crash_log.txt

```
================================================================================
[2024-01-15 22:30:45.123] CRASH DETECTED
================================================================================
Crash Type: Unhandled Thread Exception
Additional Info: Crash Type: Unhandled Thread Exception

Exception Type: System.NullReferenceException
Exception Message: TEST: Intentional crash for crash handler testing

Stack Trace:
   at LADApp.MainForm.OnTestCrash(Object sender, EventArgs e)
   at System.Windows.Forms.ToolStripItem.OnClick(EventArgs e)
   ...

================================================================================
```

---

## Troubleshooting

### Crash handler doesn't fire:
- Check that exception handlers are registered in `Program.cs` Main() method
- Verify `Application.SetUnhandledExceptionMode` is called before `Application.Run`

### Settings not reverted:
- Check if app is running as Administrator
- Verify `EmergencyRevertAllSettings()` method exists in MainForm
- Check crash_log.txt for cleanup errors

### crash_log.txt not created:
- Check app folder permissions
- Verify `AppDomain.CurrentDomain.BaseDirectory` path
- Check if antivirus is blocking file creation

---

## Cleanup After Testing

1. **Remove test code** from MainForm.cs:
   - Remove test menu item
   - Remove `OnTestCrash` method
   - Remove any crash counter variables

2. **Delete crash_log.txt** (or archive for reference)

3. **Rebuild the app** to ensure clean build

---

## Best Practices for Production

- ✅ Never leave test crash code in production builds
- ✅ Monitor crash_log.txt size (rotate if it gets too large)
- ✅ Consider adding telemetry to report crashes (optional)
- ✅ Test crash handler after major code changes

---

## Quick Test Command

For the fastest test, add this to a tray menu item:

```csharp
trayMenu.Items.Add("TEST CRASH", null, (s, e) => throw new Exception("Test crash"));
```

Then click it and verify all checklist items above.
