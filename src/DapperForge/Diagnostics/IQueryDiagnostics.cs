namespace DapperForge.Diagnostics;

/// <summary>
/// Defines the contract for query diagnostics and logging.
/// </summary>
public interface IQueryDiagnostics
{
    /// <summary>
    /// Records a successful query execution event.
    /// </summary>
    /// <param name="queryEvent">The query event data.</param>
    void Record(QueryEvent queryEvent);

    /// <summary>
    /// Records a failed query execution event.
    /// </summary>
    /// <param name="queryEvent">The query event data.</param>
    void RecordFailure(QueryEvent queryEvent);
}
