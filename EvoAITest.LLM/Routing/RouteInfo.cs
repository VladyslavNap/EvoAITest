namespace EvoAITest.LLM.Routing;

/// <summary>
/// Contains information about a routing decision.
/// Represents the result of selecting a route for an LLM request.
/// </summary>
public sealed record RouteInfo
{
    /// <summary>
    /// Gets the primary provider name.
    /// This is the preferred provider for this request.
    /// </summary>
    /// <example>
    /// "AzureOpenAI"
    /// </example>
    public required string PrimaryProvider { get; init; }

    /// <summary>
    /// Gets the primary model name.
    /// The specific model to use with the primary provider.
    /// </summary>
    /// <example>
    /// "gpt-4"
    /// </example>
    public required string PrimaryModel { get; init; }

    /// <summary>
    /// Gets the fallback provider name.
    /// Used when primary provider fails or circuit breaker opens.
    /// </summary>
    /// <remarks>
    /// Null if no fallback is configured.
    /// </remarks>
    /// <example>
    /// "Ollama"
    /// </example>
    public string? FallbackProvider { get; init; }

    /// <summary>
    /// Gets the fallback model name.
    /// The model to use with the fallback provider.
    /// </summary>
    /// <remarks>
    /// Null if no fallback is configured.
    /// </remarks>
    /// <example>
    /// "qwen2.5-7b"
    /// </example>
    public string? FallbackModel { get; init; }

    /// <summary>
    /// Gets the routing strategy used to make this decision.
    /// </summary>
    /// <example>
    /// "TaskBased", "CostOptimized", "PerformanceOptimized"
    /// </example>
    public required string Strategy { get; init; }

    /// <summary>
    /// Gets the detected or specified task type.
    /// </summary>
    public required Models.TaskType TaskType { get; init; }

    /// <summary>
    /// Gets the estimated cost per 1K tokens for this route.
    /// Used for cost tracking and analysis.
    /// </summary>
    /// <remarks>
    /// Null if cost information is not available.
    /// Value in USD per 1000 tokens (combined input + output).
    /// </remarks>
    /// <example>
    /// 0.03 (3 cents per 1K tokens for GPT-4)
    /// </example>
    public double? EstimatedCostPer1KTokens { get; init; }

    /// <summary>
    /// Gets the maximum allowed latency for this route in milliseconds.
    /// Used for performance-optimized routing.
    /// </summary>
    /// <remarks>
    /// Null if no latency constraint is specified.
    /// </remarks>
    public int? MaxLatencyMs { get; init; }

    /// <summary>
    /// Gets the confidence score for this routing decision (0.0-1.0).
    /// Higher values indicate more confidence in the decision.
    /// </summary>
    /// <remarks>
    /// Useful when task type detection is uncertain.
    /// </remarks>
    public double Confidence { get; init; } = 1.0;

    /// <summary>
    /// Gets the reason for this routing decision.
    /// Provides human-readable explanation of why this route was selected.
    /// </summary>
    /// <example>
    /// "Task type 'Planning' requires high-quality model"
    /// </example>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets metadata associated with this routing decision.
    /// Can include additional context, metrics, or debugging information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Gets the timestamp when this routing decision was made.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets a value indicating whether a fallback provider is configured.
    /// </summary>
    public bool HasFallback => !string.IsNullOrWhiteSpace(FallbackProvider);

    /// <summary>
    /// Gets a value indicating whether this route includes cost information.
    /// </summary>
    public bool HasCostInfo => EstimatedCostPer1KTokens.HasValue;

    /// <summary>
    /// Gets a value indicating whether this route has latency constraints.
    /// </summary>
    public bool HasLatencyConstraint => MaxLatencyMs.HasValue;

    /// <summary>
    /// Creates a string representation of this route.
    /// </summary>
    /// <returns>A human-readable description of the route.</returns>
    public override string ToString()
    {
        var primary = $"{PrimaryProvider}/{PrimaryModel}";
        var fallback = HasFallback ? $" ? {FallbackProvider}/{FallbackModel}" : string.Empty;
        var cost = HasCostInfo ? $" (${EstimatedCostPer1KTokens:F4}/1K)" : string.Empty;
        var latency = HasLatencyConstraint ? $" [Max: {MaxLatencyMs}ms]" : string.Empty;
        var confidence = Confidence < 1.0 ? $" (confidence: {Confidence:P0})" : string.Empty;

        return $"{TaskType}: {primary}{cost}{latency}{fallback}{confidence} via {Strategy}";
    }

    /// <summary>
    /// Creates a RouteInfo from a RouteConfiguration and task type.
    /// </summary>
    /// <param name="config">The route configuration.</param>
    /// <param name="taskType">The task type.</param>
    /// <param name="strategy">The routing strategy name.</param>
    /// <param name="confidence">The confidence score (0.0-1.0).</param>
    /// <param name="reason">Optional reason for the routing decision.</param>
    /// <returns>A new RouteInfo instance.</returns>
    public static RouteInfo FromConfiguration(
        Core.Options.RouteConfiguration config,
        Models.TaskType taskType,
        string strategy,
        double confidence = 1.0,
        string? reason = null)
    {
        return new RouteInfo
        {
            PrimaryProvider = config.PrimaryProvider,
            PrimaryModel = config.PrimaryModel,
            FallbackProvider = config.FallbackProvider,
            FallbackModel = config.FallbackModel,
            Strategy = strategy,
            TaskType = taskType,
            EstimatedCostPer1KTokens = config.CostPer1KTokens,
            MaxLatencyMs = config.MaxLatencyMs,
            Confidence = confidence,
            Reason = reason ?? $"Task type '{taskType}' routed via {strategy} strategy"
        };
    }

    /// <summary>
    /// Creates a default RouteInfo when no specific route is configured.
    /// </summary>
    /// <param name="defaultConfig">The default route configuration.</param>
    /// <param name="taskType">The task type.</param>
    /// <param name="strategy">The routing strategy name.</param>
    /// <returns>A new RouteInfo instance.</returns>
    public static RouteInfo CreateDefault(
        Core.Options.RouteConfiguration defaultConfig,
        Models.TaskType taskType,
        string strategy)
    {
        return FromConfiguration(
            defaultConfig,
            taskType,
            strategy,
            confidence: 0.5,
            reason: $"No specific route configured for '{taskType}', using default");
    }

    /// <summary>
    /// Creates a RouteInfo with metadata from task detection.
    /// </summary>
    /// <param name="baseRoute">The base route info.</param>
    /// <param name="detectionMetadata">Metadata from task type detection.</param>
    /// <returns>A new RouteInfo with added metadata.</returns>
    public static RouteInfo WithDetectionMetadata(
        RouteInfo baseRoute,
        Dictionary<string, object> detectionMetadata)
    {
        var metadata = new Dictionary<string, object>(baseRoute.Metadata);
        foreach (var (key, value) in detectionMetadata)
        {
            metadata[$"Detection.{key}"] = value;
        }

        return baseRoute with { Metadata = metadata };
    }
}
