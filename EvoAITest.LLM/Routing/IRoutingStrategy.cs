using EvoAITest.Core.Options;
using EvoAITest.LLM.Models;

namespace EvoAITest.LLM.Routing;

/// <summary>
/// Defines a strategy for routing LLM requests to appropriate providers.
/// Implementations can route based on task type, cost, performance, or custom logic.
/// </summary>
public interface IRoutingStrategy
{
    /// <summary>
    /// Gets the strategy name.
    /// Used for logging, telemetry, and configuration matching.
    /// </summary>
    /// <example>
    /// "TaskBased", "CostOptimized", "PerformanceOptimized"
    /// </example>
    string Name { get; }

    /// <summary>
    /// Gets a description of this routing strategy.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Selects the appropriate route for the given task type.
    /// </summary>
    /// <param name="taskType">The detected task type.</param>
    /// <param name="options">The routing options containing route configurations.</param>
    /// <param name="context">Optional context information for routing decision.</param>
    /// <returns>Route information including primary and fallback providers.</returns>
    /// <remarks>
    /// This method should:
    /// 1. Look up the appropriate route configuration for the task type
    /// 2. Apply strategy-specific logic (cost optimization, performance optimization, etc.)
    /// 3. Consider fallback options
    /// 4. Return a RouteInfo with the routing decision
    /// 
    /// If no specific route is configured for the task type, should use the default route.
    /// </remarks>
    RouteInfo SelectRoute(
        TaskType taskType,
        LLMRoutingOptions options,
        RoutingContext? context = null);

    /// <summary>
    /// Determines if this strategy can handle the given routing options.
    /// </summary>
    /// <param name="options">The routing options to validate.</param>
    /// <returns>True if the strategy can handle these options.</returns>
    /// <remarks>
    /// Useful for validation at startup. For example:
    /// - CostOptimizedStrategy requires cost information
    /// - PerformanceOptimizedStrategy requires latency information
    /// </remarks>
    bool CanHandle(LLMRoutingOptions options);
}

/// <summary>
/// Provides context information for routing decisions.
/// Contains additional metadata that strategies can use to make better decisions.
/// </summary>
public sealed class RoutingContext
{
    /// <summary>
    /// Gets or sets the request metadata.
    /// May include hints, priorities, or other routing-relevant information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Gets or sets the user ID or identifier.
    /// Can be used for user-specific routing rules.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the estimated token count for the request.
    /// Useful for cost calculations.
    /// </summary>
    public int? EstimatedTokenCount { get; init; }

    /// <summary>
    /// Gets or sets the priority of the request (0-10, higher is more important).
    /// High-priority requests might use better models even if more expensive.
    /// </summary>
    public int Priority { get; init; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether to prefer speed over quality.
    /// When true, strategies should favor faster models.
    /// </summary>
    public bool PreferSpeed { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to minimize cost.
    /// When true, strategies should favor cheaper models.
    /// </summary>
    public bool MinimizeCost { get; init; } = false;

    /// <summary>
    /// Gets or sets the maximum acceptable cost per request in USD.
    /// Routing should not exceed this cost if specified.
    /// </summary>
    public double? MaxCostPerRequest { get; init; }

    /// <summary>
    /// Gets or sets the maximum acceptable latency in milliseconds.
    /// Routing should not exceed this latency if specified.
    /// </summary>
    public int? MaxLatencyMs { get; init; }

    /// <summary>
    /// Gets or sets the request timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets custom tags for this request.
    /// Can be used for filtering, categorization, or custom routing logic.
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Creates a default routing context.
    /// </summary>
    /// <returns>A new routing context with default values.</returns>
    public static RoutingContext Default() => new();

    /// <summary>
    /// Creates a routing context optimized for speed.
    /// </summary>
    /// <returns>A new routing context favoring fast execution.</returns>
    public static RoutingContext ForSpeed() => new()
    {
        PreferSpeed = true,
        Priority = 7,
        Tags = new List<string> { "speed-optimized" }
    };

    /// <summary>
    /// Creates a routing context optimized for cost.
    /// </summary>
    /// <returns>A new routing context favoring low cost.</returns>
    public static RoutingContext ForCost() => new()
    {
        MinimizeCost = true,
        Priority = 3,
        Tags = new List<string> { "cost-optimized" }
    };

    /// <summary>
    /// Creates a routing context for high-priority requests.
    /// </summary>
    /// <returns>A new routing context for high-priority work.</returns>
    public static RoutingContext HighPriority() => new()
    {
        Priority = 10,
        Tags = new List<string> { "high-priority" }
    };
}
