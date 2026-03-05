using System.Globalization;

namespace JC.Core.Extensions;

/// <summary>
/// Extension methods for common <see cref="DateTime"/> operations including relative time formatting,
/// friendly date display, and age calculation.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a <see cref="DateTime"/> to a human-readable relative time string such as
    /// "just now", "5 minutes ago", "yesterday", or "in 3 days".
    /// Handles both past and future dates relative to <see cref="DateTime.UtcNow"/>.
    /// </summary>
    /// <param name="dateTime">The date and time to express as relative time.</param>
    /// <returns>A human-readable relative time string.</returns>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;
        var isFuture = diff.TotalSeconds < 0;
        var span = isFuture ? diff.Negate() : diff;

        var relative = span switch
        {
            { TotalSeconds: < 60 } => "just now",
            { TotalMinutes: < 2 } => "1 minute",
            { TotalMinutes: < 60 } => $"{(int)span.TotalMinutes} minutes",
            { TotalHours: < 2 } => "1 hour",
            { TotalHours: < 24 } => $"{(int)span.TotalHours} hours",
            { TotalDays: < 2 } => "yesterday",
            { TotalDays: < 7 } => $"{(int)span.TotalDays} days",
            { TotalDays: < 14 } => "1 week",
            { TotalDays: < 30 } => $"{(int)(span.TotalDays / 7)} weeks",
            { TotalDays: < 60 } => "1 month",
            { TotalDays: < 365 } => $"{(int)(span.TotalDays / 30)} months",
            { TotalDays: < 730 } => "1 year",
            _ => $"{(int)(span.TotalDays / 365)} years"
        };

        if (relative == "just now")
            return relative;

        if (isFuture)
            return relative == "yesterday" ? "tomorrow" : $"in {relative}";

        return relative == "yesterday" ? relative : $"{relative} ago";
    }

    /// <summary>
    /// Formats a <see cref="DateTime"/> as a friendly, fully-written date such as "Monday 5 March 2026".
    /// </summary>
    /// <param name="dateTime">The date to format.</param>
    /// <param name="culture">The culture to use for formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <returns>A human-friendly date string in the format "dddd d MMMM yyyy".</returns>
    public static string ToFriendlyDate(this DateTime dateTime, CultureInfo? culture = null)
        => dateTime.ToString("dddd d MMMM yyyy", culture ?? CultureInfo.CurrentCulture);

    /// <summary>
    /// Calculates a person's age in whole years from their date of birth.
    /// Correctly accounts for whether this year's birthday has occurred yet.
    /// </summary>
    /// <param name="dateOfBirth">The date of birth.</param>
    /// <returns>The age in whole years.</returns>
    public static int Age(this DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;

        if (dateOfBirth.Date > today.AddYears(-age))
            age--;

        return age;
    }
}
