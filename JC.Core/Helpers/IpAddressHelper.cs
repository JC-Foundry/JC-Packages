using System.Net;
using JC.Core.Enums;

namespace JC.Core.Helpers;

public class IpAddressHelper
{
    public static bool ValidateIpv4Address(string ipAddress)
    {
        if(ipAddress == "0.0.0.0/0") return true;
        
        //Get Octets:
        var octets = ipAddress.Split('.');
        if(octets.Length != 4) return false;
        
        //Check local addresses:
        if(octets[0] != "10" && octets[0] != "172" && octets[0] != "192") return false;

        //Check network type:
        var netType = octets[0] switch
        {
            "10" => NetworkFamily.Net10,
            "172" => NetworkFamily.Net172,
            "192" => NetworkFamily.Net192,
            _ => throw new ArgumentOutOfRangeException()       
        };
        
        //Ensure second octet matches network type
        switch (netType)
        {
            case NetworkFamily.Net192 when octets[1] != "168":
                return false;
            case NetworkFamily.Net172:
            {
                var res = int.TryParse(octets[1], out var i);
                if(!res || i < 16 || i > 31) return false;
                break;
            }
        }

        //Check each octet:
        var valid = true;
        foreach (var octet in octets)
        {
            //Parse out /24 etc:
            var o = octet;
            if (octet.Contains('/')) o = octet[..octet.IndexOf('/')];
            
            if (o.Length is > 3 or 0)
            {
                //Check valid length
                valid = false;
                break;
            }
            
            //Parse to int
            var res = int.TryParse(o, out var i);
            //Check valid range
            if (res && i is >= 0 and <= 255) continue;
            valid = false;
            break;
        }
        
        return valid;
    }

    public static string ParseIpAddress(IPAddress address)
    {
        var ip = address.MapToIPv4();
        return $"{ip.GetAddressBytes()[0]}.{ip.GetAddressBytes()[1]}.{ip.GetAddressBytes()[2]}.{ip.GetAddressBytes()[3]}";
    }

    public static IPAddress? ParseIpAddress(string address)
    {
        var res = IPAddress.TryParse(address, out var ip);
        return res ? ip?.MapToIPv4() : null;
    }

    public static string? EnsureIpv4(string address)
    {
        var ip = ParseIpAddress(address);
        return ip == null ? null : ParseIpAddress(ip);
    }
}