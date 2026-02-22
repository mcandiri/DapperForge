using DapperForge.Configuration;
using DapperForge.Conventions;
using DapperForge.Diagnostics;
using DapperForge.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DapperForge.Extensions;

/// <summary>
/// Extension methods for registering DapperForge services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DapperForge services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">An action to configure <see cref="ForgeOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddDapperForge(options =>
    /// {
    ///     options.ConnectionString = "Server=...";
    ///     options.Provider = DatabaseProvider.SqlServer;
    ///     options.EnableDiagnostics = true;
    ///     options.SetConvention(c =>
    ///     {
    ///         c.SelectPrefix = "sel";
    ///         c.Schema = "dbo";
    ///     });
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddDapperForge(this IServiceCollection services, Action<ForgeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ForgeOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton(options.Convention);
        services.AddSingleton<SpNameResolver>();
        services.TryAddSingleton<IQueryDiagnostics, QueryDiagnostics>();
        services.AddScoped<IForgeConnection, ForgeConnection>();

        if (options.ValidateSpOnStartup)
        {
            services.AddSingleton<ISpValidator>(_ => CreateValidator(options));
            services.AddHostedService<SpValidationHostedService>();
        }

        return services;
    }

    private static ISpValidator CreateValidator(ForgeOptions options)
    {
        return options.Provider switch
        {
            DatabaseProvider.SqlServer => new SqlServerSpValidator(options.ConnectionString),
            DatabaseProvider.PostgreSQL => new PostgresSpValidator(options.ConnectionString),
            _ => throw new NotSupportedException($"SP validation is not supported for provider '{options.Provider}'.")
        };
    }
}

/// <summary>
/// Background service that validates SP existence against the database on startup.
/// Queries the database catalog (sys.objects for SQL Server, information_schema.routines for PostgreSQL)
/// to verify that all expected stored procedures actually exist.
/// </summary>
internal class SpValidationHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly SpNameResolver _resolver;
    private readonly ForgeOptions _options;
    private readonly ISpValidator _validator;
    private readonly ILogger<SpValidationHostedService> _logger;

    public SpValidationHostedService(SpNameResolver resolver, ForgeOptions options,
        ISpValidator validator, ILogger<SpValidationHostedService> logger)
    {
        _resolver = resolver;
        _options = options;
        _validator = validator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var allExpected = _resolver.GetAllExpectedSpNames();

        if (allExpected.Count == 0)
        {
            _logger.LogInformation("[DapperForge] SP Validation: No entities registered. " +
                "Use options.RegisterEntity<T>() to enable validation.");
            return;
        }

        _logger.LogInformation("[DapperForge] SP Validation: Checking {Count} registered entities...",
            allExpected.Count);

        var missing = new List<(string SpName, string EntityName)>();

        foreach (var (type, spNames) in allExpected)
        {
            foreach (var spName in spNames)
            {
                try
                {
                    var exists = await _validator.ExistsAsync(spName, stoppingToken);

                    if (exists)
                    {
                        _logger.LogInformation("[DapperForge] SP Validated: {SpName} (for entity '{EntityName}')",
                            spName, type.Name);
                    }
                    else
                    {
                        _logger.LogWarning("[DapperForge] SP Missing: {SpName} (for entity '{EntityName}')",
                            spName, type.Name);
                        missing.Add((spName, type.Name));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DapperForge] SP Validation failed for {SpName}: {Error}",
                        spName, ex.Message);
                    missing.Add((spName, type.Name));
                }
            }
        }

        if (missing.Count > 0)
        {
            var summary = string.Join(", ", missing.Select(m => m.SpName));
            _logger.LogWarning("[DapperForge] SP Validation complete: {MissingCount} missing SPs: {Summary}",
                missing.Count, summary);

            if (_options.FailOnMissingSp)
            {
                throw new InvalidOperationException(
                    $"DapperForge SP validation failed. {missing.Count} stored procedure(s) not found: {summary}. " +
                    "Ensure all expected SPs exist in the database, or set FailOnMissingSp = false.");
            }
        }
        else
        {
            _logger.LogInformation("[DapperForge] SP Validation complete: All stored procedures verified.");
        }
    }
}
