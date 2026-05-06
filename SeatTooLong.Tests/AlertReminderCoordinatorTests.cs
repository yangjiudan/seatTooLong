using SeatTooLong.Core;

namespace SeatTooLong.Tests;

public class AlertReminderCoordinatorTests
{
    private static readonly DateTime BaseTime = new(2026, 5, 6, 9, 0, 0);

    [Fact]
    public void OnStateChanged_WhenEnteringAlerting_ShouldShowReminderImmediately()
    {
        var coordinator = new AlertReminderCoordinator(TimeSpan.FromSeconds(75));

        var action = coordinator.OnStateChanged(SittingState.Alerting, BaseTime);

        Assert.Equal(AlertReminderAction.Show, action);
    }

    [Fact]
    public void OnStateChanged_WhenLeavingAlerting_ShouldHideReminder()
    {
        var coordinator = new AlertReminderCoordinator(TimeSpan.FromSeconds(75));
        coordinator.OnStateChanged(SittingState.Alerting, BaseTime);

        var action = coordinator.OnStateChanged(SittingState.Resting, BaseTime.AddSeconds(5));

        Assert.Equal(AlertReminderAction.Hide, action);
    }

    [Fact]
    public void OnTick_WhenStillAlertingBeforeRepeatInterval_ShouldDoNothing()
    {
        var coordinator = new AlertReminderCoordinator(TimeSpan.FromSeconds(75));
        coordinator.OnStateChanged(SittingState.Alerting, BaseTime);

        var action = coordinator.OnTick(SittingState.Alerting, BaseTime.AddSeconds(74));

        Assert.Equal(AlertReminderAction.None, action);
    }

    [Fact]
    public void OnTick_WhenStillAlertingAfterRepeatInterval_ShouldShowReminderAgain()
    {
        var coordinator = new AlertReminderCoordinator(TimeSpan.FromSeconds(75));
        coordinator.OnStateChanged(SittingState.Alerting, BaseTime);

        var action = coordinator.OnTick(SittingState.Alerting, BaseTime.AddSeconds(75));

        Assert.Equal(AlertReminderAction.Show, action);
    }

    [Fact]
    public void Reset_ShouldClearPendingAlertReminderState()
    {
        var coordinator = new AlertReminderCoordinator(TimeSpan.FromSeconds(75));
        coordinator.OnStateChanged(SittingState.Alerting, BaseTime);
        coordinator.Reset();

        var action = coordinator.OnStateChanged(SittingState.Idle, BaseTime.AddSeconds(1));

        Assert.Equal(AlertReminderAction.None, action);
    }
}