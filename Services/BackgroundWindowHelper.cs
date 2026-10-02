using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LyricPin.Services;

internal static class BackgroundWindowHelper
{
    internal static async Task HideLaunchedWindowAsync(Process process)
    {
        using (process)
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(15);
                DateTime? firstWindowAt = null;
                while (DateTime.UtcNow < deadline && !process.HasExited)
                {
                    // Restrict hiding to the process we launched, never an existing client.
                    EnumWindows((window, _) =>
                    {
                        GetWindowThreadProcessId(window, out var processId);
                        if (processId == (uint)process.Id && IsWindowVisible(window))
                        {
                            ShowWindowAsync(window, 0); // SW_HIDE keeps the process and tray icon alive.
                            firstWindowAt ??= DateTime.UtcNow;
                        }
                        return true;
                    }, IntPtr.Zero);

                    // Some clients show their window again after completing initialization.
                    if (firstWindowAt is { } shownAt && DateTime.UtcNow - shownAt >= TimeSpan.FromSeconds(2))
                    {
                        return;
                    }
                    await Task.Delay(100);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(IntPtr window, int command);
}
