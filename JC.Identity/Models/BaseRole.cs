using Microsoft.AspNetCore.Identity;

namespace JC.Identity.Models;

/// <summary>
/// Base role entity extending ASP.NET Core <see cref="IdentityRole"/> with a description field.
/// </summary>
public class BaseRole : IdentityRole
{
    /// <summary>Gets or sets an optional description of the role's purpose.</summary>
    public string? Description { get; set; }
}