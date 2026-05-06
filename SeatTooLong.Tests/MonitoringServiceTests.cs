using Moq;
using SeatTooLong.Core;

namespace SeatTooLong.Tests;

public class MonitoringServiceTests
{
    private readonly Mock<ICameraService> _cameraMock;
    private readonly Mock<IPersonDetector> _detectorMock;
    private readonly Mock<IFrameRecorder> _recorderMock;
    private readonly Mock<ITimeProvider> _timeMock;
    private readonly Mock<INotificationService> _notifyMock;
    private readonly SittingMonitorOptions _options;
    private DateTime _currentTime;

    public MonitoringServiceTests()
    {
        _cameraMock = new Mock<ICameraService>();
        _detectorMock = new Mock<IPersonDetector>();
        _recorderMock = new Mock<IFrameRecorder>();
        _timeMock = new Mock<ITimeProvider>();
        _notifyMock = new Mock<INotificationService>();
        _currentTime = new DateTime(2026, 4, 30, 9, 0, 0);
        _timeMock.Setup(t => t.Now).Returns(() => _currentTime);
        _options = new SittingMonitorOptions
        {
            SitThreshold = TimeSpan.FromMinutes(45),
            RestDuration = TimeSpan.FromMinutes(5)
        };

        _cameraMock.Setup(c => c.IsAvailable).Returns(true);
        _cameraMock.Setup(c => c.CaptureFrame())
            .Returns(new CapturedFrame(new byte[640 * 480 * 3], 640, 480));
        _recorderMock.Setup(r => r.IsRecording).Returns(true);
    }

    private MonitoringService CreateService(IFrameRecorder? recorder = null) =>
        new MonitoringService(_cameraMock.Object, _detectorMock.Object, _options, _timeMock.Object, _notifyMock.Object, recorder);

    [Fact]
    public void AnalyzeTick_WhenPersonDetected_ShouldDelayMutationUntilResultIsApplied()
    {
        _detectorMock.Setup(d => d.DetectPerson(It.IsAny<byte[]>(), 640, 480)).Returns(true);
        var service = CreateService(_recorderMock.Object);

        var result = service.AnalyzeTick();

        Assert.NotNull(result);
        Assert.Equal(SittingState.Idle, service.Monitor.CurrentState);
        _recorderMock.Verify(r => r.RecordFrame(It.IsAny<CapturedFrame>(), It.IsAny<FrameRecordingMetadata>()), Times.Never);

        service.ApplyTickResult(result!);

        Assert.Equal(SittingState.Sitting, service.Monitor.CurrentState);
        _recorderMock.Verify(r => r.RecordFrame(
            It.Is<CapturedFrame>(f => f.Width == 640 && f.Height == 480),
            It.Is<FrameRecordingMetadata>(m =>
                m.Timestamp == _currentTime &&
                m.PersonDetected &&
                m.State == SittingState.Sitting &&
                m.CurrentStateDuration == TimeSpan.Zero &&
                m.CurrentSittingDuration == TimeSpan.Zero &&
                !m.IsInAbsenceGracePeriod)),
            Times.Once);
    }

    [Fact]
    public void Tick_WhenCameraUnavailable_ShouldNotCallDetector()
    {
        _cameraMock.Setup(c => c.IsAvailable).Returns(false);
        _cameraMock.Setup(c => c.CaptureFrame())
            .Returns((CapturedFrame?)null);
        var service = CreateService();

        service.Tick();

        _detectorMock.Verify(d => d.DetectPerson(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _recorderMock.Verify(r => r.RecordFrame(It.IsAny<CapturedFrame>(), It.IsAny<FrameRecordingMetadata>()), Times.Never);
    }

    [Fact]
    public void Tick_WhenPersonDetected_ShouldForwardToMonitor()
    {
        _detectorMock.Setup(d => d.DetectPerson(It.IsAny<byte[]>(), 640, 480)).Returns(true);
        var service = CreateService();

        service.Tick();

        Assert.Equal(SittingState.Sitting, service.Monitor.CurrentState);
    }

    [Fact]
    public void Tick_WhenNoPersonDetected_ShouldForwardFalseToMonitor()
    {
        _detectorMock.Setup(d => d.DetectPerson(It.IsAny<byte[]>(), 640, 480)).Returns(false);
        var service = CreateService();

        service.Tick();

        Assert.Equal(SittingState.Idle, service.Monitor.CurrentState);
    }

    [Fact]
    public void IsPaused_WhenTrue_ShouldNotProcessTick()
    {
        _detectorMock.Setup(d => d.DetectPerson(It.IsAny<byte[]>(), 640, 480)).Returns(true);
        var service = CreateService();
        service.IsPaused = true;

        service.Tick();

        Assert.Equal(SittingState.Idle, service.Monitor.CurrentState);
        _detectorMock.Verify(d => d.DetectPerson(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Tick_WhenRecorderIsRecording_ShouldRecordFrameWithDetectionMetadata()
    {
        _detectorMock.Setup(d => d.DetectPerson(It.IsAny<byte[]>(), 640, 480)).Returns(true);
        var service = CreateService(_recorderMock.Object);

        service.Tick();

        _recorderMock.Verify(r => r.RecordFrame(
            It.Is<CapturedFrame>(f => f.Width == 640 && f.Height == 480),
            It.Is<FrameRecordingMetadata>(m =>
                m.Timestamp == _currentTime &&
                m.PersonDetected &&
                m.State == SittingState.Sitting &&
                m.CurrentStateDuration == TimeSpan.Zero &&
                m.CurrentSittingDuration == TimeSpan.Zero &&
                !m.IsInAbsenceGracePeriod)),
            Times.Once);
    }

    [Fact]
    public void Tick_WhenRecorderIsNotRecording_ShouldNotRecordFrame()
    {
        _recorderMock.Setup(r => r.IsRecording).Returns(false);
        _detectorMock.Setup(d => d.DetectPerson(It.IsAny<byte[]>(), 640, 480)).Returns(true);
        var service = CreateService(_recorderMock.Object);

        service.Tick();

        _recorderMock.Verify(r => r.RecordFrame(It.IsAny<CapturedFrame>(), It.IsAny<FrameRecordingMetadata>()), Times.Never);
    }

    [Fact]
    public void Tick_WhenCaptureReturnsNull_ShouldNotRecordFrame()
    {
        _cameraMock.Setup(c => c.CaptureFrame()).Returns((CapturedFrame?)null);
        var service = CreateService(_recorderMock.Object);

        service.Tick();

        _recorderMock.Verify(r => r.RecordFrame(It.IsAny<CapturedFrame>(), It.IsAny<FrameRecordingMetadata>()), Times.Never);
    }

    [Fact]
    public void Reset_WhenMonitoringHadProgress_ShouldReturnToIdleAndClearDurations()
    {
        _detectorMock.Setup(d => d.DetectPerson(It.IsAny<byte[]>(), 640, 480)).Returns(true);
        var service = CreateService();

        service.Tick();
        AdvanceTime(TimeSpan.FromMinutes(10));
        service.Reset();

        Assert.Equal(SittingState.Idle, service.Monitor.CurrentState);
        Assert.Equal(TimeSpan.Zero, service.Monitor.CurrentSittingDuration);
        Assert.Equal(TimeSpan.Zero, service.Monitor.CurrentAbsenceDuration);
        Assert.False(service.Monitor.IsPersonCurrentlyDetected);
        Assert.False(service.Monitor.IsInAbsenceGracePeriod);
        Assert.True(service.Monitor.CurrentStateDuration == TimeSpan.Zero);
    }

    private void AdvanceTime(TimeSpan span) => _currentTime += span;
}
