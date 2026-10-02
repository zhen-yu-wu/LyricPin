using System.Windows;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LyricPin.Ui;

internal static class LyricBackdropMaterial
{
    internal static Brush CreateTintBrush(double transparency)
    {
        var opacity = (1 - Math.Clamp(transparency, 0.40, 0.95)) * 0.55;
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.8, 1) };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)Math.Round((opacity + 0.015) * 255), 64, 67, 82), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)Math.Round(opacity * 255), 30, 32, 43), 0.45));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)Math.Round((opacity + 0.01) * 255), 19, 21, 29), 1));
        brush.Freeze();
        return brush;
    }

    internal static Brush CreateGrainBrush()
    {
        // A small fixed tile is generated once, rather than updating noise each frame.
        const int size = 64;
        var pixels = new byte[size * size * 4];
        var random = new Random(17);
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            var alpha = (byte)random.Next(4, 16);
            var value = random.Next(2) == 0 ? (byte)0 : alpha;
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = value;
            pixels[offset + 3] = alpha;
        }
        var image = BitmapSource.Create(size, size, 96, 96, PixelFormats.Pbgra32, null, pixels, size * 4);
        image.Freeze();
        var brush = new ImageBrush(image)
        {
            TileMode = TileMode.Tile,
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = new Rect(0, 0, size, size),
            Stretch = Stretch.Fill
        };
        brush.Freeze();
        return brush;
    }
}
