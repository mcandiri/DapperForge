using Testcontainers.MsSql;
using Xunit;

namespace DapperForge.IntegrationTests;

/// <summary>
/// Shared SQL Server container fixture for all integration tests.
/// Starts once, shared across all test classes in the collection.
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}
