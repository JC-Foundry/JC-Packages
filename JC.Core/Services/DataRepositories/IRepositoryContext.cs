using System.Linq.Expressions;

namespace JC.Core.Services.DataRepositories;

public interface IRepositoryContext<T>
    where T : class
{
    /// <summary>
    /// Return queryable DbSet from Context
    /// </summary>
    /// <returns></returns>
    IQueryable<T> AsQueryable();


    /// <summary>
    /// Retrieves a collection of entities that satisfy the specified predicate.
    /// </summary>
    /// <param name="predicate">A lambda expression used to filter the entities.</param>
    /// <returns>An IQueryable of entities that match the provided predicate.</returns>
    IQueryable<T> GetAll(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Retrieves all entities that satisfy the specified predicate.
    /// </summary>
    /// <param name="predicate">A lambda expression to filter the entities.</param>
    /// <param name="orderBy"></param>
    /// <returns>An IQueryable of the entities that match the predicate.</returns>
    IQueryable<T> GetAll(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy);

    /// <summary>
    /// Asynchronously retrieves all entities that satisfy the specified predicate.
    /// </summary>
    /// <param name="predicate">A lambda expression to filter the entities.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation, with a result of a list of entities that match the predicate.</returns>
    Task<List<T>> GetAllAsync(
        Expression<Func<T, bool>> predicate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves all entities that satisfy the specified predicate, ordered by the provided ordering function.
    /// </summary>
    /// <param name="predicate">A lambda expression to filter the entities.</param>
    /// <param name="orderBy">A function to specify the ordering of the entities.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation, with a result of a list of entities that match the predicate and order.</returns>
    Task<List<T>> GetAllAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Asynchronously retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation, with a result of the entity if found, or null if not found.</returns>
    Task<T?> GetByIdAsync(
        int id, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task representing the asynchronous operation, with a result of the entity if found, or null if not found.</returns>
    Task<T?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>A task representing the asynchronous operation, with a result of the entity if found, or null if not found.</returns>
    Task<T?> GetByIdAsync(params object[] id);


    Task<T> AddAsync(
        T entity,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<List<T>> AddAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<List<T>> AddRangeAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);


    Task<T> UpdateAsync(
        T entity,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<List<T>> UpdateAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<List<T>> UpdateRangeAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);


    Task<T> SoftDeleteAsync(
        T entity,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<List<T>> SoftDeleteAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<List<T>> SoftDeleteRangeAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);


    Task<T> RestoreAsync(
        T entity,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<List<T>> RestoreAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<List<T>> RestoreRangeAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);


    Task<bool> DeleteAsync(
        T entity,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        IEnumerable<T> entities,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteRangeAsync(
        IEnumerable<T> entities,
        bool saveNow = true,
        CancellationToken cancellationToken = default);
}
