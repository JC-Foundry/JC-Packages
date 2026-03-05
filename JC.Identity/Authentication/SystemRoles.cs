using System.Reflection;

namespace JC.Identity.Authentication;

/// <summary>
/// Defines built-in system roles. Designed to be extended by consuming applications
/// (e.g. <c>class AppRoles : SystemRoles</c>). Role descriptions follow the naming convention
/// <c>{RoleName}Desc</c> and are discovered automatically by <see cref="GetAllRoles{T}"/>.
/// </summary>
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
    public static List<(string Role, string Description)> GetAllRoles<T>() where T : SystemRoles
    {
        var fields = typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Where(f => !f.Name.EndsWith("Desc"))
            .ToList();

        var result = new List<(string Role, string Description)>();

        foreach (var field in fields)
        {
            var role = (string?)field.GetRawConstantValue() ?? field.Name;
            var descField = typeof(T).GetField(
                $"{field.Name}Desc",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            var description = (string?)descField?.GetRawConstantValue() ?? string.Empty;
            result.Add((role, description));
        }

        return result;
    }
}