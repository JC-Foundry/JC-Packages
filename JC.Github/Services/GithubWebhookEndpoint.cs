using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JC.Github.Models.Options;
using JC.Github.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JC.Github.Services;

/// <summary>
/// Provides the GitHub webhook HTTP endpoint with HMAC-SHA256 signature validation.
/// </summary>
internal static class GithubWebhookEndpoint
{
    /// <summary>
    /// Handles an incoming GitHub webhook POST request.
    /// </summary>
    internal static async Task<IResult> HandleAsync(HttpContext context)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<GithubWebhookService>>();
        var options = context.RequestServices.GetRequiredService<IOptions<GithubOptions>>().Value;

        // Read the raw body for signature validation
        string body;
        using (var reader = new StreamReader(context.Request.Body))
            body = await reader.ReadToEndAsync();

        // Validate signature
        var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature) || !ValidateSignature(body, signature, options.WebhookSecret))
        {
            logger.LogWarning("GitHub webhook signature validation failed");
            return Results.Unauthorized();
        }

        // Get event type
        var eventType = context.Request.Headers["X-GitHub-Event"].FirstOrDefault();

        if (string.IsNullOrEmpty(eventType))
        {
            logger.LogWarning("GitHub webhook request missing X-GitHub-Event header");
            return Results.BadRequest();
        }

        // Respond to ping immediately — sent when a webhook is first registered
        if (eventType == "ping")
        {
            logger.LogInformation("Received GitHub webhook ping");
            return Results.Ok();
        }

        // Deserialise payload
        var payload = JsonSerializer.Deserialize<WebhookPayload>(body);

        if (payload?.Issue is null)
        {
            logger.LogWarning("GitHub webhook payload missing or has no issue object for event type '{EventType}'", eventType);
            return Results.BadRequest();
        }

        // Ignore pull request events — issue_comment fires for PR comments too
        if (payload.Issue.PullRequest is not null)
        {
            logger.LogDebug("Ignoring webhook event for pull request #{Number}", payload.Issue.Number);
            return Results.Ok();
        }

        // Process the event
        try
        {
            var service = context.RequestServices.GetRequiredService<GithubWebhookService>();
            await service.ProcessEventAsync(eventType, payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GitHub webhook event '{EventType}'", eventType);
            return Results.StatusCode(500);
        }

        return Results.Ok();
    }

    /// <summary>
    /// Validates the HMAC-SHA256 signature of a GitHub webhook payload.
    /// </summary>
    private static bool ValidateSignature(string payload, string signature, string secret)
    {
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(payload));

        var expected = "sha256=" + Convert.ToHexStringLower(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }
}