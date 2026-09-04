using System.Text.Json;
using CAP.SSO.Endpoints;
using CAP.SSO.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using JC.CAP.Models;
using JC.CAP.Models.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.CAP.Services;

/// <summary>
/// CAP's API, called as the application with the client-credentials token. Every endpoint identifies the
/// caller from that token, so none of them takes a client id.
/// </summary>
public class CapApiClient
{
    // Web defaults: case-insensitive, and camelCase where a contract type does not name its field.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly FlurlClient _client;
    private readonly CapAccessTokenProvider _tokens;
    private readonly ILogger<CapApiClient> _logger;

    public CapApiClient(IOptions<CapOptions> options, CapAccessTokenProvider tokens, ILogger<CapApiClient> logger)
    {
        _tokens = tokens;
        _logger = logger;

        _client = new FlurlClient(options.Value.BaseUrl).WithTimeout(TimeSpan.FromSeconds(30));
        _client.Settings.JsonSerializer = new DefaultJsonSerializer(Json);
    }

    /// <summary>How CAP is configured for this application. <c>Registration</c> decides whether to offer a register link.</summary>
    public Task<ApplicationInfoDto> GetApplicationAsync(CancellationToken cancellationToken = default)
        => GetAsync<ApplicationInfoDto>(() => _client.Request(ApiEndpoints.ApplicationApi.InfoPath), cancellationToken);

    /// <summary>The application's members whose account and membership are both live, or with <paramref name="enabledAccounts"/> false, the rest.</summary>
    public Task<IReadOnlyList<ApplicationUserDto>> GetUsersAsync(string? search = null, bool enabledAccounts = true,
        CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ApplicationUserDto>>(() => _client.Request(ApiEndpoints.UsersApi.UsersPath)
            .SetQueryParam(ApiEndpoints.SearchParam, search)
            .SetQueryParam(ApiEndpoints.UsersApi.EnabledAccountsParam, Flag(enabledAccounts)), cancellationToken);

    /// <summary>Every member of the application, enabled or not.</summary>
    public Task<IReadOnlyList<ApplicationUserDto>> GetAllUsersAsync(string? search = null,
        CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ApplicationUserDto>>(() => _client.Request(ApiEndpoints.UsersApi.AllUsersPath)
            .SetQueryParam(ApiEndpoints.SearchParam, search), cancellationToken);

    /// <summary>One member by CAP account id, or <c>null</c> where they are not a member or the filter excludes them.</summary>
    public async Task<ApplicationUserDto?> GetUserAsync(string userId, bool enabledAccounts = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var response = await SendAsync(token => _client.Request(ApiEndpoints.UsersApi.UserPath(userId))
            .SetQueryParam(ApiEndpoints.UsersApi.EnabledAccountsParam, Flag(enabledAccounts))
            .WithOAuthBearerToken(token)
            .AllowAnyHttpStatus()
            .GetAsync(cancellationToken: cancellationToken), cancellationToken);

        if (response.StatusCode == StatusCodes.Status404NotFound)
            return null;

        await EnsureSuccessAsync(response);

        return await response.GetJsonAsync<ApplicationUserDto>();
    }

    /// <summary>
    /// Publishes the role catalogue. Send the full set every time: anything CAP holds that is not named is
    /// marked stale rather than removed. An empty list is a valid publish meaning no roles.
    /// </summary>
    public async Task<CatalogueSync> PublishRolesAsync(IReadOnlyList<ApplicationRoleDto> roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var response = await SendAsync(token => _client.Request(ApiEndpoints.RolesApi.RoleCatalogueSyncPath)
            .WithOAuthBearerToken(token)
            .AllowAnyHttpStatus()
            .PostJsonAsync(roles, cancellationToken: cancellationToken), cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.GetJsonAsync<CatalogueSync>();
    }

    private async Task<T> GetAsync<T>(Func<IFlurlRequest> request, CancellationToken cancellationToken)
    {
        var response = await SendAsync(token => request()
            .WithOAuthBearerToken(token)
            .AllowAnyHttpStatus()
            .GetAsync(cancellationToken: cancellationToken), cancellationToken);

        await EnsureSuccessAsync(response);

        return await response.GetJsonAsync<T>();
    }

    // One retry after a 401: the token may have been revoked, or the process's copy gone stale.
    private async Task<IFlurlResponse> SendAsync(Func<string, Task<IFlurlResponse>> send, CancellationToken cancellationToken)
    {
        var response = await send(await _tokens.GetTokenAsync(cancellationToken));
        if (response.StatusCode != StatusCodes.Status401Unauthorized)
            return response;

        _tokens.Invalidate();

        return await send(await _tokens.GetTokenAsync(cancellationToken));
    }

    private async Task EnsureSuccessAsync(IFlurlResponse response)
    {
        if (response.StatusCode is >= 200 and < 300)
            return;

        throw await ToExceptionAsync(response);
    }

    // ApiError for CAP's own refusals and faults; ProblemDetails where the framework refused first.
    private async Task<CapApiException> ToExceptionAsync(IFlurlResponse response)
    {
        var body = await response.GetStringAsync();
        var status = response.StatusCode;

        if (TryRead<ApiError>(body) is { Error.Length: > 0 } apiError)
        {
            _logger.LogWarning("CAP's API answered {Status} ({Reason}): {Error}", status, apiError.Reason, apiError.Error);
            return new CapApiException(apiError.Error, status, apiError.Reason);
        }

        if (TryRead<ProblemDetails>(body) is { Title.Length: > 0 } problem)
        {
            _logger.LogWarning("CAP's API answered {Status}: {Title}", status, problem.Title);
            return new CapApiException(problem.Title, status);
        }

        _logger.LogWarning("CAP's API answered {Status} with no readable body.", status);
        return new CapApiException($"CAP's API answered {status}.", status);
    }

    private static T? TryRead<T>(string body) where T : class
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(body, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Always true or false, per the contract: never empty.
    private static string Flag(bool value) => value ? "true" : "false";
}
