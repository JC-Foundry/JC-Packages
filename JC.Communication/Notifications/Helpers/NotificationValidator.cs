using JC.Communication.Notifications.Models;

namespace JC.Communication.Notifications.Helpers;

/// <summary>
/// Provides validation logic for <see cref="Notification"/> and <see cref="NotificationStyle"/> entities
/// before persistence.
/// </summary>
public static class NotificationValidator
{
    /// <summary>
    /// Validates a notification.
    /// </summary>
    /// <param name="notification">The notification to validate.</param>
    /// <returns>A <see cref="NotificationValidationResponse"/> indicating success or containing validation errors.</returns>
    public static NotificationValidationResponse Validate(Notification notification)
    {
        var errors = ValidateNotification(notification);
        return string.IsNullOrEmpty(errors)
            ? new NotificationValidationResponse(notification)
            : new NotificationValidationResponse(errors);
    }

    /// <summary>
    /// Validates a notification and its associated style.
    /// </summary>
    /// <param name="notification">The notification to validate.</param>
    /// <param name="style">The style to validate.</param>
    /// <returns>A <see cref="NotificationValidationResponse"/> indicating success or containing validation errors.</returns>
    public static NotificationValidationResponse Validate(Notification notification, NotificationStyle style)
    {
        var notifErrors = ValidateNotification(notification);
        var styleErrors = ValidateStyle(style);
        return string.IsNullOrEmpty(notifErrors) && string.IsNullOrEmpty(styleErrors)
            ? new NotificationValidationResponse(notification, style)
            : new NotificationValidationResponse($"{(string.IsNullOrEmpty(notifErrors)
                ? styleErrors
                : string.IsNullOrEmpty(styleErrors)
                    ? notifErrors
                    : $"{notifErrors}{Environment.NewLine}{styleErrors}")}");
    }

    /// <summary>
    /// Validates notification properties including title, body, read state, user, and length constraints.
    /// </summary>
    /// <param name="notification">The notification to validate.</param>
    /// <returns>A newline-delimited error string, or <c>null</c> if valid.</returns>
    private static string? ValidateNotification(Notification notification)
    {
        var errorMessage = string.Empty;
        if(string.IsNullOrWhiteSpace(notification.Title))
            errorMessage = AppendError(errorMessage, "Title is required.");
        
        if(string.IsNullOrWhiteSpace(notification.Body))
            errorMessage = AppendError(errorMessage, "Body is required.");
        
        if(notification.IsRead || notification.ReadAtUtc.HasValue)
            errorMessage = AppendError(errorMessage, "Notification is already read.");
        
        var isGuid = Guid.TryParse(notification.UserId, out _);
        if(!isGuid || string.IsNullOrWhiteSpace(notification.UserId))
            errorMessage = AppendError(errorMessage, "A valid target user is required.");
        
        if(notification.Title.Length > 255)
            errorMessage = AppendError(errorMessage, "Title cannot exceed 255 characters.");
        
        if(notification.Body.Length > 8192)
            errorMessage = AppendError(errorMessage, "Body cannot exceed 8,192 characters.");

        return string.IsNullOrWhiteSpace(errorMessage)
            ? null
            : errorMessage;
    }

    /// <summary>
    /// Validates that at least one custom style property is set.
    /// </summary>
    /// <param name="style">The style to validate.</param>
    /// <returns>A newline-delimited error string, or <c>null</c> if valid.</returns>
    private static string? ValidateStyle(NotificationStyle style)
    {
        var errorMessage = string.Empty;
        if(string.IsNullOrWhiteSpace(style.CustomColourClass) 
           && string.IsNullOrWhiteSpace(style.CustomIconClass))
            errorMessage = AppendError(errorMessage, "At least one custom style is required.");
        
        return string.IsNullOrWhiteSpace(errorMessage)
            ? null
            : errorMessage;
    }
    
    /// <summary>
    /// Appends an error message to an existing error string, separated by a newline.
    /// </summary>
    /// <param name="errors">The existing error string.</param>
    /// <param name="err">The error message to append.</param>
    /// <returns>The combined error string.</returns>
    private static string AppendError(string errors, string err)
    {
        if(!string.IsNullOrEmpty(errors)) errors += Environment.NewLine;
        return errors + err;
    }
}

/// <summary>
/// Represents the result of a notification validation operation.
/// Contains either the validated entities on success, or an error message on failure.
/// </summary>
public sealed class NotificationValidationResponse
{
    /// <summary>Gets whether the validation passed.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the validated notification, or <c>null</c> if validation failed.</summary>
    public Notification? ValidatedNotification { get; }

    /// <summary>Gets the validated style, or <c>null</c> if no style was provided or validation failed.</summary>
    public NotificationStyle? ValidatedStyle { get; }

    /// <summary>Gets the validation error message, or <c>null</c> if validation passed.</summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful validation response.
    /// </summary>
    /// <param name="notification">The validated notification.</param>
    /// <param name="style">The optional validated style.</param>
    public NotificationValidationResponse(Notification notification, NotificationStyle? style = null)
    {
        IsValid = true;
        ValidatedNotification = notification;
        ValidatedStyle = style;
    }

    /// <summary>
    /// Creates a failed validation response.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public NotificationValidationResponse(string errorMessage)
    {
        IsValid = false;
        ErrorMessage = errorMessage;
    }
}