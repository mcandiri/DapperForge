using DapperForge.Conventions;
using DapperForge.Diagnostics;

namespace DapperForge.Configuration;

/// <summary>
/// Configuration options for DapperForge.
/// </summary>
public class ForgeOptions
{
    private readonly Dictionary<Type, string> _entityNameMap = new();
    private readonly HashSet<Type> _registeredEntities = new();

    /// <summary>
    /// Gets or sets the database connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database provider. Defaults to <see cref="DatabaseProvider.SqlServer"/>.
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    /// <summary>
    /// Gets or sets the stored procedure naming convention.
    /// Defaults to <see cref="DefaultNamingConvention"/>.
    /// </summary>
    public ISpNamingConvention Convention { get; set; } = new DefaultNamingConvention();

    /// <summary>
    /// Gets or sets a value indicating whether query diagnostics logging is enabled.
    /// </summary>
    public bool EnableDiagnostics { get; set; }

    /// <summary>
    /// Gets or sets the threshold for slow query warnings. Defaults to 2 seconds.
    /// </summary>
    public TimeSpan SlowQueryThreshold { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets an optional callback invoked after every SP execution.
    /// </summary>
    public Action<QueryEvent>? OnQueryExecuted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether SP existence should be validated on startup.
    /// When enabled, DapperForge queries the database catalog to verify that all expected
    /// stored procedures exist. Recommended for development/staging environments only.
    /// </summary>
    public bool ValidateSpOnStartup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application should throw an exception
    /// if any expected stored procedures are missing during startup validation.
    /// Only takes effect when <see cref="ValidateSpOnStartup"/> is also <c>true</c>.
    /// Recommended for CI/staging environments to fail fast on deployment issues.
    /// </summary>
    public bool FailOnMissingSp { get; set; }

    /// <summary>
    /// Gets or sets the custom entity name resolver function.
    /// Default behavior appends "s" to the type name (e.g., Student → Students).
    /// </summary>
    public Func<Type, string> EntityNameResolver { get; set; } = type => $"{type.Name}s";

    /// <summary>
    /// Configures the stored procedure naming convention using a builder.
    /// </summary>
    /// <param name="configure">An action to configure the convention.</param>
    public void SetConvention(Action<ConventionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ConventionBuilder();
        configure(builder);
        Convention = builder.Build(this);
    }

    /// <summary>
    /// Maps a specific entity type to a custom SP entity name.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entityName">The SP entity name (e.g., "Students").</param>
    public void MapEntity<T>(string entityName) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        _entityNameMap[typeof(T)] = entityName;
        _registeredEntities.Add(typeof(T));
    }

    /// <summary>
    /// Registers an entity type for SP validation on startup.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public void RegisterEntity<T>() where T : class
    {
        _registeredEntities.Add(typeof(T));
    }

    /// <summary>
    /// Registers all entity types from the specified assembly that match the given predicate.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="predicate">Optional filter for types. Defaults to all public classes.</param>
    public void RegisterEntitiesFromAssembly(System.Reflection.Assembly assembly, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var filter = predicate ?? (t => t.IsClass && t.IsPublic && !t.IsAbstract);

        foreach (var type in assembly.GetTypes().Where(filter))
        {
            _registeredEntities.Add(type);
        }
    }

    /// <summary>
    /// Gets the registered entity types for SP validation.
    /// </summary>
    internal IReadOnlyCollection<Type> RegisteredEntities => _registeredEntities;

    /// <summary>
    /// Resolves the entity name for the given type, checking custom mappings first.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>The resolved entity name.</returns>
    internal string ResolveEntityName(Type entityType)
    {
        if (_entityNameMap.TryGetValue(entityType, out var mapped))
            return mapped;

        return EntityNameResolver(entityType);
    }
}

/// <summary>
/// Builder for configuring stored procedure naming conventions.
/// </summary>
public class ConventionBuilder
{
    /// <summary>
    /// Gets or sets the prefix for SELECT stored procedures. Default: "Get".
    /// </summary>
    public string SelectPrefix { get; set; } = "Get";

    /// <summary>
    /// Gets or sets the prefix for INSERT/UPDATE stored procedures. Default: "Save".
    /// </summary>
    public string UpsertPrefix { get; set; } = "Save";

    /// <summary>
    /// Gets or sets the prefix for DELETE stored procedures. Default: "Remove".
    /// </summary>
    public string DeletePrefix { get; set; } = "Remove";

    /// <summary>
    /// Gets or sets the database schema. Default: empty (no schema prefix).
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the separator between prefix and entity name. Default: "_".
    /// </summary>
    public string Separator { get; set; } = "_";

    /// <summary>
    /// Builds the naming convention from the current configuration.
    /// </summary>
    internal ISpNamingConvention Build(ForgeOptions options)
    {
        return new DefaultNamingConvention(
            selectPrefix: SelectPrefix,
            upsertPrefix: UpsertPrefix,
            deletePrefix: DeletePrefix,
            schema: Schema,
            separator: Separator,
            options: options);
    }
}
