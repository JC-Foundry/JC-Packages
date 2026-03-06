namespace JC.Web.Security.Models;

/// <summary>
/// Specifies the value for the <c>X-Frame-Options</c> HTTP response header.
/// Controls whether a browser should be allowed to render a page in a frame or iframe.
/// </summary>
public enum XFrameOptionsMode
{
    /// <summary>The page cannot be displayed in a frame, regardless of the site attempting to do so.</summary>
    Deny,

    /// <summary>The page can only be displayed in a frame on the same origin as the page itself.</summary>
    SameOrigin
}

/// <summary>
/// Specifies the value for the <c>Referrer-Policy</c> HTTP response header.
/// Controls how much referrer information is included with requests.
/// </summary>
public enum ReferrerPolicyMode
{
    /// <summary>No referrer information is sent with any request.</summary>
    NoReferrer,

    /// <summary>Full referrer is sent for same-origin requests; only origin for cross-origin HTTPS-to-HTTPS; nothing for HTTPS-to-HTTP.</summary>
    NoReferrerWhenDowngrade,

    /// <summary>Only the origin (scheme, host, port) is sent as referrer.</summary>
    Origin,

    /// <summary>Full referrer for same-origin; only origin for cross-origin.</summary>
    OriginWhenCrossOrigin,

    /// <summary>Full referrer for same-origin requests only; nothing for cross-origin.</summary>
    SameOrigin,

    /// <summary>Origin-only for same-security-level requests; nothing for downgrades.</summary>
    StrictOrigin,

    /// <summary>Full referrer for same-origin; origin for cross-origin at same security; nothing for downgrades.</summary>
    StrictOriginWhenCrossOrigin,

    /// <summary>Full referrer is always sent. Use with caution as this may leak private information.</summary>
    UnsafeUrl
}

/// <summary>
/// Specifies the value for the <c>Cross-Origin-Opener-Policy</c> HTTP response header.
/// Controls whether a top-level document shares a browsing context group with cross-origin documents.
/// </summary>
public enum CrossOriginOpenerPolicyMode
{
    /// <summary>Allows the document to be added to its opener's browsing context group (default browser behaviour).</summary>
    UnsafeNone,

    /// <summary>Same-origin isolation but allows popups to retain a reference to the opener.</summary>
    SameOriginAllowPopups,

    /// <summary>Isolates the browsing context exclusively to same-origin documents.</summary>
    SameOrigin,

    /// <summary>Breaks opener references for cross-origin navigations while allowing popups.</summary>
    NoOpenerAllowPopups
}

/// <summary>
/// Specifies the value for the <c>Cross-Origin-Resource-Policy</c> HTTP response header.
/// Controls which origins can load the resource.
/// </summary>
public enum CrossOriginResourcePolicyMode
{
    /// <summary>Only requests from the same site (scheme + eTLD+1) can load the resource.</summary>
    SameSite,

    /// <summary>Only requests from the same origin (scheme + host + port) can load the resource.</summary>
    SameOrigin,

    /// <summary>Any origin can load the resource.</summary>
    CrossOrigin
}

/// <summary>
/// Specifies the value for the <c>Cross-Origin-Embedder-Policy</c> HTTP response header.
/// Controls whether a document can load cross-origin resources that don't explicitly grant permission.
/// </summary>
public enum CrossOriginEmbedderPolicyMode
{
    /// <summary>Allows loading cross-origin resources without CORS or CORP headers (default browser behaviour).</summary>
    UnsafeNone,

    /// <summary>Requires all cross-origin resources to have a valid <c>Cross-Origin-Resource-Policy</c> header or be served via CORS.</summary>
    RequireCorp,

    /// <summary>No-CORS cross-origin requests are sent without credentials, providing a lighter alternative to <see cref="RequireCorp"/>.</summary>
    Credentialless
}
