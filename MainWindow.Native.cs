using System.Runtime.InteropServices;

namespace LyricPin;

public partial class MainWindow
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNotTopmost = new(-2);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;

    private static IntPtr GetWindowLongPtr(IntPtr windowHandle, int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(windowHandle, index)
            : new IntPtr(GetWindowLong32(windowHandle, index));
    }

    private static IntPtr SetWindowLongPtr(IntPtr windowHandle, int index, IntPtr newValue)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(windowHandle, index, newValue)
            : new IntPtr(SetWindowLong32(windowHandle, index, newValue.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(IntPtr windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr windowHandle, int index, IntPtr newValue);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong32(IntPtr windowHandle, int index, int newValue);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
