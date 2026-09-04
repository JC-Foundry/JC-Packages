namespace JC.Core.Helpers;

/// <summary>Accepts only a local return URL, so no endpoint that redirects afterwards is an open redirect.</summary>
public static class LocalUrlHelper
{
    public const string Root = "/";

    /// <summary>The URL where it is local, otherwise <paramref name="fallback"/>.</summary>
    public static string OrDefault(string? url, string fallback = Root)
        => IsLocal(url) ? url! : fallback;

    // The same test as IUrlHelper.IsLocalUrl, which a minimal endpoint has no IUrlHelper to ask.
    public static bool IsLocal(string? url)
    {
        if (string.IsNullOrEmpty(url) || url.Any(char.IsControl))
            return false;

        if (url[0] == '/')
            return url.Length == 1 || (url[1] != '/' && url[1] != '\\');

        if (url[0] == '~' && url.Length > 1 && url[1] == '/')
            return url.Length == 2 || (url[2] != '/' && url[2] != '\\');

        return false;
    }
}
