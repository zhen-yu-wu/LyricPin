using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace LyricPin.Ui;

internal static class TrayIconFactory
{
    internal static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(1, 1, 30, 30);
        using var backgroundPath = CreateRoundedRectangle(bounds, 9);
        using var backgroundBrush = new LinearGradientBrush(
            bounds,
            Color.FromArgb(211, 69, 184),
            Color.FromArgb(105, 82, 219),
            45f);
        graphics.FillPath(backgroundBrush, backgroundPath);

        using var highlightPen = new Pen(Color.FromArgb(76, 255, 255, 255), 1f);
        graphics.DrawPath(highlightPen, backgroundPath);

        using var notePen = new Pen(Color.White, 2.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLine(notePen, 13, 10, 23, 7);
        graphics.DrawLine(notePen, 13, 10, 13, 22);
        graphics.DrawLine(notePen, 23, 7, 23, 19);
        using var noteBrush = new SolidBrush(Color.White);
        graphics.FillEllipse(noteBrush, 8.5f, 19f, 7f, 5.5f);
        graphics.FillEllipse(noteBrush, 18.5f, 16f, 7f, 5.5f);

        var iconHandle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(iconHandle).Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr iconHandle);
}
