namespace JC.CAP.Enums;

/// <summary>
/// What a role refusal becomes: an authenticated user reaching a page whose <c>[Authorize(Roles = ...)]</c>
/// they do not satisfy. Distinct from the identity rules' denied route, which handles a disabled account.
/// </summary>
public enum CapAccessDenied
{
    /// <summary>A plain 403 for the application to style, for instance with status-code pages.</summary>
    Forbid,

    /// <summary>A redirect to CAP's denied page, branded for the application. The user leaves the application.</summary>
    CapDeniedPage,

    /// <summary>A redirect to the application's own page at <c>CapOptions.AccessDeniedPath</c>, carrying the return URL.</summary>
    LocalPath
}
