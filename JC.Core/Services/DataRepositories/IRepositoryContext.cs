using System.Linq.Expressions;

namespace JC.Core.Services.DataRepositories;

/// <summary>
/// Generic repository interface providing full CRUD, soft-delete, and restore operations for entity type <typeparamref name="T"/>.
/// Automatically populates audit fields for entities extending <see cref="JC.Core.Models.Auditing.AuditModel"/>.
/// </summary>
/// <typeparam name="T">The entity type managed by this repository.</typeparam>
public interface IRepositoryContext<T>
    where T : class
{
    /// <summary>
    /// Returns the underlying <see cref="IQueryable{T}"/> for the entity set, allowing custom query composition.
    /// </summary>
    /// <returns>An <see cref="IQueryable{T}"/> for the entity set.</returns>
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
    /// Asynchronously retrieves an entity by a composite key.
    /// </summary>
    /// <param name="id">The composite key values identifying the entity.</param>
    /// <returns>A task representing the asynchronous operation, with a result of the entity if found, or <c>null</c> if not found.</returns>
    Task<T?> GetByIdAsync(params object[] id);


    /// <summary>
    /// Adds a single entity to the database. Populates audit fields if the entity extends <see cref="JC.Core.Models.Auditing.AuditModel"/>.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The added entity.</returns>
    Task<T> AddAsync(
        T entity,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the database. Populates audit fields if the entities extend <see cref="JC.Core.Models.Auditing.AuditModel"/>.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of added entities.</returns>
    Task<List<T>> AddAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a range of entities to the database. Populates audit fields if the entities extend <see cref="JC.Core.Models.Auditing.AuditModel"/>.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of added entities.</returns>
    Task<List<T>> AddRangeAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Updates a single entity in the database. Populates modification audit fields if the entity extends <see cref="JC.Core.Models.Auditing.AuditModel"/>.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    Task<T> UpdateAsync(
        T entity,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities in the database. Populates modification audit fields if the entities extend <see cref="JC.Core.Models.Auditing.AuditModel"/>.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of updated entities.</returns>
    Task<List<T>> UpdateAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a range of entities in the database. Populates modification audit fields if the entities extend <see cref="JC.Core.Models.Auditing.AuditModel"/>.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of updated entities.</returns>
    Task<List<T>> UpdateRangeAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Soft-deletes a single entity. Populates deletion audit fields if the entity extends <see cref="JC.Core.Models.Auditing.AuditModel"/>,
    /// otherwise sets an <c>IsDeleted</c> property via reflection if present.
    /// </summary>
    /// <param name="entity">The entity to soft-delete.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The soft-deleted entity.</returns>
    Task<T> SoftDeleteAsync(
        T entity,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes multiple entities.
    /// </summary>
    /// <param name="entities">The entities to soft-delete.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of soft-deleted entities.</returns>
    Task<List<T>> SoftDeleteAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a range of entities.
    /// </summary>
    /// <param name="entities">The entities to soft-delete.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of soft-deleted entities.</returns>
    Task<List<T>> SoftDeleteRangeAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Restores a single soft-deleted entity. Clears deletion fields and populates restore audit fields
    /// if the entity extends <see cref="JC.Core.Models.Auditing.AuditModel"/>.
    /// </summary>
    /// <param name="entity">The entity to restore.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The restored entity.</returns>
    Task<T> RestoreAsync(
        T entity,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores multiple soft-deleted entities.
    /// </summary>
    /// <param name="entities">The entities to restore.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of restored entities.</returns>
    Task<List<T>> RestoreAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a range of soft-deleted entities.
    /// </summary>
    /// <param name="entities">The entities to restore.</param>
    /// <param name="userId">Optional user identifier for audit fields. Defaults to the current user.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of restored entities.</returns>
    Task<List<T>> RestoreRangeAsync(
        IEnumerable<T> entities,
        string? userId = null,
        bool saveNow = true,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Permanently deletes a single entity from the database.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the deletion succeeded.</returns>
    Task<bool> DeleteAsync(
        T entity,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes multiple entities from the database.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the deletion succeeded.</returns>
    Task<bool> DeleteAsync(
        IEnumerable<T> entities,
        bool saveNow = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a range of entities from the database.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="saveNow">Whether to persist changes immediately. Defaults to <c>true</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the deletion succeeded.</returns>
    Task<bool> DeleteRangeAsync(
        IEnumerable<T> entities,
        bool saveNow = true,
        CancellationToken cancellationToken = default);
}
