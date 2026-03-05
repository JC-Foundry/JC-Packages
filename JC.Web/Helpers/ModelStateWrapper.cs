using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JC.Web.Helpers;

/// <summary>
/// Wraps <see cref="ModelStateDictionary"/> with automatic key prefixing for cleaner error access
/// in Razor Pages and MVC scenarios where model properties are nested under a prefix (e.g. <c>"Input."</c>).
/// </summary>
/// <param name="modelState">The model state dictionary to wrap.</param>
/// <param name="prefix">The key prefix to prepend. Defaults to <c>"Input."</c>. A trailing <c>.</c> is appended automatically if missing.</param>
/// <param name="ignorePrefix">Set to <c>true</c> to disable prefixing entirely.</param>
public class ModelStateWrapper(ModelStateDictionary modelState, string? prefix = null, bool ignorePrefix = false)
{
    private ModelStateDictionary ModelState => modelState;

    /// <summary>Gets whether the underlying model state is valid.</summary>
    public bool IsValid => ModelState.IsValid;

    private readonly string _prefix = ignorePrefix
        ? string.Empty
        : string.IsNullOrEmpty(prefix)
            ? "Input."
            : prefix.EndsWith('.')
                ? prefix
                : $"{prefix}.";

    /// <summary>
    /// Gets the first error message for the specified key (with prefix applied), or an empty string if no errors.
    /// </summary>
    /// <param name="key">The property name (without prefix).</param>
    /// <returns>The first error message, or an empty string.</returns>
    public string this[string key] =>
        ModelState.TryGetValue($"{_prefix}{key}", out var entry) && entry.Errors.Count > 0
            ? entry.Errors[0].ErrorMessage
            : string.Empty;

    /// <summary>
    /// Adds a model error for the specified key with the prefix applied.
    /// </summary>
    /// <param name="key">The property name (without prefix).</param>
    /// <param name="errorMessage">The error message.</param>
    public void AddModelError(string key, string errorMessage)
        => ModelState.AddModelError($"{_prefix}{key}", errorMessage);

    /// <summary>
    /// Checks whether the specified key (with prefix) has any validation errors.
    /// </summary>
    /// <param name="key">The property name (without prefix).</param>
    /// <returns><c>true</c> if errors exist for the key; otherwise <c>false</c>.</returns>
    public bool HasError(string key)
        => ModelState.TryGetValue($"{_prefix}{key}", out var entry) && entry.Errors.Count > 0;

    /// <summary>
    /// Gets all error messages for the specified key (with prefix).
    /// </summary>
    /// <param name="key">The property name (without prefix).</param>
    /// <returns>An enumerable of error messages, or an empty collection if none.</returns>
    public IEnumerable<string> GetErrors(string key)
        => ModelState.TryGetValue($"{_prefix}{key}", out var entry)
            ? entry.Errors.Select(e => e.ErrorMessage)
            : [];

    /// <summary>
    /// Gets all validation errors across all keys as a dictionary.
    /// </summary>
    /// <returns>A dictionary mapping full keys to their error message arrays.</returns>
    public Dictionary<string, string[]> GetAllErrors()
        => ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
}