using System.Data;
using System.Diagnostics;
using Dapper;
using DapperForge.Diagnostics;

namespace DapperForge.Execution;

/// <summary>
/// Default implementation of <see cref="ISpExecutor"/> using Dapper.
/// Delegates command building to <see cref="ISpCommandBuilder"/> for database-specific syntax.
/// </summary>
public class SpExecutor : ISpExecutor
{
    private readonly IDbConnection _connection;
    private readonly IQueryDiagnostics _diagnostics;
    private readonly ISpCommandBuilder _commandBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpExecutor"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="diagnostics">The query diagnostics logger.</param>
    /// <param name="commandBuilder">The database-specific command builder.</param>
    public SpExecutor(IDbConnection connection, IQueryDiagnostics diagnostics, ISpCommandBuilder commandBuilder)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryAsync<T>(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where T : class
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var command = _commandBuilder.BuildCommand(spName, parameters, transaction, cancellationToken);
            var result = (await _connection.QueryAsync<T>(command)).AsList();
            sw.Stop();

            _diagnostics.Record(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, RowCount = result.Count, Parameters = parameters
            });

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _diagnostics.RecordFailure(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, Parameters = parameters,
                IsSuccess = false, Exception = ex
            });
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T?> QuerySingleAsync<T>(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where T : class
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var command = _commandBuilder.BuildCommand(spName, parameters, transaction, cancellationToken);
            var result = await _connection.QueryFirstOrDefaultAsync<T>(command);
            sw.Stop();

            _diagnostics.Record(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, RowCount = result is null ? 0 : 1, Parameters = parameters
            });

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _diagnostics.RecordFailure(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, Parameters = parameters,
                IsSuccess = false, Exception = ex
            });
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T> ScalarAsync<T>(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var command = _commandBuilder.BuildCommand(spName, parameters, transaction, cancellationToken);
            var result = await _connection.ExecuteScalarAsync<T>(command);
            sw.Stop();

            _diagnostics.Record(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, RowCount = 1, Parameters = parameters
            });

            return result!;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _diagnostics.RecordFailure(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, Parameters = parameters,
                IsSuccess = false, Exception = ex
            });
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var command = _commandBuilder.BuildCommand(spName, parameters, transaction, cancellationToken);
            var result = await _connection.ExecuteAsync(command);
            sw.Stop();

            _diagnostics.Record(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, RowCount = result, Parameters = parameters
            });

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _diagnostics.RecordFailure(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, Parameters = parameters,
                IsSuccess = false, Exception = ex
            });
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<T1>, IEnumerable<T2>)> QueryMultipleAsync<T1, T2>(
        string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        where T1 : class where T2 : class
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var command = _commandBuilder.BuildCommand(spName, parameters, transaction, cancellationToken);
            using var multi = await _connection.QueryMultipleAsync(command);
            var result1 = (await multi.ReadAsync<T1>()).AsList();
            var result2 = (await multi.ReadAsync<T2>()).AsList();
            sw.Stop();

            _diagnostics.Record(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, RowCount = result1.Count + result2.Count,
                Parameters = parameters
            });

            return (result1, result2);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _diagnostics.RecordFailure(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, Parameters = parameters,
                IsSuccess = false, Exception = ex
            });
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>)> QueryMultipleAsync<T1, T2, T3>(
        string spName, object? parameters = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        where T1 : class where T2 : class where T3 : class
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var command = _commandBuilder.BuildCommand(spName, parameters, transaction, cancellationToken);
            using var multi = await _connection.QueryMultipleAsync(command);
            var result1 = (await multi.ReadAsync<T1>()).AsList();
            var result2 = (await multi.ReadAsync<T2>()).AsList();
            var result3 = (await multi.ReadAsync<T3>()).AsList();
            sw.Stop();

            _diagnostics.Record(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed,
                RowCount = result1.Count + result2.Count + result3.Count,
                Parameters = parameters
            });

            return (result1, result2, result3);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _diagnostics.RecordFailure(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, Parameters = parameters,
                IsSuccess = false, Exception = ex
            });
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<SpResult> ExecuteWithOutputAsync(string spName, object? parameters,
        IDictionary<string, DbType> outputParameters,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputParameters);

        var sw = Stopwatch.StartNew();
        try
        {
            var dynamicParams = new DynamicParameters(parameters);

            foreach (var (name, dbType) in outputParameters)
            {
                dynamicParams.Add(name, dbType: dbType, direction: ParameterDirection.Output);
            }

            var command = _commandBuilder.BuildCommand(spName, dynamicParams, transaction, cancellationToken);
            var rowsAffected = await _connection.ExecuteAsync(command);
            sw.Stop();

            var outputValues = new Dictionary<string, object?>();
            foreach (var name in outputParameters.Keys)
            {
                outputValues[name] = dynamicParams.Get<object?>(name);
            }

            _diagnostics.Record(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, RowCount = rowsAffected, Parameters = parameters
            });

            return new SpResult
            {
                RowsAffected = rowsAffected,
                OutputValues = outputValues
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _diagnostics.RecordFailure(new QueryEvent
            {
                SpName = spName, Duration = sw.Elapsed, Parameters = parameters,
                IsSuccess = false, Exception = ex
            });
            throw;
        }
    }
}
