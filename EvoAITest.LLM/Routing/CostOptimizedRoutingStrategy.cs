using EvoAITest.Core.Options;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;

namespace EvoAITest.LLM.Routing;

/// <summary>
/// Routes LLM requests based on cost optimization.
/// Selects the cheapest route that meets minimum quality requirements.
/// </summary>
public sealed class CostOptimizedRoutingStrategy : IRoutingStrategy
{
    private readonly ILogger<CostOptimizedRoutingStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CostOptimizedRoutingStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger for routing decisions.</param>
    public CostOptimizedRoutingStrategy(ILogger<CostOptimizedRoutingStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string Name => "CostOptimized";

    /// <inheritdoc/>
    public string Description => "Routes requests to the cheapest provider that meets quality requirements";

    /// <inheritdoc/>
    public RouteInfo SelectRoute(
        TaskType taskType,
        LLMRoutingOptions options,
        RoutingContext? context = null)
    {
        context ??= RoutingContext.Default();

        // Determine minimum quality required for this task type
        var minQuality = DetermineMinimumQuality(taskType, context);

        // Get all applicable routes for this task type
        var applicableRoutes = GetApplicableRoutes(taskType, options, minQuality);

        if (applicableRoutes.Count == 0)
        {
            _logger.LogWarning(
                "No cost-optimized routes available for task type '{TaskType}' with quality >= {MinQuality}, using default",
                taskType,
                minQuality);

            return RouteInfo.CreateDefault(options.DefaultRoute, taskType, Name);
        }

        // Sort by cost (cheapest first)
        var sortedRoutes = applicableRoutes
            .OrderBy(r => r.Value.CostPer1KTokens ?? double.MaxValue)
            .ToList();

        // Select the cheapest route
        var (_, selectedConfig) = sortedRoutes.First();

        var estimatedTokens = context.EstimatedTokenCount ?? taskType.GetTypicalTokenCount();
        var estimatedCost = CalculateEstimatedCost(selectedConfig, estimatedTokens);

        _logger.LogInformation(
            "Cost-optimized routing selected {Provider}/{Model} for {TaskType} " +
            "(cost: ${Cost:F4}/1K tokens, estimated: ${EstimatedCost:F4} for {Tokens} tokens, quality: {Quality})",
            selectedConfig.PrimaryProvider,
            selectedConfig.PrimaryModel,
            taskType,
            selectedConfig.CostPer1KTokens ?? 0,
            estimatedCost,
            estimatedTokens,
            selectedConfig.MinimumQuality);

        return RouteInfo.FromConfiguration(
            selectedConfig,
            taskType,
            Name,
            confidence: 1.0,
            reason: $"Cheapest route meeting quality threshold {minQuality:P0}");
    }

    /// <inheritdoc/>
    public bool CanHandle(LLMRoutingOptions options)
    {
        // Cost-optimized routing requires cost information on all routes
        if (options.DefaultRoute?.CostPer1KTokens == null)
        {
            _logger.LogError("CostOptimized routing requires CostPer1KTokens on default route");
            return false;
        }

        var routesWithoutCost = options.Routes
            .Where(r => !r.Value.CostPer1KTokens.HasValue)
            .Select(r => r.Key)
            .ToList();

        if (routesWithoutCost.Any())
        {
            _logger.LogError(
                "CostOptimized routing requires CostPer1KTokens on all routes. Missing cost for: {Routes}",
                string.Join(", ", routesWithoutCost));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines the minimum quality required for a task type.
    /// </summary>
    private double DetermineMinimumQuality(TaskType taskType, RoutingContext context)
    {
        // Start with task type's inherent quality requirement
        var baseQuality = taskType.RequiresHighQuality() ? 0.9 : 0.7;

        // Adjust based on context
        if (context.Priority >= 8)
        {
            // High priority requests should use higher quality models
            baseQuality = Math.Max(baseQuality, 0.8);
        }
        else if (context.MinimizeCost)
        {
            // If explicitly minimizing cost, can tolerate lower quality
            baseQuality = Math.Max(baseQuality - 0.1, 0.5);
        }

        return baseQuality;
    }

    /// <summary>
    /// Gets all routes that are applicable for the task type and meet quality requirements.
    /// </summary>
    private Dictionary<string, RouteConfiguration> GetApplicableRoutes(
        TaskType taskType,
        LLMRoutingOptions options,
        double minQuality)
    {
        var applicable = new Dictionary<string, RouteConfiguration>();

        // Check task-specific route
        var taskTypeName = taskType.ToString();
        if (options.Routes.TryGetValue(taskTypeName, out var taskRoute) &&
            taskRoute.Enabled &&
            taskRoute.MinimumQuality >= minQuality &&
            taskRoute.CostPer1KTokens.HasValue)
        {
            applicable[taskTypeName] = taskRoute;
        }

        // Also consider default route if it meets quality requirements
        if (options.DefaultRoute.Enabled &&
            options.DefaultRoute.MinimumQuality >= minQuality &&
            options.DefaultRoute.CostPer1KTokens.HasValue)
        {
            applicable["Default"] = options.DefaultRoute;
        }

        // Could also consider other routes that might be suitable for this task type
        // (e.g., general routes, similar task types, etc.)
        foreach (var (routeName, route) in options.Routes)
        {
            if (routeName != taskTypeName &&
                route.Enabled &&
                route.MinimumQuality >= minQuality &&
                route.CostPer1KTokens.HasValue &&
                !applicable.ContainsKey(routeName) &&
                IsReasonableAlternative(taskType, routeName))
            {
                // Only add if it's a reasonable alternative
                applicable[routeName] = route;
            }
        }

        return applicable;
    }

    /// <summary>
    /// Determines if a route configured for one task type is a reasonable alternative for another.
    /// </summary>
    private bool IsReasonableAlternative(TaskType requestedType, string routeTaskType)
    {
        // General routes can serve any task
        if (routeTaskType.Equals("General", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Analysis can often substitute for IntentDetection
        if (requestedType == TaskType.IntentDetection &&
            routeTaskType.Equals("Analysis", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Planning can substitute for Analysis (overkill but works)
        if (requestedType == TaskType.Analysis &&
            routeTaskType.Equals("Planning", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates the estimated cost for a request.
    /// </summary>
    private double CalculateEstimatedCost(RouteConfiguration config, int tokenCount)
    {
        if (!config.CostPer1KTokens.HasValue)
        {
            return 0.0;
        }

        // Cost = (tokens / 1000) * cost_per_1k
        return (tokenCount / 1000.0) * config.CostPer1KTokens.Value;
    }
}
