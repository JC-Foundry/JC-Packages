namespace JC.Web.Security.Models;

/// <summary>
/// The result of a cookie validation operation, containing the comparison outcome and the actual cookie value.
/// </summary>
/// <param name="isValid">Whether the cookie value matched the expected value.</param>
/// <param name="actualValue">The actual value read from the cookie, or <c>null</c> if the cookie was not found.</param>
public class CookieValidationResponse(bool isValid, string? actualValue)
{
    /// <summary>
    /// Whether the cookie value matched the expected value.
    /// </summary>
    public bool IsValid { get; init; } = isValid;

    /// <summary>
    /// The actual value read from the cookie, or <c>null</c> if the cookie was not found or decryption failed.
    /// </summary>
    public string? ActualValue { get; init; } = actualValue;

    public bool ValidationError => !IsValid && ActualValue == null;
}
