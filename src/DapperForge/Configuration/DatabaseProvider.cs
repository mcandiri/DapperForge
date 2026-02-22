namespace DapperForge.Configuration;

/// <summary>
/// Supported database providers.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// Microsoft SQL Server. Uses EXEC syntax for stored procedure calls.
    /// </summary>
    SqlServer,

    /// <summary>
    /// PostgreSQL. Uses SELECT * FROM function() syntax for stored procedure calls.
    /// </summary>
    PostgreSQL
}
