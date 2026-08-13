using System.ComponentModel;
using JC.Core.Helpers;

namespace JC.Core.Extensions;

public static class EnumExtensions
{
    public readonly record struct EnumOption(string Name, int Value);

    /// <summary>
    /// Retrieves all enum options for a specified enum type as a list of structured name-value pairs.
    /// </summary>
    /// <param name="_">An enum value, which is only used to infer the type of the enum.</param>
    /// <typeparam name="T">The type of the enum for which options will be retrieved.</typeparam>
    /// <returns>A list of <see cref="EnumOption"/> records, each containing the name and numeric value of an enum member.</returns>
    public static List<EnumOption> GetAllOptions<T>(this T _)
        where T : struct, Enum
        => Enum.GetValues(typeof(T))
            .Cast<T>()
            .Select(e => new EnumOption(e.ToString(), Convert.ToInt32(e)))
            .ToList();


    /// <summary>
    /// Converts an enum value to a display-friendly string by formatting its name.
    /// Replaces underscores with spaces, adds spaces between words in PascalCase,
    /// and capitalises the first letter of each word. Acronyms such as those in
    /// <c>XMLExport</c> keep their casing unless the member name is entirely uppercase.
    /// </summary>
    /// <param name="value">The enum value to be converted into a display-friendly string.</param>
    /// <param name="splitDigits">
    /// Whether a digit adjoining a letter starts a new word, so that <c>Version2</c> becomes 'Version 2'.
    /// Adjacent digits are kept together either way. Set to <c>false</c> to leave digits attached
    /// to the word they follow, giving 'Version2'.
    /// </param>
    /// <returns>A formatted string representation of the enum value's name.</returns>
    public static string ToDisplayName(this Enum value, bool splitDigits = true)
        => CoreHelpers.ToDisplayName(value.ToString(), splitDigits);

    /// <summary>
    /// Retrieves the description of an enum value based on the DescriptionAttribute.
    /// If no description attribute is found, converts the enum value to a display-friendly string.
    /// </summary>
    /// <param name="value">The enum value for which to retrieve the description or display-friendly string.</param>
    /// <returns>The description defined in the DescriptionAttribute for the enum value,
    /// or a display-friendly string if the attribute is not present.</returns>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());

        if (field is null)
            return value.ToDisplayName();

        var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

        return attribute?.Description ?? value.ToDisplayName();
    }

    /// <summary>
    /// Attempts to parse a string into the specified enum type. Returns a default value if parsing fails or if the input is null or whitespace.
    /// </summary>
    /// <typeparam name="T">The enum type to which the string will be parsed.</typeparam>
    /// <param name="value">The string to parse into the enum type.</param>
    /// <param name="defaultValue">The default enum value to return if parsing fails.</param>
    /// <returns>The parsed enum value if successful; otherwise, the provided default value.</returns>
    public static T TryParse<T>(string? value, T defaultValue = default) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : defaultValue;
    }
}