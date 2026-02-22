using System.Data;
using System.Reflection;
using Dapper;

namespace DapperForge.Execution;

/// <summary>
/// Builds commands for PostgreSQL stored procedure/function execution.
/// PostgreSQL does not support <see cref="CommandType.StoredProcedure"/> via Npgsql.
/// Instead, generates <c>SELECT * FROM function_name(@param1, @param2)</c> syntax.
/// </summary>
public sealed class PostgresCommandBuilder : ISpCommandBuilder
{
    /// <inheritdoc />
    public CommandDefinition BuildCommand(string spName, object? parameters,
        IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        var paramNames = ExtractParameterNames(parameters);
        var sql = BuildCallSyntax(spName, paramNames);

        return new CommandDefinition(sql, parameters, transaction,
            commandType: CommandType.Text, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public CommandDefinition BuildCommand(string spName, DynamicParameters parameters,
        IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        var paramNames = parameters.ParameterNames.ToList();
        var sql = BuildCallSyntax(spName, paramNames);

        return new CommandDefinition(sql, parameters, transaction,
            commandType: CommandType.Text, cancellationToken: cancellationToken);
    }

    private static string BuildCallSyntax(string spName, IReadOnlyList<string> paramNames)
    {
        if (paramNames.Count == 0)
            return $"SELECT * FROM {spName}()";

        var paramList = string.Join(", ", paramNames.Select(p => $"@{p}"));
        return $"SELECT * FROM {spName}({paramList})";
    }

    private static IReadOnlyList<string> ExtractParameterNames(object? parameters)
    {
        if (parameters is null)
            return Array.Empty<string>();

        if (parameters is DynamicParameters dynamicParams)
            return dynamicParams.ParameterNames.ToList();

        return parameters.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToList();
    }
}
