namespace JC.Core.Models.MultiTenancy;

/// <summary>
/// The tenant the current operation is scoped to, and what is known about it. Registered scoped by
/// JC.Tenancy.
/// </summary>
/// <remarks>
/// Lives in Core so a package can read the operational tenant for entities it has marked
/// <see cref="IMultiTenancy"/> without referencing JC.Tenancy. Resolve it optionally — no tenancy
/// registered means the null partition. JC.Tenancy's <c>ITenantInfo</c> extends this with the
/// members that need the concrete tenant record.
/// </remarks>
public interface ITenantContext
{
    /// <summary>
    /// Gets or sets the tenant this operation is scoped to, or <c>null</c> for the null partition.
    /// </summary>
    /// <remarks>
    /// Read live from the current user unless assigned. Assigning overrides that for the rest of the
    /// scope, including assigning <c>null</c> to pin the null partition deliberately.
    /// </remarks>
    string? TenantId { get; set; }

    /// <summary>Gets whether a tenant is in scope, as opposed to the null partition.</summary>
    bool HasTenant { get; }

    /// <summary>
    /// Gets whether the tenant was set explicitly rather than derived from the current user.
    /// </summary>
    bool IsOverridden { get; }

    /// <summary>Gets the tenant's name, or <c>null</c> in the null partition.</summary>
    string? Name { get; }

    /// <summary>Gets the tenant's description, if it has one.</summary>
    string? Description { get; }

    /// <summary>Gets the domain associated with the tenant, if it has one.</summary>
    string? Domain { get; }

    /// <summary>Gets the maximum number of users allowed in this tenant, if one is set.</summary>
    uint? MaxUsers { get; }

    /// <summary>Gets the UTC date and time this tenant expires, if one is set.</summary>
    DateTime? ExpiryDateUtc { get; }

    /// <summary>
    /// Gets whether the tenant's expiry has passed. Reported, never enforced — whether an expired
    /// tenant may still be used is application policy.
    /// </summary>
    bool IsExpired { get; }

    /// <summary>
    /// Gets the value of an active tenant setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <returns>The value, or <c>null</c> if the key is absent or the setting is inactive.</returns>
    string? GetSetting(string key);

    /// <summary>
    /// Gets the value of an active tenant setting, converted to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The value to return when the key is absent, inactive, or unconvertible.</param>
    /// <returns>The converted value, or <paramref name="defaultValue"/>.</returns>
    T? GetSetting<T>(string key, T? defaultValue = default);
}
