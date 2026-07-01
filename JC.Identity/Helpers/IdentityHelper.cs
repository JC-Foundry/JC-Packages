using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

namespace JC.Identity.Helpers;

public class IdentityHelper
{
    private readonly UrlEncoder _urlEncoder;
    private readonly string _authenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}";

    public IdentityHelper(UrlEncoder urlEncoder)
    {
        _urlEncoder = urlEncoder;
    }

    public IdentityHelper(UrlEncoder urlEncoder,
        string authenticatorUriFormat)
    {
        _urlEncoder = urlEncoder;
        _authenticatorUriFormat = authenticatorUriFormat;
    }
    
    public string Generate2faQrCodeUri(string name, string email, string unformattedKey)
        => string.Format(CultureInfo.InvariantCulture, _authenticatorUriFormat,
            name, _urlEncoder.Encode(email), unformattedKey);

    public string Format2faKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    public (string AuthenticatorUri, string FormattedKey) Generate2faKey(string name, string email, string secret)
        => (Generate2faQrCodeUri(name, email, secret), Format2faKey(secret));
}