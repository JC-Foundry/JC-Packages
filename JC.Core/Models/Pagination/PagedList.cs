using System.Collections;

namespace JC.Core.Models.Pagination;

/// <summary>
/// Default implementation of <see cref="IPagination{T}"/>. Wraps a page of items with
/// pagination metadata. Implements <see cref="IReadOnlyList{T}"/> for direct enumeration.
/// </summary>
/// <typeparam name="T">The type of items in the paginated collection.</typeparam>
public class PagedList<T> : IPagination<T>
{
    /// <inheritdoc />
    public IReadOnlyList<T> Items { get; }

    /// <inheritdoc />
    public int PageNumber { get; }

    /// <inheritdoc />
    public int PageSize { get; }

    /// <inheritdoc />
    public int TotalCount { get; }

    /// <inheritdoc />
    public int TotalPages { get; }

    /// <inheritdoc />
    public bool HasPreviousPage => PageNumber > 1;

    /// <inheritdoc />
    public bool HasNextPage => PageNumber < TotalPages;

    /// <inheritdoc />
    public bool IsFirstPage => !HasPreviousPage;

    /// <inheritdoc />
    public bool IsLastPage => !HasNextPage;

    /// <summary>
    /// Initialises a new instance of <see cref="PagedList{T}"/>.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than 1.</exception>
    public PagedList(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        if(pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0.");
        if(pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");
        
        Items = items.ToList().AsReadOnly();
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        TotalCount = totalCount;
        
        PageNumber = pageNumber > TotalPages ? Math.Max(1, TotalPages) : pageNumber;
        PageSize = pageSize;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int Count => Items.Count;

    /// <inheritdoc />
    public T this[int index] => Items[index];
}