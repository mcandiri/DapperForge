using Dapper;
using DapperForge.Configuration;
using DapperForge.Diagnostics;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DapperForge.IntegrationTests;

[Collection("SqlServer")]
public class ForgeConnectionTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private readonly ForgeOptions _options;
    private readonly IQueryDiagnostics _diagnostics;

    public ForgeConnectionTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
        _options = new ForgeOptions
        {
            ConnectionString = fixture.ConnectionString,
            Provider = DatabaseProvider.SqlServer,
            EnableDiagnostics = true,
            SlowQueryThreshold = TimeSpan.FromSeconds(5)
        };
        _diagnostics = new QueryDiagnostics(
            NullLogger<QueryDiagnostics>.Instance, _options);
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        // Create test table
        await connection.ExecuteAsync("""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Students')
            CREATE TABLE Students (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Name NVARCHAR(100) NOT NULL,
                Email NVARCHAR(200) NOT NULL,
                IsActive BIT NOT NULL DEFAULT 1
            )
        """);

        // Create stored procedures
        await connection.ExecuteAsync("""
            IF OBJECT_ID('Get_Students', 'P') IS NOT NULL DROP PROCEDURE Get_Students;
        """);
        await connection.ExecuteAsync("""
            CREATE PROCEDURE Get_Students
                @IsActive BIT = NULL,
                @Id INT = NULL
            AS
            BEGIN
                IF @Id IS NOT NULL
                    SELECT * FROM Students WHERE Id = @Id
                ELSE IF @IsActive IS NOT NULL
                    SELECT * FROM Students WHERE IsActive = @IsActive
                ELSE
                    SELECT * FROM Students
            END
        """);

        await connection.ExecuteAsync("""
            IF OBJECT_ID('Save_Students', 'P') IS NOT NULL DROP PROCEDURE Save_Students;
        """);
        await connection.ExecuteAsync("""
            CREATE PROCEDURE Save_Students
                @Id INT = NULL,
                @Name NVARCHAR(100),
                @Email NVARCHAR(200),
                @IsActive BIT = 1
            AS
            BEGIN
                IF @Id IS NULL OR @Id = 0
                BEGIN
                    INSERT INTO Students (Name, Email, IsActive) VALUES (@Name, @Email, @IsActive)
                    SELECT SCOPE_IDENTITY()
                END
                ELSE
                BEGIN
                    UPDATE Students SET Name = @Name, Email = @Email, IsActive = @IsActive WHERE Id = @Id
                    SELECT @Id
                END
            END
        """);

        await connection.ExecuteAsync("""
            IF OBJECT_ID('Remove_Students', 'P') IS NOT NULL DROP PROCEDURE Remove_Students;
        """);
        await connection.ExecuteAsync("""
            CREATE PROCEDURE Remove_Students
                @Id INT
            AS
            BEGIN
                DELETE FROM Students WHERE Id = @Id
            END
        """);

        await connection.ExecuteAsync("""
            IF OBJECT_ID('sel_StudentCount', 'P') IS NOT NULL DROP PROCEDURE sel_StudentCount;
        """);
        await connection.ExecuteAsync("""
            CREATE PROCEDURE sel_StudentCount
            AS
            BEGIN
                SELECT COUNT(*) FROM Students
            END
        """);

        // Clear data
        await connection.ExecuteAsync("DELETE FROM Students");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ─── Convention-Based Tests ──────────────────────────────────────────

    [Fact]
    public async Task GetAsync_ShouldReturnAllStudents()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);
        await SeedStudents(forge);

        var students = await forge.GetAsync<Student>();

        students.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAsync_WithFilter_ShouldReturnFiltered()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);
        await SeedStudents(forge);

        var active = await forge.GetAsync<Student>(new { IsActive = true });

        active.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSingleAsync_ShouldReturnOneStudent()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);
        await SeedStudents(forge);

        var students = await forge.GetAsync<Student>();
        var firstId = students.First().Id;

        var student = await forge.GetSingleAsync<Student>(new { Id = firstId });

        student.Should().NotBeNull();
        student!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task SaveAsync_ShouldInsertNewStudent()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);

        var result = await forge.SaveAsync(new Student
        {
            Name = "NewStudent",
            Email = "new@test.com",
            IsActive = true
        });

        result.Should().BeGreaterThan(0);

        var all = await forge.GetAsync<Student>();
        all.Should().Contain(s => s.Name == "NewStudent");
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteStudent()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);
        await SeedStudents(forge);

        var students = await forge.GetAsync<Student>();
        var firstId = students.First().Id;

        await forge.RemoveAsync<Student>(new { Id = firstId });

        var after = await forge.GetAsync<Student>();
        after.Should().HaveCount(2);
    }

    // ─── Direct SP Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteSpAsync_DirectCall_ShouldWork()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);
        await SeedStudents(forge);

        var students = await forge.ExecuteSpAsync<Student>("Get_Students", new { IsActive = true });

        students.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteSpScalarAsync_ShouldReturnCount()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);
        await SeedStudents(forge);

        var count = await forge.ExecuteSpScalarAsync<int>("sel_StudentCount");

        count.Should().Be(3);
    }

    // ─── Transaction Tests ───────────────────────────────────────────────

    [Fact]
    public async Task InTransactionAsync_ShouldCommitOnSuccess()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);

        await forge.InTransactionAsync(async tx =>
        {
            await tx.SaveAsync(new Student { Name = "TxStudent1", Email = "tx1@test.com" });
            await tx.SaveAsync(new Student { Name = "TxStudent2", Email = "tx2@test.com" });
        });

        var all = await forge.GetAsync<Student>();
        all.Should().Contain(s => s.Name == "TxStudent1");
        all.Should().Contain(s => s.Name == "TxStudent2");
    }

    [Fact]
    public async Task InTransactionAsync_ShouldRollbackOnException()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);

        var act = async () =>
        {
            await forge.InTransactionAsync(async tx =>
            {
                await tx.SaveAsync(new Student { Name = "WillRollback", Email = "rb@test.com" });
                throw new InvalidOperationException("Simulated failure");
            });
        };

        await act.Should().ThrowAsync<InvalidOperationException>();

        var all = await forge.GetAsync<Student>();
        all.Should().NotContain(s => s.Name == "WillRollback");
    }

    [Fact]
    public async Task BeginTransaction_ManualCommit_ShouldWork()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);

        using var tx = forge.BeginTransaction();
        await tx.SaveAsync(new Student { Name = "ManualTx", Email = "manual@test.com" });
        tx.Commit();

        var all = await forge.GetAsync<Student>();
        all.Should().Contain(s => s.Name == "ManualTx");
    }

    [Fact]
    public async Task BeginTransaction_ManualRollback_ShouldRevert()
    {
        await using var forge = new ForgeConnection(_options, _diagnostics);

        using var tx = forge.BeginTransaction();
        await tx.SaveAsync(new Student { Name = "WillRevert", Email = "revert@test.com" });
        tx.Rollback();

        var all = await forge.GetAsync<Student>();
        all.Should().NotContain(s => s.Name == "WillRevert");
    }

    // ─── Diagnostics Test ────────────────────────────────────────────────

    [Fact]
    public async Task OnQueryExecuted_ShouldFireCallback()
    {
        var events = new List<QueryEvent>();
        var options = new ForgeOptions
        {
            ConnectionString = _fixture.ConnectionString,
            Provider = DatabaseProvider.SqlServer,
            EnableDiagnostics = true,
            OnQueryExecuted = e => events.Add(e)
        };
        var diagnostics = new QueryDiagnostics(
            NullLogger<QueryDiagnostics>.Instance, options);

        await using var forge = new ForgeConnection(options, diagnostics);
        await forge.GetAsync<Student>();

        events.Should().ContainSingle();
        events[0].SpName.Should().Be("Get_Students");
        events[0].IsSuccess.Should().BeTrue();
        events[0].Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private async Task SeedStudents(IForgeConnection forge)
    {
        await forge.SaveAsync(new Student { Name = "Alice", Email = "alice@test.com", IsActive = true });
        await forge.SaveAsync(new Student { Name = "Bob", Email = "bob@test.com", IsActive = true });
        await forge.SaveAsync(new Student { Name = "Charlie", Email = "charlie@test.com", IsActive = false });
    }
}

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
