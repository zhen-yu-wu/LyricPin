using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LyricPin.Ui;

internal static class AcrylicMenuHelper
{
    private const int DwmWindowAttributeUseImmersiveDarkMode = 20;
    private const int DwmWindowAttributeWindowCornerPreference = 33;
    private const int DwmWindowAttributeSystemBackdropType = 38;
    private const int DwmWindowCornerPreferenceRound = 2;
    private const int DwmSystemBackdropTypeNone = 1;

    internal static void Attach(ToolStripDropDown menu)
    {
        menu.Opacity = 1;
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
            var enabled = 0;
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

            var backdropType = DwmSystemBackdropTypeNone;
            DwmSetWindowAttribute(
                windowHandle,
                DwmWindowAttributeSystemBackdropType,
                ref backdropType,
                sizeof(int));

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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
