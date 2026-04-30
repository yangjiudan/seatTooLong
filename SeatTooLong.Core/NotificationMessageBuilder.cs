namespace SeatTooLong.Core;

public record NotificationMessage(string Title, string Body);

public static class NotificationMessageBuilder
{
    public static NotificationMessage BuildSitTooLongMessage(TimeSpan duration, TimeSpan suggestedRest, string lang)
    {
        int durationMin = (int)duration.TotalMinutes;
        int restMin = (int)suggestedRest.TotalMinutes;

        if (lang == "en")
        {
            return new NotificationMessage(
                $"You've been sitting for {durationMin} minutes!",
                $"Time to stand up and move around for {restMin} minutes.");
        }

        return new NotificationMessage(
            $"您已连续坐了 {durationMin} 分钟！",
            $"建议站起来活动 {restMin} 分钟。");
    }

    public static NotificationMessage BuildRestCompleteMessage(string lang)
    {
        if (lang == "en")
        {
            return new NotificationMessage(
                "Rest complete!",
                "Great job! You can sit back down now.");
        }

        return new NotificationMessage(
            "休息完成！",
            "做得好！可以回到座位继续工作了。");
    }

    public static NotificationMessage BuildRestInsufficientMessage(TimeSpan remaining, string lang)
    {
        int remainingMin = (int)Math.Ceiling(remaining.TotalMinutes);

        if (lang == "en")
        {
            return new NotificationMessage(
                "Rest not enough",
                $"You still need about {remainingMin} minutes of rest.");
        }

        return new NotificationMessage(
            "休息不足",
            $"建议再休息 {remainingMin} 分钟。");
    }
}
