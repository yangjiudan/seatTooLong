namespace SeatTooLong.Core;

public enum SittingState
{
    Idle,       // 无人在座
    Sitting,    // 久坐中（计时累加）
    Alerting,   // 提醒中（等待离开）
    Resting     // 休息中（倒计时）
}
