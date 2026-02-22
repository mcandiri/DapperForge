namespace DapperForge.Execution;

/// <summary>
/// Represents the result of a stored procedure execution that includes output parameters.
/// </summary>
public sealed class SpResult
{
    /// <summary>
    /// Gets the number of rows affected by the stored procedure.
    /// </summary>
    public int RowsAffected { get; init; }

    /// <summary>
    /// Gets the output parameter values returned by the stored procedure.
    /// </summary>
    public IReadOnlyDictionary<string, object?> OutputValues { get; init; } = new Dictionary<string, object?>();
}
