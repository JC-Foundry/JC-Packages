namespace JC.Tenancy.Data;

/// <summary>
/// Records which context owns tenant storage, so a second registration is detected and can name the
/// first.
/// </summary>
/// <remarks>
/// Separate from the <see cref="ITenantDbContext"/> registration deliberately. That one is a factory,
/// so its descriptor carries no implementation type to report, and an application registering
/// <see cref="ITenantDbContext"/> for its own reasons would trip a guard placed on it.
/// </remarks>
/// <param name="contextType">The context type that owns tenant storage.</param>
internal sealed class TenantStoreOwner(Type contextType)
{
    /// <summary>Gets the context type that owns tenant storage.</summary>
    public Type ContextType { get; } = contextType;
}
