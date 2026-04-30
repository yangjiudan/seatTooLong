namespace SeatTooLong.Core.Settings;

public interface IAutoStartService
{
    bool IsEnabled { get; }
    void Enable();
    void Disable();
}
