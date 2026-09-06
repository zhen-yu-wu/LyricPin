using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using LyricPin.Services;
using LyricPin.Ui;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Media = System.Windows.Media;
using Animation = System.Windows.Media.Animation;

namespace LyricPin;

public partial class MainWindow : Window
{
    private static readonly Drawing.Color ReadableBlue = Drawing.Color.FromArgb(79, 124, 255);
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNotTopmost = new(-2);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;

    private readonly CloudMusicCdpService _cdpService = new();
    private readonly DispatcherTimer _syncTimer;
    private readonly DispatcherTimer _marqueeRestartTimer;
    private readonly DispatcherTimer _widthIndicatorTimer;
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly Forms.ContextMenuStrip _trayMenu;
    private readonly Forms.ToolStripMenuItem _showHideMenuItem;
    private readonly Forms.ToolStripMenuItem _playPauseMenuItem;
    private readonly Forms.ToolStripMenuItem _showPlaybackControlsMenuItem;
    private readonly Forms.ToolStripMenuItem _alwaysOnTopMenuItem;
    private readonly Forms.ToolStripMenuItem _lockPositionMenuItem;
    private readonly Forms.ToolStripMenuItem _mouseThroughMenuItem;
    private readonly Forms.ToolStripMenuItem _lineCountMenuItem;
    private readonly Forms.ToolStripMenuItem _singleLineMenuItem;
    private readonly Forms.ToolStripMenuItem _threeLineMenuItem;
    private readonly Forms.ToolStripMenuItem _widthMenuItem;
    private readonly Forms.ToolStripMenuItem _fontSizeMenuItem;
    private readonly Forms.ToolStripMenuItem _fontColorMenuItem;
    private readonly Drawing.Icon _trayIconImage;

    private bool _isUpdating;
    private double _lyricWidth = 300;
    private double _currentFontSize = 20;
    private double _lineProgress = 1;
    private bool _showThreeLines = true;
    private Drawing.Color _fontColor = Drawing.Color.White;
    private bool _isPositionLocked;
    private bool _isMouseThrough;
    private bool _isAlwaysOnTop = true;
    private bool _showPlaybackControls = true;
    private bool _isPlaying = true;
    private int _playbackControlsAnimationVersion;
    private DateTime _nextTopmostRefreshAt;
    private (long SongId, int LineIndex)? _lastTransitionKey;
    private int _transitionAnimationVersion;
    private (long SongId, int LineIndex)? _lastProgressKey;
    private double _lastObservedProgress = -1;
    private int _unchangedProgressTicks;
    private double _marqueeOffset;
    private double _marqueeMaxOffset;
    private bool _isWidthIndicatorVisible;
    private int _widthIndicatorAnimationVersion;

    public MainWindow()
    {
        InitializeComponent();
        CurrentLyricText.SizeChanged += (_, _) => UpdateCurrentLyricClip();

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

        _trayIconImage = (Drawing.Icon)Drawing.SystemIcons.Application.Clone();
        _showHideMenuItem = new Forms.ToolStripMenuItem("隐藏歌词");
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
            _isPositionLocked = _lockPositionMenuItem.Checked;

        _mouseThroughMenuItem = new Forms.ToolStripMenuItem("鼠标穿透")
        {
            CheckOnClick = true
        };
        _mouseThroughMenuItem.CheckedChanged += (_, _) =>
            SetMouseThrough(_mouseThroughMenuItem.Checked);

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

        _fontColorMenuItem = new Forms.ToolStripMenuItem("字体颜色")
        {
            ShortcutKeyDisplayString = "白色"
        };
        AddFontColorItem("黑色", Drawing.Color.Black);
        AddFontColorItem("白色", Drawing.Color.White);
        AddFontColorItem("蓝色", ReadableBlue);
        _fontColorMenuItem.DropDownItems.Add(new Forms.ToolStripSeparator());
        _fontColorMenuItem.DropDownItems.Add("自定义颜色…", null, (_, _) => ChooseCustomFontColor());

        var playbackMenuItem = new Forms.ToolStripMenuItem("播放控制");
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

        _trayMenu = new Forms.ContextMenuStrip();
        _trayMenu.MinimumSize = new Drawing.Size(238, 0);
        _trayMenu.Items.Add(CreateMenuLabel("LyricPin", "header"));
        _trayMenu.Items.Add(_showHideMenuItem);
        _trayMenu.Items.Add(playbackMenuItem);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        _trayMenu.Items.Add(CreateMenuLabel("显示", "section"));
        _trayMenu.Items.Add(_lineCountMenuItem);
        _trayMenu.Items.Add(_widthMenuItem);
        _trayMenu.Items.Add(_fontSizeMenuItem);
        _trayMenu.Items.Add(_fontColorMenuItem);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        _trayMenu.Items.Add(CreateMenuLabel("窗口", "section"));
        _trayMenu.Items.Add(_alwaysOnTopMenuItem);
        _trayMenu.Items.Add(_lockPositionMenuItem);
        _trayMenu.Items.Add(_mouseThroughMenuItem);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        var exitMenuItem = new Forms.ToolStripMenuItem("退出 LyricPin") { Tag = "danger" };
        exitMenuItem.Click += (_, _) => Close();
        _trayMenu.Items.Add(exitMenuItem);
        ApplyMenuStyle(_trayMenu, new ModernMenuRenderer());
        AcrylicMenuHelper.Attach(_trayMenu);
        SetLyricWidth(_lyricWidth, false);
        SetFontSize(_currentFontSize);
        SetFontColor(_fontColor);

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
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _syncTimer.Tick += SyncTimer_Tick;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SetAlwaysOnTop(_isAlwaysOnTop);
        CloudMusicLauncher.TryLaunchWithCdpIfNotRunning();
        await UpdateLyricsAsync();
        _syncTimer.Start();
    }

