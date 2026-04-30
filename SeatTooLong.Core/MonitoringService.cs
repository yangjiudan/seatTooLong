namespace SeatTooLong.Core;

public class MonitoringService
{
    private readonly ICameraService _camera;
    private readonly IPersonDetector _detector;

    public SittingMonitor Monitor { get; }
    public bool IsPaused { get; set; }

    public MonitoringService(
        ICameraService camera,
        IPersonDetector detector,
        SittingMonitorOptions options,
        ITimeProvider timeProvider,
        INotificationService notificationService)
    {
        _camera = camera;
        _detector = detector;
        Monitor = new SittingMonitor(options, timeProvider, notificationService);
    }

    public void Tick()
    {
        if (IsPaused)
            return;

        if (!_camera.IsAvailable)
            return;

        var frame = _camera.CaptureFrame();
        if (frame == null)
            return;

        bool personDetected = _detector.DetectPerson(frame.Data, frame.Width, frame.Height);
        Monitor.OnDetectionResult(personDetected);
    }
}
