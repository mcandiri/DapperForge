using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DapperForge.Validation;

/// <summary>
/// Validates stored procedure existence against SQL Server using sys.objects.
/// </summary>
public sealed class SqlServerSpValidator : ISpValidator
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerSpValidator"/> class.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public SqlServerSpValidator(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string spName, CancellationToken cancellationToken = default)
    {
        var (schema, name) = ParseSpName(spName);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(1) FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE o.type IN ('P', 'FN', 'IF', 'TF')
            AND s.name = @Schema AND o.name = @Name
            """;

        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Schema = schema, Name = name }, cancellationToken: cancellationToken));

        return count > 0;
    }

    private static (string schema, string name) ParseSpName(string spName)
    {
        var parts = spName.Split('.', 2);
        return parts.Length == 2
            ? (parts[0], parts[1])
            : ("dbo", parts[0]);
    }
}
