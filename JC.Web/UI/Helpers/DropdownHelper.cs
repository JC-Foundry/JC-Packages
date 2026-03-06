using JC.Core.Extensions;
using JC.Core.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JC.Web.UI.Helpers;

/// <summary>
/// Static helper methods for building <see cref="SelectListItem"/> collections from various data sources.
/// </summary>
public static class DropdownHelper
{
    /// <summary>
    /// Creates a single <see cref="SelectListItem"/>.
    /// </summary>
    /// <param name="text">The display text.</param>
    /// <param name="value">The option value.</param>
    /// <param name="selected">Whether this item is selected. Defaults to <c>false</c>.</param>
    /// <returns>A configured <see cref="SelectListItem"/>.</returns>
    public static SelectListItem ToDropdownEntry(string text, string value, bool selected = false)
        => new()
        {
            Text = text,
            Value = value,
            Selected = selected
        };

    /// <summary>
    /// Converts all values of an enum to dropdown items using <see cref="EnumExtensions.ToDisplayName"/>.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="selected">The currently selected value, if any.</param>
    /// <returns>A list of <see cref="SelectListItem"/> for each enum value.</returns>
    public static List<SelectListItem> FromEnum<T>(T? selected = null)
        where T : struct, Enum
        => Enum.GetValues<T>()
            .Select(e => ToDropdownEntry(
                e.ToDisplayName(),
                Convert.ToInt32(e).ToString(),
                selected.HasValue && EqualityComparer<T>.Default.Equals(e, selected.Value)))
            .ToList();

    /// <summary>
    /// Converts a collection of items to dropdown items using custom selector functions.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source collection.</param>
    /// <param name="textSelector">Function to extract display text from each item.</param>
    /// <param name="valueSelector">Function to extract the option value from each item.</param>
    /// <param name="selectedPredicate">Optional predicate to determine which items are selected.</param>
    /// <returns>A list of <see cref="SelectListItem"/>.</returns>
    public static List<SelectListItem> FromCollection<T>(
        IEnumerable<T> items,
        Func<T, string> textSelector,
        Func<T, string> valueSelector,
        Func<T, bool>? selectedPredicate = null)
        => items
            .Select(item => ToDropdownEntry(
                textSelector(item),
                valueSelector(item),
                selectedPredicate?.Invoke(item) ?? false))
            .ToList();

    /// <summary>
    /// Converts a dictionary to dropdown items (Key = value, Value = display text).
    /// </summary>
    /// <param name="items">The source dictionary.</param>
    /// <param name="selected">The key of the currently selected item, if any.</param>
    /// <returns>A list of <see cref="SelectListItem"/>.</returns>
    public static List<SelectListItem> FromDictionary(
        Dictionary<string, string> items,
        string? selected = null)
        => items
            .Select(kvp => ToDropdownEntry(kvp.Value, kvp.Key, kvp.Key == selected))
            .ToList();

    /// <summary>
    /// Builds a pre-populated country dropdown from <see cref="CountryHelper"/>.
    /// </summary>
    /// <param name="selected">The ISO 3166-1 alpha-2 code of the currently selected country, if any.</param>
    /// <returns>A list of <see cref="SelectListItem"/> for all countries.</returns>
    public static List<SelectListItem> GetCountryDropdown(string? selected = null)
        => CountryHelper.GetCountries()
            .Select(c => ToDropdownEntry(c.Name, c.Code, string.Equals(c.Code, selected, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    /// <summary>
    /// Inserts a placeholder item at the beginning of the dropdown list.
    /// </summary>
    /// <param name="items">The existing dropdown items.</param>
    /// <param name="text">The placeholder display text. Defaults to <c>"Please select..."</c>.</param>
    /// <param name="value">The placeholder value. Defaults to an empty string.</param>
    /// <returns>The modified list with the placeholder inserted at index 0.</returns>
    public static List<SelectListItem> WithPlaceholder(
        this List<SelectListItem> items,
        string text = "Please select...",
        string value = "")
    {
        items.Insert(0, new SelectListItem { Text = text, Value = value });
        return items;
    }
}