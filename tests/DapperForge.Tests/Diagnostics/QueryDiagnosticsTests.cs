using DapperForge.Configuration;
using DapperForge.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DapperForge.Tests.Diagnostics;

public class QueryDiagnosticsTests
{
    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var options = new ForgeOptions();

        var act = () => new QueryDiagnostics(null!, options);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var mockLogger = new Mock<ILogger<QueryDiagnostics>>();

        var act = () => new QueryDiagnostics(mockLogger.Object, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Record_WhenDiagnosticsDisabled_ShouldNotLog()
    {
        var mockLogger = new Mock<ILogger<QueryDiagnostics>>();
        var options = new ForgeOptions { EnableDiagnostics = false };
        var diagnostics = new QueryDiagnostics(mockLogger.Object, options);

        diagnostics.Record(new QueryEvent
        {
            SpName = "Get_Students", Duration = TimeSpan.FromMilliseconds(50)
        });

        mockLogger.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Record_WhenDiagnosticsEnabled_ShouldLogInformation()
    {
        var mockLogger = new Mock<ILogger<QueryDiagnostics>>();
        var options = new ForgeOptions { EnableDiagnostics = true };
        var diagnostics = new QueryDiagnostics(mockLogger.Object, options);

        diagnostics.Record(new QueryEvent
        {
            SpName = "Get_Students", Duration = TimeSpan.FromMilliseconds(50), RowCount = 10
        });

        mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Record_SlowQuery_ShouldLogWarning()
    {
        var mockLogger = new Mock<ILogger<QueryDiagnostics>>();
        var options = new ForgeOptions
        {
            EnableDiagnostics = true,
            SlowQueryThreshold = TimeSpan.FromMilliseconds(100)
        };
        var diagnostics = new QueryDiagnostics(mockLogger.Object, options);

        diagnostics.Record(new QueryEvent
        {
            SpName = "Save_Orders", Duration = TimeSpan.FromMilliseconds(500)
        });

        mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordFailure_ShouldLogError()
    {
        var mockLogger = new Mock<ILogger<QueryDiagnostics>>();
        var options = new ForgeOptions { EnableDiagnostics = true };
        var diagnostics = new QueryDiagnostics(mockLogger.Object, options);
        var exception = new InvalidOperationException("Timeout expired");

        diagnostics.RecordFailure(new QueryEvent
        {
            SpName = "sel_Reports",
            Duration = TimeSpan.FromMilliseconds(3000),
            IsSuccess = false,
            Exception = exception
        });

        mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Record_ShouldInvokeOnQueryExecutedCallback()
    {
        var mockLogger = new Mock<ILogger<QueryDiagnostics>>();
        QueryEvent? capturedEvent = null;
        var options = new ForgeOptions
        {
            EnableDiagnostics = false,
            OnQueryExecuted = e => capturedEvent = e
        };
        var diagnostics = new QueryDiagnostics(mockLogger.Object, options);

        var queryEvent = new QueryEvent
        {
            SpName = "Get_Students", Duration = TimeSpan.FromMilliseconds(50), RowCount = 5
        };

        diagnostics.Record(queryEvent);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.SpName.Should().Be("Get_Students");
        capturedEvent.RowCount.Should().Be(5);
    }

    [Fact]
    public void RecordFailure_ShouldInvokeOnQueryExecutedCallback()
    {
        var mockLogger = new Mock<ILogger<QueryDiagnostics>>();
        QueryEvent? capturedEvent = null;
        var options = new ForgeOptions
        {
            EnableDiagnostics = false,
            OnQueryExecuted = e => capturedEvent = e
        };
        var diagnostics = new QueryDiagnostics(mockLogger.Object, options);

        diagnostics.RecordFailure(new QueryEvent
        {
            SpName = "sel_Reports",
            Duration = TimeSpan.FromMilliseconds(100),
            IsSuccess = false,
            Exception = new Exception("fail")
        });

        capturedEvent.Should().NotBeNull();
        capturedEvent!.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void QueryEvent_ShouldHaveCorrectDefaults()
    {
        var ev = new QueryEvent { SpName = "test", Duration = TimeSpan.Zero };

        ev.IsSuccess.Should().BeTrue();
        ev.RowCount.Should().BeNull();
        ev.Parameters.Should().BeNull();
        ev.Exception.Should().BeNull();
    }

    [Fact]
    public void ForgeOptions_Defaults_ShouldBeCorrect()
    {
        var options = new ForgeOptions();

        options.ConnectionString.Should().BeEmpty();
        options.Provider.Should().Be(DatabaseProvider.SqlServer);
        options.EnableDiagnostics.Should().BeFalse();
        options.SlowQueryThreshold.Should().Be(TimeSpan.FromSeconds(2));
        options.OnQueryExecuted.Should().BeNull();
        options.ValidateSpOnStartup.Should().BeFalse();
    }

    [Fact]
    public void ForgeOptions_RegisterEntity_ShouldNotThrow()
    {
        var options = new ForgeOptions();

        var act = () => options.RegisterEntity<QueryDiagnosticsTests>();

        act.Should().NotThrow();
    }

    [Fact]
    public void ForgeOptions_MapEntity_NullName_ShouldThrow()
    {
        var options = new ForgeOptions();

        var act = () => options.MapEntity<QueryDiagnosticsTests>(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForgeOptions_MapEntity_EmptyName_ShouldThrow()
    {
        var options = new ForgeOptions();

        var act = () => options.MapEntity<QueryDiagnosticsTests>("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DatabaseProvider_ShouldHaveTwoValues()
    {
        Enum.GetValues<DatabaseProvider>().Should().HaveCount(2);
    }
}
