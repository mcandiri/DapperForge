using System.Data;
using DapperForge.Execution;
using DapperForge.Transaction;

namespace DapperForge;

/// <summary>
/// The main entry point for DapperForge stored procedure operations.
/// Provides convention-based SP execution, direct SP calls, and transaction support.
/// </summary>
public interface IForgeConnection : IAsyncDisposable, IDisposable
{
    // ─── Convention-Based SP Calls ───────────────────────────────────────

    /// <summary>
    /// Executes a convention-based SELECT stored procedure.
    /// SP name is resolved automatically (e.g., "Get_Students").
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="parameters">Optional parameters to pass to the stored procedure.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of entities.</returns>
    Task<IEnumerable<T>> GetAsync<T>(object? parameters = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a convention-based SELECT stored procedure and returns a single result.
    /// SP name is resolved automatically (e.g., "Get_Students" with @Id parameter).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="parameters">Parameters to identify the entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity, or null if not found.</returns>
    Task<T?> GetSingleAsync<T>(object? parameters = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a convention-based INSERT/UPDATE stored procedure (e.g., "Save_Students").
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> SaveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a convention-based DELETE stored procedure (e.g., "Remove_Students").
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="parameters">Parameters to identify the entity to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> RemoveAsync<T>(object? parameters = null, CancellationToken cancellationToken = default) where T : class;

    // ─── Direct SP Calls ─────────────────────────────────────────────────

    /// <summary>
    /// Executes a named stored procedure and returns a collection of results.
    /// </summary>
    /// <typeparam name="T">The result entity type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of entities.</returns>
    Task<IEnumerable<T>> ExecuteSpAsync<T>(string spName, object? parameters = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a stored procedure and returns a scalar value.
    /// </summary>
    /// <typeparam name="T">The scalar return type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scalar value.</returns>
    Task<T> ExecuteSpScalarAsync<T>(string spName, object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure that does not return a result set.
    /// </summary>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> ExecuteSpNonQueryAsync(string spName, object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure that returns two result sets.
    /// </summary>
    /// <typeparam name="T1">The first result set entity type.</typeparam>
    /// <typeparam name="T2">The second result set entity type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple of two result sets.</returns>
    Task<(IEnumerable<T1>, IEnumerable<T2>)> ExecuteSpMultiAsync<T1, T2>(string spName,
        object? parameters = null, CancellationToken cancellationToken = default)
        where T1 : class where T2 : class;

    /// <summary>
    /// Executes a stored procedure that returns three result sets.
    /// </summary>
    /// <typeparam name="T1">The first result set entity type.</typeparam>
    /// <typeparam name="T2">The second result set entity type.</typeparam>
    /// <typeparam name="T3">The third result set entity type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple of three result sets.</returns>
    Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>)> ExecuteSpMultiAsync<T1, T2, T3>(
        string spName, object? parameters = null, CancellationToken cancellationToken = default)
        where T1 : class where T2 : class where T3 : class;

    /// <summary>
    /// Executes a stored procedure with output parameters.
    /// </summary>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Input parameters.</param>
    /// <param name="outputParameters">Output parameter definitions (name → DbType).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing rows affected and output parameter values.</returns>
    Task<SpResult> ExecuteSpWithOutputAsync(string spName, object? parameters,
        IDictionary<string, DbType> outputParameters, CancellationToken cancellationToken = default);

    // ─── Transaction Support ─────────────────────────────────────────────

    /// <summary>
    /// Executes multiple SP operations within a transaction.
    /// Auto-commits on success, auto-rolls back on exception.
    /// </summary>
    /// <param name="action">The async action containing transactional operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task InTransactionAsync(Func<IForgeTransaction, Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a manual transaction for fine-grained control.
    /// Caller is responsible for calling Commit() or Rollback().
    /// </summary>
    /// <returns>A transaction wrapper.</returns>
    IForgeTransaction BeginTransaction();
}
