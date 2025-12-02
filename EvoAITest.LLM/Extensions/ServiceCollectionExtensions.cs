using EvoAITest.Core.Options;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Factory;
using EvoAITest.LLM.Prompts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EvoAITest.LLM.Extensions;

/// <summary>
/// Extension methods for registering LLM services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds LLM services (providers, factory, prompt builder) to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLLMServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind core options
        services.Configure<EvoAITestCoreOptions>(
            configuration.GetSection("EvoAITest:Core"));

        // Register LLM provider factory
        services.TryAddSingleton<LLMProviderFactory>();

        // Register LLM provider (scoped for per-request usage)
        services.TryAddScoped<ILLMProvider>(sp =>
        {
            var factory = sp.GetRequiredService<LLMProviderFactory>();
            return factory.CreateProvider();
        });

        // Register prompt builder (scoped for per-request customization)
        services.TryAddScoped<IPromptBuilder, DefaultPromptBuilder>();

        return services;
    }

    /// <summary>
    /// Adds LLM services with a custom prompt builder implementation.
    /// </summary>
    /// <typeparam name="TPromptBuilder">The custom prompt builder type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLLMServicesWithCustomPromptBuilder<TPromptBuilder>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TPromptBuilder : class, IPromptBuilder
    {
        // Add base services
        AddLLMServices(services, configuration);

        // Replace default prompt builder with custom implementation
        services.Replace(ServiceDescriptor.Scoped<IPromptBuilder, TPromptBuilder>());

        return services;
    }
}
