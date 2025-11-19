using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Browser;
using EvoAITest.Core.Options;
using EvoAITest.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace EvoAITest.Core.Extensions;

/// <summary>
/// Provides extension methods for registering EvoAITest core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds EvoAITest.Core services to the service collection with Aspire integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers EvoAITest-specific services including:
    /// - <see cref="IBrowserAgent"/> (Playwright-based browser automation)
    /// - <see cref="IBrowserToolRegistry"/> (13 browser automation tools)
    /// - <see cref="IToolExecutor"/> (tool execution with retry logic and error handling)
    /// </para>
    /// <para>
    /// OpenTelemetry instrumentation is added for the EvoAITest.Core assembly.
    /// Logging, health checks, and base OpenTelemetry configuration are handled by
    /// EvoAITest.ServiceDefaults and should not be configured here.
    /// </para>
    /// <para>
    /// Configuration should be provided in appsettings.json:
    /// <code>
    /// {
    ///   "EvoAITest": {
    ///     "Core": {
    ///       "LLMProvider": "AzureOpenAI",
    ///       "BrowserTimeoutMs": 30000,
    ///       "HeadlessMode": true
    ///     },
    ///     "ToolExecutor": {
    ///       "MaxRetries": 3,
    ///       "InitialRetryDelayMs": 500,
    ///       "MaxRetryDelayMs": 10000,
    ///       "UseExponentialBackoff": true,
    ///       "TimeoutPerToolMs": 30000
    ///     }
    ///   }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// builder.AddServiceDefaults(); // From EvoAITest.ServiceDefaults
    /// builder.Services.AddEvoAITestCore(builder.Configuration);
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEvoAITestCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 1. Configure EvoAITest.Core options from configuration
        services.Configure<EvoAITestCoreOptions>(
            configuration.GetSection("EvoAITest:Core"));

        // 2. Configure ToolExecutor options from configuration
        services.Configure<ToolExecutorOptions>(
            configuration.GetSection("EvoAITest:ToolExecutor"));

        // 3. Register core services
        // Register Playwright browser agent implementation
        services.TryAddScoped<IBrowserAgent, PlaywrightBrowserAgent>();

        // Register browser tool registry
        services.TryAddSingleton<IBrowserToolRegistry, DefaultBrowserToolRegistry>();

        // Register tool executor with retry logic and error handling
        services.TryAddScoped<IToolExecutor, DefaultToolExecutor>();

        // 4. Add OpenTelemetry instrumentation for EvoAITest.Core
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
