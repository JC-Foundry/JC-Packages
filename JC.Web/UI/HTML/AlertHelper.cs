namespace JC.Web.UI.HTML;

/// <summary>
/// Specifies the type of Bootstrap alert to render.
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
/// Static helper for rendering Bootstrap 5 alert components.
/// </summary>
public static class AlertHelper
{
    private const string DismissButton =
        "<button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"alert\" aria-label=\"Close\"></button>";

    /// <summary>
    /// Renders a Bootstrap success alert.
    /// </summary>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public static string Success(string message, bool dismissible = true)
        => Alert(message, "alert-success", dismissible);

    /// <summary>
    /// Renders a Bootstrap warning alert.
    /// </summary>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public static string Warning(string message, bool dismissible = true)
        => Alert(message, "alert-warning", dismissible);

    /// <summary>
    /// Renders a Bootstrap danger alert.
    /// </summary>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public static string Error(string message, bool dismissible = true)
        => Alert(message, "alert-danger", dismissible);

    /// <summary>
    /// Renders a Bootstrap info alert.
    /// </summary>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public static string Info(string message, bool dismissible = true)
        => Alert(message, "alert-info", dismissible);

    /// <summary>
    /// Renders a Bootstrap alert for the specified <see cref="AlertType"/>.
    /// </summary>
    /// <param name="type">The alert type.</param>
    /// <param name="message">The alert message content (may contain HTML).</param>
    /// <param name="dismissible">Whether the alert can be dismissed. Defaults to <c>true</c>.</param>
    /// <returns>The rendered HTML string.</returns>
    public static string ForType(AlertType type, string message, bool dismissible = true)
        => Alert(message, BootstrapClass(type), dismissible);

    private static string Alert(string message, string cssClass, bool dismissible)
    {
        var builder = new HtmlTagBuilder("div")
            .AddClass("alert")
            .AddClass(cssClass)
            .AddAttribute("role", "alert");

        if (dismissible)
        {
            builder.AddClass("alert-dismissible")
                .AddClass("fade")
                .AddClass("show")
                .SetRawContent(message + DismissButton);
        }
        else
        {
            builder.SetRawContent(message);
        }

        return builder.Build();
    }

    private static string BootstrapClass(AlertType type) => type switch
    {
        AlertType.Success => "alert-success",
        AlertType.Warning => "alert-warning",
        AlertType.Error => "alert-danger",
        _ => "alert-info"
    };
}
