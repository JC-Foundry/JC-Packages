using JC.Identity.Shared.Helpers;
using JC.Identity.Shared.Models;

namespace JC.Identity.Authentication;

/// <summary>
/// Defines built-in system roles for local ASP.NET Identity. Designed to be extended by consuming
/// applications (e.g. <c>class AppRoles : SystemRoles</c>). Role descriptions follow the naming
/// convention <c>{RoleName}Desc</c> and are discovered automatically by <see cref="GetAllRoles{T}"/>.
/// </summary>
/// <remarks>
/// Local to this package rather than shared with other identity authorities. An authority with its
/// own administrative plane brings its own role structure, and those roles are a separate security
/// domain that must not be mixed into an application's own authorisation roles.
/// </remarks>
public class SystemRoles
{
    /// <summary>Full system administrator with access to tenant management and assignment.</summary>
    public const string SystemAdmin = nameof(SystemAdmin);

    /// <summary>Description for <see cref="SystemAdmin"/>.</summary>
    public const string SystemAdminDesc = "Full system administrator with access to tenant management and assignment.";

    /// <summary>Administrator with access to all features within their tenant.</summary>
    public const string Admin = nameof(Admin);

    /// <summary>Description for <see cref="Admin"/>.</summary>
    public const string AdminDesc = "Administrator with access to all features within their tenant.";

    /// <summary>
    /// Gets all roles and their descriptions from this class and any derived class.
    /// Roles are paired with descriptions by convention: {RoleName} + {RoleName}Desc
    /// </summary>
    public static List<RoleRecord> GetAllRoles<T>() where T : SystemRoles
        => IdentityHelper.GetAllRoles<T>();
}
