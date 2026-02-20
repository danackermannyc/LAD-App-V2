# DPI Scaling - Quick Reference Guide

**Quick lookup for common DPI scaling patterns in LAD App**

---

## DpiHelper Cheat Sheet

### Basic Scaling

```csharp
// Scale a pixel value
int scaled = DpiHelper.Scale(100);

// Scale a Size
Size size = DpiHelper.Scale(new Size(800, 600));

// Scale a Point
Point point = DpiHelper.Scale(new Point(20, 20));

// Scale Padding
Padding padding = DpiHelper.Scale(new Padding(10, 20, 10, 20));

// Create DPI-aware font
Font font = DpiHelper.CreateFont("Segoe UI", 12, FontStyle.Bold);

// Get current scale factor
float factor = DpiHelper.GetScaleFactor(); // 1.0, 1.25, 1.5, 2.0, etc.
```

---

## Common Patterns

### Form Setup

```csharp
public MyForm()
{
    InitializeComponent();
    DpiHelper.ConfigureForm(this); // ← Add this line
}

private void InitializeComponent()
{
    this.Text = "My Form";
    this.Size = DpiHelper.Scale(new Size(800, 600));
    this.MinimumSize = DpiHelper.Scale(new Size(600, 400));
    this.FormBorderStyle = FormBorderStyle.Sizable; // Not FixedDialog!
    this.StartPosition = FormStartPosition.CenterScreen;
}
```

### Button

```csharp
Button button = new Button
{
    Text = "Click Me",
    Size = DpiHelper.Scale(new Size(120, 35)),
    Location = DpiHelper.Scale(new Point(20, 20)),
    Font = DpiHelper.CreateFont("Segoe UI", 9),
    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
};
```

### Label

```csharp
Label label = new Label
{
    Text = "Hello World",
    Font = DpiHelper.CreateFont("Segoe UI", 12, FontStyle.Bold),
    AutoSize = true,
    Location = DpiHelper.Scale(new Point(20, 20)),
    ForeColor = Color.White
};
```

### Panel

```csharp
Panel panel = new Panel
{
    Size = DpiHelper.Scale(new Size(220, 180)),
    Margin = DpiHelper.Scale(new Padding(10)),
    Padding = DpiHelper.Scale(new Padding(15)),
    BackColor = Color.FromArgb(45, 45, 45),
    Dock = DockStyle.Fill
};
```

---

## TableLayoutPanel Pattern

### Basic Setup

```csharp
TableLayoutPanel layout = new TableLayoutPanel
{
    Dock = DockStyle.Fill,
    ColumnCount = 1,
    RowCount = 3,
    Padding = DpiHelper.Scale(new Padding(20)),
    BackColor = Color.Transparent
};

// Fixed-height row (in scaled pixels)
layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiHelper.Scale(50)));

// Fill remaining space
layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

// Another fixed row
layout.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiHelper.Scale(40)));
```

### Adding Controls to TableLayoutPanel

```csharp
// Add to column 0, row 0
layout.Controls.Add(titleLabel, 0, 0);

// Add to column 0, row 1
layout.Controls.Add(contentPanel, 0, 1);

// Add to column 0, row 2
layout.Controls.Add(buttonPanel, 0, 2);
```

### Multi-Column Layout

```csharp
TableLayoutPanel layout = new TableLayoutPanel
{
    Dock = DockStyle.Fill,
    ColumnCount = 3,
    RowCount = 1
};

// Equal columns (33.33% each)
layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

layout.Controls.Add(button1, 0, 0);
layout.Controls.Add(button2, 1, 0);
layout.Controls.Add(button3, 2, 0);
```

---

## FlowLayoutPanel Pattern

```csharp
FlowLayoutPanel flow = new FlowLayoutPanel
{
    Dock = DockStyle.Fill,
    FlowDirection = FlowDirection.LeftToRight,
    WrapContents = true,
    AutoSize = false,
    AutoScroll = true,
    Padding = DpiHelper.Scale(new Padding(10))
};

// Add cards that will wrap automatically
flow.Controls.Add(card1);
flow.Controls.Add(card2);
flow.Controls.Add(card3);
```

---

## Custom Drawing / Paint Events

```csharp
panel.Paint += (s, e) =>
{
    Graphics g = e.Graphics;
    g.SmoothingMode = SmoothingMode.AntiAlias;

    // Scale the radius for DPI
    int radius = DpiHelper.Scale(12);

    // Draw rounded rectangle
    using (GraphicsPath path = GetRoundedRectangle(rect, radius))
    using (SolidBrush brush = new SolidBrush(Color.Blue))
    {
        g.FillPath(brush, path);
    }
};

private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
{
    GraphicsPath path = new GraphicsPath();
    path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
    path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
    path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
    path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
    path.CloseAllFigures();
    return path;
}
```

---

## Dock & Anchor Quick Reference

### Dock

```csharp
// Fill entire parent
control.Dock = DockStyle.Fill;

// Stick to top
control.Dock = DockStyle.Top;

// Stick to bottom
control.Dock = DockStyle.Bottom;

// Stick to left
control.Dock = DockStyle.Left;

// Stick to right
control.Dock = DockStyle.Right;
```

