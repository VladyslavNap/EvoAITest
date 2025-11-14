using EvoAITest.Core.Abstractions;
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
    /// This method registers EvoAITest-specific services and adds OpenTelemetry instrumentation
    /// for the EvoAITest.Core assembly. Logging, health checks, and base OpenTelemetry configuration
    /// are handled by EvoAITest.ServiceDefaults and should not be configured here.
    /// </para>
    /// <para>
    /// Configuration should be provided in appsettings.json under "EvoAITest:Core".
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

        // 2. Register core services
        // Note: IBrowserAgent implementation (PlaywrightBrowserAgent) should be registered
        // by the consuming application based on their browser automation provider choice
        // services.AddScoped<IBrowserAgent, PlaywrightBrowserAgent>();

        // Register browser tool registry
        services.TryAddSingleton<IBrowserToolRegistry, DefaultBrowserToolRegistry>();

        // 3. Add OpenTelemetry instrumentation for EvoAITest.Core
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
