using System.Windows.Threading;
using LyricPin.Ui;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace LyricPin;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        LyricBackdropGrain.Background = LyricBackdropMaterial.CreateGrainBrush();
        CurrentLyricText.SizeChanged += (_, _) => UpdateCurrentLyricClip();
        SizeChanged += (_, _) => ScheduleLyricBackdropRegion();

        _marqueeRestartTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(60)
        };
        _marqueeRestartTimer.Tick += MarqueeRestartTimer_Tick;
        _widthIndicatorTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(900)
        };
        _widthIndicatorTimer.Tick += WidthIndicatorTimer_Tick;
        _playbackControlsHideTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(180)
        };
        _playbackControlsHideTimer.Tick += (_, _) =>
        {
            _playbackControlsHideTimer.Stop();
            if (!RootGrid.IsMouseOver && !PlaybackControlsPopupSurface.IsMouseOver)
            {
                HidePlaybackControls();
            }
        };

        _trayIconImage = TrayIconFactory.Create();
        _showHideMenuItem = new Forms.ToolStripMenuItem("隐藏歌词") { Tag = "primary:visibility" };
        _showHideMenuItem.Click += (_, _) => ToggleLyricWindow();

        _alwaysOnTopMenuItem = new Forms.ToolStripMenuItem("始终置顶")
        {
            CheckOnClick = true,
            Checked = true
        };
        _alwaysOnTopMenuItem.CheckedChanged += (_, _) =>
            SetAlwaysOnTop(_alwaysOnTopMenuItem.Checked);

        _lockPositionMenuItem = new Forms.ToolStripMenuItem("固定位置")
        {
            CheckOnClick = true
        };
        _lockPositionMenuItem.CheckedChanged += (_, _) =>
        {
            _isPositionLocked = _lockPositionMenuItem.Checked;
            UpdateWindowBehaviorSummary();
        };

        _mouseThroughMenuItem = new Forms.ToolStripMenuItem("鼠标穿透")
        {
            CheckOnClick = true
        };
        _mouseThroughMenuItem.CheckedChanged += (_, _) =>
            SetMouseThrough(_mouseThroughMenuItem.Checked);

        _lyricBackdropMenuItem = new Forms.ToolStripMenuItem("启用毛玻璃")
        {
            CheckOnClick = true
        };
        _lyricBackdropMenuItem.CheckedChanged += (_, _) =>
            SetLyricBackdrop(_lyricBackdropMenuItem.Checked);

        _lyricBackdropTransparencyMenuItem = new Forms.ToolStripMenuItem("毛玻璃透明度")
        {
            ShortcutKeyDisplayString = "80%"
        };
        _lyricBackdropTransparencyMenuItem.DropDownItems.Add(
            "更透明",
            null,
            (_, _) => SetLyricBackdropTransparency(_lyricBackdropTransparency + 0.05));
        _lyricBackdropTransparencyMenuItem.DropDownItems.Add(
            "更明显",
            null,
            (_, _) => SetLyricBackdropTransparency(_lyricBackdropTransparency - 0.05));
        _lyricBackdropTransparencyMenuItem.DropDownItems.Add(
            "恢复默认",
            null,
            (_, _) => SetLyricBackdropTransparency(0.80));
        _lyricBackdropTransparencyMenuItem.DropDown.Closing += (_, e) =>
        {
            if (e.CloseReason == Forms.ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
            }
        };

        _singleLineMenuItem = new Forms.ToolStripMenuItem("一行");
        _singleLineMenuItem.Click += (_, _) => SetLineDisplayMode(false);
        _threeLineMenuItem = new Forms.ToolStripMenuItem("三行") { Checked = true };
        _threeLineMenuItem.Click += (_, _) => SetLineDisplayMode(true);
        _lineCountMenuItem = new Forms.ToolStripMenuItem("显示行数");
        _lineCountMenuItem.ShortcutKeyDisplayString = "三行";
        _lineCountMenuItem.DropDownItems.Add(_singleLineMenuItem);
        _lineCountMenuItem.DropDownItems.Add(_threeLineMenuItem);

        _widthMenuItem = new Forms.ToolStripMenuItem("歌词宽度")
        {
            ShortcutKeyDisplayString = "300 px"
        };
        _widthMenuItem.DropDownItems.Add("增加", null, (_, _) => SetLyricWidth(_lyricWidth + 50));
        _widthMenuItem.DropDownItems.Add("减小", null, (_, _) => SetLyricWidth(_lyricWidth - 50));
        _widthMenuItem.DropDownItems.Add("恢复默认", null, (_, _) => SetLyricWidth(300));
        _widthMenuItem.DropDown.Closing += (_, e) =>
        {
            if (e.CloseReason == Forms.ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
            }
        };

        _fontSizeMenuItem = new Forms.ToolStripMenuItem("字体大小")
        {
            ShortcutKeyDisplayString = "20 px"
        };
        _fontSizeMenuItem.DropDownItems.Add("增大", null, (_, _) => ChangeFontSize(2));
        _fontSizeMenuItem.DropDownItems.Add("减小", null, (_, _) => ChangeFontSize(-2));
        _fontSizeMenuItem.DropDownItems.Add("恢复默认", null, (_, _) => SetFontSize(20));
        _fontSizeMenuItem.DropDown.Closing += (_, e) =>
        {
            if (e.CloseReason == Forms.ToolStripDropDownCloseReason.ItemClicked)
            {
                e.Cancel = true;
            }
        };

        _fontFamilyMenuItem = new Forms.ToolStripMenuItem("字体")
        {
            ShortcutKeyDisplayString = "默认"
        };
        AddFontFamilyItem("默认（Segoe UI）", "Segoe UI", isDefault: true);
        AddFontFamilyItem("微软雅黑", "Microsoft YaHei");
        AddFontFamilyItem("宋体", "SimSun");

        _fontColorMenuItem = new Forms.ToolStripMenuItem("字体颜色")
        {
            ShortcutKeyDisplayString = "白色"
        };
        AddFontColorItem("黑色", Drawing.Color.Black);
        AddFontColorItem("白色", Drawing.Color.White);
        AddFontColorItem("紫红色", ReadableAccent);
        _fontColorMenuItem.DropDownItems.Add(new Forms.ToolStripSeparator());
        _fontColorMenuItem.DropDownItems.Add("自定义颜色…", null, (_, _) => ChooseCustomFontColor());

        var playbackMenuItem = new Forms.ToolStripMenuItem("播放控制") { Tag = "group:playback" };
        playbackMenuItem.DropDownItems.Add(
            "上一首",
            null,
            async (_, _) => await _cdpService.PreviousTrackAsync());
        _playPauseMenuItem = new Forms.ToolStripMenuItem("播放 / 暂停");
        _playPauseMenuItem.Click += async (_, _) => await TogglePlaybackAsync();
        playbackMenuItem.DropDownItems.Add(_playPauseMenuItem);
        playbackMenuItem.DropDownItems.Add(
            "下一首",
            null,
            async (_, _) => await _cdpService.NextTrackAsync());
        playbackMenuItem.DropDownItems.Add(new Forms.ToolStripSeparator());
        _showPlaybackControlsMenuItem = new Forms.ToolStripMenuItem("悬停显示控制按钮")
        {
            CheckOnClick = true,
            Checked = true
        };
        _showPlaybackControlsMenuItem.CheckedChanged += (_, _) =>
            SetPlaybackControlsEnabled(_showPlaybackControlsMenuItem.Checked);
        playbackMenuItem.DropDownItems.Add(_showPlaybackControlsMenuItem);

        _lyricStyleMenuItem = new Forms.ToolStripMenuItem("歌词样式") { Tag = "group:lyrics" };
        _lyricStyleMenuItem.DropDownItems.Add(_lineCountMenuItem);
        _lyricStyleMenuItem.DropDownItems.Add(_widthMenuItem);
        _lyricStyleMenuItem.DropDownItems.Add(_fontSizeMenuItem);
        _lyricStyleMenuItem.DropDownItems.Add(_fontFamilyMenuItem);
        _lyricStyleMenuItem.DropDownItems.Add(_fontColorMenuItem);

        _backdropSettingsMenuItem = new Forms.ToolStripMenuItem("毛玻璃") { Tag = "group:backdrop" };
        _backdropSettingsMenuItem.DropDownItems.Add(_lyricBackdropMenuItem);
        _backdropSettingsMenuItem.DropDownItems.Add(_lyricBackdropTransparencyMenuItem);

        _windowBehaviorMenuItem = new Forms.ToolStripMenuItem("窗口行为") { Tag = "group:window" };
        _windowBehaviorMenuItem.DropDownItems.Add(_alwaysOnTopMenuItem);
        _windowBehaviorMenuItem.DropDownItems.Add(_lockPositionMenuItem);
        _windowBehaviorMenuItem.DropDownItems.Add(_mouseThroughMenuItem);

        var startupSettingsMenuItem = new Forms.ToolStripMenuItem("启动设置") { Tag = "group:startup" };
        _startupMenuItem = new Forms.ToolStripMenuItem("开机自启动");
        _startupMenuItem.Click += (_, _) => ToggleStartupSetting();
        startupSettingsMenuItem.DropDownItems.Add(_startupMenuItem);
        startupSettingsMenuItem.DropDownItems.Add(CreateMenuLabel("自启动也会在后台唤醒网易云音乐", "subtitle"));
        startupSettingsMenuItem.DropDown.Opening += (_, _) => RefreshStartupSetting();
        RefreshStartupSetting();

        _trayMenu = new Forms.ContextMenuStrip();
        _trayMenu.MinimumSize = new Drawing.Size(286, 0);
        _trayMenu.Items.Add(CreateMenuLabel("LyricPin", "header"));
        _trayMenu.Items.Add(CreateMenuLabel("网易云桌面歌词", "subtitle"));
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        _trayMenu.Items.Add(_showHideMenuItem);
        _trayMenu.Items.Add(playbackMenuItem);
        _trayMenu.Items.Add(_lyricStyleMenuItem);
        _trayMenu.Items.Add(_backdropSettingsMenuItem);
        _trayMenu.Items.Add(_windowBehaviorMenuItem);
        _trayMenu.Items.Add(startupSettingsMenuItem);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        var exitMenuItem = new Forms.ToolStripMenuItem("退出 LyricPin") { Tag = "danger" };
        exitMenuItem.Click += (_, _) => Close();
        _trayMenu.Items.Add(exitMenuItem);
        ApplyMenuStyle(_trayMenu, new ModernMenuRenderer());
        AcrylicMenuHelper.Attach(_trayMenu);
        SetLyricWidth(_lyricWidth, false);
        SetFontSize(_currentFontSize);
        SetFontColor(_fontColor);
        SetLyricBackdropTransparency(_lyricBackdropTransparency);

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayIconImage,
            Text = "LyricPin",
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowLyricWindow();

        _syncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(60)
        };
        _syncTimer.Tick += SyncTimer_Tick;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private static void ApplyMenuStyle(Forms.ToolStripDropDown menu, Forms.ToolStripRenderer renderer)
    {
        menu.Renderer = renderer;
        menu.BackColor = Drawing.Color.FromArgb(250, 248, 252);
        menu.ForeColor = Drawing.Color.FromArgb(46, 39, 54);
        menu.Font = new Drawing.Font("Microsoft YaHei UI", 9.25f, Drawing.FontStyle.Regular, Drawing.GraphicsUnit.Point);
        menu.Padding = new Forms.Padding(8);
        menu.MinimumSize = new Drawing.Size(Math.Max(menu.MinimumSize.Width, 224), 0);

        if (menu is Forms.ToolStripDropDownMenu dropDownMenu)
        {
            dropDownMenu.ShowImageMargin = false;
            dropDownMenu.ShowCheckMargin = true;
        }

        foreach (Forms.ToolStripItem item in menu.Items)
        {
            if (item is Forms.ToolStripLabel label)
            {
                var isHeader = string.Equals(label.Tag as string, "header", StringComparison.Ordinal);
                var isSubtitle = string.Equals(label.Tag as string, "subtitle", StringComparison.Ordinal);
                label.ForeColor = isHeader
                    ? Drawing.Color.FromArgb(27, 23, 31)
                    : isSubtitle
                        ? Drawing.Color.FromArgb(117, 109, 127)
                        : Drawing.Color.FromArgb(117, 109, 127);
                label.Font = new Drawing.Font(
                    "Microsoft YaHei UI",
                    isHeader ? 14f : 8.5f,
                    isHeader ? Drawing.FontStyle.Bold : Drawing.FontStyle.Regular,
                    Drawing.GraphicsUnit.Point);
                label.Padding = isHeader
                    ? new Forms.Padding(13, 10, 12, 2)
                    : isSubtitle
                        ? new Forms.Padding(14, 1, 12, 9)
                        : new Forms.Padding(12, 6, 12, 2);
                label.Margin = new Forms.Padding(1, 0, 1, 0);
                continue;
            }

            if (item is Forms.ToolStripSeparator)
            {
                item.Margin = new Forms.Padding(6, 3, 6, 3);
                continue;
            }

            item.Padding = new Forms.Padding(14, 7, 16, 7);
            item.Margin = new Forms.Padding(2, 1, 2, 1);
            if (item is Forms.ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
            {
                ApplyMenuStyle(menuItem.DropDown, renderer);
            }
        }
    }

    private static Forms.ToolStripLabel CreateMenuLabel(string text, string role) =>
        new(text)
        {
            Tag = role,
            AutoSize = true
        };

}
