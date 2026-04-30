using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SeatTooLong.Core;

namespace SeatTooLong.App.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void UpdateState(
        SittingState state,
        TimeSpan duration,
        bool isInAbsenceGracePeriod,
        string idleLabel,
        string sittingLabel,
        string absentLabel,
        string restingLabel,
        string alertLabel)
    {
        if (isInAbsenceGracePeriod)
        {
            StatusLabel.Text = absentLabel;
            TimerLabel.Text = FormatTime(duration);
            OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0xDD, 0x55, 0x55, 0x55));
            TimerLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xD5, 0x4F));
            return;
        }

        switch (state)
        {
            case SittingState.Idle:
                StatusLabel.Text = idleLabel;
                TimerLabel.Text = FormatTime(duration);
                OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0xDD, 0x33, 0x33, 0x33));
                TimerLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
                break;
            case SittingState.Sitting:
                StatusLabel.Text = sittingLabel;
                TimerLabel.Text = FormatTime(duration);
                OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0xDD, 0x33, 0x33, 0x33));
                TimerLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
                break;
            case SittingState.Alerting:
                StatusLabel.Text = alertLabel;
                TimerLabel.Text = FormatTime(duration);
                OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0xEE, 0xB7, 0x1C, 0x1C));
                TimerLabel.Foreground = Brushes.White;
                break;
            case SittingState.Resting:
                StatusLabel.Text = restingLabel;
                TimerLabel.Text = FormatTime(duration);
                OverlayBorder.Background = new SolidColorBrush(Color.FromArgb(0xDD, 0x1B, 0x5E, 0x20));
                TimerLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7));
                break;
        }
    }

    private static string FormatTime(TimeSpan ts)
    {
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    public void SetOpacity(double opacity)
    {
        OverlayBorder.Opacity = opacity;
    }
}
