namespace JC.Core.Models.Pagination;

/// <summary>
/// Contract for a paginated collection of items, providing page metadata and navigation properties.
/// </summary>
/// <typeparam name="T">The type of items in the paginated collection.</typeparam>
public interface IPagination<out T> : IReadOnlyList<T>
{
    /// <summary>Gets the items on the current page.</summary>
    IReadOnlyList<T> Items { get; }

    /// <summary>Gets the current page number (1-based).</summary>
    int PageNumber { get; }

    /// <summary>Gets the maximum number of items per page.</summary>
    int PageSize { get; }

    /// <summary>Gets the total number of items across all pages.</summary>
    int TotalCount { get; }

    /// <summary>Gets the total number of pages.</summary>
    int TotalPages { get; }

    /// <summary>Gets whether there is a page before the current page.</summary>
    bool HasPreviousPage => PageNumber > 1;

    /// <summary>Gets whether there is a page after the current page.</summary>
    bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Gets whether the current page is the first page.</summary>
    bool IsFirstPage => !HasPreviousPage;

    /// <summary>Gets whether the current page is the last page.</summary>
    bool IsLastPage => !HasNextPage;
}