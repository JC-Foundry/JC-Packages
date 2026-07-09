using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JC.Core.Services.DataRepositories;

/// <summary>
/// Unit of work interface providing repository access and transaction management.
/// </summary>
public interface IRepositoryManager
{
    /// <summary>
    /// Gets (or creates and caches) the repository context for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The repository context for <typeparamref name="T"/>.</returns>
    IRepositoryContext<T> GetRepository<T>() where T : class;

    /// <summary>
    /// Retrieves a repository manager instance specific to the provided data context type.
    /// Bound managers are cached, so repeated calls for the same context return the same instance.
    /// Requesting the context this manager is already bound to returns this same instance.
    /// </summary>
    /// <remarks>
    /// Each context gets its own manager with its own transaction — a transaction started on one
    /// manager does not span the contexts reached through <see cref="For{T}"/>.
    /// </remarks>
    /// <typeparam name="T">The type of the data context implementing <see cref="DbContext"/>.</typeparam>
    /// <returns>An instance of <see cref="IRepositoryManager"/> configured for the specified data context type.</returns>
    IRepositoryManager For<T>() where T : DbContext;

    /// <summary>
    /// Non-generic equivalent of <see cref="For{T}"/>, for callers that only have a <see cref="Type"/>
    /// at runtime. Behaves identically, including caching.
    /// </summary>
    /// <param name="contextType">The data context type. Must derive from <see cref="DbContext"/>.</param>
    /// <returns>An instance of <see cref="IRepositoryManager"/> configured for the specified data context type.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="contextType"/> does not derive from <see cref="DbContext"/>.</exception>
    IRepositoryManager For(Type contextType);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The started transaction.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves pending changes and commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no transaction has been started.</exception>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction and discards pending changes.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no transaction has been started.</exception>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the database without committing a transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
