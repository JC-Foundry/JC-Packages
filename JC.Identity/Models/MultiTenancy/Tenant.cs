using System.Text.Json;
using JC.Core.Models.Auditing;

namespace JC.Identity.Models.MultiTenancy;

/// <summary>
/// Represents a tenant in a multi-tenancy system. Extends <see cref="AuditModel"/> for full audit trail support.
/// Tenant settings are stored as JSON and managed through the <c>SetSettings</c>/<c>GetSettings</c> methods.
/// </summary>
public sealed class Tenant : AuditModel
{
    /// <summary>Gets or sets the unique identifier for this tenant.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the tenant name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets an optional description of the tenant.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the domain associated with the tenant (indexed for lookup).</summary>
    public string? Domain { get; set; }

    /// <summary>Gets or sets the maximum number of users allowed in this tenant.</summary>
    public uint? MaxUsers { get; set; }

    /// <summary>Gets or sets the UTC date and time when this tenant expires.</summary>
    public DateTime? ExpiryDateUtc { get; set; }

    /// <summary>Gets the JSON-serialised tenant settings. Use <see cref="SetSettings"/> and <see cref="GetSettings"/> to manage.</summary>
    public string Settings { get; private set; } = "[]";

    /// <summary>
    /// Replaces all tenant settings with the provided collection.
    /// </summary>
    /// <param name="settings">The settings to serialise and store.</param>
    public void SetSettings(IEnumerable<TenantSettings> settings)
    {
        var json = JsonSerializer.Serialize(settings);
        Settings = json;
    }

    /// <summary>
    /// Adds or updates a single tenant setting by key.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <param name="isActive">Whether the setting is active. Defaults to <c>true</c>.</param>
    public void SetSetting(string key, string value, bool isActive = true)
    {
        var settings = GetSettings();
        var setting = settings.FirstOrDefault(s => s.Key == key);
        if (setting == null)
        {
            settings.Add(new TenantSettings
            {
                Key = key, 
                Value = value, 
                IsActive = isActive
            });
        }
        else
        {
            setting.Value = value;
            setting.IsActive = isActive;
        }
        SetSettings(settings);
    }
    
    /// <summary>
    /// Deserialises and returns the current tenant settings.
    /// </summary>
    /// <returns>A list of <see cref="TenantSettings"/>.</returns>
    public List<TenantSettings> GetSettings()
        => JsonSerializer.Deserialize<List<TenantSettings>>(Settings) ?? [];
}

/// <summary>
/// Represents a single key-value tenant setting with an active/inactive flag.
/// </summary>
public sealed class TenantSettings
{
    /// <summary>Gets or sets the setting key.</summary>
    public string? Key { get; set; }

    /// <summary>Gets or sets the setting value.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets whether this setting is active.</summary>
    public bool IsActive { get; set; }
} 