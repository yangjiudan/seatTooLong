using Microsoft.Toolkit.Uwp.Notifications;
using SeatTooLong.Core;

namespace SeatTooLong.App.Services;

public class ToastNotificationService : INotificationService
{
    private string _lang = "zh";

    public string Language
    {
        get => _lang;
        set => _lang = value;
    }

    public void NotifySitTooLong(TimeSpan duration, TimeSpan suggestedRest)
    {
        var msg = NotificationMessageBuilder.BuildSitTooLongMessage(duration, suggestedRest, _lang);
        ShowToast(msg.Title, msg.Body);
    }

    public void NotifyRestComplete()
    {
        var msg = NotificationMessageBuilder.BuildRestCompleteMessage(_lang);
        ShowToast(msg.Title, msg.Body);
    }

    public void NotifyRestInsufficient(TimeSpan remaining)
    {
        var msg = NotificationMessageBuilder.BuildRestInsufficientMessage(remaining, _lang);
        ShowToast(msg.Title, msg.Body);
    }

    private void ShowToast(string title, string body)
    {
        new ToastContentBuilder()
            .AddText(title)
            .AddText(body)
            .Show(toast => { toast.ExpirationTime = DateTimeOffset.Now.AddSeconds(30); });
    }
}
