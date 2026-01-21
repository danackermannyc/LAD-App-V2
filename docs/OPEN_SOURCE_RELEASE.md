# Open Source Release Guide

## Overview

This guide covers the steps to release LAD App V2 as an open-source project on GitHub, including what to prepare, what to include, and best practices.

---

## ✅ Pre-Release Checklist

### Code Preparation

- [x] Code is clean and well-commented
- [x] No hardcoded secrets or credentials
- [x] Configuration files are user-specific (not in repo)
- [x] Build instructions are clear
- [x] Dependencies are documented

### Documentation

- [x] README.md created with:
  - Project description
  - Features list
  - Installation instructions
  - Usage guide
  - Troubleshooting
- [x] LICENSE file (MIT recommended)
- [x] CONTRIBUTING.md guidelines
- [x] .gitignore configured

### GitHub Setup

- [ ] Create GitHub repository
- [ ] Add repository description and topics
- [ ] Set up repository settings:
  - Enable Issues
  - Enable Discussions
  - Enable Wiki (optional)
  - Set default branch protection rules

---

## 📦 What to Include in Repository

### Required Files

```
LAD App V2/
├── README.md                    # Main documentation
├── LICENSE                      # MIT License
├── CONTRIBUTING.md              # Contribution guidelines
├── .gitignore                   # Git ignore rules
├── LADApp.sln                   # Solution file
├── LADApp.csproj                # Main project
├── app.manifest                 # UAC manifest
├── Program.cs                   # Entry point
├── MainForm.cs                  # Main logic
├── [Other .cs files]           # Source code
├── docs/                        # Documentation
│   ├── CURRENT_STATUS.md
│   ├── PRD.md
│   └── ...
└── LADApp.Package/              # MSIX packaging (optional)
```

### Optional but Recommended

- `.github/` directory with:
  - `ISSUE_TEMPLATE/` - Issue templates
  - `PULL_REQUEST_TEMPLATE.md` - PR template
  - `workflows/` - GitHub Actions (CI/CD)
- `CHANGELOG.md` - Version history
- Screenshots in `docs/images/`

---

## 🚀 Initial Release Steps

### 1. Create GitHub Repository

1. Go to GitHub and create a new repository
2. Name: `lad-app-v2` (or your preferred name)
3. Description: "Windows utility for Zero-Touch Wake on docked laptops"
4. Visibility: Public (for open source)
5. **Don't** initialize with README (you already have one)

### 2. Prepare Local Repository

```bash
# Initialize git (if not already)
git init

# Add all files
git add .

# Initial commit
git commit -m "Initial release: LAD App V2 - Zero-Touch Wake utility"

# Add remote
git remote add origin https://github.com/yourusername/lad-app-v2.git

# Push to GitHub
git branch -M main
git push -u origin main
```

### 3. Create First Release

1. Go to **Releases** → **Create a new release**
2. **Tag:** `v1.0.0`
3. **Title:** `v1.0.0 - Initial Release`
4. **Description:** Use template below
5. **Attach files:**
   - `LADApp.zip` (self-contained build)
   - `LADApp.msix` (optional, if packaged)

### Release Notes Template

```markdown
## v1.0.0 - Initial Release

### Features
- Zero-Touch Wake functionality
- Automatic power settings configuration
- Battery Health Guard (Lenovo, ASUS, Dell, HP)
- Modern Dashboard UI
- System tray integration

### Requirements
- Windows 10/11 (x64)
- Administrator privileges
- .NET 8.0 Runtime (included in self-contained build)

### Installation
1. Download `LADApp.zip`
2. Extract and run `LADApp.exe` as Administrator
3. Complete first-run calibration wizard

### Notes
- This is the initial open-source release
- Pre-built binaries are unsigned (build from source to verify)
- See README.md for full documentation
```

---

## 📝 GitHub Repository Settings

### Repository Topics

