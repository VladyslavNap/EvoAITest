using EvoAITest.LLM.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EvoAITest.LLM.Extensions;

/// <summary>
/// Provides extension methods for registering LLM services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds LLM services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLLMServices(this IServiceCollection services)
    {
        // LLM provider implementations will be registered separately
        // services.TryAddSingleton<ILLMProvider, OpenAIProvider>();
        
        // Prompt builder
        // services.TryAddScoped<IPromptBuilder, DefaultPromptBuilder>();

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
