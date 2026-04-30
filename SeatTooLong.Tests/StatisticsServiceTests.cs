using Moq;
using SeatTooLong.Core;
using SeatTooLong.Core.Statistics;

namespace SeatTooLong.Tests;

public class StatisticsServiceTests
{
    private readonly Mock<IStatisticsRepository> _repoMock;
    private readonly Mock<ITimeProvider> _timeMock;
    private readonly StatisticsService _service;
    private DateTime _currentTime;

    public StatisticsServiceTests()
    {
        _repoMock = new Mock<IStatisticsRepository>();
        _timeMock = new Mock<ITimeProvider>();
        _currentTime = new DateTime(2026, 4, 30, 9, 0, 0);
        _timeMock.Setup(t => t.Now).Returns(() => _currentTime);
        _service = new StatisticsService(_repoMock.Object, _timeMock.Object);
    }

    [Fact]
    public void OnStateChanged_IdleToSitting_RecordsSittingStart()
    {
        _service.OnStateChanged(SittingState.Idle, SittingState.Sitting);

        // No recording yet - session not complete
        _repoMock.Verify(r => r.RecordSittingSession(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public void OnStateChanged_SittingToAlerting_DoesNotRecordYet()
    {
        _service.OnStateChanged(SittingState.Idle, SittingState.Sitting);
        _currentTime = _currentTime.AddMinutes(45);
        _service.OnStateChanged(SittingState.Sitting, SittingState.Alerting);

        // Still sitting, session not ended
        _repoMock.Verify(r => r.RecordSittingSession(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public void OnStateChanged_AlertingToResting_RecordsSittingSession()
    {
        _service.OnStateChanged(SittingState.Idle, SittingState.Sitting);
        _currentTime = _currentTime.AddMinutes(45);
        _service.OnStateChanged(SittingState.Sitting, SittingState.Alerting);
        _currentTime = _currentTime.AddMinutes(1);
        _service.OnStateChanged(SittingState.Alerting, SittingState.Resting);

        _repoMock.Verify(r => r.UpsertSittingSession(
            new DateTime(2026, 4, 30, 9, 0, 0),
            new DateTime(2026, 4, 30, 9, 46, 0)), Times.Once);
    }

    [Fact]
    public void OnStateChanged_RestingToIdle_RecordsRestSession()
    {
        _service.OnStateChanged(SittingState.Idle, SittingState.Sitting);
        _currentTime = _currentTime.AddMinutes(45);
        _service.OnStateChanged(SittingState.Sitting, SittingState.Alerting);
        _currentTime = _currentTime.AddMinutes(1);
        _service.OnStateChanged(SittingState.Alerting, SittingState.Resting);
        _currentTime = _currentTime.AddMinutes(5);
        _service.OnStateChanged(SittingState.Resting, SittingState.Idle);

        _repoMock.Verify(r => r.UpsertRestSession(
            new DateTime(2026, 4, 30, 9, 46, 0),
            new DateTime(2026, 4, 30, 9, 51, 0)), Times.Once);
    }

    [Fact]
    public void OnStateChanged_SittingToIdle_RecordsSittingSession()
    {
        // User leaves during sitting (before alert)
        _service.OnStateChanged(SittingState.Idle, SittingState.Sitting);
        _currentTime = _currentTime.AddMinutes(20);
        _service.OnStateChanged(SittingState.Sitting, SittingState.Idle);

        _repoMock.Verify(r => r.UpsertSittingSession(
            new DateTime(2026, 4, 30, 9, 0, 0),
            new DateTime(2026, 4, 30, 9, 20, 0)), Times.Once);
    }

    [Fact]
    public void OnStateChanged_RestingToSitting_RecordsRestAndStartsNewSitting()
    {
        // User comes back too early
        _service.OnStateChanged(SittingState.Idle, SittingState.Sitting);
        _currentTime = _currentTime.AddMinutes(45);
        _service.OnStateChanged(SittingState.Sitting, SittingState.Alerting);
        _currentTime = _currentTime.AddMinutes(1);
        _service.OnStateChanged(SittingState.Alerting, SittingState.Resting);
        _currentTime = _currentTime.AddMinutes(2);
        _service.OnStateChanged(SittingState.Resting, SittingState.Sitting);

        _repoMock.Verify(r => r.UpsertRestSession(
            new DateTime(2026, 4, 30, 9, 46, 0),
            new DateTime(2026, 4, 30, 9, 48, 0)), Times.Once);
    }

    [Fact]
    public void FlushActiveSessions_WhenCurrentlySitting_RecordsOpenSittingSession()
    {
        _service.OnStateChanged(SittingState.Idle, SittingState.Sitting);
        _currentTime = _currentTime.AddMinutes(12);

        _service.FlushActiveSessions();

        _repoMock.Verify(r => r.UpsertSittingSession(
            new DateTime(2026, 4, 30, 9, 0, 0),
            new DateTime(2026, 4, 30, 9, 12, 0)), Times.Once);
    }

    [Fact]
    public void FlushActiveSessions_WhenCurrentlyResting_RecordsOpenRestSession()
    {
        _service.OnStateChanged(SittingState.Idle, SittingState.Sitting);
        _currentTime = _currentTime.AddMinutes(45);
        _service.OnStateChanged(SittingState.Sitting, SittingState.Alerting);
        _currentTime = _currentTime.AddMinutes(1);
        _service.OnStateChanged(SittingState.Alerting, SittingState.Resting);
        _currentTime = _currentTime.AddMinutes(3);

        _service.FlushActiveSessions();

        _repoMock.Verify(r => r.UpsertRestSession(
            new DateTime(2026, 4, 30, 9, 46, 0),
            new DateTime(2026, 4, 30, 9, 49, 0)), Times.Once);
    }
}
