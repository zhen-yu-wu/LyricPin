using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LyricPin.Ui;

internal static class AcrylicMenuHelper
{
    private const int WindowCompositionAttributeAccentPolicy = 19;
    private const int AccentEnableAcrylicBlurBehind = 4;
    private const int DwmWindowAttributeUseImmersiveDarkMode = 20;
    private const int DwmWindowAttributeWindowCornerPreference = 33;
    private const int DwmWindowAttributeSystemBackdropType = 38;
    private const int DwmWindowCornerPreferenceRound = 2;
    private const int DwmSystemBackdropTypeTransientWindow = 3;

    internal static void Attach(ToolStripDropDown menu)
    {
        menu.Opacity = 0.97;
        menu.Opened += (_, _) => Apply(menu.Handle);

        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                Attach(menuItem.DropDown);
            }
        }
    }

    private static void Apply(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var enabled = 1;
            DwmSetWindowAttribute(
                windowHandle,
                DwmWindowAttributeUseImmersiveDarkMode,
                ref enabled,
                sizeof(int));

            var cornerPreference = DwmWindowCornerPreferenceRound;
            DwmSetWindowAttribute(
                windowHandle,
                DwmWindowAttributeWindowCornerPreference,
                ref cornerPreference,
                sizeof(int));

            var backdropType = DwmSystemBackdropTypeTransientWindow;
            DwmSetWindowAttribute(
                windowHandle,
                DwmWindowAttributeSystemBackdropType,
                ref backdropType,
                sizeof(int));

            ApplyLegacyAcrylic(windowHandle);
        }
        catch (DllNotFoundException)
        {
            // The solid translucent menu remains usable on older Windows versions.
        }
        catch (EntryPointNotFoundException)
        {
            // The solid translucent menu remains usable on older Windows versions.
        }
    }

    private static void ApplyLegacyAcrylic(IntPtr windowHandle)
    {
        var policy = new AccentPolicy
        {
            AccentState = AccentEnableAcrylicBlurBehind,
            AccentFlags = 2,
            // AABBGGRR: a cool charcoal tint with enough transparency for blur.
            GradientColor = unchecked((int)0xCC2F2822)
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
        finally
        {
            Marshal.FreeHGlobal(policyPointer);
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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
