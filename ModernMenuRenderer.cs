using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LADApp
{
    /// <summary>
    /// Custom renderer for modern dark-themed context menu.
    /// </summary>
    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color MenuBackColor = Color.FromArgb(45, 45, 45);
        private static readonly Color MenuForeColor = Color.White;
        private static readonly Color HoverBackColor = Color.FromArgb(64, 64, 64);
        private static readonly Color SeparatorColor = Color.FromArgb(80, 80, 80);

        public ModernMenuRenderer() : base(new ModernColorTable())
        {
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                // Draw rounded hover effect
                Rectangle rect = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
                using (GraphicsPath path = GetRoundedRectangle(rect, 4))
                using (SolidBrush brush = new SolidBrush(HoverBackColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            Rectangle rect = new Rectangle(0, 0, e.Item.Width, e.Item.Height);
            using (Pen pen = new Pen(SeparatorColor, 1))
            {
                e.Graphics.DrawLine(pen, rect.Left + 10, rect.Top + rect.Height / 2, 
                    rect.Right - 10, rect.Top + rect.Height / 2);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Color.White : MenuForeColor;
            base.OnRenderItemText(e);
        }

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
    }

    /// <summary>
    /// Custom color table for modern dark theme.
    /// </summary>
    public class ModernColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(64, 64, 64);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(64, 64, 64);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(64, 64, 64);
        public override Color MenuItemBorder => Color.FromArgb(80, 80, 80);
        public override Color MenuBorder => Color.FromArgb(60, 60, 60);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(54, 54, 54);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(54, 54, 54);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 45);
        public override Color SeparatorDark => Color.FromArgb(80, 80, 80);
        public override Color SeparatorLight => Color.FromArgb(80, 80, 80);
        public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 45);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 45);
        public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 45);
    }
}
