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
}