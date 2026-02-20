using System;
using System.Drawing;
using System.Windows.Forms;

namespace LADApp
{
    /// <summary>
    /// Utility class for handling DPI scaling across different display configurations.
    /// Provides methods to scale sizes, fonts, and positions based on the current DPI.
    /// </summary>
    public static class DpiHelper
    {
        private const float BaseDpi = 96.0f;

        /// <summary>
        /// Gets the current DPI scale factor for the primary screen.
        /// Returns 1.0 at 96 DPI, 1.25 at 120 DPI, 1.5 at 144 DPI, 2.0 at 192 DPI, etc.
        /// </summary>
        public static float GetScaleFactor()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return g.DpiX / BaseDpi;
            }
        }

        /// <summary>
        /// Gets the current DPI scale factor for a specific control.
        /// This is more accurate for per-monitor DPI scenarios.
        /// </summary>
        public static float GetScaleFactor(Control control)
        {
            if (control == null)
                return GetScaleFactor();

            using (var g = control.CreateGraphics())
            {
                return g.DpiX / BaseDpi;
            }
        }

        /// <summary>
        /// Scales a pixel value based on the current DPI.
        /// </summary>
        public static int Scale(int value)
        {
            return (int)Math.Round(value * GetScaleFactor());
        }

        /// <summary>
        /// Scales a pixel value based on a specific control's DPI.
        /// </summary>
        public static int Scale(int value, Control control)
        {
            return (int)Math.Round(value * GetScaleFactor(control));
        }

        /// <summary>
        /// Scales a Size structure based on the current DPI.
        /// </summary>
        public static Size Scale(Size size)
        {
            var factor = GetScaleFactor();
            return new Size(
                (int)Math.Round(size.Width * factor),
                (int)Math.Round(size.Height * factor)
            );
        }

        /// <summary>
        /// Scales a Size structure based on a specific control's DPI.
        /// </summary>
        public static Size Scale(Size size, Control control)
        {
            var factor = GetScaleFactor(control);
            return new Size(
                (int)Math.Round(size.Width * factor),
                (int)Math.Round(size.Height * factor)
            );
        }

        /// <summary>
        /// Scales a Point structure based on the current DPI.
        /// </summary>
        public static Point Scale(Point point)
        {
            var factor = GetScaleFactor();
            return new Point(
                (int)Math.Round(point.X * factor),
                (int)Math.Round(point.Y * factor)
            );
        }

        /// <summary>
        /// Scales a Point structure based on a specific control's DPI.
        /// </summary>
        public static Point Scale(Point point, Control control)
        {
            var factor = GetScaleFactor(control);
            return new Point(
                (int)Math.Round(point.X * factor),
                (int)Math.Round(point.Y * factor)
            );
        }

        /// <summary>
        /// Scales a Padding structure based on the current DPI.
        /// </summary>
        public static Padding Scale(Padding padding)
        {
            var factor = GetScaleFactor();
            return new Padding(
                (int)Math.Round(padding.Left * factor),
                (int)Math.Round(padding.Top * factor),
                (int)Math.Round(padding.Right * factor),
                (int)Math.Round(padding.Bottom * factor)
            );
        }

        /// <summary>
        /// Scales a Padding structure based on a specific control's DPI.
        /// </summary>
        public static Padding Scale(Padding padding, Control control)
        {
            var factor = GetScaleFactor(control);
            return new Padding(
                (int)Math.Round(padding.Left * factor),
                (int)Math.Round(padding.Top * factor),
                (int)Math.Round(padding.Right * factor),
                (int)Math.Round(padding.Bottom * factor)
            );
        }

        /// <summary>
        /// Creates a DPI-aware Font based on the design-time point size.
        /// </summary>
        public static Font CreateFont(string familyName, float sizeInPoints, FontStyle style = FontStyle.Regular)
        {
            // Font sizes in points are already DPI-aware in Windows Forms
            // But we can ensure consistent rendering across different DPI settings
            return new Font(familyName, sizeInPoints, style, GraphicsUnit.Point);
        }

        /// <summary>
        /// Creates a scaled Font with size adjusted for the current DPI if needed.
        /// Use this for special cases where you need explicit DPI-based font scaling.
        /// </summary>
        public static Font CreateScaledFont(string familyName, float baseSize, FontStyle style = FontStyle.Regular)
        {
            var scaleFactor = GetScaleFactor();
            // Apply minimal scaling to fonts as they're already DPI-aware
            // This is only for edge cases where exact pixel matching is needed
            var adjustedSize = baseSize * Math.Max(1.0f, scaleFactor * 0.1f);
            return new Font(familyName, adjustedSize, style, GraphicsUnit.Point);
        }

        /// <summary>
        /// Configures a form for proper DPI awareness.
        /// Call this in the form's constructor after InitializeComponent().
        /// </summary>
        public static void ConfigureForm(Form form)
        {
            if (form == null)
                return;

            // Set AutoScaleMode to Dpi for proper scaling
            form.AutoScaleMode = AutoScaleMode.Dpi;

            // For .NET 6+ and Windows Forms, AutoScaleDimensions should be set automatically
            // but we can ensure it's correct
            form.AutoScaleDimensions = new SizeF(BaseDpi, BaseDpi);
        }

        /// <summary>
        /// Gets the current DPI value for the primary screen.
        /// </summary>
        public static float GetDpi()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return g.DpiX;
            }
        }

        /// <summary>
        /// Converts logical pixels to physical pixels based on current DPI.
        /// Logical pixels are what you design with at 96 DPI.
        /// Physical pixels are what actually renders on screen.
        /// </summary>
        public static int LogicalToPhysical(int logicalPixels)
        {
            return Scale(logicalPixels);
        }

        /// <summary>
        /// Converts physical pixels to logical pixels based on current DPI.
        /// Useful for reverse calculations.
        /// </summary>
        public static int PhysicalToLogical(int physicalPixels)
        {
            var factor = GetScaleFactor();
            return (int)Math.Round(physicalPixels / factor);
        }
    }
}
