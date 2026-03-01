<div align="center">

# DapperForge

**Convention-based stored procedure toolkit for .NET**

Your team's SP naming convention shouldn't require boilerplate.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE)
[![Build & Test](https://github.com/mcandiri/DapperForge/actions/workflows/build.yml/badge.svg)](https://github.com/mcandiri/DapperForge/actions/workflows/build.yml)

</div>

---

## The Problem

Every team that uses stored procedures writes the same code over and over:

```csharp
// This. Every. Single. Time.
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
var parameters = new DynamicParameters();
parameters.Add("@IsActive", true);
var students = await connection.QueryAsync<Student>(
    "Get_Students",
    parameters,
    commandType: CommandType.StoredProcedure
);
return students;
```

## The Solution

```csharp
var students = await forge.GetAsync<Student>(new { IsActive = true });
```

Same result. Convention handles the rest.

---

## Quick Start

**Clone and reference the project:**

```bash
git clone https://github.com/mcandiri/DapperForge.git
dotnet add reference path/to/DapperForge/src/DapperForge/DapperForge.csproj
```

```csharp
// Program.cs
builder.Services.AddDapperForge(options =>
{
    options.ConnectionString = "Server=...;Database=...;";
    options.Provider = DatabaseProvider.SqlServer;
});
```

```csharp
// StudentService.cs
public class StudentService(IForgeConnection forge)
{
    public Task<IEnumerable<Student>> GetAll()
        => forge.GetAsync<Student>();

    public Task<Student?> GetById(int id)
        => forge.GetSingleAsync<Student>(new { Id = id });

    public Task<int> Save(Student student)
        => forge.SaveAsync(student);

    public Task<int> Remove(int id)
        => forge.RemoveAsync<Student>(new { Id = id });
}
```

That's it. `GetAsync` resolves to `Get_Students`, `SaveAsync` to `Save_Students`, `RemoveAsync` to `Remove_Students`.

---

## Convention Engine

The core of DapperForge. Define your naming pattern once:

```csharp
builder.Services.AddDapperForge(options =>
{
    options.ConnectionString = "...";

    options.SetConvention(c =>
    {
        c.SelectPrefix = "sel";       // sel_Students
        c.UpsertPrefix = "up";        // up_Students
        c.DeletePrefix = "del";       // del_Students
        c.Schema = "dbo";             // dbo.sel_Students
        c.Separator = "_";
    });

    // Override for specific entities
    options.MapEntity<Student>("Ogrenciler");  // dbo.sel_Ogrenciler

    // Or change the global resolver
    options.EntityNameResolver = type => type.Name.ToLowerInvariant();
});
```

| Call | Default Convention | Custom (`sel/up/del` + `dbo`) |
|---|---|---|
| `GetAsync<Student>()` | `Get_Students` | `dbo.sel_Students` |
| `SaveAsync(student)` | `Save_Students` | `dbo.up_Students` |
| `RemoveAsync<Student>()` | `Remove_Students` | `dbo.del_Students` |

---

## Direct SP Calls

When you need to call an SP that doesn't follow your convention:

```csharp
// Query
var honors = await forge.ExecuteSpAsync<Student>("rpt_HonorRoll", new { Year = 2024 });

// Scalar
var count = await forge.ExecuteSpScalarAsync<int>("sel_StudentCount");

// Non-query
await forge.ExecuteSpNonQueryAsync("job_CleanupExpired", new { DaysOld = 30 });
```

### Multiple Result Sets

```csharp
// Two result sets
var (students, teachers) = await forge.ExecuteSpMultiAsync<Student, Teacher>(
    "sel_ClassroomDetails", new { ClassId = 5 });

// Three result sets
var (orders, items, summary) = await forge.ExecuteSpMultiAsync<Order, OrderItem, Summary>(
    "sel_OrderReport", new { Year = 2024 });
```

### Output Parameters

```csharp
var result = await forge.ExecuteSpWithOutputAsync(
    "up_Students",
    new { Name = "John", Email = "john@test.com" },
    new Dictionary<string, DbType> { ["NewId"] = DbType.Int32 });

var newId = result.OutputValues["NewId"];  // 42
```

---

## Transactions

```csharp
// Automatic — commits on success, rolls back on exception
await forge.InTransactionAsync(async tx =>
{
    await tx.SaveAsync(order);
    await tx.SaveAsync(orderLine);
    await tx.RemoveAsync<CartItem>(new { CartId = cartId });
});
```

```csharp
// Manual — full control
using var tx = forge.BeginTransaction();
try
{
    await tx.SaveAsync(order);
    await tx.SaveAsync(orderLine);
    tx.Commit();
}
catch
{
    tx.Rollback();
    throw;
}
```

---

## Diagnostics

```csharp
builder.Services.AddDapperForge(options =>
{
    options.EnableDiagnostics = true;
    options.SlowQueryThreshold = TimeSpan.FromSeconds(2);

    // Hook into every execution
    options.OnQueryExecuted = e =>
    {
        // e.SpName, e.Duration, e.RowCount, e.Parameters, e.IsSuccess, e.Exception
    };
});
```

```
[DapperForge] Get_Students executed in 12ms -> 150 rows
[DapperForge] SLOW: Save_BulkImport executed in 4200ms
[DapperForge] FAILED: sel_Reports — SqlException: Timeout expired (3012ms)
```

---

## SP Validation

Catch missing stored procedures at startup, not in production. DapperForge queries the database catalog (`sys.objects` for SQL Server, `information_schema.routines` for PostgreSQL) to verify existence:

```csharp
builder.Services.AddDapperForge(options =>
{
    options.ValidateSpOnStartup = true;

    // Fail fast in CI/staging — throw if any SP is missing
    options.FailOnMissingSp = true;

    options.RegisterEntity<Student>();
    options.RegisterEntity<Teacher>();
    options.RegisterEntity<Order>();

    // Or scan an assembly
    options.RegisterEntitiesFromAssembly(typeof(Student).Assembly);
});
```

```
[DapperForge] SP Validation: Checking 3 registered entities...
[DapperForge] SP Validated: Get_Students (for entity 'Student')
[DapperForge] SP Validated: Save_Students (for entity 'Student')
[DapperForge] SP Missing: Remove_Students (for entity 'Student')
```

---

## Multi-Database

DapperForge uses a provider-specific `ISpCommandBuilder` to generate the correct syntax for each database:

```csharp
options.Provider = DatabaseProvider.SqlServer;   // CommandType.StoredProcedure → EXEC sp_name @param
options.Provider = DatabaseProvider.PostgreSQL;   // CommandType.Text → SELECT * FROM sp_name(@param)
```

SQL Server uses ADO.NET's native `CommandType.StoredProcedure`. PostgreSQL generates `SELECT * FROM function_name(@p1, @p2)` text commands, since Npgsql does not support `CommandType.StoredProcedure` reliably.

---

## Configuration

| Option | Type | Default | Description |
|---|---|---|---|
| `ConnectionString` | `string` | `""` | Database connection string |
| `Provider` | `DatabaseProvider` | `SqlServer` | SQL Server or PostgreSQL |
| `SetConvention()` | builder | `Get_/Save_/Remove_` | SP naming convention |
| `MapEntity<T>(name)` | per-entity | `TypeName + "s"` | Override SP entity name |
| `EntityNameResolver` | `Func<Type, string>` | `t => t.Name + "s"` | Global entity name resolver |
| `EnableDiagnostics` | `bool` | `false` | Enable query logging |
| `SlowQueryThreshold` | `TimeSpan` | `2s` | Slow query warning threshold |
| `OnQueryExecuted` | `Action<QueryEvent>` | `null` | Post-execution callback |
| `ValidateSpOnStartup` | `bool` | `false` | Validate SPs against DB on startup |
| `FailOnMissingSp` | `bool` | `false` | Throw on missing SPs (requires ValidateSpOnStartup) |

---

## Entity Name Resolution

By default, DapperForge resolves entity names using naive pluralization (`TypeName + "s"`). This works for most English entity names:

| Type | Resolved Name | SP Name |
|---|---|---|
| `Student` | `Students` | `Get_Students` |
| `Order` | `Orders` | `Save_Orders` |

However, irregular plurals will not be correct (`Person` → `Persons`, `Status` → `Statuss`). Use `MapEntity<T>()` or `EntityNameResolver` for these cases:

```csharp
options.MapEntity<Person>("People");
options.MapEntity<Status>("Statuses");

// Or use a library like Humanizer for global resolution
options.EntityNameResolver = type => type.Name.Pluralize();
```

---

## Thread Safety

`IForgeConnection` is **not thread-safe**. Each instance wraps a single ADO.NET connection and must not be shared across concurrent async operations.

```csharp
// WRONG — concurrent access to the same connection
await Task.WhenAll(
    forge.GetAsync<Student>(),
    forge.GetAsync<Teacher>()   // may corrupt connection state
);

// CORRECT — sequential access
var students = await forge.GetAsync<Student>();
var teachers = await forge.GetAsync<Teacher>();
```

DapperForge registers `IForgeConnection` as **Scoped** by default, which means each HTTP request gets its own connection instance. This is the recommended pattern.

---

## What DapperForge Is NOT

| Need | Use Instead |
|---|---|
| Auto CRUD / table access | [Dapper.Contrib](https://github.com/DapperLib/Dapper.Contrib) |
| LINQ queries | [EF Core](https://learn.microsoft.com/en-us/ef/core/) |
| Migrations | [DbUp](https://dbup.readthedocs.io/) or EF Migrations |
| Caching | Your preferred caching layer |
| Connection pooling | ADO.NET handles this |

**This is intentional.** DapperForge does one thing well and complements your existing stack.

---

## Trade-offs & When NOT to Use

DapperForge trades explicitness for convention. This is the right trade-off when:
- ✅ You have 50+ stored procedures following a naming convention
- ✅ Your SP parameters map directly to C# model properties
- ✅ You want to eliminate repetitive Dapper boilerplate

This is NOT the right choice when:
- ❌ Your SPs have inconsistent naming — convention-based discovery won't work
- ❌ You need full control over every parameter mapping
- ❌ You prefer explicit over implicit (and that's a valid preference)

---

## FAQ

**What happens when naming conventions change?**
DapperForge uses configurable naming strategies. Override `INamingConvention` to match your scheme. The default (`{Entity}_{Operation}`) covers the most common pattern.

**How does parameter mapping handle edge cases?**
Nullable types, enums, and nested objects are supported. For complex mappings, use `[MapTo("sp_param_name")]` attribute to override convention.

**Transaction support?**
Yes — `IUnitOfWork` wraps `IDbTransaction`. All operations within a unit of work share the same transaction and connection.

---

## Performance

DapperForge adds near-zero overhead on top of raw Dapper. Benchmarked with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) on SQLite in-memory to isolate framework cost from network I/O:

| Operation | Raw Dapper | DapperForge | Overhead | Memory |
|---|---|---|---|---|
| **Query** (80 rows) | 58.0 us | 60.1 us | +3.6% | 0 extra bytes |
| **Single row** | 4.54 us | 4.53 us | ~0% | 0 extra bytes |
| **Scalar** | 1.94 us | 1.95 us | ~0% | 0 extra bytes |

Zero additional memory allocations across all operations. The convention engine resolves SP names at negligible cost.

```bash
# Run benchmarks yourself
dotnet run -c Release --project benchmarks/DapperForge.Benchmarks
```

---

## Born From Production

> Extracted from a codebase with 200+ stored procedures where every data access method was 15 lines of repetitive Dapper setup. DapperForge reduced each to a single method call while keeping full control over edge cases through attribute overrides.

---

## Project Structure

```
src/DapperForge/
  Configuration/    ForgeOptions, DatabaseProvider, ConventionBuilder
  Conventions/      SP naming engine + entity name resolver
  Execution/        SpExecutor, ISpCommandBuilder, provider-specific builders
  Transaction/      Auto + manual transaction support
  Diagnostics/      Structured logging, QueryEvent, slow query detection
  Validation/       ISpValidator, SqlServer/Postgres catalog queries
  Extensions/       DI registration + SP validation hosted service
```

---

## Roadmap

- [ ] **Bulk execution** — `SaveManyAsync<T>(IEnumerable<T>)` with single transaction
- [ ] **Retry policies** — configurable retry with Polly integration
- [ ] **Connection-per-call mode** — option to create fresh connections instead of scoped
- [ ] **SP result caching** — optional in-memory cache with TTL per SP
- [x] **GitHub Actions CI** — automated build, test, and NuGet publish pipeline
- [ ] **Source generator** — compile-time SP name validation

Have an idea? [Open an issue](../../issues).

---

## Contributing

1. Fork the repo
2. Create your branch (`git checkout -b feature/your-feature`)
3. Write tests for your changes
4. All tests must pass (`dotnet test`)
5. Open a Pull Request

---

## License

MIT -- see [LICENSE](LICENSE) for details.
