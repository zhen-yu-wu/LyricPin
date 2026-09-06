using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LyricPin.Ui;

public sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color HoverColor = Color.FromArgb(38, 255, 255, 255);
    private static readonly Color CheckedColor = Color.FromArgb(24, 108, 140, 255);
    private static readonly Color AccentColor = Color.FromArgb(108, 140, 255);
    private static readonly Color BorderColor = Color.FromArgb(48, 255, 255, 255);
    private static readonly Color MutedColor = Color.FromArgb(145, 153, 169);

    public ModernMenuRenderer()
        : base(new ModernMenuColorTable())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is ToolStripLabel)
        {
            return;
        }

        var isChecked = e.Item is ToolStripMenuItem { Checked: true };
        if (!e.Item.Selected && !isChecked)
        {
            return;
        }

        var bounds = new Rectangle(6, 2, Math.Max(1, e.Item.Width - 12), Math.Max(1, e.Item.Height - 4));
        using var path = CreateRoundedRectangle(bounds, 8);
        var isDanger = string.Equals(e.Item.Tag as string, "danger", StringComparison.Ordinal);
        var background = e.Item.Selected
            ? isDanger ? Color.FromArgb(38, 255, 92, 92) : HoverColor
            : CheckedColor;
        using var brush = new SolidBrush(background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(brush, path);

        if (isChecked)
        {
            using var accentBrush = new SolidBrush(AccentColor);
            using var accentPath = CreateRoundedRectangle(
                new Rectangle(6, Math.Max(4, e.Item.Height / 2 - 7), 3, 14),
                2);
            e.Graphics.FillPath(accentBrush, accentPath);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var isDanger = string.Equals(e.Item.Tag as string, "danger", StringComparison.Ordinal);
        var isStatusText = e.Item is ToolStripMenuItem menuItem &&
                           !string.IsNullOrEmpty(menuItem.ShortcutKeyDisplayString) &&
                           string.Equals(e.Text, menuItem.ShortcutKeyDisplayString, StringComparison.Ordinal);
        e.TextColor = isStatusText
            ? MutedColor
            : isDanger && e.Item.Selected
                ? Color.FromArgb(255, 178, 178)
                : e.Item.Enabled
                    ? Color.FromArgb(239, 242, 248)
                    : MutedColor;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(Color.FromArgb(42, 255, 255, 255));
        e.Graphics.DrawLine(pen, 16, y, Math.Max(16, e.Item.Width - 16), y);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        var centerX = e.ArrowRectangle.Left + e.ArrowRectangle.Width / 2;
        var centerY = e.ArrowRectangle.Top + e.ArrowRectangle.Height / 2;
        using var pen = new Pen(e.Item?.Enabled != false ? Color.FromArgb(205, 210, 220) : MutedColor, 1.5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawLines(pen,
        new Point[]
        {
            new Point(centerX - 2, centerY - 4),
            new Point(centerX + 2, centerY),
            new Point(centerX - 2, centerY + 4)
        });
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var centerX = e.ImageRectangle.Left + e.ImageRectangle.Width / 2;
        var centerY = e.ImageRectangle.Top + e.ImageRectangle.Height / 2;
        using var checkBackground = new SolidBrush(AccentColor);
        using var checkPath = CreateRoundedRectangle(
            new Rectangle(centerX - 8, centerY - 8, 16, 16),
            5);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(checkBackground, checkPath);

        using var pen = new Pen(Color.White, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        e.Graphics.DrawLines(pen,
        new Point[]
        {
            new Point(centerX - 4, centerY),
            new Point(centerX - 1, centerY + 3),
            new Point(centerX + 5, centerY - 4)
        });
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
        using var brush = new LinearGradientBrush(
            bounds,
            Color.FromArgb(226, 30, 33, 41),
            Color.FromArgb(232, 23, 26, 33),
            LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(brush, bounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        var bounds = new Rectangle(0, 0, Math.Max(1, e.ToolStrip.Width - 1), Math.Max(1, e.ToolStrip.Height - 1));
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawRectangle(pen, bounds);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public sealed class ModernMenuColorTable : ProfessionalColorTable
{
    private static readonly Color Background = Color.FromArgb(228, 25, 28, 35);

    public override Color ToolStripDropDownBackground => Background;
    public override Color ImageMarginGradientBegin => Background;
    public override Color ImageMarginGradientMiddle => Background;
    public override Color ImageMarginGradientEnd => Background;
    public override Color MenuBorder => Color.FromArgb(64, 255, 255, 255);
    public override Color MenuItemBorder => Color.Transparent;
    public override Color MenuItemSelected => Color.Transparent;
    public override Color MenuItemSelectedGradientBegin => Color.Transparent;
    public override Color MenuItemSelectedGradientEnd => Color.Transparent;
    public override Color MenuItemPressedGradientBegin => Background;
    public override Color MenuItemPressedGradientMiddle => Background;
    public override Color MenuItemPressedGradientEnd => Background;
    public override Color SeparatorDark => Color.FromArgb(38, 255, 255, 255);
    public override Color SeparatorLight => Color.Transparent;
}
