using Flurl.Http;
using JC.Github.Models.Options;
using JC.Github.Models.Responses;

namespace JC.Github.Helpers;

/// <summary>
/// HTTP client wrapper for the GitHub REST API, configured from <see cref="GithubOptions"/>.
/// </summary>
/// <param name="options">Options providing API URL, version, and user agent.</param>
/// <param name="apiKey">The GitHub API key (personal access token) used for authentication.</param>
public class GitHelper(GithubOptions options, string apiKey)
{
    private readonly FlurlClient _baseUrl = new FlurlClient(options.GithubApiUrl)
        .WithHeader("Authorization", "Bearer " + apiKey)
        .WithHeader("X-GitHub-Api-Version", options.GithubApiVersion)
        .WithHeader("User-Agent", options.GitHelperUserAgent);

    /// <summary>
    /// Creates a new issue in the specified GitHub repository.
    /// </summary>
    /// <param name="owner">The username or organisation name of the repository owner.</param>
    /// <param name="repo">The name of the repository where the issue will be created.</param>
    /// <param name="title">The title of the issue to be created.</param>
    /// <param name="desc">The description or body content of the issue.</param>
    /// <returns>The issue number of the newly created issue.</returns>
    public async Task<int> RecordIssue(string owner, string repo, string title, string desc)
    {
        var response = await _baseUrl.Request("repos", owner, repo, "issues")
            .PostJsonAsync(new
            {
                title = title, 
                body=desc
            })
            .ReceiveJson<NewIssueResponse>();
        return response.Number;
    }
}