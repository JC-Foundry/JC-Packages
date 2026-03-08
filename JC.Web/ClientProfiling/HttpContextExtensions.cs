using JC.Web.ClientProfiling.Models;
using Microsoft.AspNetCore.Http;

namespace JC.Web.ClientProfiling;

public static class HttpContextExtensions
{
    public static RequestMetadata? GetRequestMetadata(this HttpContext context)
        => context.Items[typeof(RequestMetadata)] as RequestMetadata;
}