namespace SeatTooLong.Core.Statistics;

public interface IStatisticsRepository
{
    void Initialize();
    void RecordSittingSession(DateTime startTime, DateTime endTime);
    void RecordRestSession(DateTime startTime, DateTime endTime);
    void UpsertSittingSession(DateTime startTime, DateTime endTime);
    void UpsertRestSession(DateTime startTime, DateTime endTime);
    IReadOnlyList<SittingSession> GetSittingSessions(DateTime date);
    IReadOnlyList<RestSession> GetRestSessions(DateTime date);
    DailySummary GetDailySummary(DateTime date);
    IReadOnlyList<DailySummary> GetDailySummaries(DateTime from, DateTime to);
}
