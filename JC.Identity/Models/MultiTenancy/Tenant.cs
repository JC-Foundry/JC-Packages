using System.Text.Json;
using JC.Core.Models.Auditing;

namespace JC.Identity.Models.MultiTenancy;

public sealed class Tenant : AuditModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    public string? Domain { get; set; }
    public uint? MaxUsers { get; set; }
    public DateTime? ExpiryDateUtc { get; set; }
    public string Settings { get; private set; } = "[]";

    public void SetSettings(IEnumerable<TenantSettings> settings)
    {
        var json = JsonSerializer.Serialize(settings);
        Settings = json;
    }

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
    
    public List<TenantSettings> GetSettings()
        => JsonSerializer.Deserialize<List<TenantSettings>>(Settings) ?? [];
}

public sealed class TenantSettings
{
    public string? Key { get; set; }
    public string? Value { get; set; }
    public bool IsActive { get; set; }
} 