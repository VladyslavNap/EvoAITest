using EvoAITest.Core.Options;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Factory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EvoAITest.LLM.Extensions;

/// <summary>
/// Provides extension methods for registering LLM services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds LLM services to the service collection with configuration-based provider selection.
    /// Supports multi-model routing, automatic fallback, and Azure Key Vault integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the LLM provider with advanced features:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Multi-model routing - Route to GPT-4 for planning, Qwen for code generation</description></item>
    /// <item><description>Automatic fallback - Fall back to Ollama when Azure OpenAI is rate-limited</description></item>
    /// <item><description>Circuit breakers - Prevent cascading failures with health management</description></item>
    /// <item><description>Azure Key Vault - Secure API key storage with managed identity</description></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddLLMServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register EvoAITestCoreOptions
        services.Configure<EvoAITestCoreOptions>(
            configuration.GetSection("EvoAITest:Core"));

        // Register LLM provider factory
        services.TryAddSingleton<LLMProviderFactory>();

        // Register ILLMProvider using the factory
        services.TryAddScoped<ILLMProvider>(sp =>
        {
            var factory = sp.GetRequiredService<LLMProviderFactory>();
            
            // Use Task.Run to avoid deadlocks in contexts with synchronization contexts
            return Task.Run(() => factory.CreateProviderAsync()).GetAwaiter().GetResult();
        });

        return services;
    }

    /// <summary>
    /// Adds LLM services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete("Use AddLLMServices(IServiceCollection, IConfiguration) instead.")]
    public static IServiceCollection AddLLMServices(this IServiceCollection services)
    {
        // Legacy method for backward compatibility
        // LLM provider implementations will be registered separately
        return services;
    }

    /// <summary>
    /// Adds a specific LLM provider implementation.
    /// </summary>
    /// <typeparam name="TProvider">The LLM provider implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLLMProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ILLMProvider
    {
        services.TryAddSingleton<ILLMProvider, TProvider>();
        return services;
    }

    /// <summary>
    /// Adds a prompt builder implementation.
    /// </summary>
    /// <typeparam name="TBuilder">The prompt builder implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPromptBuilder<TBuilder>(this IServiceCollection services)
        where TBuilder : class, IPromptBuilder
    {
        services.TryAddScoped<IPromptBuilder, TBuilder>();
        return services;
    }
}
