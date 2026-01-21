# LAD App V2

**Laptop As Desktop** - A Windows utility that enables "Zero-Touch Wake" for docked laptops, allowing closed laptops to wake from sleep using external USB/Bluetooth keyboards and mice.

![Status](https://img.shields.io/badge/status-production--ready-brightgreen)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![License](https://img.shields.io/badge/license-MIT-green)

---

## 🎯 What It Does

LAD App automatically configures Windows power settings, display topology, and peripheral wake capabilities to make a docked laptop behave exactly like a desktop computer. When you connect an external monitor and AC power, the app:

- ✅ Sets lid close action to "Do Nothing" (allows laptop to stay on when closed)
- ✅ Forces External-Only display mode (prevents internal screen from waking)
- ✅ Enables wake from sleep for all USB/Bluetooth keyboards and mice
- ✅ Switches to High Performance power profile
- ✅ Disables hibernate timeout
- ✅ Optionally limits battery charge to 80% for health (Battery Health Guard)
- ✅ Automatically reverts all settings when undocked

**Result:** Close your laptop lid, dock it, and wake it with your external keyboard/mouse - just like a desktop!

---

## ✨ Features

### Core Functionality
- **Zero-Touch Wake** - Wake closed laptop with external peripherals
- **Intelligent Detection** - Automatically detects docked state (AC power + external monitor)
- **Automatic Configuration** - No manual setup required after first-run calibration
- **Safety First** - Emergency revert hotkey (`Ctrl+Shift+Alt+D`) and crash handler

### Advanced Features
- **Battery Health Guard** - 80% charge limiting for supported manufacturers (Lenovo, ASUS, Dell, HP)
- **Power Profile Switching** - Automatically switches to High Performance when docked
- **Hibernate Control** - Prevents unwanted hibernation during docked use
- **Modern Dashboard** - Real-time status monitoring with three status cards
- **Fan Monitoring** - WMI-based fan speed reading (when available)

### User Interface
- **System Tray Integration** - Runs quietly in the background
- **Dynamic Tray Icon** - Changes color (green) when LAD Ready
- **Modern Dark Theme** - Polished UI with rounded corners
- **Enhanced Tooltips** - Status information at a glance

---

## 📋 Requirements

- **Windows 10/11** (x64)
- **.NET 8.0 Runtime** (included in self-contained builds)
- **Administrator privileges** (required for power settings and device management)

---

## 🚀 Quick Start

### Option 1: Download Pre-built Release

1. Go to the [Releases](https://github.com/danackermannyc/lad-app/releases) page
2. Download the latest `LADApp.zip` or `LADApp.msix`
3. Extract and run `LADApp.exe` (requires Administrator)
4. Complete the first-run calibration wizard
5. Dock your laptop (connect external monitor + AC power)
6. Close the lid and test wake with external keyboard/mouse

### Option 2: Build from Source

```bash
# Clone the repository (replace YOUR_USERNAME with your GitHub username)
git clone https://github.com/YOUR_USERNAME/lad-app-v2.git
cd lad-app-v2

# Build
dotnet build -c Release

# Run
dotnet run --project LADApp.csproj

# Or publish self-contained
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**Output:** `bin/Release/net8.0-windows/win-x64/publish/LADApp.exe`

---

## 📖 Usage

### First Run

1. **Launch the app** (will prompt for Administrator privileges)
2. **Calibration Wizard** will guide you through:
   - Admin privilege verification
   - Wake device testing
   - Completion confirmation
3. The app will now run in the system tray

### Normal Operation

- **Dock your laptop:** Connect external monitor + AC power
- **LAD Ready:** Tray icon turns green, settings automatically configured
- **Close lid:** Laptop stays on, external display active
- **Wake from sleep:** Use external keyboard/mouse
- **Undock:** Settings automatically revert to normal

### System Tray Menu

Right-click the tray icon for:
- **Dashboard** - View real-time status
- **Battery Info** - Battery health and fan speeds
- **Battery Health Guard** - Toggle 80% charge limit
- **Quick Eject** - Manually revert all settings
- **Exit** - Close the app (settings auto-revert)

### Safety Hotkey

Press `Ctrl+Shift+Alt+D` at any time to:
- Revert display to Extended mode
- Useful if external display becomes unavailable

---

## 🔧 Configuration

Configuration is stored in: `%LocalAppData%\LADApp\config.json`

**Fields:**
- `batteryHealthGuardEnabled` - Enable/disable 80% charge limit
- `originalHibernateTimeout` - Stored for restoration
- `originalPowerSchemeGuid` - Stored for restoration

**Note:** Most settings are automatic and don't require manual configuration.

---

## 🛠️ Building from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (optional, for MSIX packaging)
- Windows 10/11 SDK (for MSIX packaging)

### Build Commands

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Self-contained publish (single file)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# With native libraries
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### MSIX Packaging (Optional)

See [docs/PHASE5_PACKAGING_GUIDE.md](docs/PHASE5_PACKAGING_GUIDE.md) for MSIX packaging instructions.

---

## 🐛 Troubleshooting

### App won't start
- **Check:** Running as Administrator? (Required for power settings)
- **Check:** .NET 8.0 Runtime installed? (Not needed for self-contained builds)

### Wake doesn't work
- **Check:** External monitor connected?
- **Check:** AC power connected?
- **Check:** Keyboard/mouse are USB or Bluetooth?
- **Try:** Run calibration wizard again from tray menu

### SmartScreen warning
- **Cause:** Unsigned executable (expected for open-source builds)
- **Solution:** Click "More info" → "Run anyway"
- **Note:** You can build from source to verify integrity

### Battery Health Guard not working
- **Check:** Manufacturer supported? (Lenovo, ASUS, Dell, HP)
- **Check:** WMI support available? (Check Dashboard → Health card)
- **Fallback:** Manual instructions provided in app

### Display issues
- **Use:** Safety hotkey `Ctrl+Shift+Alt+D` to revert display
- **Or:** Use Quick Eject from tray menu

---

## 📝 Known Limitations

- **WMI Fan Speed:** May not be available on all systems (shows "N/A" when unavailable)
- **Battery Health Guard:** WMI support varies by manufacturer and model
- **Power Schemes:** Custom manufacturer schemes are preserved and restored
- **Code Signing:** Pre-built releases are unsigned (build from source for verification)

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Areas for Contribution
- Hardware compatibility testing
- Additional manufacturer support for Battery Health Guard
- UI/UX improvements
- Documentation
- Bug reports and feature requests

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built with [.NET 8.0](https://dotnet.microsoft.com/)
- Uses Windows Power Management APIs
- WMI for battery and fan monitoring

---

## 📚 Documentation

- [Current Status](docs/CURRENT_STATUS.md) - Detailed project status
- [Product Requirements](docs/PRD.md) - Original requirements
- [Packaging Guide](docs/PHASE5_PACKAGING_GUIDE.md) - MSIX packaging
- [Calibration Wizard](docs/CALIBRATION_WIZARD.md) - First-run setup
- [Crash Handler Testing](docs/CRASH_HANDLER_STRESS_TEST.md) - Safety testing

---

## 🗺️ Roadmap

- [ ] Additional manufacturer support for Battery Health Guard
- [ ] Custom icon design
- [ ] Optional code signing for releases
- [ ] GitHub Actions for automated builds
- [ ] User guide and video tutorials

---

## ⚠️ Disclaimer

This utility modifies Windows power settings and display configuration. While it includes safety features (crash handler, emergency revert), use at your own risk. Always test on a system where you can recover if something goes wrong.

**The app automatically reverts all settings on exit or crash, but you should:**
- Test wake functionality before relying on it
- Keep the safety hotkey (`Ctrl+Shift+Alt+D`) in mind
- Have external monitor connected when testing

---

## 📞 Support

- **Issues:** [GitHub Issues](https://github.com/YOUR_USERNAME/lad-app-v2/issues) (replace YOUR_USERNAME)
- **Discussions:** [GitHub Discussions](https://github.com/YOUR_USERNAME/lad-app-v2/discussions) (replace YOUR_USERNAME)

---

**Made with ❤️ for the laptop-as-desktop community**
