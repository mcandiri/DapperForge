using DapperForge.Configuration;
using Microsoft.Extensions.Logging;

namespace DapperForge.Diagnostics;

/// <summary>
/// Default implementation of <see cref="IQueryDiagnostics"/> that logs to <see cref="ILogger"/>
/// and invokes the optional <see cref="ForgeOptions.OnQueryExecuted"/> callback.
/// </summary>
public class QueryDiagnostics : IQueryDiagnostics
{
    private readonly ILogger<QueryDiagnostics> _logger;
    private readonly ForgeOptions _options;

    private const string LogPrefix = "[DapperForge]";

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryDiagnostics"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The DapperForge configuration options.</param>
    public QueryDiagnostics(ILogger<QueryDiagnostics> logger, ForgeOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public void Record(QueryEvent queryEvent)
    {
        ArgumentNullException.ThrowIfNull(queryEvent);

        _options.OnQueryExecuted?.Invoke(queryEvent);

        if (!_options.EnableDiagnostics)
            return;

        var elapsedMs = queryEvent.Duration.TotalMilliseconds;
        var rowInfo = FormatRowInfo(queryEvent.RowCount);

        if (queryEvent.Duration > _options.SlowQueryThreshold)
        {
            _logger.LogWarning("{LogPrefix} SLOW: {SpName} executed in {ElapsedMs:F0}ms{RowInfo}",
                LogPrefix, queryEvent.SpName, elapsedMs, rowInfo);
        }
        else
        {
            _logger.LogInformation("{LogPrefix} {SpName} executed in {ElapsedMs:F0}ms{RowInfo}",
                LogPrefix, queryEvent.SpName, elapsedMs, rowInfo);
        }
    }

    /// <inheritdoc />
    public void RecordFailure(QueryEvent queryEvent)
    {
        ArgumentNullException.ThrowIfNull(queryEvent);

        _options.OnQueryExecuted?.Invoke(queryEvent);

        if (!_options.EnableDiagnostics)
            return;

        var elapsedMs = queryEvent.Duration.TotalMilliseconds;

        _logger.LogError(queryEvent.Exception,
            "{LogPrefix} FAILED: {SpName} — {ErrorMessage} ({ElapsedMs:F0}ms)",
            LogPrefix, queryEvent.SpName, queryEvent.Exception?.Message ?? "Unknown error", elapsedMs);
    }

    private static string FormatRowInfo(int? rowCount) =>
        rowCount.HasValue ? $" -> {rowCount.Value} rows" : string.Empty;
}
