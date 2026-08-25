using JC.Core.Models;

namespace JC.Identity.Shared.Models;

/// <summary>
/// What an <see cref="IdentityRuleSet"/> condition is given to decide whether its set applies.
/// </summary>
/// <param name="Path">The path being requested.</param>
/// <param name="IsAuthenticated">Whether the caller is authenticated.</param>
/// <param name="User">The current user information.</param>
/// <param name="Services">
/// The request's services, or <c>null</c> where there is no scope. Lets a condition read policy it
/// could not know at registration, such as which rules an application's own administrators set.
/// </param>
public readonly record struct IdentityRuleContext(
    string Path, bool IsAuthenticated, IUserInfo User, IServiceProvider? Services);
