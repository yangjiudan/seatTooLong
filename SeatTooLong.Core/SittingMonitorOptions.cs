namespace SeatTooLong.Core;

public class SittingMonitorOptions
{
    public TimeSpan SitThreshold { get; set; } = TimeSpan.FromMinutes(45);
    public TimeSpan RestDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan AbsenceGracePeriod { get; set; } = TimeSpan.FromSeconds(5);
}
