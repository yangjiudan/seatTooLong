namespace SeatTooLong.Core;

public interface ITimeProvider
{
    DateTime Now { get; }
}

public class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
}
