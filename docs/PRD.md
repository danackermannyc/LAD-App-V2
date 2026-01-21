1\. Project Master Document (The "Source of Truth")

Purpose: The high-level overview for stakeholders or for the AI to understand the "soul" of the project.

Product Overview

LAD (Laptop As Desktop) is a system utility designed to bridge the gap between portable computing and a seamless desktop experience. It automates the complex power, display, and peripheral configurations required to use a closed laptop in a vertical or horizontal stand without ever needing to touch the physical chassis.

It also provides at-a-glance access to the settings most laptop-as-desktop users would want, including key Windows 11 Display and Power settings. 

Target Audience

Primary: The Performance Gamer. Needs maximum thermal headroom and zero-latency wake-up for quick gaming sessions.

Secondary: The Creative Pro. Needs reliable multi-monitor desk setups where the laptop acts solely as a "brain," often tucked away behind other displays.

The "Killer Feature": Zero-Touch Wake

The core requirement is a Reliable Wake Protocol. LAD must ensure that inputs from specific USB or Bluetooth IDs (the user's designated keyboard/mouse) bypass Windows' tendency to ignore peripherals during deep sleep states.



Field

Description

Project Name

LAD App (Laptop As Desktop)

Vision

To eliminate the "Clamshell Dance" for Windows users, making docked laptops behave exactly like dedicated desktops.

Core Problem

Windows laptops often fail to wake from sleep when the lid is closed, even with external peripherals connected. Users are forced to open the lid to press the power button, disrupting their workflow and display setup.

Target Audience

Primary: PC Gamers (High thermal/performance needs).



Secondary: Creative Pros (Reliable multi-monitor workflows).

Key Success Metric

100% reliable wake-up via external mouse/keyboard while the lid remains closed.



2\. Product Requirements Document (PRD)

Purpose: Defines exactly what we are building and why.

A. Functional Requirements

Zero-Touch Wake: System must wake from all sleep states (S0, S3, S4) via designated USB/Bluetooth peripherals.

Intelligent Lid Policy: Automatically set "Lid Close Action" to "Do Nothing" when AC + Monitor are detected; revert to "Sleep" when mobile.

Display Topology Lock: Force "External Display Only" mode upon docking to prevent the internal screen from waking up.

One-Click "Eject": A tray icon feature to instantly revert to mobile settings before physical unplugging.

B. Non-Functional Requirements

Performance: The background service must consume < 1% CPU and < 50MB RAM.

Stability: Must include a "BIOS Failsafe"—if the app crashes, system defaults should be restored.

Security: Must be digitally signed to avoid being flagged by Game Anti-Cheat (like Riot Vanguard).

3\. Technical Specification (The "Engine")

Purpose: Tells the AI how to build the logic using Windows APIs.

A. Power State Management

API: PowerWriteACValueIndex and PowerSetActiveScheme.

Logic: Detect power state changes via WM\_POWERBROADCAST. If AC + External Monitor = True, write 0 to the Lid Close Action registry key.

B. Peripheral Wake Hooks

API: DevicePowerSetDeviceState.

Implementation: Query all HID (Human Interface Devices) and ensure the flag DEVICEPOWER\_SET\_WAKEENABLED is set to TRUE for the user's mouse and keyboard.

C. Display Control

API: SetDisplayConfig.

Logic: When an external monitor is detected, set the topology to DISPLAYCONFIG\_TOPOLOGY\_EXTERNAL. This kills the internal panel signal completely.

4\. The Development Roadmap

Purpose: Keeps the project focused on an MVP (Minimum Viable Product) first.

Phase 1 (The Core): Build the background service that manages the Lid Policy and Wake-on-USB. (Current Goal).

Phase 2 (The Interface): Build the "First Run" Calibration Wizard to grant permissions and test the wake signal.

Phase 3 (The HUD): Build the System Tray "Command Center" showing internal thermals and battery health.

Phase 4 (The Polish): Add "Battery Health Guard" (limiting charge to 80%) and custom fan curves.

5\. Setup \& Troubleshooting Guide

Purpose: Handles the "edge cases" where Windows or the BIOS fights back.

USB Selective Suspend: The app must be able to disable this globally, otherwise Windows "mutes" the mouse while the laptop sleeps.

BIOS Lock: If software cannot change the lid policy, the app must provide instructions on how to toggle "Lid Switch" in the laptop's BIOS.





