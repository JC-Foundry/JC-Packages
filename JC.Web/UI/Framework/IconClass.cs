namespace JC.Web.UI.Framework;

/// <summary>
/// Helpers for icon class values supplied by a caller rather than by a dictionary.
/// </summary>
/// <remarks>
/// The icon records hold complete values — <c>"bi bi-bell"</c>, not <c>"bi-bell"</c> — because
/// Bootstrap Icons and Font Awesome share no base class. A value that arrives from a tag helper
/// attribute or a stored record cannot be relied on to follow that rule, so it is normalised against
/// the configured set's base class before rendering. See <see cref="WithBase"/>.
/// </remarks>
public static class IconClass
{
    /// <summary>
    /// Prefixes an icon class with the icon set's base class, unless it already carries it.
    /// </summary>
    /// <param name="icon">The caller-supplied icon class.</param>
    /// <param name="baseClass">The base class the icon set requires, empty when it needs none.</param>
    /// <returns>The finished class attribute value, or an empty string when there is no icon.</returns>
    /// <remarks>
    /// This is what lets <c>"bi-star"</c> and <c>"bi bi-star"</c> both render correctly under
    /// Bootstrap Icons, so values written before the icon dictionary existed keep working. A set with
    /// no base class — Font Awesome carries its style in the glyph class itself — leaves the value
    /// untouched, so nothing is prepended where nothing belongs.
    /// </remarks>
    public static string WithBase(string? icon, string baseClass)
    {
        if (string.IsNullOrWhiteSpace(icon))
            return "";

        if (string.IsNullOrWhiteSpace(baseClass))
            return icon;

        return icon.StartsWith(baseClass + " ", StringComparison.Ordinal)
            ? icon
            : $"{baseClass} {icon}";
    }
}
