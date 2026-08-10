namespace JC.Tenancy.Models;

/// <summary>
/// Validation response for tenant operations. Contains the validated tenant on success.
/// </summary>
public class TenantValidationResponse
{
    /// <summary>Gets whether the validation passed.</summary>
    public bool IsValid { get; }

    /// <summary>Gets the error message when validation fails, or <c>null</c> when valid.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets the validated tenant, or <c>null</c> when validation fails.</summary>
    public Tenant? ValidatedTenant { get; }

    /// <summary>Creates a successful validation response with no tenant.</summary>
    public TenantValidationResponse()
    {
        IsValid = true;
    }

    /// <summary>
    /// Creates a successful validation response with the validated tenant.
    /// </summary>
    /// <param name="tenant">The validated tenant.</param>
    public TenantValidationResponse(Tenant tenant)
        : this()
    {
        ValidatedTenant = tenant;
    }

    /// <summary>
    /// Creates a failed validation response with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public TenantValidationResponse(string errorMessage)
    {
        IsValid = false;
        ErrorMessage = errorMessage;
    }
}
