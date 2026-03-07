// ReSharper disable InconsistentNaming
namespace JC.Web.Observability.Models;

public class UserAgent
{
    public string RawValue { get; }
    
    public string? Browser { get; }
    public string? BrowserVersion { get; }
    public string? OperatingSystem { get; }
    public string? OS => OperatingSystem;
    public string? OperatingSystemVersion { get; }
    public string? OSVersion => OperatingSystemVersion;
    public DeviceType DeviceType { get; }
    public bool IsMobile => DeviceType is DeviceType.Mobile or DeviceType.Tablet;
    public bool IsBot => DeviceType is DeviceType.Bot;

    public UserAgent(
        string rawValue,
        string? browser, 
        string? browserVersion,
        string? os, 
        string? osVersion,
        DeviceType type = DeviceType.Unknown)
    {
        RawValue = rawValue;
        
        Browser = browser;
        BrowserVersion = browserVersion;
        
        OperatingSystem = os;
        OperatingSystemVersion = osVersion;
        
        DeviceType = type;
    }
}

public enum DeviceType
{
    Desktop,
    Mobile,
    Tablet,
    Bot,
    Unknown
}