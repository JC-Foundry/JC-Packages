using JC.Core.Models.Pagination;

namespace JC.Core.Helpers;

/// <summary>
/// Helper methods for paginating collections and queryables with page validation and skip/take logic.
/// </summary>
public static class PaginationHelper
{
    private static int ValidatePage(int pageNumber, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Math.Max(1, Math.Min(pageNumber, totalPages));
    }

    private static IEnumerable<T> SkipTake<T>(this IEnumerable<T> list, int pageNumber, int pageSize)
        => list.Skip((pageNumber - 1) * pageSize).Take(pageSize);

    private static IQueryable<T> SkipTake<T>(this IQueryable<T> list, int pageNumber, int pageSize)
        => list.Skip((pageNumber - 1) * pageSize).Take(pageSize);


    private static IEnumerable<T> Paginate<T>(List<T> items, int pageNumber, int pageSize)
    {
        pageNumber = ValidatePage(pageNumber, pageSize, items.Count);
        return items.SkipTake(pageNumber, pageSize);
    }

    private static IQueryable<T> Paginate<T>(IQueryable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        pageNumber = ValidatePage(pageNumber, pageSize, totalCount);
        return items.SkipTake(pageNumber, pageSize);
    }


    /// <summary>
    /// Paginates an in-memory collection, materialising it once and returning a <see cref="PagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source collection.</param>
    /// <param name="pageNumber">The requested page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PagedList{T}"/> containing the requested page of items.</returns>
    public static PagedList<T> PaginateList<T>(IEnumerable<T> items, int pageNumber, int pageSize)
    {
        var list = items.ToList();
        var paged = Paginate(list, pageNumber, pageSize);
        return new PagedList<T>(paged, pageNumber, pageSize, list.Count);
    }

    /// <summary>
    /// Applies pagination to an <see cref="IQueryable{T}"/> using a pre-computed total count,
    /// returning a queryable with <c>Skip</c> and <c>Take</c> applied.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The source queryable.</param>
    /// <param name="pageNumber">The requested page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The pre-computed total count of items (to avoid a second DB query).</param>
    /// <returns>The queryable with skip/take applied.</returns>
    public static IQueryable<T> PaginateQueryable<T>(IQueryable<T> items, int pageNumber, int pageSize, int totalCount)
        => Paginate(items, pageNumber, pageSize, totalCount);
}