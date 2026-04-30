namespace SeatTooLong.Core;

public class SittingMonitor
{
    private readonly SittingMonitorOptions _options;
    private readonly ITimeProvider _time;
    private readonly INotificationService _notification;

    private DateTime? _sittingStartTime;
    private DateTime? _absenceStartTime;
    private DateTime? _restStartTime;
    private DateTime _stateStartTime;

    public SittingState CurrentState { get; private set; } = SittingState.Idle;
    public TimeSpan CurrentStateDuration => _time.Now - _stateStartTime;
    public TimeSpan CurrentSittingDuration => _sittingStartTime.HasValue
        ? _time.Now - _sittingStartTime.Value
        : TimeSpan.Zero;
    public bool IsPersonCurrentlyDetected { get; private set; }
    public bool IsInAbsenceGracePeriod => _absenceStartTime.HasValue && CurrentState is SittingState.Sitting or SittingState.Alerting;
    public TimeSpan CurrentAbsenceDuration => _absenceStartTime.HasValue
        ? _time.Now - _absenceStartTime.Value
        : TimeSpan.Zero;

    public event EventHandler<SittingState>? StateChanged;

    public SittingMonitor(SittingMonitorOptions options, ITimeProvider timeProvider, INotificationService notificationService)
    {
        _options = options;
        _time = timeProvider;
        _notification = notificationService;
        _stateStartTime = _time.Now;
    }

    public void OnDetectionResult(bool personDetected)
    {
        IsPersonCurrentlyDetected = personDetected;

        switch (CurrentState)
        {
            case SittingState.Idle:
                HandleIdle(personDetected);
                break;
            case SittingState.Sitting:
                HandleSitting(personDetected);
                break;
            case SittingState.Alerting:
                HandleAlerting(personDetected);
                break;
            case SittingState.Resting:
                HandleResting(personDetected);
                break;
        }
    }

    private void HandleIdle(bool personDetected)
    {
        if (personDetected)
        {
            _sittingStartTime = _time.Now;
            _absenceStartTime = null;
            TransitionTo(SittingState.Sitting);
        }
    }

    private void HandleSitting(bool personDetected)
    {
        if (personDetected)
        {
            _absenceStartTime = null;

            if (CurrentSittingDuration >= _options.SitThreshold)
            {
                _notification.NotifySitTooLong(CurrentSittingDuration, _options.RestDuration);
                TransitionTo(SittingState.Alerting);
            }
        }
        else
        {
            if (_absenceStartTime == null)
            {
                _absenceStartTime = _time.Now;
            }
            else if (_time.Now - _absenceStartTime.Value > _options.AbsenceGracePeriod)
            {
                ResetSitting();
                TransitionTo(SittingState.Idle);
            }
        }
    }

    private void HandleAlerting(bool personDetected)
    {
        if (!personDetected)
        {
            if (_absenceStartTime == null)
            {
                _absenceStartTime = _time.Now;
            }
            else if (_time.Now - _absenceStartTime.Value > _options.AbsenceGracePeriod)
            {
                _restStartTime = _time.Now;
                _absenceStartTime = null;
                TransitionTo(SittingState.Resting);
            }
        }
        else
        {
            _absenceStartTime = null;
        }
    }

    private void HandleResting(bool personDetected)
    {
        var restElapsed = _time.Now - _restStartTime!.Value;

        if (restElapsed >= _options.RestDuration)
        {
            _notification.NotifyRestComplete();
            ResetSitting();
            TransitionTo(SittingState.Idle);
        }
        else if (personDetected)
        {
            var remaining = _options.RestDuration - restElapsed;
            _notification.NotifyRestInsufficient(remaining);
            // 回来太早，重新开始计时坐下
            _sittingStartTime = _time.Now;
            _restStartTime = null;
            _absenceStartTime = null;
            TransitionTo(SittingState.Sitting);
        }
    }

    private void TransitionTo(SittingState newState)
    {
        if (CurrentState != newState)
        {
            CurrentState = newState;
            _stateStartTime = _time.Now;
            StateChanged?.Invoke(this, newState);
        }
    }

    private void ResetSitting()
    {
        _sittingStartTime = null;
        _absenceStartTime = null;
        _restStartTime = null;
        IsPersonCurrentlyDetected = false;
    }
}
