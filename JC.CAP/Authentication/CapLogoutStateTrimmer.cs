using OpenIddict.Client;
using static OpenIddict.Client.OpenIddictClientEvents;
using static OpenIddict.Client.OpenIddictClientHandlers;
using static OpenIddict.Client.AspNetCore.OpenIddictClientAspNetCoreConstants;

namespace JC.CAP.Authentication;

/// <summary>
/// Keeps an identity token out of the logout state token, so it can never ride the sign-out query string
/// twice.
/// </summary>
/// <remarks>
/// OpenIddict's ASP.NET Core host reads <c>.identity_token_hint</c> out of the authentication properties
/// into the sign-out context and then copies every property, that one included, into the host properties
/// bag. <c>AttachSignOutHostProperties</c> serialises the whole bag into the state token, and
/// <c>PrepareLogoutStateTokenPrincipal</c> keeps every claim it finds, so an identity token would be sent
/// again inside <c>state</c> — encrypted and base64url encoded, so larger than the copy going out as
/// <c>id_token_hint</c>. Nothing reads it back: the post-logout callback wants only the return URL.
/// <para>
/// JC.CAP's own sign-out endpoint sets no hint, so this finds nothing to remove. It is registered for the
/// application that signs out through the OpenIddict client scheme directly and sets one itself.
/// </para>
/// </remarks>
internal sealed class CapLogoutStateTrimmer : IOpenIddictClientHandler<ProcessSignOutContext>
{
    /// <summary>The descriptor registering this handler between the host's resolve and attach steps.</summary>
    public static OpenIddictClientHandlerDescriptor Descriptor { get; }
        = OpenIddictClientHandlerDescriptor.CreateBuilder<ProcessSignOutContext>()
            .UseSingletonHandler<CapLogoutStateTrimmer>()
            .SetOrder(AttachSignOutHostProperties.Descriptor.Order - 500)
            .SetType(OpenIddictClientHandlerType.Custom)
            .Build();

    /// <inheritdoc />
    public ValueTask HandleAsync(ProcessSignOutContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // context.IdentityTokenHint was populated before this, so id_token_hint still goes out.
        context.Properties.Remove(Properties.IdentityTokenHint);

        return ValueTask.CompletedTask;
    }
}
