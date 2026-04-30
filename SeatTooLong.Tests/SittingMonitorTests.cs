using Moq;
using SeatTooLong.Core;

namespace SeatTooLong.Tests;

public class SittingMonitorTests
{
    private readonly Mock<ITimeProvider> _timeMock;
    private readonly Mock<INotificationService> _notifyMock;
    private readonly SittingMonitorOptions _options;
    private DateTime _currentTime;

    public SittingMonitorTests()
    {
        _timeMock = new Mock<ITimeProvider>();
        _notifyMock = new Mock<INotificationService>();
        _currentTime = new DateTime(2026, 4, 30, 9, 0, 0);
        _timeMock.Setup(t => t.Now).Returns(() => _currentTime);
        _options = new SittingMonitorOptions
        {
            SitThreshold = TimeSpan.FromMinutes(45),
            RestDuration = TimeSpan.FromMinutes(5)
        };
    }

    private SittingMonitor CreateMonitor() =>
        new SittingMonitor(_options, _timeMock.Object, _notifyMock.Object);

    private void AdvanceTime(TimeSpan span) => _currentTime += span;

    // --- 初始状态 ---

    [Fact]
    public void DefaultOptions_ShouldUseFiveSecondAbsenceGracePeriod()
    {
        var defaultOptions = new SittingMonitorOptions();

        Assert.Equal(TimeSpan.FromSeconds(5), defaultOptions.AbsenceGracePeriod);
    }

    [Fact]
    public void InitialState_ShouldBeIdle()
    {
        var monitor = CreateMonitor();
        Assert.Equal(SittingState.Idle, monitor.CurrentState);
    }

    [Fact]
    public void WhenIdle_StateDuration_ShouldAccumulate()
    {
        var monitor = CreateMonitor();

        AdvanceTime(TimeSpan.FromSeconds(12));

        Assert.True(monitor.CurrentStateDuration >= TimeSpan.FromSeconds(12));
    }

    [Fact]
    public void WhenStateChanges_StateDuration_ShouldRestart()
    {
        var monitor = CreateMonitor();
        AdvanceTime(TimeSpan.FromSeconds(12));

        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromSeconds(3));

