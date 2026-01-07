using System.ComponentModel.DataAnnotations;

namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for LLM routing.
/// Enables intelligent routing of LLM requests to appropriate models based on task type,
/// cost optimization, and performance requirements.
/// </summary>
public sealed class LLMRoutingOptions
{
    /// <summary>
    /// Gets or sets the routing strategy name.
    /// </summary>
    /// <remarks>
    /// Valid values:
    /// - "TaskBased": Route based on detected or specified task type
    /// - "CostOptimized": Minimize costs while maintaining quality
    /// - "PerformanceOptimized": Prioritize speed and latency
    /// - "Custom": User-defined routing logic
    /// </remarks>
    [Required]
    public string RoutingStrategy { get; set; } = "TaskBased";

    /// <summary>
    /// Gets or sets a value indicating whether multi-model routing is enabled.
    /// When false, falls back to single-provider behavior.
    /// </summary>
    public bool EnableMultiModelRouting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether provider fallback is enabled.
    /// When true, automatically routes to fallback provider on primary failure.
    /// </summary>
    public bool EnableProviderFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    /// <remarks>
    /// Default: 5 failures
    /// Range: 1-100
    /// </remarks>
    [Range(1, 100)]
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the circuit breaker open duration in seconds.
    /// Time to wait before attempting to recover (half-open state).
    /// </summary>
    /// <remarks>
    /// Default: 30 seconds
    /// Range: 5-300 seconds
    /// </remarks>
    [Range(5, 300)]
    public int CircuitBreakerOpenDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets route configurations by task type.
    /// Key is the task type name (e.g., "Planning", "CodeGeneration").
    /// </summary>
    public Dictionary<string, RouteConfiguration> Routes { get; set; } = new();

    /// <summary>
    /// Gets or sets the default route for unmapped task types.
    /// Used when no specific route is configured for a task type.
    /// </summary>
    [Required]
    public RouteConfiguration DefaultRoute { get; set; } = new()
    {
        PrimaryProvider = "AzureOpenAI",
        PrimaryModel = "gpt-4"
    };

    /// <summary>
    /// Gets or sets a value indicating whether to cache routing decisions.
    /// Improves performance by avoiding redundant task type detection.
    /// </summary>
    public bool EnableRoutingCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the routing cache duration in minutes.
    /// How long to cache routing decisions for identical requests.
    /// </summary>
    /// <remarks>
    /// Default: 5 minutes
    /// Range: 1-60 minutes
    /// </remarks>
    [Range(1, 60)]
    public int RoutingCacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of routing entries to cache.
    /// Prevents unbounded memory growth.
    /// </summary>
    /// <remarks>
    /// Default: 1000 entries
    /// Range: 100-10000
    /// </remarks>
    [Range(100, 10000)]
    public int MaxRoutingCacheSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets a value indicating whether to emit detailed telemetry for routing decisions.
    /// Useful for debugging but may impact performance.
    /// </summary>
    public bool EnableDetailedTelemetry { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum confidence threshold for task type detection.
    /// Routing falls back to default route if confidence is below this threshold.
    /// </summary>
    /// <remarks>
    /// Default: 0.7 (70%)
    /// Range: 0.0-1.0
    /// </remarks>
    [Range(0.0, 1.0)]
    public double MinimumTaskDetectionConfidence { get; set; } = 0.7;

    /// <summary>
    /// Validates the routing options configuration.
    /// </summary>
    /// <returns>A tuple indicating if configuration is valid and any error messages.</returns>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // Validate routing strategy
        var validStrategies = new[] { "TaskBased", "CostOptimized", "PerformanceOptimized", "Custom" };
        if (!validStrategies.Contains(RoutingStrategy))
        {
            errors.Add($"Invalid RoutingStrategy '{RoutingStrategy}'. Valid values: {string.Join(", ", validStrategies)}");
        }

        // Validate default route
        if (DefaultRoute == null)
        {
            errors.Add("DefaultRoute is required");
        }
        else
        {
            var (isValid, routeErrors) = DefaultRoute.Validate();
            if (!isValid)
            {
                errors.Add("DefaultRoute validation failed:");
                errors.AddRange(routeErrors.Select(e => $"  - {e}"));
            }
        }

        // Validate routes if multi-model routing is enabled
        if (EnableMultiModelRouting && Routes.Count == 0)
        {
            errors.Add("EnableMultiModelRouting is true but no routes are configured. Either disable multi-model routing or configure at least one route.");
        }

        // Validate each route
        foreach (var (taskType, route) in Routes)
        {
            if (route == null)
            {
                errors.Add($"Route for task type '{taskType}' is null");
                continue;
            }

            var (isValid, routeErrors) = route.Validate();
            if (!isValid)
            {
                errors.Add($"Route for task type '{taskType}' validation failed:");
                errors.AddRange(routeErrors.Select(e => $"  - {e}"));
            }
        }

        // Validate circuit breaker settings
        if (CircuitBreakerFailureThreshold < 1)
        {
            errors.Add("CircuitBreakerFailureThreshold must be at least 1");
        }

        if (CircuitBreakerOpenDurationSeconds < 5)
        {
            errors.Add("CircuitBreakerOpenDurationSeconds must be at least 5 seconds");
        }

        return (errors.Count == 0, errors);
    }
}
