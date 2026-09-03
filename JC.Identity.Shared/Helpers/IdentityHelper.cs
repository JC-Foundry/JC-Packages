using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using JC.Identity.Shared.Models;

namespace JC.Identity.Shared.Helpers;

/// <summary>
/// Formatting helpers for authenticator-app two-factor setup: the <c>otpauth://</c> URI a QR code
/// encodes, and the human-readable grouping of the shared key shown beside it.
/// </summary>
public class IdentityHelper
{
    private readonly UrlEncoder _urlEncoder;
    private readonly string _authenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}";

    /// <summary>
    /// Initialises a new instance using the standard <c>otpauth://totp</c> URI format.
    /// </summary>
    /// <param name="urlEncoder">The encoder used to escape the email in the generated URI.</param>
    public IdentityHelper(UrlEncoder urlEncoder)
    {
        _urlEncoder = urlEncoder;
    }

    /// <summary>
    /// Initialises a new instance using a custom authenticator URI format.
    /// </summary>
    /// <param name="urlEncoder">The encoder used to escape the email in the generated URI.</param>
    /// <param name="authenticatorUriFormat">
    /// A composite format string taking the issuer name, the encoded email and the shared secret,
    /// in that order.
    /// </param>
    public IdentityHelper(UrlEncoder urlEncoder,
        string authenticatorUriFormat)
    {
        _urlEncoder = urlEncoder;
        _authenticatorUriFormat = authenticatorUriFormat;
    }

    /// <summary>
    /// Builds the authenticator URI for a user, suitable for encoding into a QR code.
    /// </summary>
    /// <param name="name">The issuer name, usually the application name.</param>
    /// <param name="email">The user's email address, used as the account label.</param>
    /// <param name="unformattedKey">The shared secret, unformatted.</param>
    /// <returns>The <c>otpauth://</c> URI.</returns>
    public string Generate2faQrCodeUri(string name, string email, string unformattedKey)
        => string.Format(CultureInfo.InvariantCulture, _authenticatorUriFormat,
            name, _urlEncoder.Encode(email), unformattedKey);

    /// <summary>
    /// Splits a shared secret into space-separated groups of four for display.
    /// </summary>
    /// <param name="unformattedKey">The shared secret, unformatted.</param>
    /// <returns>The key in lowercase, grouped in fours.</returns>
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

    /// <summary>
    /// Builds both halves of an authenticator setup screen in one call.
    /// </summary>
    /// <param name="name">The issuer name, usually the application name.</param>
    /// <param name="email">The user's email address, used as the account label.</param>
    /// <param name="secret">The shared secret, unformatted.</param>
    /// <returns>The authenticator URI and the display-formatted key.</returns>
    public (string AuthenticatorUri, string FormattedKey) Generate2faKey(string name, string email, string secret)
        => (Generate2faQrCodeUri(name, email, secret), Format2faKey(secret));
    
    internal static List<RoleRecord> GetAllRoles<T>()
    {
        var fields = typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Where(f => !f.Name.EndsWith("Desc"))
            .ToList();

        var result = new List<RoleRecord>();

        foreach (var field in fields)
        {
            var role = (string?)field.GetRawConstantValue() ?? field.Name;
            var descField = typeof(T).GetField(
                $"{field.Name}Desc",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            var description = (string?)descField?.GetRawConstantValue() ?? string.Empty;
            result.Add(new RoleRecord(role, description));
        }

        return result;
    }
}
