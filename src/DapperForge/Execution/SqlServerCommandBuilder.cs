using System.Data;
using Dapper;

namespace DapperForge.Execution;

/// <summary>
/// Builds commands for SQL Server stored procedure execution.
/// Uses <see cref="CommandType.StoredProcedure"/> which maps to EXEC syntax.
/// </summary>
public sealed class SqlServerCommandBuilder : ISpCommandBuilder
{
    /// <inheritdoc />
    public CommandDefinition BuildCommand(string spName, object? parameters,
        IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        return new CommandDefinition(spName, parameters, transaction,
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public CommandDefinition BuildCommand(string spName, DynamicParameters parameters,
        IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        return new CommandDefinition(spName, parameters, transaction,
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
    }
}
