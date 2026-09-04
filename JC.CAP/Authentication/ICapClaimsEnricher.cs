namespace JC.CAP.Authentication;

/// <summary>Adds claims to the session principal after the CAP translation, at sign-in and on every refresh.</summary>
/// <remarks>Resolved as <c>IEnumerable</c> from the request scope and run in registration order. JC.CAP.Tenancy stamps <c>tenant_id</c> through one of these.</remarks>
public interface ICapClaimsEnricher
{
    /// <summary>Adds claims to <see cref="CapPrincipalContext.Identity"/>.</summary>
    /// <param name="context">The identity being built and where it came from.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    Task EnrichAsync(CapPrincipalContext context, CancellationToken cancellationToken = default);
}
