# GitHub Release Checklist

Quick checklist for releasing LAD App V2 as an open-source project.

---

## ✅ Pre-Release

- [ ] Update README.md with your GitHub username
- [ ] Review all documentation for accuracy
- [ ] Test build from clean clone
- [ ] Verify .gitignore excludes sensitive files
- [ ] Check LICENSE file has correct year/name

---

## 🚀 Initial GitHub Setup

### 1. Create Repository

- [ ] Create new GitHub repository
- [ ] Name: `lad-app-v2` (or your choice)
- [ ] Description: "Windows utility for Zero-Touch Wake on docked laptops"
- [ ] Visibility: **Public**
- [ ] **Don't** initialize with README (you have one)

### 2. Push Code

```bash
# In your project directory
git init
git add .
git commit -m "Initial release: LAD App V2"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/lad-app-v2.git
git push -u origin main
```

- [ ] Code pushed successfully
- [ ] All files visible on GitHub

### 3. Repository Settings

- [ ] Enable **Issues**
- [ ] Enable **Discussions**
- [ ] Add repository topics:
  - `windows`
  - `csharp`
  - `dotnet`
  - `laptop`
  - `power-management`
  - `system-utility`
- [ ] Add repository description
- [ ] Set up branch protection (optional)

---

## 📦 First Release

### 1. Build Release

```bash
# Build self-contained release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

- [ ] Build successful
- [ ] Test the built EXE
- [ ] Create ZIP file: `LADApp.zip` containing:
  - `LADApp.exe`
  - `README.md` (optional, but helpful)

### 2. Create GitHub Release

- [ ] Go to **Releases** → **Create a new release**
- [ ] **Tag:** `v1.0.0`
- [ ] **Title:** `v1.0.0 - Initial Release`
- [ ] **Description:** Copy from template in `docs/OPEN_SOURCE_RELEASE.md`
- [ ] **Attach:** `LADApp.zip`
- [ ] **Publish release**

---

## 📝 Post-Release

### Immediate

- [ ] Verify release is visible
- [ ] Test download link works
- [ ] Update README if needed (fix any placeholder URLs)

### Within First Week

- [ ] Share on social media (if desired)
- [ ] Post on relevant subreddits
- [ ] Respond to any issues/questions
- [ ] Monitor for feedback

### Ongoing

- [ ] Respond to issues promptly
- [ ] Review pull requests
- [ ] Update documentation based on feedback
- [ ] Plan next release

---

## 🎯 Quick Commands Reference

```bash
# Build release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Create ZIP (PowerShell)
Compress-Archive -Path "bin\Release\net8.0-windows\win-x64\publish\*" -DestinationPath "LADApp-v1.0.0.zip"

# Tag release
git tag v1.0.0
git push origin v1.0.0
```

---

## 📋 Files to Review Before Release

- [ ] `README.md` - All placeholder URLs updated?
- [ ] `LICENSE` - Correct year and name?
- [ ] `.gitignore` - Excludes logs and config?
- [ ] `CONTRIBUTING.md` - Guidelines clear?
- [ ] `docs/OPEN_SOURCE_RELEASE.md` - Review guide

---

## ⚠️ Common Issues

### "Repository not found"
- Check repository name matches
- Verify repository is public
- Check GitHub username is correct

### "Build fails"
- Ensure .NET 8.0 SDK is installed
- Check all dependencies are in `.csproj`
- Try clean build: `dotnet clean && dotnet build`

### "Release not showing"
- Check tag was pushed: `git push origin v1.0.0`
- Verify release is published (not draft)
- Check GitHub Actions (if using) completed

---

## 🎉 You're Ready!

Once all checkboxes are complete, your project is ready for the open-source community!

---

*Last Updated: January 2026*
