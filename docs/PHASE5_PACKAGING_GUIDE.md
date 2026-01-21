# Phase 5: Release and Packaging Guide

## Overview
This guide covers the packaging and signing process for LAD App V2 to create a professional, signed installer that doesn't trigger SmartScreen warnings.

---

## ✅ Completed Setup

### 1. Administrator Manifest
- **File:** `app.manifest`
- **Status:** ✅ Created and configured
- **Execution Level:** `requireAdministrator`
- **Result:** App will prompt for UAC immediately on launch

### 2. Final BIOS Failsafe
- **File:** `Program.cs`
- **Status:** ✅ Enhanced with `FinalBiosFailsafe()` method
- **Functionality:** 
  - Directly reverts Lid Policy to Sleep
  - Directly restores Display to Extended mode
  - Runs independently of MainForm instance
  - Executes before MainForm cleanup in crash handler

### 3. MSIX Packaging Project
- **Location:** `LADApp.Package\`
- **Status:** ✅ Created
- **Project Files:**
  - `LADApp.Package.csproj` - Packaging project
  - `Package.appxmanifest` - MSIX manifest
  - `Images\` - Placeholder directory for app icons

---

## 📋 Next Steps

### Step 1: Create App Icons

The MSIX package requires several icon images. Create or obtain the following images in the `LADApp.Package\Images\` directory:

- **SplashScreen.png** - 620x300 pixels
- **LockScreenLogo.png** - 24x24 pixels
- **Square150x150Logo.png** - 150x150 pixels
- **Square44x44Logo.png** - 44x44 pixels
- **StoreLogo.png** - 50x50 pixels
- **Wide310x150Logo.png** - 310x150 pixels

**Note:** You can use a single icon image and resize it for all sizes, or create placeholder images for now.

### Step 2: Update Package Identity

Edit `LADApp.Package\Package.appxmanifest`:

1. **Update Publisher:**
   ```xml
   <Identity
     Name="LADApp"
     Publisher="CN=YourPublisherName"  <!-- Change this to your actual publisher name -->
     Version="1.0.0.0" />
   ```

2. **Publisher Name Format:**
   - For code signing certificate: Use the certificate's subject name
   - Example: `CN=Your Company Name, O=Your Company, L=City, S=State, C=US`
   - For testing: `CN=YourName` (will need to be updated for production)

### Step 3: Build the MSIX Package

#### Option A: Using Visual Studio (Recommended)
1. Open `LADApp.sln` in Visual Studio 2022
2. Right-click `LADApp.Package` project
3. Select **Publish** → **Create App Packages...**
4. Choose **Sideloading** (not Microsoft Store)
5. Select **x64** architecture
6. Choose output location
7. Click **Create**

#### Option B: Using Command Line
```powershell
cd "c:\Users\Lenovo\Documents\LAD App V2"
dotnet publish LADApp.Package\LADApp.Package.csproj -c Release -r win-x64
```

**Output Location:** `LADApp.Package\bin\Release\net8.0-windows10.0.17763.0\AppX\`

---

## 🔐 Code Signing with SignTool

### Prerequisites

1. **Obtain a Code Signing Certificate:**
   - Purchase from a trusted Certificate Authority (CA) like:
     - DigiCert
     - Sectigo (formerly Comodo)
     - GlobalSign
     - SSL.com
   - Or use a self-signed certificate for testing (will still trigger SmartScreen)

2. **Certificate Format:**
   - `.pfx` file (Personal Information Exchange)
   - Password-protected
   - Contains both public and private key

### SignTool Commands

#### Sign the EXE (Standalone)

```powershell
# Navigate to the directory containing the EXE
cd "c:\Users\Lenovo\Documents\LAD App V2\bin\Release\net8.0-windows\win-x64\publish"

# Sign the EXE
signtool sign /f "C:\Path\To\Your\Certificate.pfx" /p "YourCertificatePassword" /t "http://timestamp.digicert.com" /fd SHA256 /td SHA256 "LADApp.exe"
```

**Parameters:**
- `/f` - Path to certificate file (.pfx)
- `/p` - Certificate password
- `/t` - Timestamp server URL (DigiCert for 2026 standards)
- `/fd SHA256` - File digest algorithm (SHA-256)
- `/td SHA256` - Timestamp digest algorithm (SHA-256)

#### Sign the MSIX Package

```powershell
# Navigate to the MSIX package directory
cd "c:\Users\Lenovo\Documents\LAD App V2\LADApp.Package\bin\Release\net8.0-windows10.0.17763.0\AppX"

