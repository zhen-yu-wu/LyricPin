using System.Runtime.InteropServices;

namespace LyricPin.Ui;

internal static class WindowLevelHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct WindowBounds { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out WindowBounds bounds);

    private const uint PreviousWindow = 3;
    private const int ExtendedStyle = -20;
    private const long TopmostStyle = 0x8;

    internal static bool IsCoveredByAnotherTopmostWindow(IntPtr window)
    {
        if (!GetWindowRect(window, out var targetBounds)) { return false; }
        GetWindowThreadProcessId(window, out var ownProcess);
        // Ignore our own popups, menus and dialogs so settings remain usable.
        for (var above = GetWindow(window, PreviousWindow); above != IntPtr.Zero;
             above = GetWindow(above, PreviousWindow))
        {
            if (!IsWindowVisible(above))
            {
                continue;
            }
            GetWindowThreadProcessId(above, out var process);
            var style = IntPtr.Size == 8
                ? GetWindowLongPtr(above, ExtendedStyle).ToInt64()
                : GetWindowLong(above, ExtendedStyle);
            if (process != ownProcess && (style & TopmostStyle) != 0 &&
                GetWindowRect(above, out var bounds) &&
                bounds.Left < targetBounds.Right && bounds.Right > targetBounds.Left &&
                bounds.Top < targetBounds.Bottom && bounds.Bottom > targetBounds.Top)
            {
                return true;
            }
        }
        return false;
    }

    internal static bool RestoreTopmost(IntPtr window) =>
        SetWindowPos(window, new IntPtr(-1), 0, 0, 0, 0, 0x1 | 0x2 | 0x10);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr window, uint command);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr window, int index);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter,
        int x, int y, int width, int height, uint flags);
}


