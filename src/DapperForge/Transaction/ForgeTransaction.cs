using System.Data;
using DapperForge.Conventions;
using DapperForge.Execution;

namespace DapperForge.Transaction;

/// <summary>
/// Default implementation of <see cref="IForgeTransaction"/>.
/// Wraps an <see cref="IDbTransaction"/> and provides convention-based SP execution.
/// </summary>
public class ForgeTransaction : IForgeTransaction
{
    private readonly IDbTransaction _transaction;
    private readonly ISpExecutor _executor;
    private readonly ISpNamingConvention _convention;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgeTransaction"/> class.
    /// </summary>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="executor">The SP executor.</param>
    /// <param name="convention">The SP naming convention.</param>
    public ForgeTransaction(IDbTransaction transaction, ISpExecutor executor, ISpNamingConvention convention)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _convention = convention ?? throw new ArgumentNullException(nameof(convention));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetAsync<T>(object? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        var spName = _convention.ResolveSelect<T>();
        return await _executor.QueryAsync<T>(spName, parameters, _transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> GetSingleAsync<T>(object? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        var spName = _convention.ResolveSelect<T>();
        return await _executor.QuerySingleAsync<T>(spName, parameters, _transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        var spName = _convention.ResolveUpsert<T>();
        return await _executor.ExecuteAsync(spName, entity, _transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> RemoveAsync<T>(object? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        var spName = _convention.ResolveDelete<T>();
        return await _executor.ExecuteAsync(spName, parameters, _transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ExecuteSpAsync<T>(string spName, object? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        return await _executor.QueryAsync<T>(spName, parameters, _transaction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteSpNonQueryAsync(string spName, object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        return await _executor.ExecuteAsync(spName, parameters, _transaction, cancellationToken);
    }

    /// <inheritdoc />
    public void Commit()
    {
        EnsureNotDisposed();
        _transaction.Commit();
    }

    /// <inheritdoc />
    public void Rollback()
    {
        EnsureNotDisposed();
        _transaction.Rollback();
    }

    /// <summary>
    /// Disposes the transaction.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _transaction.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
