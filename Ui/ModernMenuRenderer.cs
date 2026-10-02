using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LyricPin.Ui;

public sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color HoverColor = Color.FromArgb(242, 237, 247);
    private static readonly Color CheckedColor = Color.FromArgb(248, 232, 244);
    private static readonly Color AccentColor = Color.FromArgb(193, 63, 176);
    private static readonly Color BorderColor = Color.FromArgb(225, 220, 231);
    private static readonly Color MutedColor = Color.FromArgb(117, 109, 127);

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

        var role = e.Item.Tag as string ?? string.Empty;
        var isChecked = e.Item is ToolStripMenuItem { Checked: true };
        var isPrimary = role.StartsWith("primary:", StringComparison.Ordinal);
        if (e.Item.Enabled && (e.Item.Selected || isChecked || isPrimary))
        {
            var bounds = new Rectangle(5, 2, Math.Max(1, e.Item.Width - 10), Math.Max(1, e.Item.Height - 4));
            using var path = CreateRoundedRectangle(bounds, 9);
            var isDanger = string.Equals(role, "danger", StringComparison.Ordinal);
            var background = e.Item.Selected
                ? isDanger
                    ? Color.FromArgb(255, 235, 237)
                    : isPrimary
                        ? Color.FromArgb(243, 222, 239)
                        : HoverColor
                : isChecked
                    ? CheckedColor
                    : Color.FromArgb(250, 239, 247);
            using var brush = new SolidBrush(background);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brush, path);
        }

        if (role.StartsWith("group:", StringComparison.Ordinal) || isPrimary)
        {
            DrawMenuIcon(e.Graphics, e.Item.Bounds, role, e.Item.Selected);
        }

        if (isChecked)
        {
            using var accentBrush = new SolidBrush(AccentColor);
            using var accentPath = CreateRoundedRectangle(
                new Rectangle(5, Math.Max(4, e.Item.Height / 2 - 7), 3, 14),
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
        e.TextColor = !e.Item.Enabled
            ? Color.FromArgb(158, 151, 165)
            : e.Item is ToolStripLabel
                ? e.Item.ForeColor
                : isStatusText
                    ? MutedColor
                    : isDanger
                        ? Color.FromArgb(180, 66, 77)
                        : e.Item is ToolStripMenuItem { Checked: true }
                            ? Color.FromArgb(151, 43, 136)
                            : Color.FromArgb(46, 39, 54);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(Color.FromArgb(233, 229, 237));
        e.Graphics.DrawLine(pen, 14, y, Math.Max(14, e.Item.Width - 14), y);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        var centerX = e.ArrowRectangle.Left + e.ArrowRectangle.Width / 2;
        var centerY = e.ArrowRectangle.Top + e.ArrowRectangle.Height / 2;
        using var pen = new Pen(e.Item?.Enabled != false ? Color.FromArgb(134, 121, 145) : MutedColor, 1.5f)
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
            Color.FromArgb(255, 255, 254, 255),
            Color.FromArgb(255, 249, 247, 251),
            LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(brush, bounds);

        using var glowBrush = new SolidBrush(Color.FromArgb(6, AccentColor));
        e.Graphics.FillEllipse(glowBrush, e.ToolStrip.Width - 118, -62, 150, 118);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        var bounds = new Rectangle(1, 1, Math.Max(1, e.ToolStrip.Width - 3), Math.Max(1, e.ToolStrip.Height - 3));
        using var pen = new Pen(BorderColor);
        using var path = CreateRoundedRectangle(bounds, 11);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
    }

    private static void DrawMenuIcon(Graphics graphics, Rectangle itemBounds, string role, bool selected)
    {
        var centerY = itemBounds.Height / 2;
        var tile = new Rectangle(11, centerY - 10, 20, 20);
        using var tilePath = CreateRoundedRectangle(tile, 6);
        var iconColor = role.EndsWith("playback", StringComparison.Ordinal)
            ? Color.FromArgb(78, 115, 197)
            : role.EndsWith("backdrop", StringComparison.Ordinal)
                ? Color.FromArgb(46, 143, 137)
                : role.EndsWith("startup", StringComparison.Ordinal)
                    ? Color.FromArgb(186, 126, 46)
                    : AccentColor;
        using var tileBrush = new SolidBrush(Color.FromArgb(selected ? 32 : 18, iconColor));
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillPath(tileBrush, tilePath);

        using var pen = new Pen(iconColor, 1.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        var x = tile.Left;
        var y = tile.Top;
        if (role.EndsWith("playback", StringComparison.Ordinal))
        {
            using var playPath = new GraphicsPath();
            playPath.AddPolygon(new[]
            {
                new Point(x + 8, y + 6),
                new Point(x + 15, y + 10),
                new Point(x + 8, y + 14)
            });
            using var iconBrush = new SolidBrush(pen.Color);
            graphics.FillPath(iconBrush, playPath);
        }
        else if (role.EndsWith("lyrics", StringComparison.Ordinal))
        {
            graphics.DrawLine(pen, x + 6, y + 6, x + 14, y + 6);
            graphics.DrawLine(pen, x + 5, y + 10, x + 15, y + 10);
            graphics.DrawLine(pen, x + 7, y + 14, x + 13, y + 14);
        }
        else if (role.EndsWith("backdrop", StringComparison.Ordinal))
        {
            graphics.DrawEllipse(pen, x + 5, y + 5, 10, 10);
            graphics.DrawArc(pen, x + 8, y + 8, 7, 7, 205, 185);
        }
        else if (role.EndsWith("startup", StringComparison.Ordinal))
        {
            graphics.DrawArc(pen, x + 5, y + 5, 10, 10, -55, 290);
            graphics.DrawLine(pen, x + 10, y + 4, x + 10, y + 10);
        }
        else if (role.EndsWith("window", StringComparison.Ordinal))
        {
            graphics.DrawRectangle(pen, x + 5, y + 6, 10, 9);
            graphics.DrawLine(pen, x + 5, y + 9, x + 15, y + 9);
        }
        else
        {
            graphics.DrawEllipse(pen, x + 4, y + 7, 12, 7);
            using var pupilBrush = new SolidBrush(pen.Color);
            graphics.FillEllipse(pupilBrush, x + 8, y + 9, 4, 4);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        radius = Math.Min(radius, Math.Max(1, Math.Min(bounds.Width, bounds.Height) / 2));
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
    private static readonly Color Background = Color.FromArgb(255, 250, 248, 252);

    public override Color ToolStripDropDownBackground => Background;
    public override Color ImageMarginGradientBegin => Background;
    public override Color ImageMarginGradientMiddle => Background;
    public override Color ImageMarginGradientEnd => Background;
    public override Color MenuBorder => Color.Transparent;
    public override Color MenuItemBorder => Color.Transparent;
    public override Color MenuItemSelected => Color.Transparent;
    public override Color MenuItemSelectedGradientBegin => Color.Transparent;
    public override Color MenuItemSelectedGradientEnd => Color.Transparent;
    public override Color MenuItemPressedGradientBegin => Background;
    public override Color MenuItemPressedGradientMiddle => Background;
    public override Color MenuItemPressedGradientEnd => Background;
    public override Color SeparatorDark => Color.FromArgb(233, 229, 237);
    public override Color SeparatorLight => Color.Transparent;
}
