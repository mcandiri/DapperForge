using DapperForge.Configuration;
using DapperForge.Conventions;
using DapperForge.Diagnostics;
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
            services.AddHostedService<SpValidationHostedService>();
        }

        return services;
    }
}

/// <summary>
/// Background service that validates SP existence on startup.
/// </summary>
internal class SpValidationHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly SpNameResolver _resolver;
    private readonly ForgeOptions _options;
    private readonly ILogger<SpValidationHostedService> _logger;

    public SpValidationHostedService(SpNameResolver resolver, ForgeOptions options,
        ILogger<SpValidationHostedService> logger)
    {
        _resolver = resolver;
        _options = options;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var allExpected = _resolver.GetAllExpectedSpNames();

        if (allExpected.Count == 0)
        {
            _logger.LogInformation("[DapperForge] SP Validation: No entities registered. " +
                "Use options.RegisterEntity<T>() to enable validation.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("[DapperForge] SP Validation: Checking {Count} registered entities...",
            allExpected.Count);

        foreach (var (type, spNames) in allExpected)
        {
            foreach (var spName in spNames)
            {
                _logger.LogInformation("[DapperForge] Expected SP: {SpName} (for entity '{EntityName}')",
                    spName, type.Name);
            }
        }

        return Task.CompletedTask;
    }
}
