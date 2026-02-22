using System.Data;

namespace DapperForge.Execution;

/// <summary>
/// Defines the low-level contract for executing stored procedures against the database.
/// </summary>
public interface ISpExecutor
{
    /// <summary>
    /// Executes a stored procedure and returns a collection of results.
    /// </summary>
    /// <typeparam name="T">The result entity type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of entities.</returns>
    Task<IEnumerable<T>> QueryAsync<T>(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a stored procedure and returns a single result or null.
    /// </summary>
    /// <typeparam name="T">The result entity type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first result entity, or null.</returns>
    Task<T?> QuerySingleAsync<T>(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a stored procedure and returns a scalar value.
    /// </summary>
    /// <typeparam name="T">The scalar return type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scalar value.</returns>
    Task<T> ScalarAsync<T>(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure that does not return a result set.
    /// </summary>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> ExecuteAsync(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure that returns multiple result sets.
    /// </summary>
    /// <typeparam name="T1">The first result set entity type.</typeparam>
    /// <typeparam name="T2">The second result set entity type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple of two result sets.</returns>
    Task<(IEnumerable<T1>, IEnumerable<T2>)> QueryMultipleAsync<T1, T2>(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        where T1 : class where T2 : class;

    /// <summary>
    /// Executes a stored procedure that returns three result sets.
    /// </summary>
    /// <typeparam name="T1">The first result set entity type.</typeparam>
    /// <typeparam name="T2">The second result set entity type.</typeparam>
    /// <typeparam name="T3">The third result set entity type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple of three result sets.</returns>
    Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>)> QueryMultipleAsync<T1, T2, T3>(
        string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        where T1 : class where T2 : class where T3 : class;

    /// <summary>
    /// Executes a stored procedure with output parameters.
    /// </summary>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Input parameters.</param>
    /// <param name="outputParameters">Output parameter definitions (name → DbType).</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing rows affected and output parameter values.</returns>
    Task<SpResult> ExecuteWithOutputAsync(string spName, object? parameters,
        IDictionary<string, System.Data.DbType> outputParameters,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
