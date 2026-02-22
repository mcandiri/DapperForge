using DapperForge.Configuration;

namespace DapperForge.Conventions;

/// <summary>
/// Utility class that resolves stored procedure names for entity types
/// using the configured naming convention.
/// </summary>
public class SpNameResolver
{
    private readonly ISpNamingConvention _convention;
    private readonly ForgeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpNameResolver"/> class.
    /// </summary>
    /// <param name="convention">The naming convention to use.</param>
    /// <param name="options">The DapperForge configuration options.</param>
    public SpNameResolver(ISpNamingConvention convention, ForgeOptions options)
    {
        _convention = convention ?? throw new ArgumentNullException(nameof(convention));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Resolves the SELECT stored procedure name for the given entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The fully qualified stored procedure name.</returns>
    public string ResolveSelect<T>() => _convention.ResolveSelect<T>();

    /// <summary>
    /// Resolves the INSERT/UPDATE stored procedure name for the given entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The fully qualified stored procedure name.</returns>
    public string ResolveUpsert<T>() => _convention.ResolveUpsert<T>();

    /// <summary>
    /// Resolves the DELETE stored procedure name for the given entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The fully qualified stored procedure name.</returns>
    public string ResolveDelete<T>() => _convention.ResolveDelete<T>();

    /// <summary>
    /// Returns all expected SP names for the given entity type (select, upsert, delete).
    /// Useful for SP validation on startup.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <returns>A list of expected SP names.</returns>
    public IReadOnlyList<string> GetExpectedSpNames(Type entityType)
    {
        return new[]
        {
            _convention.ResolveSelect(entityType),
            _convention.ResolveUpsert(entityType),
            _convention.ResolveDelete(entityType)
        };
    }

    /// <summary>
    /// Returns all expected SP names for all registered entity types.
    /// </summary>
    /// <returns>A dictionary mapping entity types to their expected SP names.</returns>
    public IReadOnlyDictionary<Type, IReadOnlyList<string>> GetAllExpectedSpNames()
    {
        var result = new Dictionary<Type, IReadOnlyList<string>>();
        foreach (var type in _options.RegisteredEntities)
        {
            result[type] = GetExpectedSpNames(type);
        }
        return result;
    }
}
