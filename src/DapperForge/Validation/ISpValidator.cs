namespace DapperForge.Validation;

/// <summary>
/// Validates stored procedure existence against the database catalog.
/// </summary>
public interface ISpValidator
{
    /// <summary>
    /// Checks whether the specified stored procedure exists in the database.
    /// </summary>
    /// <param name="spName">The fully qualified stored procedure name (may include schema).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the stored procedure exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string spName, CancellationToken cancellationToken = default);
}
