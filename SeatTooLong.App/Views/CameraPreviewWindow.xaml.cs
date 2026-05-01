using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SeatTooLong.Core;
using SeatTooLong.Core.Localization;

namespace SeatTooLong.App.Views;

public partial class CameraPreviewWindow : Window
{
    private readonly Func<CapturedFrame?> _captureFrame;
    private readonly DispatcherTimer _refreshTimer;
    private string _cameraIssueText = string.Empty;
    private int _refreshInProgress;
    private bool _isClosed;

    public CameraPreviewWindow(Func<CapturedFrame?> captureFrame, ILocalizationService localization)
    {
        InitializeComponent();
        _captureFrame = captureFrame;
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _refreshTimer.Tick += OnRefreshTimerTick;
        Loaded += OnLoaded;
        Closed += OnClosed;

        ApplyLocalization(localization);
    }

    public void ApplyLocalization(ILocalizationService localization)
    {
        Title = localization.Get("camera.preview.title");
        _cameraIssueText = localization.Get("overlay.camera_issue");
        PlaceholderText.Text = _cameraIssueText;

        if (PreviewImage.Source == null)
            StatusText.Text = _cameraIssueText;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer.Start();
        _ = RefreshFrameAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        _refreshTimer.Stop();
    }

    private async void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        await RefreshFrameAsync();
    }

    private async Task RefreshFrameAsync()
    {
        if (_isClosed)
            return;

        if (Interlocked.Exchange(ref _refreshInProgress, 1) == 1)
            return;

        try
        {
            var frame = await Task.Run(_captureFrame);
            if (_isClosed)
                return;

            if (frame == null)
            {
                PreviewImage.Source = null;
                PlaceholderText.Visibility = Visibility.Visible;
                StatusText.Text = _cameraIssueText;
                return;
            }

            PreviewImage.Source = CreateBitmap(frame.Data);
            PlaceholderText.Visibility = Visibility.Collapsed;
            StatusText.Text = $"{frame.Width} x {frame.Height}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            if (_isClosed)
                return;

            PreviewImage.Source = null;
            PlaceholderText.Visibility = Visibility.Visible;
            StatusText.Text = _cameraIssueText;
        }
        finally
        {
            Interlocked.Exchange(ref _refreshInProgress, 0);
        }
    }

    private static ImageSource CreateBitmap(byte[] frameData)
    {
        using var stream = new MemoryStream(frameData);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}