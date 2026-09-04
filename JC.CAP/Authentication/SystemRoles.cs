using CAP.SSO.Models;
using JC.Core.Extensions;
using JC.Identity.Shared.Helpers;
using JC.Identity.Shared.Models;

namespace JC.CAP.Authentication;

public class SystemRoles
{
    //No defined roles as a consuming app using SSO via CAP must define their full role catalogue
    //by inheriting from this class. There is no SystemAdmin or Admin defined, unlike JC.Identity.
    
    /// <summary>
    /// Gets all roles and their descriptions from this class and any derived class.
    /// Roles are paired with descriptions by convention: {RoleName} + {RoleName}Desc
    /// </summary>
    public static List<RoleRecord> GetAllRoles<T>() where T : SystemRoles
        => IdentityHelper.GetAllRoles<T>();
    
    /// <summary>
    /// Projects role declarations onto CAP's catalogue shape, deriving each display name from its key so
    /// <c>PageEditor</c> shows as <c>Page Editor</c>. What <see cref="Services.CapRoleSyncJob{TRoles}"/> publishes.
    /// </summary>
    /// <param name="roles">The declarations, typically <c>SystemRoles.GetAllRoles&lt;AppRoles&gt;()</c>.</param>
    /// <returns>The catalogue to publish.</returns>
    public static IReadOnlyList<ApplicationRoleDto> ToCatalogue(IEnumerable<RoleRecord> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        return roles.Select(role => new ApplicationRoleDto
        {
            Key = role.Role,
            DisplayName = role.Role.ToDisplayName(),
            Description = string.IsNullOrWhiteSpace(role.Description) ? null : role.Description
        }).ToList();
    }
}