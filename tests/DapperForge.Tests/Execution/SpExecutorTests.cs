using System.Data;
using DapperForge.Configuration;
using DapperForge.Diagnostics;
using DapperForge.Execution;
using FluentAssertions;
using Moq;
using Xunit;

namespace DapperForge.Tests.Execution;

public class SpExecutorTests
{
    [Fact]
    public void Constructor_NullConnection_ShouldThrow()
    {
        var mockDiagnostics = new Mock<IQueryDiagnostics>();
        var mockCommandBuilder = new Mock<ISpCommandBuilder>();

        var act = () => new SpExecutor(null!, mockDiagnostics.Object, mockCommandBuilder.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public void Constructor_NullDiagnostics_ShouldThrow()
    {
        var mockConnection = new Mock<IDbConnection>();
        var mockCommandBuilder = new Mock<ISpCommandBuilder>();

        var act = () => new SpExecutor(mockConnection.Object, null!, mockCommandBuilder.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("diagnostics");
    }

    [Fact]
    public void Constructor_NullCommandBuilder_ShouldThrow()
    {
        var mockConnection = new Mock<IDbConnection>();
        var mockDiagnostics = new Mock<IQueryDiagnostics>();

        var act = () => new SpExecutor(mockConnection.Object, mockDiagnostics.Object, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("commandBuilder");
    }

    [Fact]
    public void SpResult_ShouldHaveDefaultValues()
    {
        var result = new SpResult();

        result.RowsAffected.Should().Be(0);
        result.OutputValues.Should().BeEmpty();
    }

    [Fact]
    public void SpResult_ShouldStoreValues()
    {
        var outputValues = new Dictionary<string, object?> { ["NewId"] = 42 };
        var result = new SpResult
        {
            RowsAffected = 1,
            OutputValues = outputValues
        };

        result.RowsAffected.Should().Be(1);
        result.OutputValues["NewId"].Should().Be(42);
    }

    [Fact]
    public void ForgeConnection_Constructor_NullOptions_ShouldThrow()
    {
        var mockDiagnostics = new Mock<IQueryDiagnostics>();

        var act = () => new ForgeConnection(null!, mockDiagnostics.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void ForgeConnection_Constructor_NullDiagnostics_ShouldThrow()
    {
        var options = new ForgeOptions { ConnectionString = "Server=test" };

        var act = () => new ForgeConnection(options, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ForgeConnection_SqlServer_ShouldCreateWithoutError()
    {
        var options = new ForgeOptions
        {
            ConnectionString = "Server=test;Database=test;",
            Provider = DatabaseProvider.SqlServer
        };
        var mockDiagnostics = new Mock<IQueryDiagnostics>();

        var connection = new ForgeConnection(options, mockDiagnostics.Object);

        connection.Should().NotBeNull();
        connection.Dispose();
    }

    [Fact]
    public void ForgeConnection_PostgreSQL_ShouldCreateWithoutError()
    {
        var options = new ForgeOptions
        {
            ConnectionString = "Host=localhost;Database=test;",
            Provider = DatabaseProvider.PostgreSQL
        };
        var mockDiagnostics = new Mock<IQueryDiagnostics>();

        var connection = new ForgeConnection(options, mockDiagnostics.Object);

        connection.Should().NotBeNull();
        connection.Dispose();
    }

    [Fact]
    public void ForgeConnection_Dispose_ShouldNotThrow()
    {
        var options = new ForgeOptions { ConnectionString = "Server=test;Database=test;" };
        var mockDiagnostics = new Mock<IQueryDiagnostics>();
        var connection = new ForgeConnection(options, mockDiagnostics.Object);

        var act = () => connection.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ForgeConnection_DisposeAsync_ShouldNotThrow()
    {
        var options = new ForgeOptions { ConnectionString = "Server=test;Database=test;" };
        var mockDiagnostics = new Mock<IQueryDiagnostics>();
        var connection = new ForgeConnection(options, mockDiagnostics.Object);

        var act = async () => await connection.DisposeAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void ForgeConnection_AfterDispose_ShouldThrowOnUse()
    {
        var options = new ForgeOptions { ConnectionString = "Server=test;Database=test;" };
        var mockDiagnostics = new Mock<IQueryDiagnostics>();
        var connection = new ForgeConnection(options, mockDiagnostics.Object);
        connection.Dispose();

        var act = async () => await connection.GetAsync<SpExecutorTests>();

        act.Should().ThrowAsync<ObjectDisposedException>();
    }
}
