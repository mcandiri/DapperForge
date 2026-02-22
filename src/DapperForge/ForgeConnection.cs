using System.Data;
using System.Data.Common;
using DapperForge.Configuration;
using DapperForge.Conventions;
using DapperForge.Diagnostics;
using DapperForge.Execution;
using DapperForge.Transaction;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace DapperForge;

/// <summary>
/// Default implementation of <see cref="IForgeConnection"/>.
/// Manages database connections and provides convention-based stored procedure execution.
/// <para>
/// <b>Thread safety:</b> This class is NOT thread-safe. Each instance wraps a single
/// <see cref="IDbConnection"/> and must not be shared across concurrent operations.
/// Register as <c>Scoped</c> in DI (the default) and do not use with <c>Task.WhenAll</c>.
/// </para>
/// </summary>
public class ForgeConnection : IForgeConnection
{
    private readonly ForgeOptions _options;
    private readonly IDbConnection _connection;
    private readonly ISpExecutor _executor;
    private readonly ISpNamingConvention _convention;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgeConnection"/> class.
    /// </summary>
    /// <param name="options">The DapperForge configuration options.</param>
    /// <param name="diagnostics">The query diagnostics logger.</param>
    public ForgeConnection(ForgeOptions options, IQueryDiagnostics diagnostics)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(diagnostics);
        _connection = CreateConnection(options);
        var commandBuilder = CreateCommandBuilder(options.Provider);
        _executor = new SpExecutor(_connection, diagnostics, commandBuilder);
        _convention = options.Convention;
    }

    // ─── Convention-Based SP Calls ───────────────────────────────────────

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetAsync<T>(object? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        var spName = _convention.ResolveSelect<T>();
        return await _executor.QueryAsync<T>(spName, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> GetSingleAsync<T>(object? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        var spName = _convention.ResolveSelect<T>();
        return await _executor.QuerySingleAsync<T>(spName, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        var spName = _convention.ResolveUpsert<T>();
        return await _executor.ExecuteAsync(spName, entity, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> RemoveAsync<T>(object? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        var spName = _convention.ResolveDelete<T>();
        return await _executor.ExecuteAsync(spName, parameters, cancellationToken: cancellationToken);
    }

    // ─── Direct SP Calls ─────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ExecuteSpAsync<T>(string spName, object? parameters = null,
        CancellationToken cancellationToken = default) where T : class
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        return await _executor.QueryAsync<T>(spName, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T> ExecuteSpScalarAsync<T>(string spName, object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        return await _executor.ScalarAsync<T>(spName, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteSpNonQueryAsync(string spName, object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        return await _executor.ExecuteAsync(spName, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<T1>, IEnumerable<T2>)> ExecuteSpMultiAsync<T1, T2>(
        string spName, object? parameters = null, CancellationToken cancellationToken = default)
        where T1 : class where T2 : class
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        return await _executor.QueryMultipleAsync<T1, T2>(spName, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>)> ExecuteSpMultiAsync<T1, T2, T3>(
        string spName, object? parameters = null, CancellationToken cancellationToken = default)
        where T1 : class where T2 : class where T3 : class
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        return await _executor.QueryMultipleAsync<T1, T2, T3>(spName, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SpResult> ExecuteSpWithOutputAsync(string spName, object? parameters,
        IDictionary<string, DbType> outputParameters, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);
        return await _executor.ExecuteWithOutputAsync(spName, parameters, outputParameters, cancellationToken: cancellationToken);
    }

    // ─── Transaction Support ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task InTransactionAsync(Func<IForgeTransaction, Task> action,
        CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        await EnsureConnectionOpenAsync(cancellationToken);

        using var transaction = _connection.BeginTransaction();
        var forgeTx = new ForgeTransaction(transaction, _executor, _convention);
        try
        {
            await action(forgeTx);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <inheritdoc />
    public IForgeTransaction BeginTransaction()
    {
        EnsureNotDisposed();
        if (_connection.State == ConnectionState.Closed)
            _connection.Open();

        var transaction = _connection.BeginTransaction();
        return new ForgeTransaction(transaction, _executor, _convention);
    }

    // ─── Dispose ─────────────────────────────────────────────────────────

    /// <summary>
    /// Disposes the database connection.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the database connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_connection is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else
                _connection.Dispose();

            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }

    // ─── Private Helpers ─────────────────────────────────────────────────

    private static IDbConnection CreateConnection(ForgeOptions options)
    {
        return options.Provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnection(options.ConnectionString),
            DatabaseProvider.PostgreSQL => new NpgsqlConnection(options.ConnectionString),
            _ => throw new NotSupportedException($"Database provider '{options.Provider}' is not supported.")
        };
    }

    private static ISpCommandBuilder CreateCommandBuilder(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.SqlServer => new SqlServerCommandBuilder(),
            DatabaseProvider.PostgreSQL => new PostgresCommandBuilder(),
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported.")
        };
    }

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == ConnectionState.Closed)
        {
            if (_connection is DbConnection dbConnection)
                await dbConnection.OpenAsync(cancellationToken);
            else
                _connection.Open();
        }
    }

    private void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
