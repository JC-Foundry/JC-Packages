namespace JC.Web.UI.Framework;

/// <summary>
/// Helpers for class values that embed a runtime value, such as a contextual colour supplied as a
/// tag helper attribute.
/// </summary>
/// <remarks>
/// Most class values are fixed strings, but a few are not known until render time —
/// <c>"card border-{0} d-print-none"</c> takes the caller's colour. Storing the whole format keeps
/// decision 5 of the design intact: the dictionary owns the finished value, so a framework that
/// composes its colours differently, or not at all, can still express that.
/// </remarks>
public static class FrameworkClass
{
    /// <summary>
    /// Applies <paramref name="args"/> to a class format, returning an empty string when the
    /// dictionary does not define one.
    /// </summary>
    /// <param name="format">The class format, as stored on a dictionary record.</param>
    /// <param name="args">The runtime values to substitute.</param>
    /// <returns>The finished class attribute value, or an empty string.</returns>
    /// <remarks>
    /// An unset entry short-circuits rather than throwing, matching <c>AddClass</c>, which ignores
    /// empty values. That is what lets a property be added to an existing record without breaking
    /// dictionaries that have not filled it in.
    /// </remarks>
    public static string Format(string format, params object?[] args)
        => string.IsNullOrWhiteSpace(format) ? "" : string.Format(format, args);

    /// <summary>
    /// Joins class values, skipping any that are empty.
    /// </summary>
    /// <param name="classes">The values to combine.</param>
    /// <returns>The combined class attribute value, or an empty string when none are set.</returns>
    /// <remarks>
    /// Used where a dictionary value and a caller-supplied class both apply. Skipping empties means
    /// a framework that leaves an entry blank produces the caller's value alone, with no stray
    /// whitespace.
    /// </remarks>
    public static string Join(params string?[] classes)
        => string.Join(" ", classes.Where(c => !string.IsNullOrWhiteSpace(c)));
}
