namespace SeatTooLong.Core;

public interface INotificationService
{
    void NotifySitTooLong(TimeSpan duration, TimeSpan suggestedRest);
    void NotifyRestComplete();
    void NotifyRestInsufficient(TimeSpan remaining);
}
