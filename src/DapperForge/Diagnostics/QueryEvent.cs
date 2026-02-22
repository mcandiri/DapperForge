namespace DapperForge.Diagnostics;

/// <summary>
/// Represents a diagnostic event raised after a stored procedure execution.
/// </summary>
public sealed class QueryEvent
{
    /// <summary>
    /// Gets the stored procedure name that was executed.
    /// </summary>
    public required string SpName { get; init; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the number of rows affected or returned. Null if not applicable.
    /// </summary>
    public int? RowCount { get; init; }

    /// <summary>
    /// Gets the parameters passed to the stored procedure.
    /// </summary>
    public object? Parameters { get; init; }

    /// <summary>
    /// Gets a value indicating whether the execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// Gets the exception if the execution failed.
    /// </summary>
    public Exception? Exception { get; init; }
}
