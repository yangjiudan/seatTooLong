namespace SeatTooLong.Core.Statistics;

public class StatisticsService
{
    private readonly IStatisticsRepository _repository;
    private readonly ITimeProvider _time;

    private DateTime? _sittingStartTime;
    private DateTime? _restStartTime;

    public StatisticsService(IStatisticsRepository repository, ITimeProvider timeProvider)
    {
        _repository = repository;
        _time = timeProvider;
    }

    public void OnStateChanged(SittingState fromState, SittingState toState)
    {
        var now = _time.Now;

        // Leaving sitting state → record sitting session
        if (fromState is SittingState.Sitting or SittingState.Alerting
            && toState is SittingState.Resting or SittingState.Idle)
        {
            if (_sittingStartTime.HasValue)
            {
                _repository.UpsertSittingSession(_sittingStartTime.Value, now);
                _sittingStartTime = null;
            }
        }

        // Leaving resting state → record rest session
        if (fromState == SittingState.Resting && toState is SittingState.Idle or SittingState.Sitting)
        {
            if (_restStartTime.HasValue)
            {
                _repository.UpsertRestSession(_restStartTime.Value, now);
                _restStartTime = null;
            }
        }

        // Entering sitting state → mark start
        if (toState == SittingState.Sitting && fromState == SittingState.Idle)
        {
            _sittingStartTime = now;
        }

        // Entering resting state → mark start
        if (toState == SittingState.Resting)
        {
            _restStartTime = now;
        }

        // Resting → Sitting (came back too early) → start new sitting
        if (fromState == SittingState.Resting && toState == SittingState.Sitting)
        {
            _sittingStartTime = now;
        }
    }

    public void FlushActiveSessions()
    {
        var now = _time.Now;

        if (_sittingStartTime.HasValue && now > _sittingStartTime.Value)
            _repository.UpsertSittingSession(_sittingStartTime.Value, now);

        if (_restStartTime.HasValue && now > _restStartTime.Value)
            _repository.UpsertRestSession(_restStartTime.Value, now);
    }
}