# Sign the MSIX
signtool sign /f "C:\Path\To\Your\Certificate.pfx" /p "YourCertificatePassword" /t "http://timestamp.digicert.com" /fd SHA256 /td SHA256 "LADApp_1.0.0.0_x64.msix"
```

#### Alternative Timestamp Servers (2026 Standards)

If DigiCert timestamp server is unavailable, use one of these:

- **DigiCert:** `http://timestamp.digicert.com`
- **Sectigo (Comodo):** `http://timestamp.sectigo.com`
- **GlobalSign:** `http://timestamp.globalsign.com/tsa/r6advanced1`
- **SSL.com:** `http://timestamp.ssl.com`

**Note:** Always use SHA-256 (`/fd SHA256 /td SHA256`) for 2026 standards. SHA-1 is deprecated.

### Verify Signing

After signing, verify the signature:

```powershell
# Verify EXE signature
signtool verify /pa /v "LADApp.exe"

# Verify MSIX signature
signtool verify /pa /v "LADApp_1.0.0.0_x64.msix"
```

**Parameters:**
- `/pa` - Verify all signatures
- `/v` - Verbose output

---

## 📦 Distribution

### MSIX Installation

Users can install the MSIX package by:

1. **Double-clicking** the `.msix` file
2. Windows will prompt for administrator privileges
3. Click **Install** when prompted
4. The app will be installed and available in Start Menu

### EXE Distribution

For standalone EXE distribution:

1. **Zip the entire publish folder** including:
   - `LADApp.exe` (signed)
   - All DLL dependencies
   - `app.manifest`
   - Runtime files

2. **Or create an installer** using:
   - Inno Setup
   - WiX Toolset
   - Advanced Installer
   - NSIS

---

## 🔍 Troubleshooting

### SmartScreen Still Appears

Even with code signing, SmartScreen may appear if:
- Certificate is new (needs reputation building)
- Certificate is self-signed
- File is not widely distributed

**Solutions:**
- Use a certificate from a trusted CA
- Build reputation over time with downloads
- Submit to Windows Defender SmartScreen (requires reputation)

### MSIX Build Fails

**Error:** "The Windows SDK version X was not found"

**Solution:**
- Install Windows 10 SDK (version 10.0.17763.0 or later)
- Or update `TargetPlatformMinVersion` in `LADApp.Package.csproj`

**Error:** "Package.appxmanifest validation failed"

**Solution:**
- Ensure all required images exist in `Images\` directory
- Verify Publisher name matches certificate subject
- Check XML syntax in `Package.appxmanifest`

### Signing Fails

**Error:** "SignTool Error: No certificates were found"

**Solution:**
- Verify certificate file path is correct
- Check certificate password
- Ensure certificate is valid and not expired

**Error:** "SignTool Error: The specified timestamp server either could not be reached"

**Solution:**
- Check internet connection
- Try alternative timestamp server
- Verify timestamp server URL is correct

---

## 📝 Checklist Before Release

- [ ] All app icons created and placed in `Images\` directory
- [ ] Publisher name updated in `Package.appxmanifest`
- [ ] Version number set correctly
- [ ] Code signing certificate obtained
- [ ] EXE signed with SignTool
- [ ] MSIX package built successfully
- [ ] MSIX package signed with SignTool
- [ ] Signatures verified with `signtool verify`
- [ ] Test installation on clean Windows machine
- [ ] Test UAC prompt appears on launch
- [ ] Test crash handler with Final BIOS Failsafe
- [ ] Documentation updated

---

## 🎯 Production Release

### Version Management

Update version numbers in:
1. `LADApp.Package\Package.appxmanifest` - `<Identity Version="X.X.X.X" />`
2. `LADApp.Package\LADApp.Package.csproj` - `<Version>X.X.X.X</Version>`
3. `LADApp.Package\LADApp.Package.csproj` - `<PackageVersion>X.X.X.X</PackageVersion>`

### Release Notes

Create a `RELEASE_NOTES.md` file documenting:
- Version number
- New features
- Bug fixes
- Known issues
- System requirements

---

## 📚 Additional Resources

- [MSIX Packaging Documentation](https://docs.microsoft.com/en-us/windows/msix/)
- [SignTool Documentation](https://docs.microsoft.com/en-us/windows/win32/seccrypto/signtool)
- [Code Signing Best Practices](https://docs.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)
- [Windows Application Packaging Project](https://docs.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net)

---

*Last Updated: January 2026 - Phase 5 Release and Packaging*