Add these topics for discoverability:
- `windows`
- `csharp`
- `dotnet`
- `laptop`
- `power-management`
- `system-utility`
- `wake-on-usb`
- `desktop-docking`

### Repository Description

```
Windows utility that enables Zero-Touch Wake for docked laptops. Automatically configures power settings and display topology to make closed laptops behave like desktops.
```

### Social Preview

- Add a banner image (1280x640px recommended)
- Add a project logo/icon

---

## 🔄 Ongoing Maintenance

### Issue Management

- **Label issues** appropriately:
  - `bug` - Bugs
  - `enhancement` - Feature requests
  - `question` - Questions
  - `help wanted` - Good for contributors
  - `good first issue` - Beginner-friendly

### Release Process

1. **Create release branch:** `git checkout -b release/v1.1.0`
2. **Update version numbers:**
   - `LADApp.Package/Package.appxmanifest`
   - `LADApp.Package/LADApp.Package.csproj`
3. **Update CHANGELOG.md**
4. **Build release:**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```
5. **Create release on GitHub**
6. **Tag and push:** `git tag v1.1.0 && git push origin v1.1.0`

### Community Engagement

- Respond to issues promptly
- Review pull requests
- Engage in discussions
- Update documentation based on feedback

---

## 🔐 Code Signing (Optional)

For open-source projects, code signing is optional but can build trust:

### Options

1. **Self-Signed Certificate** (Free, but triggers SmartScreen)
   - Users can build from source to verify
   - Good for development/testing

2. **Community Certificate** (If project grows)
   - Can be funded by donations
   - Builds reputation over time

3. **Individual Signing** (If you have a certificate)
   - Sign releases yourself
   - Document in README

### Recommendation for Open Source

- **Don't sign initial releases** - Let users build from source
- **Document build process** clearly
- **Consider signing later** if project gains traction

---

## 📊 GitHub Actions (CI/CD) - Optional

Create `.github/workflows/build.yml`:

```yaml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Build
        run: dotnet build -c Release
      - name: Publish
        run: dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
      - name: Upload Artifacts
        uses: actions/upload-artifact@v3
        with:
          name: LADApp
          path: bin/Release/net8.0-windows/win-x64/publish/
```

---

## 🎯 Marketing Your Project

### Where to Share

- **Reddit:** r/Windows10, r/software, r/SideProject
- **Product Hunt:** Launch as a product
- **Hacker News:** Show HN post
- **Twitter/X:** Announce with screenshots
- **GitHub Explore:** Add to collections

### What to Highlight

- Solves a real problem (laptop-as-desktop)
- Open source and free
- Modern UI
- Safety features (crash handler, emergency revert)
- Active development

---

## 📈 Metrics to Track

- **Stars** - Project popularity
- **Forks** - Developer interest
- **Issues** - User engagement
- **Pull Requests** - Community contributions
- **Downloads** - Release usage

---

## ⚠️ Important Notes

### Security

- **Never commit:**
  - API keys
  - Passwords
  - Personal information
  - Certificate private keys

### Legal

- **License:** MIT is permissive and good for utilities
- **Copyright:** Include in LICENSE file
- **Third-party:** Document any third-party code/licenses

### User Expectations

- **Set clear expectations:**
  - "Use at your own risk"
  - "No warranty"
  - "Test before relying on it"

---

## 🎉 Success Criteria

Your open-source release is successful when:

- [ ] Repository is public and accessible
- [ ] README is clear and helpful
- [ ] Issues can be created
- [ ] First release is available
- [ ] Build instructions work
- [ ] At least one external user tries it
- [ ] You respond to issues/feedback

---

## 📚 Additional Resources

- [GitHub Open Source Guide](https://opensource.guide/)
- [Choose a License](https://choosealicense.com/)
- [Semantic Versioning](https://semver.org/)
- [Keep a Changelog](https://keepachangelog.com/)

---

*Last Updated: January 2026*
