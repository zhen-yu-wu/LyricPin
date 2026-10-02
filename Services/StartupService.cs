using System.IO;
using Microsoft.Win32;

namespace LyricPin.Services;

internal static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LyricPin";
    internal const string StartupArgument = "--startup";

    internal static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string command && !string.IsNullOrWhiteSpace(command);
    }

    internal static void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            using var existingKey = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            existingKey?.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var command = CreateCommand(Environment.ProcessPath, Path.Combine(AppContext.BaseDirectory, "LyricPin.dll"));
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    internal static string CreateCommand(string? processPath, string? assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("无法获取程序路径，请使用发布版 LyricPin.exe 设置开机自启动。");
        }

        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new InvalidOperationException("无法获取程序路径，请使用发布版 LyricPin.exe 设置开机自启动。");
            }

            return $"\"{processPath}\" \"{assemblyPath}\" {StartupArgument}";
        }

        return $"\"{processPath}\" {StartupArgument}";
    }
}
