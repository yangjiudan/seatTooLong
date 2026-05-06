namespace SeatTooLong.Core;

public class MonitoringService
{
    private readonly ICameraService _camera;
    private readonly IPersonDetector _detector;
    private readonly IFrameRecorder? _frameRecorder;
    private readonly ITimeProvider _timeProvider;

    public SittingMonitor Monitor { get; }
    public bool IsPaused { get; set; }

    public MonitoringService(
        ICameraService camera,
        IPersonDetector detector,
        SittingMonitorOptions options,
        ITimeProvider timeProvider,
        INotificationService notificationService,
        IFrameRecorder? frameRecorder = null)
    {
        _camera = camera;
        _detector = detector;
        _frameRecorder = frameRecorder;
        _timeProvider = timeProvider;
        Monitor = new SittingMonitor(options, timeProvider, notificationService);
    }

    public void Tick()
    {
        var result = AnalyzeTick();
        if (result != null)
            ApplyTickResult(result);
    }

    public MonitoringTickResult? AnalyzeTick()
    {
        if (IsPaused)
            return null;

        if (!_camera.IsAvailable)
            return null;

        var frame = _camera.CaptureFrame();
        if (frame == null)
            return null;

        bool personDetected = _detector.DetectPerson(frame.Data, frame.Width, frame.Height);
        return new MonitoringTickResult(frame, personDetected, _timeProvider.Now);
    }

    public void ApplyTickResult(MonitoringTickResult result)
    {
        Monitor.OnDetectionResult(result.PersonDetected);

        if (_frameRecorder?.IsRecording == true)
        {
            _frameRecorder.RecordFrame(result.Frame, new FrameRecordingMetadata(
                result.Timestamp,
                result.PersonDetected,
                Monitor.CurrentState,
                Monitor.CurrentStateDuration,
                Monitor.CurrentSittingDuration,
                Monitor.IsInAbsenceGracePeriod,
                Monitor.CurrentAbsenceDuration));
        }
    }

    public void Reset()
    {
        Monitor.Reset();
    }

    public sealed record MonitoringTickResult(
        CapturedFrame Frame,
        bool PersonDetected,
        DateTime Timestamp);
}
