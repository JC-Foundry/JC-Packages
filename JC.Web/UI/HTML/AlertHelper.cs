using JC.Web.UI.Framework;

namespace JC.Web.UI.HTML;

/// <summary>
/// Specifies the type of alert to render.
/// </summary>
public enum AlertType
{
    /// <summary>A success alert (green).</summary>
    Success,

    /// <summary>A warning alert (yellow).</summary>
    Warning,

    /// <summary>An error/danger alert (red).</summary>
    Error,

    /// <summary>An informational alert (blue).</summary>
    Info
}

/// <summary>
/// Renders alert components using the configured UI framework's classes.
/// Registered as a singleton by <c>AddUi</c> — inject it where alerts are built in code.
/// </summary>
/// <param name="dictionary">The class dictionary for the configured framework.</param>
public class AlertHelper(IWebFrameworkDictionary dictionary)
{
    private const string DismissButton =
        "<button type=\"button\" class=\"{0}\" data-bs-dismiss=\"alert\" aria-label=\"Close\"></button>";

    /// <summary>
    /// Renders a success alert.
    /// </summary>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public string Success(string message, bool dismissible = true)
        => ForType(AlertType.Success, message, dismissible);

    /// <summary>
    /// Renders a warning alert.
    /// </summary>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public string Warning(string message, bool dismissible = true)
        => ForType(AlertType.Warning, message, dismissible);

    /// <summary>
    /// Renders an error alert.
    /// </summary>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public string Error(string message, bool dismissible = true)
        => ForType(AlertType.Error, message, dismissible);

    /// <summary>
    /// Renders an informational alert.
    /// </summary>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public string Info(string message, bool dismissible = true)
        => ForType(AlertType.Info, message, dismissible);

    /// <summary>
    /// Renders an alert for the specified <see cref="AlertType"/>.
    /// </summary>
    /// <param name="type">The alert type.</param>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public string ForType(AlertType type, string message, bool dismissible = true)
    {
        var classes = dictionary.Alert;

        var builder = new HtmlTagBuilder("div")
            .AddClass(classes.Container)
            .AddClass(classes.Variant(type))
            .AddAttribute("role", "alert");

        if (!dismissible)
            return builder.SetRawContent(message).Build();

        return builder
            .AddClass(classes.Dismissible)
            .SetRawContent(message + string.Format(DismissButton, classes.CloseButton))
            .Build();
    }
}
