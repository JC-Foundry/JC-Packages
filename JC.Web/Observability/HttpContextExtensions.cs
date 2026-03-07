using JC.Web.Observability.Models;
using Microsoft.AspNetCore.Http;

namespace JC.Web.Observability;

public static class HttpContextExtensions
{
    public static RequestMetadata? GetRequestMetadata(this HttpContext context)
        => context.Items[typeof(RequestMetadata)] as RequestMetadata;
}