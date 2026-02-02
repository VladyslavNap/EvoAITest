using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Browser;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using EvoAITest.Core.Options;
using EvoAITest.Core.Repositories;
using EvoAITest.Core.Services;
using EvoAITest.Core.Services.ErrorRecovery;
using EvoAITest.Core.Services.Recording;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EvoAITest.Core.Extensions;

/// <summary>
/// Provides extension methods for registering EvoAITest core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds EvoAITest Core services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers core services including browser automation, tool execution,
    /// database persistence, and Azure Key Vault integration. The <see cref="EvoAIDbContext"/> 
    /// is registered only when the "EvoAIDatabase" connection string is configured and not empty.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> If the "EvoAIDatabase" connection string is missing or empty,
    /// the <see cref="EvoAIDbContext"/> will NOT be registered. This allows running the application
    /// without database persistence. However, attempting to inject <see cref="EvoAIDbContext"/> when
    /// it's not registered will result in a runtime error. Ensure your code checks for DbContext
    /// availability or provides appropriate configuration.
    /// </para>
    /// <para>
    /// Example configuration in appsettings.json:
    /// <code>
    /// {
    ///   "ConnectionStrings": {
    ///     "EvoAIDatabase": "Server=.;Database=EvoAITest;Trusted_Connection=True;"
    ///   }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEvoAITestCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register configuration options
        services.Configure<EvoAITestCoreOptions>(configuration.GetSection("EvoAITest:Core"));
        services.Configure<ToolExecutorOptions>(configuration.GetSection("EvoAITest:ToolExecutor"));
        services.Configure<ErrorRecoveryOptions>(configuration.GetSection("EvoAITest:Core:ErrorRecovery"));
        services.Configure<RecordingOptions>(configuration.GetSection("Recording"));
        services.Configure<KeyVaultOptions>(configuration.GetSection("KeyVault"));

        // Register Azure Key Vault secret provider (if enabled)
        var keyVaultOptions = configuration.GetSection("KeyVault").Get<KeyVaultOptions>();
        if (keyVaultOptions?.Enabled == true && !string.IsNullOrWhiteSpace(keyVaultOptions.VaultUri))
        {
            services.TryAddSingleton<ISecretProvider, KeyVaultSecretProvider>();
        }
        else
        {
            // Register a no-op provider for development without Key Vault
            services.TryAddSingleton<ISecretProvider, NoOpSecretProvider>();
        }

        // Register Error Recovery services
        services.TryAddScoped<IErrorClassifier, ErrorClassifier>();
        services.TryAddScoped<IErrorRecoveryService, ErrorRecoveryService>();

        // Register browser services
        services.TryAddScoped<IBrowserAgent, PlaywrightBrowserAgent>();

        // Register tool registry
        services.TryAddSingleton<IBrowserToolRegistry, DefaultBrowserToolRegistry>();

        // Register tool executor
        services.TryAddScoped<IToolExecutor, DefaultToolExecutor>();

        // Register visual regression services
        services.TryAddSingleton<VisualComparisonEngine>();
        services.TryAddSingleton<IFileStorageService, LocalFileStorageService>();
        services.TryAddScoped<IVisualComparisonService, VisualComparisonService>();

        // Register DbContext with SQL Server (only if connection string is configured)
        // Note: If the connection string is not configured, DbContext will NOT be registered.
        // This allows running without database persistence but will cause runtime errors
        // if code attempts to inject EvoAIDbContext. Ensure proper configuration or
        // conditional DbContext usage in your application code.
        var connectionString = configuration.GetConnectionString("EvoAIDatabase");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<EvoAIDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                }));

            // Register repositories
            services.TryAddScoped<IAutomationTaskRepository, AutomationTaskRepository>();
            services.TryAddScoped<IRecordingRepository, RecordingRepository>();

            // Register analytics service
            services.TryAddScoped<IAnalyticsService, AnalyticsService>();
        }

        // Register recording services (no DB dependency)
        services.TryAddScoped<IRecordingService, BrowserRecordingService>();
        services.TryAddScoped<InteractionNormalizer>();

        // Add OpenTelemetry instrumentation for EvoAITest.Core
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                // Add meter for EvoAITest.Core custom metrics
                metrics.AddMeter("EvoAITest.Core");
            })
            .WithTracing(tracing =>
            {
                // Add activity source for EvoAITest.Core custom traces
                tracing.AddSource("EvoAITest.Core");
            });

        return services;
    }

    /// <summary>
    /// Adds the browser automation tool executor to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="IToolExecutor"/> as a scoped service, which provides
    /// browser automation tool execution with:
    /// - Exponential backoff with jitter for retry logic
    /// - Transient vs terminal error classification
    /// - Comprehensive structured logging
    /// - In-memory execution history per correlation ID
    /// - Support for sequential execution and fallback strategies
    /// </para>
    /// <para>
    /// The tool executor requires:
    /// - <see cref="IBrowserAgent"/> for browser operations
    /// - <see cref="IBrowserToolRegistry"/> for tool validation
    /// - <see cref="ToolExecutorOptions"/> from configuration
    /// </para>
    /// <para>
    /// This method is called automatically by <see cref="AddEvoAITestCore"/>.
    /// Use this method directly only if you need to customize the registration or
    /// register the tool executor separately from the core services.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// builder.Services
    ///     .AddBrowserAgent&lt;PlaywrightBrowserAgent&gt;()
    ///     .AddToolExecutor();
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddToolExecutor(this IServiceCollection services)
    {
        services.TryAddScoped<IToolExecutor, DefaultToolExecutor>();
        return services;
    }

    /// <summary>
    /// Adds a custom tool executor implementation.
    /// </summary>
    /// <typeparam name="TExecutor">The tool executor implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use this method to register a custom implementation of <see cref="IToolExecutor"/>
    /// instead of the default <see cref="DefaultToolExecutor"/>.
    /// </remarks>
    public static IServiceCollection AddToolExecutor<TExecutor>(this IServiceCollection services)
        where TExecutor : class, IToolExecutor
    {
        services.TryAddScoped<IToolExecutor, TExecutor>();
        return services;
    }

    /// <summary>
    /// Adds BrowserAI core services to the service collection (legacy method).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureBrowser">Optional browser configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This is a legacy method. For new code, use <see cref="AddEvoAITestCore"/> instead.
    /// </remarks>
    public static IServiceCollection AddBrowserAICore(
        this IServiceCollection services,
        Action<BrowserOptions>? configureBrowser = null)
    {
        // Register browser options
        if (configureBrowser != null)
        {
            services.Configure(configureBrowser);
        }
        else
        {
            services.Configure<BrowserOptions>(options => { });
        }

        // Browser driver will be registered by specific implementations (Playwright, Selenium, etc.)
        // services.TryAddSingleton<IBrowserDriver, PlaywrightBrowserDriver>();

        // Page analyzer
        // services.TryAddScoped<IPageAnalyzer, DefaultPageAnalyzer>();

        return services;
    }

    /// <summary>
    /// Adds a specific browser driver implementation.
    /// </summary>
    /// <typeparam name="TDriver">The browser driver implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBrowserDriver<TDriver>(this IServiceCollection services)
        where TDriver : class, IBrowserDriver
    {
        services.TryAddSingleton<IBrowserDriver, TDriver>();
        return services;
    }

    /// <summary>
    /// Adds a browser agent implementation.
    /// </summary>
    /// <typeparam name="TAgent">The browser agent implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Browser agents are registered as scoped services to support multiple concurrent browser sessions.
    /// </remarks>
    public static IServiceCollection AddBrowserAgent<TAgent>(this IServiceCollection services)
        where TAgent : class, IBrowserAgent
    {
        services.TryAddScoped<IBrowserAgent, TAgent>();
        return services;
    }

    /// <summary>
    /// Adds a page analyzer implementation.
    /// </summary>
    /// <typeparam name="TAnalyzer">The page analyzer implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPageAnalyzer<TAnalyzer>(this IServiceCollection services)
        where TAnalyzer : class, IPageAnalyzer
    {
        services.TryAddScoped<IPageAnalyzer, TAnalyzer>();
        return services;
    }
}
