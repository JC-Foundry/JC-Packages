namespace JC.Core.Helpers;

public static class ColourHelper
{
    /// <summary>
    /// Generates a hover colour by lightening the given background colour using a fixed factor.
    /// </summary>
    /// <param name="col">A string representing the background colour in hexadecimal format (e.g., "#RRGGBB").</param>
    /// <returns>A string representing the hex code of the hover colour, with the original colour lightened.</returns>
    public static string HoverColour(string col)
    {
        var rgb = ExtractRGB(col);

        var r = (int)(rgb.R + (255 - rgb.R) * 0.4);
        var g = (int)(rgb.G + (255 - rgb.G) * 0.4);
        var b = (int)(rgb.B + (255 - rgb.B) * 0.4);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Determines an appropriate font colour (black or white) for a given background colour based on its luminance.
    /// </summary>
    /// <param name="col">A string representing the background colour in hexadecimal format (e.g., "#RRGGBB").</param>
    /// <returns>A string representing the hex code of the font colour, either "#000000" (black) or "#ffffff" (white), based on the luminance of the given background colour.</returns>
    public static string FontColour(string col)
    {
        var rgb = ExtractRGB(col);

        var rNormalised = rgb.R / 255.0;
        var gNormalised = rgb.G / 255.0;
        var bNormalised = rgb.B / 255.0;

        var luminance = 0.2126 * rNormalised + 0.7152 * gNormalised + 0.0722 * bNormalised;
        return luminance > 0.5 ? "#000000" : "#ffffff";
    }

    /// <summary>
    /// Extracts the red, green, and blue (RGB) components from a hexadecimal colour string.
    /// </summary>
    /// <param name="colour">A string representing the colour in hexadecimal format (e.g., "#RRGGBB").</param>
    /// <returns>A tuple containing the RGB components as integers (R, G, B).</returns>
    // ReSharper disable once InconsistentNaming
    private static (int R, int G, int B) ExtractRGB(string colour)
    {
        if (string.IsNullOrWhiteSpace(colour))
            throw new ArgumentException("Colour must not be null or empty.", nameof(colour));

        var hex = colour.StartsWith('#') ? colour[1..] : colour;

        // Expand shorthand (e.g. "fff" → "ffffff")
        if (hex.Length == 3)
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";

        if (hex.Length != 6)
            throw new ArgumentException("Colour must be in '#RRGGBB', '#RGB', 'RRGGBB', or 'RGB' format.", nameof(colour));

        if (!int.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) ||
            !int.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g) ||
            !int.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b))
            throw new ArgumentException("Colour contains invalid hexadecimal characters.", nameof(colour));

        return (r, g, b);
    }
}