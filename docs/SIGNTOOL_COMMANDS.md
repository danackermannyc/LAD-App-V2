# SignTool Commands for LAD App V2

## Quick Reference

### Prerequisites
- Code signing certificate (`.pfx` file)
- Certificate password
- SignTool (included with Windows SDK)

---

## Sign EXE (Standalone)

```powershell
# Navigate to publish directory
cd "c:\Users\Lenovo\Documents\LAD App V2\bin\Release\net8.0-windows\win-x64\publish"

# Sign the EXE
signtool sign /f "C:\Path\To\Your\Certificate.pfx" /p "YourCertificatePassword" /t "http://timestamp.digicert.com" /fd SHA256 /td SHA256 "LADApp.exe"
```

---

## Sign MSIX Package

```powershell
# Navigate to MSIX package directory
cd "c:\Users\Lenovo\Documents\LAD App V2\LADApp.Package\bin\Release\AppxPackages"

# Sign the MSIX
signtool sign /f "C:\Path\To\Your\Certificate.pfx" /p "YourCertificatePassword" /t "http://timestamp.digicert.com" /fd SHA256 /td SHA256 "LADApp_1.0.0.0_x64.msix"
```

---

## Verify Signature

```powershell
# Verify EXE
signtool verify /pa /v "LADApp.exe"

# Verify MSIX
signtool verify /pa /v "LADApp_1.0.0.0_x64.msix"
```

---

## Timestamp Servers (2026 Standards)

Use SHA-256 compatible timestamp servers:

- **DigiCert (Recommended):** `http://timestamp.digicert.com`
- **Sectigo (Comodo):** `http://timestamp.sectigo.com`
- **GlobalSign:** `http://timestamp.globalsign.com/tsa/r6advanced1`
- **SSL.com:** `http://timestamp.ssl.com`

---

## Command Parameters

| Parameter | Description |
|-----------|-------------|
| `/f` | Path to certificate file (.pfx) |
| `/p` | Certificate password |
| `/t` | Timestamp server URL |
| `/fd SHA256` | File digest algorithm (SHA-256) |
| `/td SHA256` | Timestamp digest algorithm (SHA-256) |
| `/pa` | Verify all signatures |
| `/v` | Verbose output |

---

## Example with Variables

```powershell
# Set variables
$certPath = "C:\Certificates\LADApp.pfx"
$certPassword = "YourPassword123"
$timestampServer = "http://timestamp.digicert.com"
$exePath = "LADApp.exe"

# Sign
signtool sign /f $certPath /p $certPassword /t $timestampServer /fd SHA256 /td SHA256 $exePath

# Verify
signtool verify /pa /v $exePath
```

---

## Troubleshooting

### Error: "No certificates were found"
- Verify certificate file path is correct
- Check certificate password
- Ensure certificate is valid and not expired

### Error: "Timestamp server could not be reached"
- Check internet connection
- Try alternative timestamp server
- Verify timestamp server URL is correct

### Error: "The file is being used by another process"
- Close any running instances of LADApp.exe
- Close Visual Studio or other applications using the file

---

*Last Updated: January 2026*
