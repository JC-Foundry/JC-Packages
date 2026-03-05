using Flurl.Http;
using JC.Github.Models.Responses;

namespace JC.Github.Helpers;

public class GitHelper(string url, string apiKey)
{
    private readonly FlurlClient _baseUrl = new FlurlClient(url)
        .WithHeader("Authorization","Bearer "+apiKey)
        .WithHeader("X-GitHub-Api-Version", "2022-11-28")
        .WithHeader("User-Agent", "JC.Core");

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