        Assert.Equal(SittingState.Sitting, monitor.CurrentState);
        Assert.True(monitor.CurrentStateDuration >= TimeSpan.FromSeconds(3));
        Assert.True(monitor.CurrentStateDuration < TimeSpan.FromSeconds(12));
    }

    // --- Idle → Sitting ---

    [Fact]
    public void WhenPersonDetected_FromIdle_ShouldTransitionToSitting()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: true);
        Assert.Equal(SittingState.Sitting, monitor.CurrentState);
    }

    [Fact]
    public void WhenNoPersonDetected_FromIdle_ShouldStayIdle()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: false);
        Assert.Equal(SittingState.Idle, monitor.CurrentState);
    }

    // --- Sitting 计时 ---

    [Fact]
    public void WhenSitting_SittingDuration_ShouldAccumulate()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(10));
        monitor.OnDetectionResult(personDetected: true);
        Assert.True(monitor.CurrentSittingDuration >= TimeSpan.FromMinutes(10));
    }

    // --- Sitting → Alerting (到达阈值) ---

    [Fact]
    public void WhenSittingDurationReachesThreshold_ShouldTransitionToAlerting()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(45));
        monitor.OnDetectionResult(personDetected: true);
        Assert.Equal(SittingState.Alerting, monitor.CurrentState);
    }

    [Fact]
    public void WhenSittingDurationReachesThreshold_ShouldNotify()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(45));
        monitor.OnDetectionResult(personDetected: true);
        _notifyMock.Verify(
            n => n.NotifySitTooLong(It.IsAny<TimeSpan>(), _options.RestDuration),
            Times.Once);
    }

    // --- Sitting → Idle (离开超过 GracePeriod) ---

    [Fact]
    public void WhenPersonLeavesLongerThanGrace_FromSitting_ShouldResetToIdle()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(10));

        // 离开超过 grace period
        monitor.OnDetectionResult(personDetected: false);
        AdvanceTime(TimeSpan.FromSeconds(6));
        monitor.OnDetectionResult(personDetected: false);

        Assert.Equal(SittingState.Idle, monitor.CurrentState);
    }

    [Fact]
    public void WhenPersonLeavesBrieflyWithinGrace_FromSitting_ShouldStaySitting()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(10));

        // 离开少于 grace period
        monitor.OnDetectionResult(personDetected: false);
        AdvanceTime(TimeSpan.FromSeconds(3));
        monitor.OnDetectionResult(personDetected: true);

        Assert.Equal(SittingState.Sitting, monitor.CurrentState);
    }

    [Fact]
    public void WhenPersonLeavesWithinGrace_ShouldExposeAbsenceGraceState()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(10));

        monitor.OnDetectionResult(personDetected: false);
        AdvanceTime(TimeSpan.FromSeconds(5));

        Assert.Equal(SittingState.Sitting, monitor.CurrentState);
        Assert.False(monitor.IsPersonCurrentlyDetected);
        Assert.True(monitor.IsInAbsenceGracePeriod);
        Assert.True(monitor.CurrentAbsenceDuration >= TimeSpan.FromSeconds(5));
    }

    // --- Alerting → Resting (检测到离开) ---

    [Fact]
    public void WhenPersonLeaves_FromAlerting_ShouldTransitionToResting()
    {
        var monitor = CreateMonitor();
        // 进入 Alerting
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(45));
        monitor.OnDetectionResult(personDetected: true);
        Assert.Equal(SittingState.Alerting, monitor.CurrentState);

        // 离开
        monitor.OnDetectionResult(personDetected: false);
        AdvanceTime(TimeSpan.FromSeconds(6));
        monitor.OnDetectionResult(personDetected: false);

        Assert.Equal(SittingState.Resting, monitor.CurrentState);
    }

    // --- Resting → Idle (休息够了) ---

    [Fact]
    public void WhenRestDurationCompleted_ShouldTransitionToIdle()
    {
        var monitor = CreateMonitor();
        // 到 Alerting
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(45));
        monitor.OnDetectionResult(personDetected: true);
        // 到 Resting
        monitor.OnDetectionResult(personDetected: false);
        AdvanceTime(TimeSpan.FromSeconds(6));
        monitor.OnDetectionResult(personDetected: false);
        Assert.Equal(SittingState.Resting, monitor.CurrentState);

        // 休息满 5 分钟
        AdvanceTime(TimeSpan.FromMinutes(5));
        monitor.OnDetectionResult(personDetected: false);

        Assert.Equal(SittingState.Idle, monitor.CurrentState);
    }

    [Fact]
    public void WhenRestDurationCompleted_ShouldNotifyRestComplete()
    {
        var monitor = CreateMonitor();
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(45));
        monitor.OnDetectionResult(personDetected: true);
        monitor.OnDetectionResult(personDetected: false);
        AdvanceTime(TimeSpan.FromSeconds(6));
        monitor.OnDetectionResult(personDetected: false);
        AdvanceTime(TimeSpan.FromMinutes(5));
        monitor.OnDetectionResult(personDetected: false);

        _notifyMock.Verify(n => n.NotifyRestComplete(), Times.Once);
    }

    // --- Resting → 回来太早 ---

    [Fact]
    public void WhenPersonComesBackTooEarly_ShouldNotifyInsufficient()
    {
        var monitor = CreateMonitor();
        // 到 Resting
        monitor.OnDetectionResult(personDetected: true);
        AdvanceTime(TimeSpan.FromMinutes(45));
        monitor.OnDetectionResult(personDetected: true);
        monitor.OnDetectionResult(personDetected: false);
        AdvanceTime(TimeSpan.FromSeconds(6));
        monitor.OnDetectionResult(personDetected: false);
        Assert.Equal(SittingState.Resting, monitor.CurrentState);

        // 只休息了 2 分钟就回来
        AdvanceTime(TimeSpan.FromMinutes(2));
        monitor.OnDetectionResult(personDetected: true);

        _notifyMock.Verify(
            n => n.NotifyRestInsufficient(It.Is<TimeSpan>(t => t > TimeSpan.Zero)),
            Times.Once);
    }

    // --- 事件通知 ---

    [Fact]
    public void StateChanged_Event_ShouldFire_WhenStateChanges()
    {
        var monitor = CreateMonitor();
        var firedStates = new List<SittingState>();
        monitor.StateChanged += (_, state) => firedStates.Add(state);

        monitor.OnDetectionResult(personDetected: true);

        Assert.Contains(SittingState.Sitting, firedStates);
    }
}
