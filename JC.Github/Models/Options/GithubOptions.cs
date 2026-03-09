namespace JC.Github.Models.Options;

/// <summary>
/// Configuration options for JC.Github services.
/// </summary>
public class GithubOptions
{
    /// <summary>Gets or sets the base URL for the GitHub REST API. Defaults to <c>https://api.github.com</c>.</summary>
    public string GithubApiUrl { get; set; } = "https://api.github.com";

    /// <summary>Gets or sets the GitHub API version header value. Defaults to <c>2022-11-28</c>.</summary>
    public string GithubApiVersion { get; set; } = "2022-11-28";

    /// <summary>Gets or sets the <c>User-Agent</c> header sent with GitHub API requests. Defaults to <c>JC-Application</c>.</summary>
    public string GitHelperUserAgent { get; set; } = "JC-Application";

    /// <summary>Gets or sets the GitHub repository owner (user or organisation) used by <see cref="JC.Github.Services.BugReportService"/>.</summary>
    public string GithubRepoOwner { get; set; } = string.Empty;

    /// <summary>Gets or sets the GitHub repository name used by <see cref="JC.Github.Services.BugReportService"/>.</summary>
    public string GithubRepoName { get; set; } = string.Empty;
    
    /// <summary>Gets or sets whether webhook endpoint registration is enabled. Defaults to <c>true</c>.</summary>
    public bool EnableWebhooks { get; set; } = true;

    /// <summary>Gets or sets the URL path for the GitHub webhook endpoint. Defaults to <c>/api/github/webhook</c>.</summary>
    public string WebhookPath { get; set; } = "/api/github/webhook";

    /// <summary>Gets the HMAC-SHA256 secret used to validate incoming webhook payloads from GitHub. Set from configuration.</summary>
    public string WebhookSecret { get; internal set; } = string.Empty;
}