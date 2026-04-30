using System.IO;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using SeatTooLong.App.Services;
using SeatTooLong.App.Views;
using SeatTooLong.Core;
using SeatTooLong.Core.Localization;
using SeatTooLong.Core.Settings;
using SeatTooLong.Core.Statistics;

namespace SeatTooLong.App;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MonitoringService? _monitoringService;
    private DispatcherTimer? _timer;
    private DispatcherTimer? _overlayTimer;
    private OpenCvCameraService? _cameraService;
    private OverlayWindow? _overlayWindow;
    private SqliteStatisticsRepository? _statsRepo;
    private StatisticsService? _statsService;
    private LocalizationService? _localization;
    private JsonSettingsService? _settingsService;
    private RegistryAutoStartService? _autoStartService;
    private SittingMonitorOptions? _monitorOptions;
    private ToastNotificationService? _notificationService;
    private AppSettings _currentSettings = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Settings
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SeatTooLong");
        Directory.CreateDirectory(appDataDir);
        _settingsService = new JsonSettingsService(Path.Combine(appDataDir, "settings.json"));
        _currentSettings = _settingsService.Load();

        // Localization
        _localization = new LocalizationService(_currentSettings.Language);

        // Statistics
        _statsRepo = new SqliteStatisticsRepository($"Data Source={Path.Combine(appDataDir, "stats.db")}");
        _statsRepo.Initialize();
        var timeProvider = new SystemTimeProvider();
        _statsService = new StatisticsService(_statsRepo, timeProvider);

        // Auto-start
        _autoStartService = new RegistryAutoStartService();
        if (_currentSettings.AutoStart && !_autoStartService.IsEnabled)
            _autoStartService.Enable();
        else if (!_currentSettings.AutoStart && _autoStartService.IsEnabled)
            _autoStartService.Disable();

        // Core monitoring
        _monitorOptions = new SittingMonitorOptions
        {
            SitThreshold = TimeSpan.FromMinutes(_currentSettings.SitThresholdMinutes),
            RestDuration = TimeSpan.FromMinutes(_currentSettings.RestDurationMinutes),
            AbsenceGracePeriod = TimeSpan.FromSeconds(_currentSettings.AbsenceGracePeriodSeconds)
        };
        _notificationService = new ToastNotificationService { Language = _localization.CurrentLanguage };
        _cameraService = new OpenCvCameraService();
        var detector = new HaarPersonDetector();
        _cameraService.Open(_currentSettings.CameraIndex);

        _monitoringService = new MonitoringService(
            _cameraService, detector, _monitorOptions, timeProvider, _notificationService);

        // Wire statistics to state changes
        _monitoringService.Monitor.StateChanged += (_, newState) =>
        {
            var oldState = GetPreviousState(newState);
            _statsService.OnStateChanged(oldState, newState);
        };

        // Tray
        SetupTrayIcon();
        StartMonitoringTimer(TimeSpan.FromSeconds(_currentSettings.DetectionIntervalSeconds));

        // Overlay
        if (_currentSettings.ShowOverlay)
            ShowOverlay();
    }

    private SittingState _previousState = SittingState.Idle;
    private SittingState GetPreviousState(SittingState newState)
    {
        var prev = _previousState;
        _previousState = newState;
        return prev;
    }

    private void ShowOverlay()
    {
        _overlayWindow = new OverlayWindow();
        _overlayWindow.SetOpacity(_currentSettings.OverlayOpacity);
        _overlayWindow.Show();

        _overlayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _overlayTimer.Tick += (_, _) => UpdateOverlay();
        _overlayTimer.Start();
    }

    private void HideOverlay()
    {
        _overlayTimer?.Stop();
        _overlayWindow?.Close();
        _overlayWindow = null;
    }

    private void UpdateOverlay()
    {
        if (_overlayWindow == null || _monitoringService == null) return;

        var state = _monitoringService.Monitor.CurrentState;
        var isInAbsenceGracePeriod = _monitoringService.Monitor.IsInAbsenceGracePeriod;
        var duration = state switch
        {
            _ when isInAbsenceGracePeriod => _monitoringService.Monitor.CurrentAbsenceDuration,
            SittingState.Idle or SittingState.Resting => _monitoringService.Monitor.CurrentStateDuration,
            _ => _monitoringService.Monitor.CurrentSittingDuration
        };

        _overlayWindow.UpdateState(state, duration,
            isInAbsenceGracePeriod,
            _localization!.Get("overlay.idle"),
            _localization!.Get("overlay.sitting"),
            _localization.Get("overlay.absent"),
            _localization.Get("overlay.resting"),
            _localization.Get("overlay.alert"));
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = _localization!.Get("tray.tooltip"),
            Icon = System.Drawing.SystemIcons.Information,
            ContextMenu = CreateContextMenu()
        };
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem { Header = _localization!.Get("tray.open") };
        openItem.Click += (_, _) => ShowMainWindow();

        var pauseItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.pause") };
        pauseItem.Click += (_, _) =>
        {
            if (_monitoringService == null) return;
            _monitoringService.IsPaused = !_monitoringService.IsPaused;
            pauseItem.Header = _monitoringService.IsPaused
                ? _localization.Get("tray.resume")
                : _localization.Get("tray.pause");
        };

        var todayItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.today") };
        todayItem.Click += (_, _) => ShowReport();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.settings") };
        settingsItem.Click += (_, _) => ShowSettings();

        var exitItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.exit") };
        exitItem.Click += (_, _) => Shutdown();

        menu.Items.Add(openItem);
        menu.Items.Add(pauseItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(todayItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ShowMainWindow()
    {
        ShowReport();
    }

    private void ShowReport()
    {
        var win = new ReportWindow(
            _statsRepo!,
            _localization!,
            () => _statsService?.FlushActiveSessions());
        win.Show();
    }

    private void ShowSettings()
    {
        var cameras = _cameraService?.EnumerateCameras() ?? new List<string>();
        var win = new SettingsWindow(_settingsService!, _localization!, cameras);
        win.SettingsSaved += OnSettingsSaved;
        win.Show();
    }

    private void OnSettingsSaved(AppSettings settings)
    {
        var previousCameraIndex = _currentSettings.CameraIndex;
        _currentSettings = settings;

        // Update localization
        _localization!.SetLanguage(settings.Language);
        if (_notificationService != null)
            _notificationService.Language = _localization.CurrentLanguage;

        // Update monitoring options
        if (_monitorOptions != null)
        {
            _monitorOptions.SitThreshold = TimeSpan.FromMinutes(settings.SitThresholdMinutes);
            _monitorOptions.RestDuration = TimeSpan.FromMinutes(settings.RestDurationMinutes);
            _monitorOptions.AbsenceGracePeriod = TimeSpan.FromSeconds(settings.AbsenceGracePeriodSeconds);
        }

        if (_monitoringService != null)
        {
            _timer?.Stop();
            StartMonitoringTimer(TimeSpan.FromSeconds(settings.DetectionIntervalSeconds));
        }

        if (_cameraService != null && settings.CameraIndex != previousCameraIndex)
            _cameraService.Open(settings.CameraIndex);

        // Update overlay
        if (settings.ShowOverlay && _overlayWindow == null)
            ShowOverlay();
        else if (!settings.ShowOverlay && _overlayWindow != null)
            HideOverlay();
        _overlayWindow?.SetOpacity(settings.OverlayOpacity);

        // Update auto-start
        if (settings.AutoStart)
            _autoStartService?.Enable();
        else
            _autoStartService?.Disable();

        // Rebuild tray menu with new language
        if (_trayIcon != null)
        {
            _trayIcon.ToolTipText = _localization.Get("tray.tooltip");
            _trayIcon.ContextMenu = CreateContextMenu();
        }
    }

    private void StartMonitoringTimer(TimeSpan interval)
    {
        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += (_, _) =>
        {
            _monitoringService?.Tick();
            _statsService?.FlushActiveSessions();
        };
        _timer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _timer?.Stop();
        _overlayTimer?.Stop();
        _statsService?.FlushActiveSessions();
        _overlayWindow?.Close();
        _trayIcon?.Dispose();
        _cameraService?.Dispose();
        _statsRepo?.Dispose();
        base.OnExit(e);
    }
}

