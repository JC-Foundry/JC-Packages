namespace JC.Github.Models.Options;

/// <summary>
/// Configuration options for JC.Github services.
/// </summary>
public class GithubOptions
{
    /// <summary>Gets or sets whether webhook endpoint registration is enabled. Defaults to <c>true</c>.</summary>
    public bool EnableWebhooks { get; set; } = true;

    /// <summary>Gets or sets the URL path for the GitHub webhook endpoint. Defaults to <c>/api/github/webhook</c>.</summary>
    public string WebhookPath { get; set; } = "/api/github/webhook";

    /// <summary>Gets the HMAC-SHA256 secret used to validate incoming webhook payloads from GitHub. Set from configuration.</summary>
    public string? WebhookSecret { get; internal set; }
}