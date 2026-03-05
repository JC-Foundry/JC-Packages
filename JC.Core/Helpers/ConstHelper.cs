using System.Reflection;

namespace JC.Core.Helpers;

/// <summary>
/// Reflection-based helper for discovering constant fields on a type.
/// </summary>
public static class ConstHelper
{
    /// <summary>
    /// Returns all <c>const</c> fields declared on <typeparamref name="T"/> (including inherited fields) as a dictionary.
    /// </summary>
    /// <typeparam name="T">The type to inspect for constant fields.</typeparam>
    /// <returns>A dictionary mapping field names to their constant values.</returns>
    public static Dictionary<string, object> GetAllConsts<T>()
    {
        return typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly) // const = literal, not initonly (readonly)
            .ToDictionary(f => f.Name, f => f.GetRawConstantValue()!);
    }
}