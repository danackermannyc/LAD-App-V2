# Contributing to LAD App V2

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing to the project.

---

## 🤝 How to Contribute

### Reporting Bugs

1. **Check existing issues** - Your bug might already be reported
2. **Create a new issue** with:
   - Clear title and description
   - Steps to reproduce
   - Expected vs actual behavior
   - System information (Windows version, laptop model)
   - Relevant logs (`session_log.txt`, `crash_log.txt`)

### Suggesting Features

1. **Check existing discussions** - Feature might already be suggested
2. **Create a feature request** with:
   - Clear description of the feature
   - Use case and motivation
   - Potential implementation approach (if you have ideas)

### Code Contributions

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Make your changes**
4. **Test thoroughly** (see Testing section)
5. **Commit with clear messages** (see Commit Guidelines)
6. **Push to your fork** (`git push origin feature/amazing-feature`)
7. **Open a Pull Request**

---

## 📝 Code Style Guidelines

### C# Style

- Follow existing code style in the project
- Use meaningful variable and method names
- Add XML documentation comments for public methods
- Keep methods focused and single-purpose
- Use `nullable` annotations appropriately

### Formatting

- Use 4 spaces for indentation (not tabs)
- Use `var` for local variables when type is obvious
- Use explicit types for public APIs
- Follow existing brace style (opening brace on same line)

### Example

```csharp
/// <summary>
/// Sets the Lid Close Action to "Sleep" when on AC power.
/// </summary>
/// <returns>True if successful, false otherwise</returns>
public bool SetLidCloseSleep()
{
    try
    {
        // Implementation
        return true;
    }
    catch
    {
        return false;
    }
}
```

---

## 🧪 Testing

### Before Submitting

1. **Build in Release mode** - Ensure no build errors
2. **Test core functionality:**
   - LAD Ready detection
   - Lid policy changes
   - Display topology switching
   - Wake functionality (if possible)
3. **Test edge cases:**
   - Undocking while app is running
   - Crash handler (if modifying critical code)
   - Multiple monitor scenarios
4. **Check resource usage:**
   - CPU usage should remain < 1%
   - Memory usage should remain < 50MB

### Testing Checklist

- [ ] App starts without errors
- [ ] LAD Ready detection works (AC + Monitor)
- [ ] Settings revert on undock
- [ ] Crash handler works (if applicable)
- [ ] No memory leaks (run for extended period)
- [ ] No console errors or warnings

---

## 📋 Commit Guidelines

### Commit Message Format

```
<type>: <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Examples

```
feat: Add support for HP Battery Health Guard

Implemented WMI-based battery charge limiting for HP laptops
using HP_BIOSSetting WMI class.

Closes #42
```

```
fix: Restore display mode on crash

Enhanced Final BIOS Failsafe to restore Extended display mode
even when MainForm is not initialized.

Fixes #38
```

---

## 🏗️ Project Structure

### Core Components

- `Program.cs` - Entry point, crash handlers
- `MainForm.cs` - Main application logic
- `PowerManager.cs` - Power settings management
- `DisplayManager.cs` - Display topology control
- `PeripheralWakeManager.cs` - Wake device management
- `SystemMonitor.cs` - System state detection
- `BatteryHealthManager.cs` - Battery health features
- `AppConfig.cs` - Configuration management

### Adding New Features

1. **Create new manager class** (if needed) following existing patterns
2. **Add to MainForm** integration
3. **Update Dashboard** (if UI-related)
4. **Add configuration** fields to `AppConfig.cs`
5. **Update documentation**

---

## 🔍 Code Review Process

1. **All PRs require review** before merging
2. **Address feedback** promptly
3. **Keep PRs focused** - One feature/fix per PR
4. **Update documentation** if needed
5. **Test after addressing feedback**

---

## 🐛 Bug Fix Guidelines

### Critical Bugs (Priority)

- Crash handler failures
- Settings not reverting on exit
- Display mode not restoring
- Power settings not applying

### Important Bugs

- Wake functionality not working
- Battery Health Guard not working
- UI issues
- Performance problems

### Nice to Have

- Minor UI polish
- Documentation improvements
- Code cleanup

---

## 📚 Documentation

### When to Update Documentation

- Adding new features
- Changing behavior
- Fixing bugs that affect user experience
- Adding new configuration options

### Documentation Files

- `README.md` - Main project documentation
- `docs/CURRENT_STATUS.md` - Technical status
- `docs/PRD.md` - Product requirements
- Code comments - XML documentation

---

## 🎯 Areas for Contribution

### High Priority

- **Hardware Compatibility Testing**
  - Test on various laptop models
  - Verify WMI implementations
  - Report compatibility issues

- **Battery Health Guard**
  - Additional manufacturer support
  - WMI implementation improvements
  - Testing on real hardware

### Medium Priority

- **UI/UX Improvements**
  - Dashboard enhancements
  - Tray menu improvements
  - Icon design

- **Documentation**
  - User guides
  - Video tutorials
  - Troubleshooting guides

### Low Priority

- **Code Quality**
  - Refactoring
  - Performance optimizations
  - Test coverage

---

## ❓ Questions?

- **Open a Discussion** on GitHub
- **Ask in Issues** (tag with `question`)
- **Check existing documentation**

---

## 📜 License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing to LAD App V2!** 🎉
