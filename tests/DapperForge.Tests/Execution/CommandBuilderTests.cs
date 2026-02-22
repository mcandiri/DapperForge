using System.Data;
using Dapper;
using DapperForge.Execution;
using FluentAssertions;
using Xunit;

namespace DapperForge.Tests.Execution;

public class CommandBuilderTests
{
    // ─── SqlServerCommandBuilder ──────────────────────────────────────

    [Fact]
    public void SqlServer_ShouldUseStoredProcedureCommandType()
    {
        var builder = new SqlServerCommandBuilder();

        var command = builder.BuildCommand("Get_Students", new { IsActive = true }, null, default);

        command.CommandType.Should().Be(CommandType.StoredProcedure);
        command.CommandText.Should().Be("Get_Students");
    }

    [Fact]
    public void SqlServer_WithNullParameters_ShouldUseStoredProcedureCommandType()
    {
        var builder = new SqlServerCommandBuilder();

        var command = builder.BuildCommand("Get_Students", (object?)null, null, default);

        command.CommandType.Should().Be(CommandType.StoredProcedure);
        command.CommandText.Should().Be("Get_Students");
    }

    [Fact]
    public void SqlServer_WithDynamicParameters_ShouldUseStoredProcedureCommandType()
    {
        var builder = new SqlServerCommandBuilder();
        var dynParams = new DynamicParameters();
        dynParams.Add("Id", 1);

        var command = builder.BuildCommand("Get_Students", dynParams, null, default);

        command.CommandType.Should().Be(CommandType.StoredProcedure);
        command.CommandText.Should().Be("Get_Students");
    }

    // ─── PostgresCommandBuilder ───────────────────────────────────────

    [Fact]
    public void Postgres_ShouldUseTextCommandType()
    {
        var builder = new PostgresCommandBuilder();

        var command = builder.BuildCommand("Get_Students", new { IsActive = true }, null, default);

        command.CommandType.Should().Be(CommandType.Text);
    }

    [Fact]
    public void Postgres_ShouldGenerateSelectFromSyntax()
    {
        var builder = new PostgresCommandBuilder();

        var command = builder.BuildCommand("Get_Students", new { IsActive = true }, null, default);

        command.CommandText.Should().Be("SELECT * FROM Get_Students(@IsActive)");
    }

    [Fact]
    public void Postgres_WithMultipleParams_ShouldGenerateCorrectSyntax()
    {
        var builder = new PostgresCommandBuilder();

        var command = builder.BuildCommand("Save_Students",
            new { Name = "John", Email = "john@test.com", IsActive = true }, null, default);

        command.CommandText.Should().Be("SELECT * FROM Save_Students(@Name, @Email, @IsActive)");
    }

    [Fact]
    public void Postgres_WithNullParameters_ShouldGenerateEmptyParens()
    {
        var builder = new PostgresCommandBuilder();

        var command = builder.BuildCommand("Get_Students", (object?)null, null, default);

        command.CommandText.Should().Be("SELECT * FROM Get_Students()");
        command.CommandType.Should().Be(CommandType.Text);
    }

    [Fact]
    public void Postgres_WithDynamicParameters_ShouldGenerateCorrectSyntax()
    {
        var builder = new PostgresCommandBuilder();
        var dynParams = new DynamicParameters();
        dynParams.Add("Id", 1);
        dynParams.Add("Name", "John");

        var command = builder.BuildCommand("Get_Students", dynParams, null, default);

        command.CommandText.Should().Contain("SELECT * FROM Get_Students(");
        command.CommandText.Should().Contain("@Id");
        command.CommandText.Should().Contain("@Name");
        command.CommandType.Should().Be(CommandType.Text);
    }

    [Fact]
    public void Postgres_WithSchemaPrefix_ShouldPreserveFullName()
    {
        var builder = new PostgresCommandBuilder();

        var command = builder.BuildCommand("public.Get_Students", new { Id = 1 }, null, default);

        command.CommandText.Should().Be("SELECT * FROM public.Get_Students(@Id)");
    }
}
