using JC.Core.Helpers;
using JC.Core.Models.Pagination;
using Microsoft.EntityFrameworkCore;

namespace JC.Core.Extensions;

/// <summary>
/// Extension methods for paginating <see cref="IEnumerable{T}"/> and <see cref="IQueryable{T}"/> collections.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Paginates an in-memory collection into a <see cref="PagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="pageNumber">The requested page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PagedList{T}"/> containing the requested page.</returns>
    public static PagedList<T> ToPagedList<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
        => PaginationHelper.PaginateList(source, pageNumber, pageSize);

    /// <summary>
    /// Asynchronously paginates a queryable into a <see cref="PagedList{T}"/>.
    /// Executes a single count query and a single data query against the database.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="pageNumber">The requested page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PagedList{T}"/> containing the requested page.</returns>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var paged = PaginationHelper.PaginateQueryable(source, pageNumber, pageSize, count);
        
        var list = await paged.ToListAsync();
        return new PagedList<T>(list, pageNumber, pageSize, count);
    }

    /// <summary>
    /// Synchronously paginates a queryable into a <see cref="PagedList{T}"/>.
    /// Executes a single count query and a single data query against the database.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="pageNumber">The requested page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PagedList{T}"/> containing the requested page.</returns>
    public static PagedList<T> ToPagedList<T>(this IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = source.Count();
        var paged = PaginationHelper.PaginateQueryable(source, pageNumber, pageSize, count);
        
        var list = paged.ToList();
        return new PagedList<T>(list, pageNumber, pageSize, count);
    }
}