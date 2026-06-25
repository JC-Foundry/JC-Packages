using System.ComponentModel;
using System.Text;

namespace JC.Core.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Retrieves all options of a given enum type, including their names and integer values.
    /// </summary>
    /// <typeparam name="T">The enum type from which to retrieve the options.</typeparam>
    /// <param name="_">An instance of the enum type (can be default).</param>
    /// <returns>A list of tuples, where each tuple contains the name and integer value of an enum option.</returns>
    public static List<(string Name, int Value)> GetAllOptions<T>(this T _)
        where T : struct, Enum
        => Enum.GetValues(typeof(T))
            .Cast<T>()
            .Select(e => (e.ToString(), Convert.ToInt32(e)))
            .ToList();


    /// <summary>
    /// Converts an enum value to a display-friendly string by formatting its name.
    /// Replaces underscores with spaces, adds spaces between words in PascalCase,
    /// and capitalises the first letter of each word.
    /// </summary>
    /// <param name="value">The enum value to be converted into a display-friendly string.</param>
    /// <returns>A formatted string representation of the enum value's name.</returns>
    public static string ToDisplayName(this Enum value)
    {
        var name = value.ToString();

        if (string.IsNullOrEmpty(name))
            return string.Empty;

        var result = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (current == '_')
            {
                if (result.Length > 0)
                    result.Append(' ');
                continue;
            }

            var isUpper = char.IsUpper(current);
            var isFirst = result.Length == 0 || result[^1] == ' ';

            // Insert space before uppercase if:
            // - Not at start of word
            // - Previous char was lowercase, OR
            // - Next char is lowercase (handles "XMLParser" -> "XML Parser")
            var newWord = false;
            if (isUpper && !isFirst)
            {
                var prevIsLower = i > 0 && char.IsLower(name[i - 1]);
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                if (prevIsLower || nextIsLower)
                {
                    result.Append(' ');
                    newWord = true;
                }
            }

            // Capitalise the first letter of each word, lowercase the rest
            result.Append(isFirst || newWord ? char.ToUpper(current) : char.ToLower(current));
        }

        return result.ToString();
    }

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