    private async void SyncTimer_Tick(object? sender, EventArgs e)
    {
        MaintainWindowLevel();
        await UpdateLyricsAsync();
    }

    private async Task UpdateLyricsAsync()
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            var nativeLyric = await _cdpService.GetCurrentLyricAsync();
            if (nativeLyric is not null)
            {
                UpdateSyncInterval(nativeLyric);
                UpdatePlaybackState(nativeLyric.IsPlaying);
                var currentText = string.IsNullOrWhiteSpace(nativeLyric.Text)
                    ? nativeLyric.SongName
                    : nativeLyric.Text;
                SetLyricLines(
                    nativeLyric.PreviousText,
                    currentText,
                    nativeLyric.NextText,
                    string.IsNullOrWhiteSpace(nativeLyric.Text) ? 1 : nativeLyric.LineProgress,
                    (nativeLyric.SongId, nativeLyric.LineIndex));
                return;
            }

            if (_cdpService.IsConnected)
            {
                SetIdleSyncInterval();
                SetStatusText("请在网易云音乐中播放歌曲");
                return;
            }

            if (CloudMusicLauncher.IsRunning())
            {
                SetIdleSyncInterval();
                SetStatusText("请退出网易云，然后先启动 LyricPin");
                return;
            }

            SetIdleSyncInterval();
            SetStatusText("正在启动网易云音乐…");
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isPositionLocked && !_isMouseThrough && e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
            EnsureWindowInScreenBounds();
        }
    }

    private async void PreviousTrackButton_Click(object sender, RoutedEventArgs e) =>
        await _cdpService.PreviousTrackAsync();

    private async void PlayPauseButton_Click(object sender, RoutedEventArgs e) =>
        await TogglePlaybackAsync();

    private async void NextTrackButton_Click(object sender, RoutedEventArgs e) =>
        await _cdpService.NextTrackAsync();

    private async Task TogglePlaybackAsync()
    {
        if (await _cdpService.TogglePlaybackAsync())
        {
            UpdatePlaybackState(!_isPlaying);
        }
    }

    private void UpdatePlaybackState(bool isPlaying)
    {
        _isPlaying = isPlaying;
        _playPauseMenuItem.Text = isPlaying ? "暂停" : "播放";
        PlayPauseOverlayButton.Content = isPlaying ? "\uE769" : "\uE768";
        PlayPauseOverlayButton.ToolTip = isPlaying ? "暂停" : "播放";
    }

    private void RootGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_showPlaybackControls && !_isMouseThrough)
        {
            ShowPlaybackControls();
        }
    }

    private void RootGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) =>
        HidePlaybackControls();

    private void ShowPlaybackControls()
    {
        _playbackControlsAnimationVersion++;
        PlaybackControlsPanel.Visibility = Visibility.Visible;
        PlaybackControlsPanel.IsHitTestVisible = true;
        PlaybackControlsPanel.BeginAnimation(
            OpacityProperty,
            new Animation.DoubleAnimation(PlaybackControlsPanel.Opacity, 1, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new Animation.QuadraticEase
                {
                    EasingMode = Animation.EasingMode.EaseOut
                }
            });
        PlaybackControlsTranslateTransform.BeginAnimation(
            Media.TranslateTransform.YProperty,
            new Animation.DoubleAnimation(
                PlaybackControlsTranslateTransform.Y,
                0,
                TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new Animation.QuarticEase
                {
                    EasingMode = Animation.EasingMode.EaseOut
                }
            });
    }

    private void HidePlaybackControls(bool immediately = false)
    {
        var animationVersion = ++_playbackControlsAnimationVersion;
        PlaybackControlsPanel.IsHitTestVisible = false;
        if (immediately)
        {
            PlaybackControlsPanel.BeginAnimation(OpacityProperty, null);
            PlaybackControlsTranslateTransform.BeginAnimation(Media.TranslateTransform.YProperty, null);
            PlaybackControlsPanel.Opacity = 0;
            PlaybackControlsTranslateTransform.Y = 6;
            return;
        }

        var fade = new Animation.DoubleAnimation(PlaybackControlsPanel.Opacity, 0, TimeSpan.FromMilliseconds(170))
        {
            EasingFunction = new Animation.QuadraticEase
            {
                EasingMode = Animation.EasingMode.EaseIn
            }
        };
        fade.Completed += (_, _) =>
        {
            if (animationVersion == _playbackControlsAnimationVersion)
            {
                PlaybackControlsPanel.Opacity = 0;
            }
        };
        PlaybackControlsPanel.BeginAnimation(OpacityProperty, fade);
        PlaybackControlsTranslateTransform.BeginAnimation(
            Media.TranslateTransform.YProperty,
            new Animation.DoubleAnimation(PlaybackControlsTranslateTransform.Y, 4, TimeSpan.FromMilliseconds(170)));
    }

    private void SetPlaybackControlsEnabled(bool enabled)
    {
        _showPlaybackControls = enabled;
        PlaybackControlsHost.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        HidePlaybackControls(true);
        UpdateWindowHeight();

        if (enabled && RootGrid.IsMouseOver && !_isMouseThrough)
        {
            ShowPlaybackControls();
        }
    }

    private void ToggleLyricWindow()
    {
        if (IsVisible)
        {
            Hide();
            _showHideMenuItem.Text = "显示歌词";
            return;
        }

        ShowLyricWindow();
    }

    private void ShowLyricWindow()
    {
        if (!IsVisible)
        {
            Show();
        }

        EnsureWindowInScreenBounds();
        SetAlwaysOnTop(_isAlwaysOnTop);
        Activate();
        ScheduleHorizontalMarquee();
        _showHideMenuItem.Text = "隐藏歌词";
    }

    private void SetStatusText(string text)
    {
        SetLyricLines(string.Empty, text, string.Empty);
    }

    private static void ApplyMenuStyle(Forms.ToolStripDropDown menu, Forms.ToolStripRenderer renderer)
    {
        menu.Renderer = renderer;
        menu.BackColor = Drawing.Color.FromArgb(28, 31, 38);
        menu.ForeColor = Drawing.Color.FromArgb(238, 240, 245);
        menu.Font = new Drawing.Font("Microsoft YaHei UI", 9f, Drawing.FontStyle.Regular, Drawing.GraphicsUnit.Point);
        menu.Padding = new Forms.Padding(7);

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
                label.ForeColor = isHeader
                    ? Drawing.Color.FromArgb(244, 246, 251)
                    : Drawing.Color.FromArgb(132, 141, 158);
                label.Font = new Drawing.Font(
                    "Microsoft YaHei UI",
                    isHeader ? 11f : 8.5f,
                    Drawing.FontStyle.Bold,
                    Drawing.GraphicsUnit.Point);
                label.Padding = isHeader
                    ? new Forms.Padding(10, 7, 10, 5)
                    : new Forms.Padding(10, 7, 10, 2);
                label.Margin = new Forms.Padding(1, 0, 1, 0);
                continue;
            }

            if (item is Forms.ToolStripSeparator)
            {
                item.Margin = new Forms.Padding(4, 2, 4, 2);
                continue;
            }

            item.Padding = new Forms.Padding(10, 6, 10, 6);
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

    private void SetLyricLines(
        string previous,
        string current,
        string next,
        double progress = 1,
        (long SongId, int LineIndex)? transitionKey = null)
    {
        var transitionChanged = transitionKey is not null &&
                                transitionKey != _lastTransitionKey;
        var shouldAnimate = transitionChanged && _lastTransitionKey is not null && _showThreeLines;
        if (shouldAnimate)
        {
            CopyCurrentLinesToOutgoing();
        }

        var textChanged = false;
        if (!string.Equals(PreviousLyricText.Text, previous, StringComparison.Ordinal))
        {
            PreviousLyricText.Text = previous;
            textChanged = true;
        }
        if (!string.Equals(CurrentLyricBaseText.Text, current, StringComparison.Ordinal))
        {
            CurrentLyricBaseText.Text = current;
            CurrentLyricText.Text = current;
            textChanged = true;
        }
        if (!string.Equals(NextLyricText.Text, next, StringComparison.Ordinal))
        {
            NextLyricText.Text = next;
            textChanged = true;
        }
        _lineProgress = Math.Clamp(progress, 0, 1);
        UpdateCurrentLyricClip();
        if (textChanged)
        {
            ScheduleHorizontalMarquee(!shouldAnimate);
        }
        else
        {
            UpdateCurrentLyricMarqueeProgress();
        }

        if (transitionKey is null)
        {
            _lastTransitionKey = null;
            OutgoingLyricLinesPanel.Visibility = Visibility.Collapsed;
            ResetLyricLineAnimations();
        }
        else if (transitionChanged)
        {
            _lastTransitionKey = transitionKey;
            if (shouldAnimate)
            {
                AnimateLyricTransition();
            }
        }
    }

    private void CopyCurrentLinesToOutgoing()
    {
        OutgoingPreviousLyricText.Text = PreviousLyricText.Text;
        OutgoingCurrentLyricText.Text = CurrentLyricBaseText.Text;
        OutgoingNextLyricText.Text = NextLyricText.Text;

        OutgoingPreviousLyricText.FontSize = PreviousLyricText.FontSize;
        OutgoingCurrentLyricText.FontSize = CurrentLyricText.FontSize;
        OutgoingNextLyricText.FontSize = NextLyricText.FontSize;
        OutgoingPreviousLyricText.Foreground = PreviousLyricText.Foreground;
        OutgoingCurrentLyricText.Foreground = CurrentLyricText.Foreground;
        OutgoingNextLyricText.Foreground = NextLyricText.Foreground;
    }

    private void AnimateLyricTransition()
    {
        if (!_showThreeLines)
        {
            return;
        }

        var animationVersion = ++_transitionAnimationVersion;
        var easing = new Animation.QuarticEase
        {
            EasingMode = Animation.EasingMode.EaseOut
        };
        var duration = new Duration(TimeSpan.FromMilliseconds(460));
        var secondarySize = Math.Max(6, _currentFontSize * 0.72);
        var distance = (_currentFontSize + secondarySize) / 2 + 2;
        var secondaryScale = secondarySize / _currentFontSize;
        var previousStartScale = _currentFontSize / secondarySize;

        OutgoingLyricLinesPanel.Visibility = Visibility.Collapsed;
        ResetLyricLineAnimations();
        LyricLinesPanel.Opacity = 1;

        PreviousLyricTranslateTransform.BeginAnimation(
            Media.TranslateTransform.YProperty,
            new Animation.DoubleAnimation(distance, 0, duration)
            {
                EasingFunction = easing
            });
        PreviousLyricScaleTransform.BeginAnimation(
            Media.ScaleTransform.ScaleXProperty,
            new Animation.DoubleAnimation(previousStartScale, 1, duration) { EasingFunction = easing });
        PreviousLyricScaleTransform.BeginAnimation(
            Media.ScaleTransform.ScaleYProperty,
            new Animation.DoubleAnimation(previousStartScale, 1, duration) { EasingFunction = easing });
        PreviousLyricText.BeginAnimation(
            OpacityProperty,
            new Animation.DoubleAnimation(1, 0.58, duration) { EasingFunction = easing });

        CurrentLyricTranslateTransform.BeginAnimation(
            Media.TranslateTransform.YProperty,
            new Animation.DoubleAnimation(distance, 0, duration) { EasingFunction = easing });
        CurrentLyricScaleTransform.BeginAnimation(
            Media.ScaleTransform.ScaleXProperty,
            new Animation.DoubleAnimation(secondaryScale, 1, duration) { EasingFunction = easing });
        CurrentLyricScaleTransform.BeginAnimation(
            Media.ScaleTransform.ScaleYProperty,
            new Animation.DoubleAnimation(secondaryScale, 1, duration) { EasingFunction = easing });
        CurrentLyricRow.BeginAnimation(
            OpacityProperty,
            new Animation.DoubleAnimation(0.58, 1, duration) { EasingFunction = easing });

        var nextDelay = TimeSpan.FromMilliseconds(55);
        var nextDuration = new Duration(TimeSpan.FromMilliseconds(405));
        NextLyricTranslateTransform.BeginAnimation(
            Media.TranslateTransform.YProperty,
            new Animation.DoubleAnimation(distance * 0.72, 0, nextDuration)
            {
                BeginTime = nextDelay,
                EasingFunction = easing
            });
        NextLyricScaleTransform.BeginAnimation(
            Media.ScaleTransform.ScaleXProperty,
            new Animation.DoubleAnimation(0.94, 1, nextDuration)
            {
                BeginTime = nextDelay,
                EasingFunction = easing
            });
        NextLyricScaleTransform.BeginAnimation(
            Media.ScaleTransform.ScaleYProperty,
            new Animation.DoubleAnimation(0.94, 1, nextDuration)
            {
                BeginTime = nextDelay,
                EasingFunction = easing
            });
        var nextFade = new Animation.DoubleAnimation(0, 0.58, nextDuration)
        {
            BeginTime = nextDelay,
            EasingFunction = easing
        };
        nextFade.Completed += (_, _) =>
        {
            if (animationVersion == _transitionAnimationVersion)
            {
                ResetLyricLineAnimations();
                StartHorizontalMarquee();
            }
        };
        NextLyricText.BeginAnimation(OpacityProperty, nextFade);
    }

    private void ResetLyricLineAnimations()
    {
        PreviousLyricTranslateTransform.BeginAnimation(Media.TranslateTransform.YProperty, null);
        CurrentLyricTranslateTransform.BeginAnimation(Media.TranslateTransform.YProperty, null);
        NextLyricTranslateTransform.BeginAnimation(Media.TranslateTransform.YProperty, null);

        PreviousLyricScaleTransform.BeginAnimation(Media.ScaleTransform.ScaleXProperty, null);
        PreviousLyricScaleTransform.BeginAnimation(Media.ScaleTransform.ScaleYProperty, null);
        CurrentLyricScaleTransform.BeginAnimation(Media.ScaleTransform.ScaleXProperty, null);
        CurrentLyricScaleTransform.BeginAnimation(Media.ScaleTransform.ScaleYProperty, null);
        NextLyricScaleTransform.BeginAnimation(Media.ScaleTransform.ScaleXProperty, null);
        NextLyricScaleTransform.BeginAnimation(Media.ScaleTransform.ScaleYProperty, null);

        PreviousLyricText.BeginAnimation(OpacityProperty, null);
        CurrentLyricRow.BeginAnimation(OpacityProperty, null);
        NextLyricText.BeginAnimation(OpacityProperty, null);
    }

    private void UpdateSyncInterval(Models.CloudMusicLyricSnapshot lyric)
    {
        var progressKey = (lyric.SongId, lyric.LineIndex);
        if (progressKey == _lastProgressKey &&
            Math.Abs(lyric.LineProgress - _lastObservedProgress) < 0.0001)
        {
            _unchangedProgressTicks++;
        }
        else
        {
            _lastProgressKey = progressKey;
            _lastObservedProgress = lyric.LineProgress;
            _unchangedProgressTicks = 0;
            _syncTimer.Interval = TimeSpan.FromMilliseconds(50);
        }

        if (_unchangedProgressTicks >= 10)
        {
            _syncTimer.Interval = TimeSpan.FromMilliseconds(200);
        }
    }

    private void SetIdleSyncInterval()
    {
        _lastProgressKey = null;
        _lastObservedProgress = -1;
        _unchangedProgressTicks = 0;
        _syncTimer.Interval = TimeSpan.FromMilliseconds(500);
    }

    private void ChangeFontSize(double delta)
    {
        SetFontSize(_currentFontSize + delta);
    }

    private void SetFontSize(double size)
    {
        _currentFontSize = Math.Clamp(size, 8, 72);
        CurrentLyricBaseText.FontSize = _currentFontSize;
        CurrentLyricText.FontSize = _currentFontSize;

        var secondarySize = Math.Max(6, _currentFontSize * 0.72);
        PreviousLyricText.FontSize = secondarySize;
        NextLyricText.FontSize = secondarySize;
        UpdateWindowHeight();
        ScheduleHorizontalMarquee();
        _fontSizeMenuItem.ShortcutKeyDisplayString = $"{_currentFontSize:0} px";
    }

    private void SetLyricWidth(double width, bool showIndicator = true)
    {
        var screenBounds = GetCurrentScreenBounds();
        var maximumWidth = Math.Max(80, Math.Min(1600, screenBounds.Width));
        _lyricWidth = Math.Clamp(width, 80, maximumWidth);
        LyricsViewport.Width = _lyricWidth;
        CurrentLyricViewport.Width = _lyricWidth;

        var newWindowWidth = _lyricWidth;
        if (IsLoaded)
        {
            Left += (Width - newWindowWidth) / 2;
        }
        Width = newWindowWidth;
        _widthMenuItem.ShortcutKeyDisplayString = $"{_lyricWidth:0} px";
        if (showIndicator)
        {
            ShowWidthIndicator();
        }
        ScheduleHorizontalMarquee();
        ScheduleScreenBoundsCorrection();
    }

    private void ShowWidthIndicator()
    {
        var wasHidden = WidthIndicatorPanel.Visibility != Visibility.Visible;
        _widthIndicatorAnimationVersion++;
        WidthIndicatorPanel.BeginAnimation(OpacityProperty, null);
        WidthIndicatorPanel.Opacity = 1;
        WidthIndicatorPanel.Visibility = Visibility.Visible;

        if (wasHidden)
        {
            _isWidthIndicatorVisible = true;
            UpdateWindowHeight(false);
            WidthIndicatorPanel.BeginAnimation(
                OpacityProperty,
                new Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)));
            WidthIndicatorTranslateTransform.BeginAnimation(
                Media.TranslateTransform.YProperty,
                new Animation.DoubleAnimation(4, 0, TimeSpan.FromMilliseconds(160))
                {
                    EasingFunction = new Animation.QuadraticEase
                    {
                        EasingMode = Animation.EasingMode.EaseOut
                    }
                });
        }

        _widthIndicatorTimer.Stop();
        _widthIndicatorTimer.Start();
    }

    private void WidthIndicatorTimer_Tick(object? sender, EventArgs e)
    {
        _widthIndicatorTimer.Stop();
        var animationVersion = _widthIndicatorAnimationVersion;
        var fade = new Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
        fade.Completed += (_, _) =>
        {
            if (animationVersion != _widthIndicatorAnimationVersion)
            {
                return;
            }

            WidthIndicatorPanel.Visibility = Visibility.Collapsed;
            _isWidthIndicatorVisible = false;
            UpdateWindowHeight(false);
        };
        WidthIndicatorPanel.BeginAnimation(OpacityProperty, fade);
    }

    private void AddFontColorItem(string text, Drawing.Color color)
    {
        var item = new Forms.ToolStripMenuItem(text) { Tag = color };
        item.Click += (_, _) => SetFontColor(color);
        _fontColorMenuItem.DropDownItems.Add(item);
    }

    private void ChooseCustomFontColor()
    {
        using var dialog = new Forms.ColorDialog
        {
            Color = _fontColor,
            FullOpen = true,
            AnyColor = true
        };

        var wasTopmost = Topmost;
        Topmost = false;
        try
        {
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                SetFontColor(dialog.Color);
            }
        }
        finally
        {
            Topmost = wasTopmost;
        }
    }

    private void SetFontColor(Drawing.Color color)
    {
        _fontColor = color;
        _fontColorMenuItem.ShortcutKeyDisplayString = color.ToArgb() switch
        {
            var value when value == Drawing.Color.Black.ToArgb() => "黑色",
            var value when value == Drawing.Color.White.ToArgb() => "白色",
            var value when value == ReadableBlue.ToArgb() => "蓝色",
            _ => $"#{color.R:X2}{color.G:X2}{color.B:X2}"
        };
        foreach (Forms.ToolStripItem item in _fontColorMenuItem.DropDownItems)
        {
            if (item is Forms.ToolStripMenuItem colorItem && colorItem.Tag is Drawing.Color itemColor)
            {
                colorItem.Checked = itemColor.ToArgb() == color.ToArgb();
            }
        }
        var foreground = Media.Color.FromArgb(255, color.R, color.G, color.B);
        var faded = Media.Color.FromArgb(120, color.R, color.G, color.B);

        PreviousLyricText.Foreground = new Media.SolidColorBrush(foreground);
        CurrentLyricText.Foreground = new Media.SolidColorBrush(foreground);
        CurrentLyricBaseText.Foreground = new Media.SolidColorBrush(faded);
        NextLyricText.Foreground = new Media.SolidColorBrush(foreground);
    }

    private void SetMouseThrough(bool enabled)
    {
        _isMouseThrough = enabled;
        if (enabled)
        {
            HidePlaybackControls(true);
        }
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var extendedStyle = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        extendedStyle = enabled
            ? extendedStyle | WsExTransparent
            : extendedStyle & ~WsExTransparent;
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(extendedStyle));
    }

    private void SetAlwaysOnTop(bool enabled)
    {
        _isAlwaysOnTop = enabled;
        Topmost = enabled;

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(
            handle,
            enabled ? HwndTopmost : HwndNotTopmost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate);
        _nextTopmostRefreshAt = DateTime.UtcNow.AddSeconds(2);
    }

    private void MaintainWindowLevel()
    {
        if (!_isAlwaysOnTop || !IsVisible || DateTime.UtcNow < _nextTopmostRefreshAt)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            SetWindowPos(
                handle,
                HwndTopmost,
                0,
                0,
                0,
                0,
                SwpNoMove | SwpNoSize | SwpNoActivate);
        }
        _nextTopmostRefreshAt = DateTime.UtcNow.AddSeconds(2);
    }

    private void SetLineDisplayMode(bool showThreeLines)
    {
        _showThreeLines = showThreeLines;
        if (!showThreeLines)
        {
            _transitionAnimationVersion++;
            OutgoingLyricLinesPanel.Visibility = Visibility.Collapsed;
            LyricLinesTranslateTransform.BeginAnimation(Media.TranslateTransform.YProperty, null);
            LyricLinesPanel.BeginAnimation(OpacityProperty, null);
            LyricLinesPanel.Opacity = 1;
            ResetLyricLineAnimations();
        }
        PreviousLyricText.Visibility = showThreeLines ? Visibility.Visible : Visibility.Collapsed;
        NextLyricText.Visibility = showThreeLines ? Visibility.Visible : Visibility.Collapsed;
        _singleLineMenuItem.Checked = !showThreeLines;
        _threeLineMenuItem.Checked = showThreeLines;
        _lineCountMenuItem.ShortcutKeyDisplayString = showThreeLines ? "三行" : "一行";
        UpdateWindowHeight();
        ScheduleHorizontalMarquee();
    }

    private void UpdateWindowHeight(bool preserveCenter = true)
    {
        var secondarySize = Math.Max(6, _currentFontSize * 0.72);
        var contentHeight = _showThreeLines
            ? Math.Max(80, _currentFontSize + secondarySize * 2 + 28)
            : Math.Max(44, _currentFontSize + 24);
        var newHeight = contentHeight +
                        (_isWidthIndicatorVisible ? 12 : 0) +
                        (_showPlaybackControls ? 42 : 0);

        if (IsLoaded && preserveCenter)
        {
            Top += (Height - newHeight) / 2;
        }

        Height = newHeight;
        ScheduleScreenBoundsCorrection();
    }

    private void ScheduleScreenBoundsCorrection()
    {
        if (!IsLoaded)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            new Action(EnsureWindowInScreenBounds));
    }

    private void EnsureWindowInScreenBounds()
    {
        var screenBounds = GetCurrentScreenBounds();
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var maximumLeft = Math.Max(screenBounds.Left, screenBounds.Right - width);
        var maximumTop = Math.Max(screenBounds.Top, screenBounds.Bottom - height);

        Left = Math.Clamp(Left, screenBounds.Left, maximumLeft);
        Top = Math.Clamp(Top, screenBounds.Top, maximumTop);
    }

    private Rect GetCurrentScreenBounds()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        }

        var bounds = Forms.Screen.FromHandle(handle).Bounds;
        var source = HwndSource.FromHwnd(handle);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Media.Matrix.Identity;
        var topLeft = transform.Transform(new System.Windows.Point(bounds.Left, bounds.Top));
        var bottomRight = transform.Transform(new System.Windows.Point(bounds.Right, bounds.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private void UpdateCurrentLyricClip()
    {
        CurrentLyricClip.Rect = new Rect(
            0,
            0,
            CurrentLyricText.ActualWidth * _lineProgress,
            CurrentLyricText.ActualHeight);
    }

    private void ScheduleHorizontalMarquee(bool restart = true)
    {
        _marqueeRestartTimer.Stop();
        _marqueeOffset = 0;
        _marqueeMaxOffset = 0;
        CurrentLyricTranslateTransform.BeginAnimation(Media.TranslateTransform.XProperty, null);
        CurrentLyricTranslateTransform.X = 0;
        CurrentLyricViewport.ScrollToHorizontalOffset(0);
        if (restart)
        {
            _marqueeRestartTimer.Start();
        }
    }

    private void MarqueeRestartTimer_Tick(object? sender, EventArgs e)
    {
        _marqueeRestartTimer.Stop();
        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(StartHorizontalMarquee));
    }

    private void StartHorizontalMarquee()
    {
        UpdateLyricsContentWidth();
        LyricsViewport.UpdateLayout();
        CurrentLyricViewport.UpdateLayout();
        _marqueeMaxOffset = Math.Max(0, CurrentLyricViewport.ScrollableWidth);
        CurrentLyricTranslateTransform.BeginAnimation(Media.TranslateTransform.XProperty, null);
        CurrentLyricTranslateTransform.X = 0;
        if (_marqueeMaxOffset <= 1 || !IsVisible)
        {
            CurrentLyricViewport.ScrollToHorizontalOffset(0);
            return;
        }

        UpdateCurrentLyricMarqueeProgress();
    }

    private void UpdateLyricsContentWidth()
    {
        LyricsContent.Width = _lyricWidth;
        var currentTextWidth = MeasureTextWidth(CurrentLyricBaseText, CurrentLyricBaseText.Text);
        CurrentLyricRow.Width = Math.Max(_lyricWidth, Math.Ceiling(currentTextWidth + 2));
    }

    private static double MeasureTextWidth(System.Windows.Controls.TextBlock textBlock, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var typeface = new Media.Typeface(
            textBlock.FontFamily,
            textBlock.FontStyle,
            textBlock.FontWeight,
            textBlock.FontStretch);
        var formattedText = new Media.FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            System.Windows.FlowDirection.LeftToRight,
            typeface,
            textBlock.FontSize,
            Media.Brushes.Transparent,
            Media.VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);
        return formattedText.WidthIncludingTrailingWhitespace;
    }

    private void UpdateCurrentLyricMarqueeProgress()
    {
        if (_marqueeMaxOffset <= 1 || CurrentLyricViewport.ViewportWidth <= 1)
        {
            CurrentLyricViewport.ScrollToHorizontalOffset(0);
            return;
        }

        var progress = double.IsFinite(_lineProgress)
            ? Math.Clamp(_lineProgress, 0, 1)
            : 0;
        const double leadingHold = 0.08;
        const double trailingHold = 0.10;
        var scrollingRange = 1 - leadingHold - trailingHold;
        var scrollProgress = Math.Clamp(
            (progress - leadingHold) / scrollingRange,
            0,
            1);

        _marqueeOffset = _marqueeMaxOffset * scrollProgress;
        CurrentLyricViewport.ScrollToHorizontalOffset(_marqueeOffset);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _syncTimer.Stop();
        _marqueeRestartTimer.Stop();
        _widthIndicatorTimer.Stop();
        _cdpService.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayMenu.Dispose();
        _trayIconImage.Dispose();
    }

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
