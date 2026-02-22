namespace DapperForge.Transaction;

/// <summary>
/// Represents a transactional scope for executing multiple stored procedures atomically.
/// Provides the same convention-based SP execution as <see cref="IForgeConnection"/>
/// but within a transaction context.
/// </summary>
public interface IForgeTransaction : IDisposable
{
    /// <summary>
    /// Executes a convention-based SELECT stored procedure within the transaction.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of entities.</returns>
    Task<IEnumerable<T>> GetAsync<T>(object? parameters = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a convention-based SELECT stored procedure within the transaction and returns a single result.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity, or null if not found.</returns>
    Task<T?> GetSingleAsync<T>(object? parameters = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a convention-based INSERT/UPDATE stored procedure within the transaction.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> SaveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a convention-based DELETE stored procedure within the transaction.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="parameters">Parameters to identify the entity to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> RemoveAsync<T>(object? parameters = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a named stored procedure within the transaction.
    /// </summary>
    /// <typeparam name="T">The result entity type.</typeparam>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of entities.</returns>
    Task<IEnumerable<T>> ExecuteSpAsync<T>(string spName, object? parameters = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a non-query stored procedure within the transaction.
    /// </summary>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> ExecuteSpNonQueryAsync(string spName, object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits all operations within the transaction.
    /// </summary>
    void Commit();

    /// <summary>
    /// Rolls back all operations within the transaction.
    /// </summary>
    void Rollback();
}
