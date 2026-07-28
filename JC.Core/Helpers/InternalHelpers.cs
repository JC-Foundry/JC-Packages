using System.Text;

namespace JC.Core.Helpers;

internal static class InternalHelpers
{
    internal static string ToDisplayName(string name)
    {
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
}