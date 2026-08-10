using System.ComponentModel;
using JC.Core.Models;
using JC.Tenancy.Services;

namespace JC.Tenancy.Models;

/// <summary>
/// Default <see cref="ITenantInfo"/>, deriving the tenant from the current user unless overridden,
/// and resolving tenant metadata from <see cref="TenantCache"/> on first read.
/// </summary>
/// <remarks>
/// <paramref name="userInfo"/> is read on every access rather than captured at construction. It is
/// populated in place by the claims middleware, and this can be built earlier in the request —
/// authentication touches the DbContext, which resolves this — so a value read at construction
/// would be the unpopulated one and would pin the whole request to the null partition.
/// </remarks>
public class TenantInfo(TenantCache cache, IUserInfo? userInfo = null) : ITenantInfo
{
    private Tenant? _tenant;
    private bool _resolved;
    private string? _resolvedFor;

    private string? _overrideTenantId;

    /// <inheritdoc />
    public string? TenantId
    {
        get => IsOverridden ? _overrideTenantId : userInfo?.TenantId;
        set
        {
            _overrideTenantId = value;
            IsOverridden = true;
        }
    }

    /// <inheritdoc />
    public bool HasTenant => !string.IsNullOrEmpty(TenantId);

    /// <inheritdoc />
    public bool IsOverridden { get; private set; }

    /// <inheritdoc />
    public string? Name => Resolve()?.Name;

    /// <inheritdoc />
    public string? Description => Resolve()?.Description;

    /// <inheritdoc />
    public string? Domain => Resolve()?.Domain;

    /// <inheritdoc />
    public uint? MaxUsers => Resolve()?.MaxUsers;

    /// <inheritdoc />
    public DateTime? ExpiryDateUtc => Resolve()?.ExpiryDateUtc;

    /// <inheritdoc />
    public bool IsExpired => ExpiryDateUtc is { } expiry && expiry <= DateTime.UtcNow;

    /// <inheritdoc />
    public string? GetSetting(string key)
        => GetSettings().FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;

    /// <inheritdoc />
    public T? GetSetting<T>(string key, T? defaultValue = default)
    {
        var raw = GetSetting(key);
        if (string.IsNullOrEmpty(raw)) return defaultValue;

        try
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (!converter.CanConvertFrom(typeof(string))) return defaultValue;

            return (T?)converter.ConvertFromInvariantString(raw);
        }
        catch (Exception)
        {
            // A malformed setting value is consuming-application data, not a framework fault.
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<TenantSettings> GetSettings()
        => Resolve()?.GetSettings().Where(s => s.IsActive).ToList() ?? [];

    /// <inheritdoc />
    public void SetTenant(Tenant? tenant)
    {
        _overrideTenantId = tenant?.Id;
        IsOverridden = true;

        _tenant = tenant;
        _resolvedFor = tenant?.Id;
        _resolved = true;
    }

    private Tenant? Resolve()
    {
        var tenantId = TenantId;

        // Keyed to what was resolved, not just whether anything was: the underlying tenant can
        // change within a scope, either by override or by the claims middleware populating the user.
        if (_resolved && string.Equals(_resolvedFor, tenantId, StringComparison.Ordinal)) return _tenant;

        _tenant = string.IsNullOrEmpty(tenantId) ? null : cache.Get(tenantId);
        _resolvedFor = tenantId;
        _resolved = true;

        return _tenant;
    }
}
