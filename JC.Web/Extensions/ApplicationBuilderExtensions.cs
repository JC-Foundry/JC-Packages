using JC.Web.Security.Middleware;
using Microsoft.AspNetCore.Builder;

namespace JC.Web.Extensions;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> providing JC.Web middleware registration.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the security headers middleware to the request pipeline.
    /// Must be called after <see cref="ServiceCollectionExtensions.AddSecurityHeaders"/>.
    /// Place early in the pipeline to ensure headers are applied to all responses.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeaderMiddleware>();
        return app;
    }
}
