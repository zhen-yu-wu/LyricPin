using System.Runtime.InteropServices;

namespace LyricPin.Ui;

internal static class WindowBackdropHelper
{
    private const int WindowCompositionAttributeAccentPolicy = 19;
    private const int AccentDisabled = 0;
    private const int AccentEnableBlurBehind = 3;

    internal static void SetAcrylic(
        IntPtr windowHandle,
        bool enabled,
        double transparency)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var tintAlpha = (byte)Math.Clamp(
            Math.Round((1 - transparency) * 255),
            8,
            153);
        var policy = new AccentPolicy
        {
            // BLURBEHIND is composited more cheaply than the Acrylic state. It
            // keeps the live background visible without making window dragging
            // stutter on transparent WPF windows.
            AccentState = enabled ? AccentEnableBlurBehind : AccentDisabled,
            AccentFlags = 0,
            // AABBGGRR: a neutral charcoal tint that remains readable over light
            // and dark windows while still allowing the blurred scene through.
            GradientColor = enabled
                ? unchecked((int)(((uint)tintAlpha << 24) | 0x00211A17u))
                : 0
        };

        var policySize = Marshal.SizeOf<AccentPolicy>();
        var policyPointer = Marshal.AllocHGlobal(policySize);
        try
        {
            Marshal.StructureToPtr(policy, policyPointer, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttributeAccentPolicy,
                Data = policyPointer,
                SizeOfData = policySize
            };
            SetWindowCompositionAttribute(windowHandle, ref data);
        }
        catch (DllNotFoundException)
        {
            // The semi-transparent WPF surface remains as the visual fallback.
        }
        catch (EntryPointNotFoundException)
        {
            // The semi-transparent WPF surface remains as the visual fallback.
        }
        finally
        {
            Marshal.FreeHGlobal(policyPointer);
        }
    }

    internal static void SetRoundedRegion(
        IntPtr windowHandle,
        double width,
        double lyricHeight,
        double radius,
        bool includePlaybackControls,
        double playbackControlsWidth,
        double totalHeight)
    {
        if (windowHandle == IntPtr.Zero || width <= 0 || lyricHeight <= 0)
        {
            return;
        }

        var diameter = Math.Max(2, (int)Math.Ceiling(radius * 2));
        var region = CreateRoundRectRgn(
            0,
            0,
            (int)Math.Ceiling(width) + 1,
            (int)Math.Ceiling(lyricHeight) + 1,
            diameter,
            diameter);
        if (region == IntPtr.Zero)
        {
            return;
        }

        if (includePlaybackControls &&
            playbackControlsWidth > 0 &&
            totalHeight > lyricHeight)
        {
            var controlLeft = (int)Math.Floor((width - playbackControlsWidth) / 2);
            var controlRight = (int)Math.Ceiling(controlLeft + playbackControlsWidth) + 1;
            var controlRegion = CreateRoundRectRgn(
                controlLeft,
                (int)Math.Floor(lyricHeight),
                controlRight,
                (int)Math.Ceiling(totalHeight) + 1,
                diameter,
                diameter);
            if (controlRegion != IntPtr.Zero)
            {
                CombineRgn(region, region, controlRegion, RegionOr);
                DeleteObject(controlRegion);
            }
        }

        // Windows owns the region after a successful SetWindowRgn call.
        if (SetWindowRgn(windowHandle, region, true) == 0)
        {
            DeleteObject(region);
        }
    }

    internal static void ClearRoundedRegion(IntPtr windowHandle)
    {
        if (windowHandle != IntPtr.Zero)
        {
            SetWindowRgn(windowHandle, IntPtr.Zero, true);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(
        IntPtr windowHandle,
        ref WindowCompositionAttributeData data);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(
        int left,
        int top,
        int right,
        int bottom,
        int ellipseWidth,
        int ellipseHeight);

    private const int RegionOr = 2;

    [DllImport("gdi32.dll")]
    private static extern int CombineRgn(
        IntPtr destinationRegion,
        IntPtr sourceRegion1,
        IntPtr sourceRegion2,
        int combineMode);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr windowHandle, IntPtr region, bool redraw);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr objectHandle);
}
