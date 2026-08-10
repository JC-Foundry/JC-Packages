using System.ComponentModel;
using JC.Tenancy.Services;

namespace JC.Tenancy.Models;

/// <summary>
/// Default <see cref="ITenantInfo"/>, resolving tenant metadata from <see cref="TenantCache"/> the
/// first time any of it is read.
/// </summary>
/// <remarks>
/// Registered scoped by <c>AddTenancy</c>, which sets <see cref="ITenantInfo.TenantId"/> from the
/// signed-in user where there is one. Metadata resolution is deferred so that the query filters,
/// which read only the identifier, never trigger a store lookup.
/// </remarks>
public class TenantInfo(TenantCache cache) : ITenantInfo
{
    private Tenant? _tenant;
    private bool _resolved;
    private string? _tenantId;

    /// <inheritdoc />
    public string? TenantId
    {
        get => _tenantId;
        set
        {
            if (string.Equals(_tenantId, value, StringComparison.Ordinal)) return;

            // Scope changed - anything resolved for the previous tenant no longer applies.
            _tenantId = value;
            _tenant = null;
            _resolved = false;
        }
    }

    /// <inheritdoc />
    public bool HasTenant => !string.IsNullOrEmpty(_tenantId);

    /// <inheritdoc />
    public bool IsSetup { get; set; }

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
        _tenantId = tenant?.Id;
        _tenant = tenant;
        _resolved = true;
        IsSetup = true;
    }

    private Tenant? Resolve()
    {
        if (_resolved) return _tenant;

        _tenant = string.IsNullOrEmpty(_tenantId) ? null : cache.Get(_tenantId);
        _resolved = true;

        return _tenant;
    }
}
