using SeatTooLong.Core.Statistics;

namespace SeatTooLong.Tests;

public class SqliteStatisticsRepositoryTests : IDisposable
{
    private readonly SqliteStatisticsRepository _repo;

    public SqliteStatisticsRepositoryTests()
    {
        // Use in-memory SQLite for tests
        _repo = new SqliteStatisticsRepository("Data Source=:memory:");
        _repo.Initialize();
    }

    public void Dispose() => _repo.Dispose();

    [Fact]
    public void RecordSittingSession_ShouldPersist()
    {
        var start = new DateTime(2026, 4, 30, 9, 0, 0);
        var end = new DateTime(2026, 4, 30, 9, 45, 0);

        _repo.RecordSittingSession(start, end);

        var sessions = _repo.GetSittingSessions(start.Date);
        Assert.Single(sessions);
        Assert.Equal(start, sessions[0].StartTime);
        Assert.Equal(end, sessions[0].EndTime);
        Assert.Equal(TimeSpan.FromMinutes(45), sessions[0].Duration);
    }

    [Fact]
    public void RecordRestSession_ShouldPersist()
    {
        var start = new DateTime(2026, 4, 30, 9, 45, 0);
        var end = new DateTime(2026, 4, 30, 9, 50, 0);

        _repo.RecordRestSession(start, end);

        var sessions = _repo.GetRestSessions(start.Date);
        Assert.Single(sessions);
        Assert.Equal(TimeSpan.FromMinutes(5), sessions[0].Duration);
    }

    [Fact]
    public void GetSittingSessions_FiltersByDate()
    {
        _repo.RecordSittingSession(
            new DateTime(2026, 4, 29, 9, 0, 0),
            new DateTime(2026, 4, 29, 9, 30, 0));
        _repo.RecordSittingSession(
            new DateTime(2026, 4, 30, 10, 0, 0),
            new DateTime(2026, 4, 30, 10, 45, 0));

        var apr30 = _repo.GetSittingSessions(new DateTime(2026, 4, 30));
        Assert.Single(apr30);
        Assert.Equal(new DateTime(2026, 4, 30, 10, 0, 0), apr30[0].StartTime);
    }

    [Fact]
    public void GetDailySummary_CalculatesCorrectly()
    {
        var date = new DateTime(2026, 4, 30);
        _repo.RecordSittingSession(new DateTime(2026, 4, 30, 9, 0, 0), new DateTime(2026, 4, 30, 9, 45, 0));
        _repo.RecordSittingSession(new DateTime(2026, 4, 30, 10, 0, 0), new DateTime(2026, 4, 30, 10, 30, 0));
        _repo.RecordRestSession(new DateTime(2026, 4, 30, 9, 45, 0), new DateTime(2026, 4, 30, 9, 50, 0));

        var summary = _repo.GetDailySummary(date);

        Assert.Equal(date, summary.Date);
        Assert.Equal(TimeSpan.FromMinutes(75), summary.TotalSittingDuration);
        Assert.Equal(1, summary.StandUpCount); // 1 rest session = 1 stand up
        Assert.Equal(TimeSpan.FromMinutes(45), summary.LongestContinuousSitting);
    }

    [Fact]
    public void GetDailySummary_EmptyDay_ReturnsZeros()
    {
        var summary = _repo.GetDailySummary(new DateTime(2026, 5, 1));

        Assert.Equal(TimeSpan.Zero, summary.TotalSittingDuration);
        Assert.Equal(0, summary.StandUpCount);
        Assert.Equal(TimeSpan.Zero, summary.LongestContinuousSitting);
    }

    [Fact]
    public void GetDailySummaries_ReturnsRangeCorrectly()
    {
        _repo.RecordSittingSession(new DateTime(2026, 4, 28, 9, 0, 0), new DateTime(2026, 4, 28, 9, 30, 0));
        _repo.RecordSittingSession(new DateTime(2026, 4, 29, 9, 0, 0), new DateTime(2026, 4, 29, 10, 0, 0));
        _repo.RecordSittingSession(new DateTime(2026, 4, 30, 9, 0, 0), new DateTime(2026, 4, 30, 9, 45, 0));
        _repo.RecordRestSession(new DateTime(2026, 4, 29, 10, 0, 0), new DateTime(2026, 4, 29, 10, 5, 0));

        var summaries = _repo.GetDailySummaries(new DateTime(2026, 4, 28), new DateTime(2026, 4, 30));

        Assert.Equal(3, summaries.Count);
        Assert.Equal(TimeSpan.FromMinutes(30), summaries[0].TotalSittingDuration);
        Assert.Equal(TimeSpan.FromMinutes(60), summaries[1].TotalSittingDuration);
        Assert.Equal(1, summaries[1].StandUpCount);
        Assert.Equal(TimeSpan.FromMinutes(45), summaries[2].TotalSittingDuration);
    }

    [Fact]
    public void MultipleSessions_SameDay_AllReturned()
    {
        _repo.RecordSittingSession(new DateTime(2026, 4, 30, 9, 0, 0), new DateTime(2026, 4, 30, 9, 45, 0));
        _repo.RecordSittingSession(new DateTime(2026, 4, 30, 10, 0, 0), new DateTime(2026, 4, 30, 10, 30, 0));
        _repo.RecordSittingSession(new DateTime(2026, 4, 30, 11, 0, 0), new DateTime(2026, 4, 30, 11, 20, 0));

        var sessions = _repo.GetSittingSessions(new DateTime(2026, 4, 30));
        Assert.Equal(3, sessions.Count);
    }

    [Fact]
    public void UpsertSittingSession_WithSameStart_UpdatesExistingSession()
    {
        var start = new DateTime(2026, 4, 30, 9, 0, 0);

        _repo.UpsertSittingSession(start, start.AddMinutes(5));
        _repo.UpsertSittingSession(start, start.AddMinutes(12));

        var sessions = _repo.GetSittingSessions(start.Date);
        Assert.Single(sessions);
        Assert.Equal(TimeSpan.FromMinutes(12), sessions[0].Duration);
    }

    [Fact]
    public void UpsertRestSession_WithSameStart_UpdatesExistingSession()
    {
        var start = new DateTime(2026, 4, 30, 10, 0, 0);

        _repo.UpsertRestSession(start, start.AddMinutes(2));
        _repo.UpsertRestSession(start, start.AddMinutes(5));

        var sessions = _repo.GetRestSessions(start.Date);
        Assert.Single(sessions);
        Assert.Equal(TimeSpan.FromMinutes(5), sessions[0].Duration);
    }
}
