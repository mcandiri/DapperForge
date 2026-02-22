using System.Data;
using Dapper;

namespace DapperForge.Execution;

/// <summary>
/// Builds database-specific command definitions for stored procedure execution.
/// SQL Server and PostgreSQL use fundamentally different syntax for calling stored procedures.
/// </summary>
public interface ISpCommandBuilder
{
    /// <summary>
    /// Creates a command definition for executing a stored procedure.
    /// </summary>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">Optional parameters.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Dapper <see cref="CommandDefinition"/> configured for the target database provider.</returns>
    CommandDefinition BuildCommand(string spName, object? parameters,
        IDbTransaction? transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a command definition for executing a stored procedure with DynamicParameters
    /// (used for output parameters).
    /// </summary>
    /// <param name="spName">The stored procedure name.</param>
    /// <param name="parameters">The dynamic parameters including output parameters.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A Dapper <see cref="CommandDefinition"/> configured for the target database provider.</returns>
    CommandDefinition BuildCommand(string spName, DynamicParameters parameters,
        IDbTransaction? transaction, CancellationToken cancellationToken);
}
