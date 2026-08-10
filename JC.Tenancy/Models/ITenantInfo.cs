namespace JC.Tenancy.Models;

/// <summary>
/// The tenant the current operation is scoped to. Registered scoped, and resolved per scope from
/// the signed-in user or set explicitly for work that has no user.
/// </summary>
/// <remarks>
/// The counterpart to <see cref="JC.Core.Models.IUserInfo"/>: that answers <i>who</i> is acting,
/// this answers <i>which tenant they are acting within</i>. The two are related but not the same —
/// a system administrator can operate inside a tenant that is not their own, and a background job
/// has a tenant with no user at all.
/// <para>
/// <see cref="TenantId"/> is resolved eagerly and costs nothing, because the EF query filters read
/// it on every query. Everything else describes the persisted <c>Tenant</c> record and is resolved
/// from the cache on first access, so an application that never reads tenant metadata never pays
/// for the lookup.
/// </para>
/// <para>
/// A <c>null</c> <see cref="TenantId"/> is the null tenant partition, which is valid. It does not
/// mean resolution failed.
/// </para>
/// </remarks>
public interface ITenantInfo
{
    /// <summary>
    /// Gets or sets the tenant this operation is scoped to, or <c>null</c> for the null partition.
    /// </summary>
    /// <remarks>
    /// Assigning this changes the operational scope and discards any metadata already resolved for
    /// the previous tenant, so the next metadata read resolves the new one.
    /// </remarks>
    string? TenantId { get; set; }

    /// <summary>Gets whether a tenant is in scope, as opposed to the null partition.</summary>
    bool HasTenant { get; }

    /// <summary>Gets whether the scope has been established, whether or not it found a tenant.</summary>
    bool IsSetup { get; set; }

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
    /// Gets whether the tenant's expiry has passed. Always <c>false</c> where no expiry is set or
    /// no tenant is in scope.
    /// </summary>
    /// <remarks>
    /// Reported, not enforced. Whether an expired tenant may still be used is application policy —
    /// the tenancy engine does not block it.
    /// </remarks>
    bool IsExpired { get; }

    /// <summary>
    /// Scopes to an already-loaded tenant, skipping the cache lookup entirely.
    /// </summary>
    /// <param name="tenant">The tenant to scope to, or <c>null</c> for the null partition.</param>
    /// <remarks>
    /// For callers holding the record already — seeding, or work immediately after creating a
    /// tenant — so a freshly written tenant is visible without waiting on the cache.
    /// </remarks>
    void SetTenant(Tenant? tenant);

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

    /// <summary>
    /// Gets the tenant's active settings.
    /// </summary>
    /// <returns>The active settings, or an empty collection in the null partition.</returns>
    IReadOnlyList<TenantSettings> GetSettings();
}
