using JC.Communication.Notifications.Helpers;
using JC.Communication.Notifications.Models;

namespace JC.Communication.Notifications.Services;

public class NotificationSender
{
    private readonly NotificationService _notificationService;
    private readonly NotificationCache _cache;

    public NotificationSender(NotificationService notificationService,
        NotificationCache cache)
    {
        _notificationService = notificationService;
        _cache = cache;
    }


    public async Task<NotificationValidationResponse> SendNotification(string userId,
        string title, string body, NotificationType type, string? htmlBody = null, string? link = null, DateTime? expiryUtc = null,
        string? colourClass = null, string? iconClass = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            BodyHtml = htmlBody,
            Type = type,
            UrlLink = link,
            ExpiresAtUtc = expiryUtc
        };

        NotificationStyle? style = null;
        if(!string.IsNullOrWhiteSpace(colourClass) || !string.IsNullOrWhiteSpace(iconClass))
            style = new NotificationStyle
            {
                CustomColourClass = colourClass,
                CustomIconClass = iconClass
            };

        return await SendNotification(notification, style);
    }

    public async Task<NotificationValidationResponse> SendNotification(string userId,
        string title, string body, NotificationType type, TimeSpan expiryTimespan, string? htmlBody = null, string? link = null,
        string? colourClass = null, string? iconClass = null)
        => await SendNotification(userId, title, body, type, htmlBody, link, DateTime.UtcNow.Add(expiryTimespan), 
            colourClass, iconClass);

    public async Task<NotificationValidationResponse> SendNotification(Notification notification, NotificationStyle? style = null)
    {
        var valid = NotificationValidator.ValidateUserId(notification.UserId);
        if(!valid) return new NotificationValidationResponse("Invalid target user.");
        
        var response = await _notificationService.TryAddNotification(notification, style);
        if(!response.IsValid) return response;
        
        await _cache.AddNotificationAsync(notification);
        return response;
    }


    public async Task<(bool Result, List<NotificationValidationResponse> Responses)>
        SendNotifications(IEnumerable<Notification> notifications, params IEnumerable<NotificationStyle> styles)
    {
        var notificationsList = notifications.ToList();
        var stylesList = styles.ToList();

        if (notificationsList.Select(notification => NotificationValidator.ValidateUserId(notification.UserId)).Any(valid => !valid))
        {
            return (false, [new NotificationValidationResponse("One or more invalid target users.")]);
        }
        
        foreach (var style in stylesList)
        {
            var notification = notificationsList.FirstOrDefault(n => n.Id == style.NotificationId);
            if (notification == null)
                return (false, [new NotificationValidationResponse(
                    "One or more of the passed styles does not correspond to a given notification")]);
            
            notification.Style = style;
        }
        
        var response = await _notificationService.TryAddNotificationBatch(notificationsList);
        if (!response.Result) return response;
        
        foreach (var nvr in response.Responses)
        {
            if (nvr.ValidatedNotification == null)
                //Unlikely to happen because validation hydrates this value when valid, validation has already succeeded.
                continue;

            await _cache.AddNotificationAsync(nvr.ValidatedNotification);
        }
        
        return response;
    }
}