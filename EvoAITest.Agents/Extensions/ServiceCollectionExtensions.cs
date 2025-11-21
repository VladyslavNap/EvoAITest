using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EvoAITest.Agents.Extensions;

/// <summary>
/// Provides extension methods for registering agent services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds agent orchestration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentServices(this IServiceCollection services)
    {
        // Register default implementations
        services.TryAddScoped<IPlanner, PlannerAgent>();
        
        // Other agent implementations will be registered as they are implemented
        // services.TryAddScoped<IExecutor, DefaultExecutor>();
        // services.TryAddScoped<IHealer, DefaultHealer>();
        // services.TryAddScoped<IAgent, BrowserAutomationAgent>();

        return services;
    }

    /// <summary>
    /// Adds a specific agent implementation.
    /// </summary>
    /// <typeparam name="TAgent">The agent implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgent<TAgent>(this IServiceCollection services)
        where TAgent : class, IAgent
    {
        services.TryAddScoped<IAgent, TAgent>();
        return services;
    }

    /// <summary>
    /// Adds a planner implementation.
    /// </summary>
    /// <typeparam name="TPlanner">The planner implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlanner<TPlanner>(this IServiceCollection services)
        where TPlanner : class, IPlanner
    {
        services.TryAddScoped<IPlanner, TPlanner>();
        return services;
    }

    /// <summary>
    /// Adds an executor implementation.
    /// </summary>
    /// <typeparam name="TExecutor">The executor implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExecutor<TExecutor>(this IServiceCollection services)
        where TExecutor : class, IExecutor
    {
        services.TryAddScoped<IExecutor, TExecutor>();
        return services;
    }

    /// <summary>
    /// Adds a healer implementation.
    /// </summary>
    /// <typeparam name="THealer">The healer implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealer<THealer>(this IServiceCollection services)
        where THealer : class, IHealer
    {
        services.TryAddScoped<IHealer, THealer>();
        return services;
    }
}
