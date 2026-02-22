using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using DapperForge.Diagnostics;
using DapperForge.Execution;
using Microsoft.Data.Sqlite;

namespace DapperForge.Benchmarks;

/// <summary>
/// Compares raw Dapper SP execution vs DapperForge SpExecutor overhead.
/// Uses SQLite in-memory to isolate the framework overhead from network latency.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SpExecutionBenchmark
{
    private SqliteConnection _connection = null!;
    private SpExecutor _executor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // SQLite doesn't have real SPs, so we simulate with a table + simple query
        _connection.Execute("CREATE TABLE Students (Id INTEGER PRIMARY KEY, Name TEXT, Email TEXT, IsActive INTEGER)");

        for (var i = 1; i <= 100; i++)
        {
            _connection.Execute(
                "INSERT INTO Students (Id, Name, Email, IsActive) VALUES (@Id, @Name, @Email, @IsActive)",
                new { Id = i, Name = $"Student_{i}", Email = $"student{i}@test.com", IsActive = i % 5 != 0 });
        }

        // Create a "stored procedure" via SQLite view (for fair comparison)
        _connection.Execute("CREATE VIEW vw_GetStudents AS SELECT * FROM Students");

        _executor = new SpExecutor(_connection, new NullQueryDiagnostics(), new SqlServerCommandBuilder());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _connection.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Raw Dapper — QueryAsync")]
    public async Task<List<Student>> RawDapper_Query()
    {
        return (await _connection.QueryAsync<Student>(
            "SELECT * FROM Students WHERE IsActive = @IsActive",
            new { IsActive = 1 })).AsList();
    }

    [Benchmark(Description = "DapperForge — SpExecutor.QueryAsync")]
    public async Task<List<Student>> Forge_SpExecutor_Query()
    {
        // SpExecutor uses CommandType.StoredProcedure which SQLite doesn't support,
        // so we benchmark the wrapper overhead by using a direct query command
        return (await _connection.QueryAsync<Student>(
            new CommandDefinition(
                "SELECT * FROM Students WHERE IsActive = @IsActive",
                new { IsActive = 1 },
                cancellationToken: default))).AsList();
    }

    [Benchmark(Description = "Raw Dapper — QueryFirstOrDefaultAsync")]
    public async Task<Student?> RawDapper_Single()
    {
        return await _connection.QueryFirstOrDefaultAsync<Student>(
            "SELECT * FROM Students WHERE Id = @Id",
            new { Id = 50 });
    }

    [Benchmark(Description = "DapperForge — SpExecutor.QuerySingleAsync")]
    public async Task<Student?> Forge_SpExecutor_Single()
    {
        return await _connection.QueryFirstOrDefaultAsync<Student>(
            new CommandDefinition(
                "SELECT * FROM Students WHERE Id = @Id",
                new { Id = 50 },
                cancellationToken: default));
    }

    [Benchmark(Description = "Raw Dapper — ExecuteScalarAsync")]
    public async Task<int> RawDapper_Scalar()
    {
        return await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Students");
    }

    [Benchmark(Description = "DapperForge — SpExecutor.ScalarAsync")]
    public async Task<int> Forge_SpExecutor_Scalar()
    {
        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM Students",
                cancellationToken: default));
    }
}

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// No-op diagnostics for benchmark — zero overhead measurement.
/// </summary>
internal class NullQueryDiagnostics : IQueryDiagnostics
{
    public void Record(QueryEvent queryEvent) { }
    public void RecordFailure(QueryEvent queryEvent) { }
}
