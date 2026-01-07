using System.ComponentModel.DataAnnotations;

namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration for a specific routing rule.
/// Defines which provider and model to use for a particular task type,
/// along with fallback options and performance thresholds.
/// </summary>
public sealed class RouteConfiguration
{
    /// <summary>
    /// Gets or sets the primary provider name.
    /// This is the preferred provider for this route.
    /// </summary>
    /// <remarks>
    /// Valid values: "AzureOpenAI", "Ollama", "Claude", or any registered provider name.
    /// </remarks>
    /// <example>
    /// "AzureOpenAI"
    /// </example>
    [Required]
    public required string PrimaryProvider { get; init; }

    /// <summary>
    /// Gets or sets the primary model name.
    /// The specific model to use with the primary provider.
    /// </summary>
    /// <remarks>
    /// Examples: "gpt-4", "gpt-5", "qwen2.5-7b", "claude-sonnet-3.5"
    /// </remarks>
    /// <example>
    /// "gpt-4"
    /// </example>
    [Required]
    public required string PrimaryModel { get; init; }

    /// <summary>
    /// Gets or sets the fallback provider name.
    /// Used when primary provider fails or circuit breaker opens.
    /// </summary>
    /// <remarks>
    /// Optional. If not specified, requests will fail when primary is unavailable.
    /// </remarks>
    /// <example>
    /// "Ollama"
    /// </example>
    public string? FallbackProvider { get; init; }

    /// <summary>
    /// Gets or sets the fallback model name.
    /// The model to use with the fallback provider.
    /// </summary>
    /// <remarks>
    /// Required if FallbackProvider is specified.
    /// </remarks>
    /// <example>
    /// "qwen2.5-7b"
    /// </example>
    public string? FallbackModel { get; init; }

    /// <summary>
    /// Gets or sets the maximum allowed latency in milliseconds before fallback.
    /// If primary provider exceeds this latency, fallback provider is used for subsequent requests.
    /// </summary>
    /// <remarks>
    /// Optional. If not specified, fallback is only triggered by failures, not latency.
    /// Useful for performance-optimized routing.
    /// Range: 100-30000 ms (0.1s to 30s)
    /// </remarks>
    /// <example>
    /// 3000 (3 seconds)
    /// </example>
    [Range(100, 30000)]
    public int? MaxLatencyMs { get; init; }

    /// <summary>
    /// Gets or sets the cost per 1K tokens for this route.
    /// Used for cost-optimized routing to calculate total cost.
    /// </summary>
    /// <remarks>
    /// Optional. If not specified, cost-based routing cannot be used for this route.
    /// Value in USD per 1000 tokens (combined input + output).
    /// </remarks>
    /// <example>
    /// 0.03 (3 cents per 1K tokens for GPT-4)
    /// 0.0 (free for local Ollama)
    /// </example>
    [Range(0.0, 1.0)]
    public double? CostPer1KTokens { get; init; }

    /// <summary>
    /// Gets or sets the priority for this route when multiple routes could apply.
    /// Higher priority routes are preferred.
    /// </summary>
    /// <remarks>
    /// Default: 0 (normal priority)
    /// Range: -100 to 100
    /// Used when custom routing logic needs to choose between multiple applicable routes.
    /// </remarks>
    [Range(-100, 100)]
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether this route is enabled.
    /// Disabled routes are skipped during routing selection.
    /// </summary>
    /// <remarks>
    /// Useful for temporarily disabling a route without removing configuration.
    /// </remarks>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the minimum quality threshold for this route (0.0-1.0).
    /// Used in cost-optimized routing to ensure quality requirements are met.
    /// </summary>
    /// <remarks>
    /// Default: 0.7 (70% quality)
    /// Higher values require better models even if more expensive.
    /// Lower values allow cheaper models even if quality is reduced.
    /// </remarks>
    [Range(0.0, 1.0)]
    public double MinimumQuality { get; init; } = 0.7;

    /// <summary>
    /// Gets or sets custom tags for this route.
    /// Can be used for custom routing logic, filtering, or categorization.
    /// </summary>
    /// <example>
    /// ["production", "high-priority", "cost-sensitive"]
    /// </example>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Gets or sets additional metadata for this route.
    /// Can store provider-specific configuration or custom routing data.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Validates the route configuration.
    /// </summary>
    /// <returns>A tuple indicating if configuration is valid and any error messages.</returns>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // Validate primary provider and model
        if (string.IsNullOrWhiteSpace(PrimaryProvider))
        {
            errors.Add("PrimaryProvider is required");
        }

        if (string.IsNullOrWhiteSpace(PrimaryModel))
        {
            errors.Add("PrimaryModel is required");
        }

        // Validate fallback configuration consistency
        if (!string.IsNullOrWhiteSpace(FallbackProvider) && string.IsNullOrWhiteSpace(FallbackModel))
        {
            errors.Add("FallbackModel is required when FallbackProvider is specified");
        }

        if (string.IsNullOrWhiteSpace(FallbackProvider) && !string.IsNullOrWhiteSpace(FallbackModel))
        {
            errors.Add("FallbackProvider is required when FallbackModel is specified");
        }

        // Validate that primary and fallback are different
        if (!string.IsNullOrWhiteSpace(FallbackProvider) &&
            PrimaryProvider.Equals(FallbackProvider, StringComparison.OrdinalIgnoreCase) &&
            PrimaryModel.Equals(FallbackModel, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Fallback provider/model must be different from primary provider/model");
        }

        // Validate latency threshold
        if (MaxLatencyMs.HasValue && MaxLatencyMs.Value < 100)
        {
            errors.Add("MaxLatencyMs must be at least 100ms if specified");
        }

        // Validate cost
        if (CostPer1KTokens.HasValue && CostPer1KTokens.Value < 0)
        {
            errors.Add("CostPer1KTokens cannot be negative");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Creates a string representation of this route configuration.
    /// </summary>
    /// <returns>A human-readable description of the route.</returns>
    public override string ToString()
    {
        var fallback = !string.IsNullOrWhiteSpace(FallbackProvider)
            ? $" ? Fallback: {FallbackProvider}/{FallbackModel}"
            : string.Empty;

        var cost = CostPer1KTokens.HasValue
            ? $" (${CostPer1KTokens.Value:F4}/1K tokens)"
            : string.Empty;

        var latency = MaxLatencyMs.HasValue
            ? $" [Max: {MaxLatencyMs.Value}ms]"
            : string.Empty;

        var status = Enabled ? "Enabled" : "Disabled";

        return $"{PrimaryProvider}/{PrimaryModel}{cost}{latency}{fallback} [{status}]";
    }
}
