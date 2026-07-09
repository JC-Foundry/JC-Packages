using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using JC.Core.Models.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JC.Core.Services.DataRepositories;

/// <summary>
/// Unit of work implementation providing thread-safe repository caching and transaction management.
/// </summary>
public class RepositoryManager : IRepositoryManager, IDisposable, IAsyncDisposable
{
    private readonly DbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RepositoryManager> _logger;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private readonly ConcurrentDictionary<Type, RepositoryManager> _boundManagers = new();
    private IDbContextTransaction? _currentTransaction;
    
    // For<TContext>() is generic-only; cache the closed method per managed context type.
    private static readonly ConcurrentDictionary<Type, MethodInfo> ForMethods = new();

    // GetMethod(nameof(For)) is ambiguous — For<T>() and For(Type) share a name.
    private static readonly MethodInfo ForDefinition = typeof(IRepositoryManager)
        .GetMethods()
        .Single(m => m.Name == nameof(IRepositoryManager.For) && m.IsGenericMethodDefinition);

    public RepositoryManager(DbContext context, 
        IServiceProvider serviceProvider,
        ILogger<RepositoryManager> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IRepositoryContext<T> GetRepository<T>() where T : class
    {
        var type = typeof(T);
        
        return (IRepositoryContext<T>)_repositories.GetOrAdd(type, _ =>
            new RepositoryContext<T>(_context, _serviceProvider, 
                _serviceProvider.GetRequiredService<ILogger<RepositoryContext<T>>>()));
    }

    public IRepositoryManager For<T>() where T : DbContext
    {
        //Already bound to this context — hand back the same manager so callers share its transaction.
        if (typeof(T) == _context.GetType()) return this;

        return _boundManagers.GetOrAdd(typeof(T),
            _ => new RepositoryManager(_serviceProvider.GetRequiredService<T>(), _serviceProvider, _logger));
    }

    public IRepositoryManager For(Type contextType)
    {
        if (!typeof(DbContext).IsAssignableFrom(contextType))
            throw new ArgumentException($"{contextType} is not a {nameof(DbContext)}.", nameof(contextType));

        try
        {
            var forMethod = ForMethods.GetOrAdd(contextType, t => ForDefinition.MakeGenericMethod(t));
            return (IRepositoryManager)forMethod.Invoke(this, null)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            _logger.LogError(ex.InnerException, "Error creating repository manager for context type {ContextType}", contextType);
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; //Unreachable — keeps the compiler happy.
        }
    }


    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction ??= await _context.Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction has been started.");

        await _context.SaveChangesAsync(cancellationToken);
        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction has been started.");

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _currentTransaction = null;

        foreach (var manager in _boundManagers.Values)
            manager.Dispose();
        _boundManagers.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        foreach (var manager in _boundManagers.Values)
            await manager.DisposeAsync();
        _boundManagers.Clear();
    }
}
