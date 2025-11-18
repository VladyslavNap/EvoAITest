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
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
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
            return factory.CreateProvider();
        });

        // Prompt builder (when implemented)
        // services.TryAddScoped<IPromptBuilder, DefaultPromptBuilder>();

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
