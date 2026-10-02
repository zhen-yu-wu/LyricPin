using System.Windows;
using LyricPin.Services;

namespace LyricPin;

public partial class MainWindow
{
    private void RefreshStartupSetting()
    {
        try
        {
            _startupMenuItem.Checked = StartupService.IsEnabled();
            _startupMenuItem.Enabled = true;
            _startupMenuItem.ToolTipText = "自启动也会在后台唤醒网易云音乐，仅保留系统托盘图标";
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            _startupMenuItem.Enabled = false;
            _startupMenuItem.ToolTipText = "无法读取开机自启动设置";
        }
    }

    private void ToggleStartupSetting()
    {
        try
        {
            StartupService.SetEnabled(!StartupService.IsEnabled());
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                $"无法修改开机自启动设置。\n{exception.Message}",
                "LyricPin",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        RefreshStartupSetting();
    }
}
