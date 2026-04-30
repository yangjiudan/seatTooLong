namespace SeatTooLong.Core.Statistics;

public record SittingSession
{
    public long Id { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
}

public record RestSession
{
    public long Id { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
}

public record DailySummary
{
    public DateTime Date { get; init; }
    public TimeSpan TotalSittingDuration { get; init; }
    public int StandUpCount { get; init; }
    public TimeSpan LongestContinuousSitting { get; init; }
}
