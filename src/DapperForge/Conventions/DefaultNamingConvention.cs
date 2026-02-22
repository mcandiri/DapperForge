using DapperForge.Configuration;

namespace DapperForge.Conventions;

/// <summary>
/// Default naming convention that resolves SP names using configurable prefix, separator, and schema.
/// Default format: {Schema}.{Prefix}{Separator}{EntityName} (e.g., dbo.Get_Students).
/// </summary>
public class DefaultNamingConvention : ISpNamingConvention
{
    private readonly string _selectPrefix;
    private readonly string _upsertPrefix;
    private readonly string _deletePrefix;
    private readonly string _schema;
    private readonly string _separator;
    private readonly ForgeOptions? _options;

    /// <summary>
    /// Initializes a new instance with default values: Get_, Save_, Remove_.
    /// </summary>
    public DefaultNamingConvention()
        : this("Get", "Save", "Remove", string.Empty, "_", null)
    {
    }

    /// <summary>
    /// Initializes a new instance with custom configuration.
    /// </summary>
    /// <param name="selectPrefix">The prefix for SELECT SPs.</param>
    /// <param name="upsertPrefix">The prefix for INSERT/UPDATE SPs.</param>
    /// <param name="deletePrefix">The prefix for DELETE SPs.</param>
    /// <param name="schema">The database schema (empty for no schema).</param>
    /// <param name="separator">The separator between prefix and entity name.</param>
    /// <param name="options">The ForgeOptions for entity name resolution.</param>
    public DefaultNamingConvention(
        string selectPrefix,
        string upsertPrefix,
        string deletePrefix,
        string schema,
        string separator,
        ForgeOptions? options)
    {
        _selectPrefix = selectPrefix ?? throw new ArgumentNullException(nameof(selectPrefix));
        _upsertPrefix = upsertPrefix ?? throw new ArgumentNullException(nameof(upsertPrefix));
        _deletePrefix = deletePrefix ?? throw new ArgumentNullException(nameof(deletePrefix));
        _schema = schema ?? string.Empty;
        _separator = separator ?? throw new ArgumentNullException(nameof(separator));
        _options = options;
    }

    /// <inheritdoc />
    public string ResolveSelect<T>() => ResolveSelect(typeof(T));

    /// <inheritdoc />
    public string ResolveUpsert<T>() => ResolveUpsert(typeof(T));

    /// <inheritdoc />
    public string ResolveDelete<T>() => ResolveDelete(typeof(T));

    /// <inheritdoc />
    public string ResolveSelect(Type entityType) => BuildSpName(_selectPrefix, entityType);

    /// <inheritdoc />
    public string ResolveUpsert(Type entityType) => BuildSpName(_upsertPrefix, entityType);

    /// <inheritdoc />
    public string ResolveDelete(Type entityType) => BuildSpName(_deletePrefix, entityType);

    private string BuildSpName(string prefix, Type entityType)
    {
        var entityName = _options?.ResolveEntityName(entityType) ?? $"{entityType.Name}s";
        var spName = $"{prefix}{_separator}{entityName}";

        return string.IsNullOrEmpty(_schema)
            ? spName
            : $"{_schema}.{spName}";
    }
}
