using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace LyricPin.Services;

public static class CloudMusicLauncher
{
    public static bool TryLaunchWithCdpIfNotRunning()
    {
        if (IsRunning())
        {
            return false;
        }

        var executable = FindExecutable();
        if (executable is null)
        {
            return false;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "--remote-debugging-address=127.0.0.1 --remote-debugging-port=9223",
            UseShellExecute = true
        });
        return true;
    }

    public static bool IsRunning()
    {
        var processes = Process.GetProcessesByName("cloudmusic");
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static string? FindExecutable()
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            foreach (var registryPath in new[]
                     {
                         @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                         @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                     })
            {
                using var uninstall = hive.OpenSubKey(registryPath);
                if (uninstall is null)
                {
                    continue;
                }

                foreach (var name in uninstall.GetSubKeyNames())
                {
                    using var entry = uninstall.OpenSubKey(name);
                    var displayName = entry?.GetValue("DisplayName") as string;
                    if (displayName is null ||
                        (!displayName.Contains("网易云", StringComparison.OrdinalIgnoreCase) &&
                         !displayName.Contains("NetEase Cloud", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var displayIcon = entry?.GetValue("DisplayIcon") as string;
                    var candidate = displayIcon?.Trim().Trim('"').Split(',')[0];
                    if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        var candidates = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Netease", "CloudMusic", "cloudmusic.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "NetEase", "CloudMusic", "cloudmusic.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "NetEase", "CloudMusic", "cloudmusic.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
