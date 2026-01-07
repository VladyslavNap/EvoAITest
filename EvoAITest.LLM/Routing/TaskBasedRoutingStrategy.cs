using EvoAITest.Core.Options;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;

namespace EvoAITest.LLM.Routing;

/// <summary>
/// Routes LLM requests based on task type.
/// Maps each task type to a configured route, falling back to default route if needed.
/// </summary>
public sealed class TaskBasedRoutingStrategy : IRoutingStrategy
{
    private readonly ILogger<TaskBasedRoutingStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskBasedRoutingStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger for routing decisions.</param>
    public TaskBasedRoutingStrategy(ILogger<TaskBasedRoutingStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string Name => "TaskBased";

    /// <inheritdoc/>
    public string Description => "Routes requests based on detected or specified task type";

    /// <inheritdoc/>
    public RouteInfo SelectRoute(
        TaskType taskType,
        LLMRoutingOptions options,
        RoutingContext? context = null)
    {
        context ??= RoutingContext.Default();

        // Try to find a specific route for this task type
        var taskTypeName = taskType.ToString();
        if (options.Routes.TryGetValue(taskTypeName, out var routeConfig))
        {
            // Check if route is enabled
            if (!routeConfig.Enabled)
            {
                _logger.LogWarning(
                    "Route for task type '{TaskType}' is disabled, using default route",
                    taskType);
                
                return RouteInfo.CreateDefault(options.DefaultRoute, taskType, Name);
            }

            // Apply context-based adjustments if needed
            routeConfig = ApplyContextAdjustments(routeConfig, context);

            var route = RouteInfo.FromConfiguration(
                routeConfig,
                taskType,
                Name,
                confidence: 1.0,
                reason: $"Task type '{taskType}' matched to configured route");

            _logger.LogInformation(
                "Routed {TaskType} to {Provider}/{Model} (fallback: {HasFallback})",
                taskType,
                route.PrimaryProvider,
                route.PrimaryModel,
                route.HasFallback);

            return route;
        }

        // No specific route found, use default
        _logger.LogInformation(
            "No specific route configured for task type '{TaskType}', using default route",
            taskType);

        return RouteInfo.CreateDefault(options.DefaultRoute, taskType, Name);
    }

    /// <inheritdoc/>
    public bool CanHandle(LLMRoutingOptions options)
    {
        // Task-based routing can handle any valid configuration
        // It doesn't require specific metadata like cost or latency

        if (options.DefaultRoute == null)
        {
            _logger.LogError("TaskBased routing requires a default route configuration");
            return false;
        }

        // Validate that all configured routes are valid
        foreach (var (taskTypeName, route) in options.Routes)
        {
            if (route == null)
            {
                _logger.LogError("Route for task type '{TaskType}' is null", taskTypeName);
                return false;
            }

            var (isValid, errors) = route.Validate();
            if (!isValid)
            {
                _logger.LogError(
                    "Route for task type '{TaskType}' is invalid: {Errors}",
                    taskTypeName,
                    string.Join("; ", errors));
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Applies context-based adjustments to the route configuration.
    /// For example, might choose faster fallback if context prefers speed.
    /// </summary>
    private RouteConfiguration ApplyContextAdjustments(
        RouteConfiguration baseConfig,
        RoutingContext context)
    {
        // If context prefers speed and we have a fallback, consider swapping
        // (This is a simple example - real implementation might be more sophisticated)
        if (context.PreferSpeed && baseConfig.FallbackProvider != null)
        {
            // Check if fallback is likely faster (e.g., local Ollama vs cloud OpenAI)
            if (IsLikelyFaster(baseConfig.FallbackProvider, baseConfig.PrimaryProvider))
            {
                _logger.LogDebug(
                    "Context prefers speed, considering fallback {Fallback} over primary {Primary}",
                    baseConfig.FallbackProvider,
                    baseConfig.PrimaryProvider);

                // Note: In a real implementation, we'd create a new RouteConfiguration
                // For now, we'll just log the consideration
            }
        }

        return baseConfig;
    }

    /// <summary>
    /// Heuristic to determine if one provider is likely faster than another.
    /// </summary>
    private bool IsLikelyFaster(string provider1, string provider2)
    {
        // Local providers (Ollama) are typically faster than cloud providers
        var localProviders = new[] { "Ollama" };
        var cloudProviders = new[] { "AzureOpenAI", "Claude", "OpenAI" };

        var provider1IsLocal = localProviders.Any(p => 
            provider1.Contains(p, StringComparison.OrdinalIgnoreCase));
        var provider2IsCloud = cloudProviders.Any(p => 
            provider2.Contains(p, StringComparison.OrdinalIgnoreCase));

        return provider1IsLocal && provider2IsCloud;
    }
}
