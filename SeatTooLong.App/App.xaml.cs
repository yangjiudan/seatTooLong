using System.IO;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using SeatTooLong.App.Services;
using SeatTooLong.App.Views;
using SeatTooLong.Core;
using SeatTooLong.Core.Localization;
using SeatTooLong.Core.Settings;
using SeatTooLong.Core.Statistics;
using SeatTooLong.Core.Vision;

namespace SeatTooLong.App;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MonitoringService? _monitoringService;
    private DispatcherTimer? _timer;
    private DispatcherTimer? _overlayTimer;
    private OpenCvCameraService? _cameraService;
    private OverlayWindow? _overlayWindow;
    private AlertPopupWindow? _alertPopupWindow;
    private CameraPreviewWindow? _cameraPreviewWindow;
    private SqliteStatisticsRepository? _statsRepo;
    private StatisticsService? _statsService;
    private LocalizationService? _localization;
    private JsonSettingsService? _settingsService;
    private RegistryAutoStartService? _autoStartService;
    private SittingMonitorOptions? _monitorOptions;
    private ToastNotificationService? _notificationService;
    private FileFrameRecorder? _frameRecorder;
    private DnnDeskPresenceDetector? _personDetector;
    private ITimeProvider? _timeProvider;
    private readonly CameraCatalog _cameraCatalog = new();
    private readonly AlertReminderCoordinator _alertReminderCoordinator = new(TimeSpan.FromSeconds(75));
    private AppSettings _currentSettings = new();
    private CameraFailureKind _cameraFailure = CameraFailureKind.None;
    private int _monitoringTickInProgress;
    private bool _isShuttingDown;
    private string _appVersion = "unknown";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Settings
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SeatTooLong");
        Directory.CreateDirectory(appDataDir);
        _settingsService = new JsonSettingsService(Path.Combine(appDataDir, "settings.json"));
        _currentSettings = _settingsService.Load();
        _frameRecorder = new FileFrameRecorder(Path.Combine(appDataDir, "recordings"));

        // Localization
        _localization = new LocalizationService(_currentSettings.Language);
        _appVersion = AppVersionProvider.GetVersion(typeof(App).Assembly);

        // Statistics
        _statsRepo = new SqliteStatisticsRepository($"Data Source={Path.Combine(appDataDir, "stats.db")}");
        _statsRepo.Initialize();
        var timeProvider = new SystemTimeProvider();
        _timeProvider = timeProvider;
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
        var faceModelPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Models", "ultraface-version-RFB-320.onnx");
        var profileFaceCascadePath = Path.Combine(AppContext.BaseDirectory, "haarcascade_profileface.xml");
        var upperBodyCascadePath = Path.Combine(AppContext.BaseDirectory, "haarcascade_upperbody.xml");
        _personDetector = new DnnDeskPresenceDetector(faceModelPath, profileFaceCascadePath, upperBodyCascadePath);
        _cameraCatalog.UpdateKnownCameras(_cameraService.EnumerateCameras());
        var cameraIndex = _cameraCatalog.ResolveCameraIndex(_currentSettings.CameraDeviceName, _currentSettings.CameraIndex);
        _currentSettings.CameraIndex = cameraIndex;
        var cameraOpened = _cameraService.Open(cameraIndex);

        _monitoringService = new MonitoringService(
            _cameraService, _personDetector, _monitorOptions, timeProvider, _notificationService, _frameRecorder);

        // Wire statistics to state changes
        _monitoringService.Monitor.StateChanged += (_, newState) =>
        {
            var oldState = GetPreviousState(newState);
            _statsService.OnStateChanged(oldState, newState);
            HandleAlertReminderStateChanged(newState);
        };

        // Tray
        SetupTrayIcon();
        UpdateCameraFailure(cameraOpened ? CameraFailureKind.None : _cameraService.LastFailure);
        StartMonitoringTimer(TimeSpan.FromSeconds(_currentSettings.DetectionIntervalSeconds));
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

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

    private void HandleAlertReminderStateChanged(SittingState newState)
    {
        if (_timeProvider == null)
            return;

        ExecuteAlertReminderAction(_alertReminderCoordinator.OnStateChanged(newState, _timeProvider.Now));
    }

    private void UpdateAlertReminder()
    {
        if (_monitoringService == null || _timeProvider == null || _monitoringService.IsPaused)
            return;

        ExecuteAlertReminderAction(_alertReminderCoordinator.OnTick(_monitoringService.Monitor.CurrentState, _timeProvider.Now));
    }

    private void ExecuteAlertReminderAction(AlertReminderAction action)
    {
        switch (action)
        {
            case AlertReminderAction.Show:
                ShowAlertPopup();
                break;
            case AlertReminderAction.Hide:
                HideAlertPopup();
                break;
        }
    }

    private void ShowAlertPopup()
    {
        if (_monitoringService == null || _localization == null || _monitorOptions == null)
            return;

        var message = NotificationMessageBuilder.BuildSitTooLongMessage(
            _monitoringService.Monitor.CurrentSittingDuration,
            _monitorOptions.RestDuration,
            _localization.CurrentLanguage);

        _alertPopupWindow ??= new AlertPopupWindow();
        _alertPopupWindow.ShowAlert(message.Title, message.Body, _localization.Get("notify.dismiss"));
    }

    private void HideAlertPopup()
    {
        _alertPopupWindow?.HideAlert();
    }

    private void ResetAlertReminder()
    {
        _alertReminderCoordinator.Reset();
        HideAlertPopup();
    }

    private void RefreshAlertPopup()
    {
        if (_alertPopupWindow?.IsVisible != true)
            return;

        ShowAlertPopup();
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

        var idleLabelKey = _cameraFailure == CameraFailureKind.None ? "overlay.idle" : "overlay.camera_issue";
        var sittingLabelKey = _cameraFailure == CameraFailureKind.None ? "overlay.sitting" : "overlay.camera_issue";

        _overlayWindow.UpdateState(state, duration,
            isInAbsenceGracePeriod,
            _localization!.Get(idleLabelKey),
            _localization!.Get(sittingLabelKey),
            _localization.Get("overlay.absent"),
            _localization.Get("overlay.resting"),
            _localization.Get("overlay.alert"));
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = _localization!.Get("tray.tooltip"),
            Icon = LoadTrayIcon(),
            ContextMenu = CreateContextMenu()
        };
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "seattoolong-app.ico");
        if (File.Exists(iconPath))
        {
            return new System.Drawing.Icon(iconPath);
        }

        return System.Drawing.SystemIcons.Information;
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

            if (_monitoringService.IsPaused)
                ResetAlertReminder();
            else
                HandleAlertReminderStateChanged(_monitoringService.Monitor.CurrentState);
        };

        var recordItem = new System.Windows.Controls.MenuItem
        {
            Header = _frameRecorder?.IsRecording == true
                ? _localization.Get("tray.record_stop")
                : _localization.Get("tray.record_start")
        };
        recordItem.Click += (_, _) => ToggleRecording(recordItem);

        var previewItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.preview") };
        previewItem.Click += (_, _) => ShowCameraPreview();

        var todayItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.today") };
        todayItem.Click += (_, _) => ShowReport();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.settings") };
        settingsItem.Click += (_, _) => ShowSettings();

        var aboutItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.about") };
        aboutItem.Click += (_, _) => ShowAboutDeferred();

        var exitItem = new System.Windows.Controls.MenuItem { Header = _localization.Get("tray.exit") };
        exitItem.Click += (_, _) => Shutdown();

        menu.Items.Add(openItem);
        menu.Items.Add(pauseItem);
        menu.Items.Add(recordItem);
        menu.Items.Add(previewItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(todayItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(aboutItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ToggleRecording(System.Windows.Controls.MenuItem recordItem)
    {
        if (_frameRecorder == null) return;

        if (_frameRecorder.IsRecording)
        {
            _frameRecorder.StopRecording();
            recordItem.Header = _localization!.Get("tray.record_start");
        }
        else
        {
            _frameRecorder.StartRecording();
            recordItem.Header = _localization!.Get("tray.record_stop");
        }
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
        var cameras = GetSettingsCameras();
        var win = new SettingsWindow(_settingsService!, _localization!, cameras);
        win.SettingsSaved += OnSettingsSaved;
        win.Show();
    }

    private IReadOnlyList<CameraOption> GetSettingsCameras()
    {
        if (_cameraCatalog.Cameras.Count > 0)
            return _cameraCatalog.Cameras;

        var fallbackName = string.IsNullOrWhiteSpace(_currentSettings.CameraDeviceName)
            ? $"Camera {_currentSettings.CameraIndex}"
            : _currentSettings.CameraDeviceName;

        return [new CameraOption(fallbackName, _currentSettings.CameraIndex)];
    }

    private void ShowAboutDeferred()
    {
        Dispatcher.BeginInvoke(ShowAboutDialog, DispatcherPriority.ApplicationIdle);
    }

    private void ShowAboutDialog()
    {
        if (_localization == null)
            return;

        var appName = _localization.Get("app.name");
        var win = new AboutWindow(appName, _appVersion, _localization);
        win.ShowDialog();
    }

    private void ShowCameraPreview()
    {
        if (_cameraPreviewWindow != null)
        {
            if (_cameraPreviewWindow.WindowState == WindowState.Minimized)
                _cameraPreviewWindow.WindowState = WindowState.Normal;

            _cameraPreviewWindow.Activate();
            return;
        }

        if (_cameraService == null || _localization == null)
            return;

        var win = new CameraPreviewWindow(() => _cameraService.CaptureFrame(), _localization);
        win.Closed += (_, _) => _cameraPreviewWindow = null;
        _cameraPreviewWindow = win;
        win.Show();
        win.Activate();
    }

    private void OnSettingsSaved(AppSettings settings)
    {
        var cameras = GetSettingsCameras();
        var previousCameraIndex = CameraSelection.ResolveCameraIndex(cameras, _currentSettings.CameraDeviceName, _currentSettings.CameraIndex);
        settings.CameraIndex = CameraSelection.ResolveCameraIndex(cameras, settings.CameraDeviceName, settings.CameraIndex);
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
        {
            var opened = _cameraService.Open(settings.CameraIndex);
            UpdateCameraFailure(opened ? CameraFailureKind.None : _cameraService.LastFailure);
        }

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
            _trayIcon.ContextMenu = CreateContextMenu();
        }

        _cameraPreviewWindow?.ApplyLocalization(_localization);
        RefreshAlertPopup();

        RefreshStatusSurfaces();
    }

    private void StartMonitoringTimer(TimeSpan interval)
    {
        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += OnMonitoringTimerTick;
        _timer.Start();
    }

    private async void OnMonitoringTimerTick(object? sender, EventArgs e)
    {
        var monitoringService = _monitoringService;
        if (monitoringService == null)
            return;

        if (Interlocked.Exchange(ref _monitoringTickInProgress, 1) == 1)
            return;

        try
        {
            var result = await Task.Run(monitoringService.AnalyzeTick);
            if (_isShuttingDown)
                return;

            if (result != null)
            {
                UpdateCameraFailure(CameraFailureKind.None);
                monitoringService.ApplyTickResult(result);
            }
            else if (!monitoringService.IsPaused && _cameraService != null)
            {
                UpdateCameraFailure(_cameraService.LastFailure);
            }

            UpdateAlertReminder();
            _statsService?.FlushActiveSessions();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _monitoringTickInProgress, 0);
        }
    }

    private void UpdateCameraFailure(CameraFailureKind failure)
    {
        if (failure == CameraFailureKind.None)
        {
            if (_cameraFailure == CameraFailureKind.None)
                return;

            _cameraFailure = CameraFailureKind.None;
            RefreshStatusSurfaces();
            return;
        }

        if (_cameraFailure == failure)
            return;

        _cameraFailure = failure;
        RefreshStatusSurfaces();

        if (_notificationService == null)
            return;

        if (failure == CameraFailureKind.OpenFailed)
            _notificationService.NotifyCameraOpenFailed();
        else if (failure == CameraFailureKind.ReadFailed)
            _notificationService.NotifyCameraReadFailed();
    }

    private void RefreshStatusSurfaces()
    {
        if (_trayIcon != null && _localization != null)
        {
            _trayIcon.ToolTipText = _cameraFailure == CameraFailureKind.None
                ? _localization.Get("tray.tooltip")
                : _localization.Get("tray.tooltip.camera_issue");
        }

        if (_overlayWindow != null)
            UpdateOverlay();
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
            return;

        _ = Dispatcher.InvokeAsync(ResetMonitoringAfterResume, DispatcherPriority.Normal);
    }

    private void ResetMonitoringAfterResume()
    {
        _monitoringService?.Reset();
        _previousState = SittingState.Idle;
        ResetAlertReminder();
        RefreshStatusSurfaces();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isShuttingDown = true;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _timer?.Stop();
        _overlayTimer?.Stop();
        _statsService?.FlushActiveSessions();
        _overlayWindow?.Close();
        _alertPopupWindow?.Close();
        _cameraPreviewWindow?.Close();
        _frameRecorder?.Dispose();
        _trayIcon?.Dispose();
        _personDetector?.Dispose();
        _cameraService?.Dispose();
        _statsRepo?.Dispose();
        base.OnExit(e);
    }
}