### Anchor

```csharp
// Top-left (default)
control.Anchor = AnchorStyles.Top | AnchorStyles.Left;

// Top-right
control.Anchor = AnchorStyles.Top | AnchorStyles.Right;

// Bottom-left
control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

// Bottom-right
control.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

// Stretch horizontally (stays at top)
control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

// Stretch vertically (stays at left)
control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

// Stretch both directions (fill)
control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
```

---

## Common Mistakes to Avoid

### ❌ Wrong

```csharp
// Hardcoded size
this.Size = new Size(800, 600);

// Hardcoded position
button.Location = new Point(20, 20);

// Hardcoded font
label.Font = new Font("Segoe UI", 12);

// FixedDialog prevents scaling
this.FormBorderStyle = FormBorderStyle.FixedDialog;

// Hardcoded padding
panel.Padding = new Padding(20);
```

### ✅ Correct

```csharp
// Scaled size
this.Size = DpiHelper.Scale(new Size(800, 600));

// Scaled position
button.Location = DpiHelper.Scale(new Point(20, 20));

// DPI-aware font
label.Font = DpiHelper.CreateFont("Segoe UI", 12);

// Sizable with minimum
this.FormBorderStyle = FormBorderStyle.Sizable;
this.MinimumSize = DpiHelper.Scale(new Size(600, 400));

// Scaled padding
panel.Padding = DpiHelper.Scale(new Padding(20));
```

---

## Testing Quick Check

### Quick Visual Test at Different DPI

1. Windows Settings → Display → Scale: Set to 150%
2. Sign out and sign back in
3. Launch your app
4. Check:
   - ✅ Text is crisp and readable
   - ✅ Buttons are properly sized
   - ✅ No content clipping
   - ✅ Spacing looks consistent
   - ✅ Window minimum size works

### Quick Code Audit

Search your code for these patterns (should be rare/none):

```csharp
// Search for:
"new Size(" // Should be DpiHelper.Scale(new Size(...))
"new Point(" // Should be DpiHelper.Scale(new Point(...))
"new Padding(" // Should be DpiHelper.Scale(new Padding(...))
"new Font(" // Should be DpiHelper.CreateFont(...)
"FixedDialog" // Consider changing to Sizable
```

---

## DPI Scale Reference

| Setting | DPI | Scale Factor | Example Display |
|---------|-----|--------------|----------------|
| 100% | 96 | 1.0 | Standard 1080p |
| 125% | 120 | 1.25 | Laptop default |
| 150% | 144 | 1.5 | High-DPI laptop |
| 200% | 192 | 2.0 | 4K monitor, Surface |
| 250% | 240 | 2.5 | Ultra high-DPI |

### Size Examples

| Design Size | 100% | 125% | 150% | 200% |
|-------------|------|------|------|------|
| 100px | 100px | 125px | 150px | 200px |
| 800x600 | 800x600 | 1000x750 | 1200x900 | 1600x1200 |
| Button 120x35 | 120x35 | 150x44 | 180x53 | 240x70 |

---

## Decision Tree

**Adding a new control?**
```
Is it a fixed size? (button, card, etc.)
  → Yes: Use DpiHelper.Scale(new Size(...))
  → No: Use Dock or Anchor

Does it need to be positioned?
  → Absolute: Use DpiHelper.Scale(new Point(...))
  → Relative: Use TableLayoutPanel or Dock/Anchor

Does it need padding/margin?
  → Yes: Use DpiHelper.Scale(new Padding(...))

Does it need a font?
  → Yes: Use DpiHelper.CreateFont(...)
```

**Adding a new form?**
```
1. Set Size with DpiHelper.Scale()
2. Set MinimumSize with DpiHelper.Scale()
3. Use FormBorderStyle.Sizable (not FixedDialog)
4. Call DpiHelper.ConfigureForm(this) in constructor
5. Use TableLayoutPanel for main layout
6. Test at 100%, 150%, 200% DPI
```

---

## Performance Tips

### Efficient DPI Scaling in Loops

```csharp
// ❌ Less efficient (calculates scale factor repeatedly)
for (int i = 0; i < 100; i++)
{
    controls[i].Width = DpiHelper.Scale(100);
    controls[i].Height = DpiHelper.Scale(50);
}

// ✅ More efficient (calculate once, reuse)
float scale = DpiHelper.GetScaleFactor();
for (int i = 0; i < 100; i++)
{
    controls[i].Width = (int)Math.Round(100 * scale);
    controls[i].Height = (int)Math.Round(50 * scale);
}
```

---

## Where to Get Help

1. **Implementation Guide:** `/docs/DPI_SCALING_IMPLEMENTATION.md`
2. **Changes Summary:** `/docs/DPI_SCALING_CHANGES_SUMMARY.md`
3. **This Quick Reference:** `/docs/DPI_QUICK_REFERENCE.md`
4. **Example Code:** See `StatusLogWindow.cs` or `CalibrationWizard.cs`

---

**Keep this guide handy when developing new UI components!**
