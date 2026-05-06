using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SeatTooLong.App.Views;

public partial class AlertPopupWindow : Window
{
    private static readonly TimeSpan AutoCloseDuration = TimeSpan.FromSeconds(12);
    private readonly DispatcherTimer _autoCloseTimer;

    public AlertPopupWindow()
    {
        InitializeComponent();

        _autoCloseTimer = new DispatcherTimer { Interval = AutoCloseDuration };
        _autoCloseTimer.Tick += AutoCloseTimer_Tick;
        SourceInitialized += (_, _) => ApplyExtendedStyles();
        Loaded += (_, _) => PositionWindow();
        Closed += (_, _) => _autoCloseTimer.Stop();
    }

    public void ShowAlert(string title, string body, string dismissLabel)
    {
        TitleText.Text = title;
        BodyText.Text = body;
        DismissButton.Content = dismissLabel;

        if (!IsVisible)
            Show();

        UpdateLayout();
        PositionWindow();
        RestartAutoCloseTimer();
    }

    public void HideAlert()
    {
        _autoCloseTimer.Stop();

        if (IsVisible)
            Hide();
    }

    private void RestartAutoCloseTimer()
    {
        _autoCloseTimer.Stop();
        _autoCloseTimer.Start();
    }

    private void AutoCloseTimer_Tick(object? sender, EventArgs e)
    {
        HideAlert();
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        HideAlert();
    }

    private void ApplyExtendedStyles()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
            return;

        var styles = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        styles |= WsExToolWindow | WsExNoActivate;
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(styles));
        RepositionWindow(handle);
    }

    private void PositionWindow()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
            return;

        RepositionWindow(handle);
    }

    private void RepositionWindow(IntPtr handle)
    {
        var workArea = GetTargetWorkArea();
        var width = (int)Math.Ceiling(ActualWidth > 0 ? ActualWidth : Width);
        var height = (int)Math.Ceiling(ActualHeight > 0 ? ActualHeight : Height);
        var left = workArea.Right - width - PopupMargin;
        var top = workArea.Bottom - height - PopupMargin;

        SetWindowPos(
            handle,
            HwndTopmost,
            left,
            top,
            width,
            height,
            SwpNoActivate | SwpNoOwnerZOrder | SwpShowWindow);
    }

    private static RectInt GetTargetWorkArea()
    {
        var foregroundWindow = GetForegroundWindow();
        var monitor = foregroundWindow != IntPtr.Zero
            ? MonitorFromWindow(foregroundWindow, MonitorDefaultToNearest)
            : MonitorFromPoint(new PointInt(), MonitorDefaultToPrimary);

        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref monitorInfo))
                return monitorInfo.rcWork;
        }

        return new RectInt
        {
            Left = (int)SystemParameters.WorkArea.Left,
            Top = (int)SystemParameters.WorkArea.Top,
            Right = (int)SystemParameters.WorkArea.Right,
            Bottom = (int)SystemParameters.WorkArea.Bottom
        };
    }

    private static IntPtr GetWindowLongPtr(IntPtr handle, int index)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(handle, index)
            : new IntPtr(GetWindowLong32(handle, index));
    }

    private static void SetWindowLongPtr(IntPtr handle, int index, IntPtr newValue)
    {
        if (IntPtr.Size == 8)
            SetWindowLongPtr64(handle, index, newValue);
        else
            SetWindowLong32(handle, index, newValue.ToInt32());
    }

    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const int MonitorDefaultToPrimary = 1;
    private const int MonitorDefaultToNearest = 2;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpShowWindow = 0x0040;
    private const int PopupMargin = 16;
    private static readonly IntPtr HwndTopmost = new(-1);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(PointInt point, int flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr handle, int index, int newValue);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr handle, int index, IntPtr newValue);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr handle,
        IntPtr insertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct PointInt
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RectInt
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public RectInt rcMonitor;
        public RectInt rcWork;
        public int dwFlags;
    }
}