namespace SeatTooLong.Core;

public enum AlertReminderAction
{
    None,
    Show,
    Hide
}

public class AlertReminderCoordinator
{
    private readonly TimeSpan _repeatInterval;
    private DateTime? _lastReminderAt;
    private bool _isAlerting;

    public AlertReminderCoordinator(TimeSpan repeatInterval)
    {
        _repeatInterval = repeatInterval;
    }

    public AlertReminderAction OnStateChanged(SittingState newState, DateTime now)
    {
        if (newState == SittingState.Alerting)
        {
            _isAlerting = true;
            _lastReminderAt = now;
            return AlertReminderAction.Show;
        }

        if (!_isAlerting)
            return AlertReminderAction.None;

        Reset();
        return AlertReminderAction.Hide;
    }

    public AlertReminderAction OnTick(SittingState currentState, DateTime now)
    {
        if (!_isAlerting || currentState != SittingState.Alerting)
            return AlertReminderAction.None;

        if (!_lastReminderAt.HasValue || now - _lastReminderAt.Value < _repeatInterval)
            return AlertReminderAction.None;

        _lastReminderAt = now;
        return AlertReminderAction.Show;
    }

    public void Reset()
    {
        _isAlerting = false;
        _lastReminderAt = null;
    }
}