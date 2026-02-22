using Dapper;
using Npgsql;

namespace DapperForge.Validation;

/// <summary>
/// Validates stored procedure/function existence against PostgreSQL using information_schema.routines.
/// </summary>
public sealed class PostgresSpValidator : ISpValidator
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresSpValidator"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public PostgresSpValidator(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string spName, CancellationToken cancellationToken = default)
    {
        var (schema, name) = ParseSpName(spName);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT COUNT(1) FROM information_schema.routines
            WHERE routine_schema = @Schema AND routine_name = @Name
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
            : ("public", parts[0]);
    }
}
