using Microsoft.Extensions.Options;

namespace EvoAITest.Core.Options.Validation;

/// <summary>
/// Validates <see cref="LLMRoutingOptions"/> configuration at startup.
/// Ensures that routing configuration is valid before the application starts.
/// </summary>
public sealed class LLMRoutingOptionsValidator : IValidateOptions<LLMRoutingOptions>
{
    /// <summary>
    /// Validates the LLM routing options.
    /// </summary>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A validation result indicating success or failure with error messages.</returns>
    public ValidateOptionsResult Validate(string? name, LLMRoutingOptions options)
    {
        // Validate routing strategy
        var validStrategies = new[] { "TaskBased", "CostOptimized", "PerformanceOptimized", "Custom" };
        if (!validStrategies.Contains(options.RoutingStrategy))
        {
            return ValidateOptionsResult.Fail(
                $"Invalid RoutingStrategy '{options.RoutingStrategy}'. " +
                $"Valid values: {string.Join(", ", validStrategies)}");
        }

        // Validate default route
        if (options.DefaultRoute == null)
        {
            return ValidateOptionsResult.Fail("DefaultRoute is required");
        }

        var (isDefaultValid, defaultErrors) = options.DefaultRoute.Validate();
        if (!isDefaultValid)
        {
            return ValidateOptionsResult.Fail(
                $"DefaultRoute validation failed: {string.Join("; ", defaultErrors)}");
        }

        // Validate that routes exist if multi-model routing is enabled
        if (options.EnableMultiModelRouting && options.Routes.Count == 0)
        {
            return ValidateOptionsResult.Fail(
                "EnableMultiModelRouting is true but no routes are configured. " +
                "Either disable multi-model routing or configure at least one route.");
        }

        // Validate each configured route
        foreach (var (taskType, route) in options.Routes)
        {
            if (route == null)
            {
                return ValidateOptionsResult.Fail($"Route for task type '{taskType}' is null");
            }

            var (isRouteValid, routeErrors) = route.Validate();
            if (!isRouteValid)
            {
                return ValidateOptionsResult.Fail(
                    $"Route for task type '{taskType}' validation failed: {string.Join("; ", routeErrors)}");
            }
        }

        // Validate circuit breaker settings
        if (options.CircuitBreakerFailureThreshold < 1)
        {
            return ValidateOptionsResult.Fail(
                $"CircuitBreakerFailureThreshold must be at least 1, got {options.CircuitBreakerFailureThreshold}");
        }

        if (options.CircuitBreakerFailureThreshold > 100)
        {
            return ValidateOptionsResult.Fail(
                $"CircuitBreakerFailureThreshold cannot exceed 100, got {options.CircuitBreakerFailureThreshold}");
        }

        if (options.CircuitBreakerOpenDurationSeconds < 5)
        {
            return ValidateOptionsResult.Fail(
                $"CircuitBreakerOpenDurationSeconds must be at least 5, got {options.CircuitBreakerOpenDurationSeconds}");
        }

        if (options.CircuitBreakerOpenDurationSeconds > 300)
        {
            return ValidateOptionsResult.Fail(
                $"CircuitBreakerOpenDurationSeconds cannot exceed 300, got {options.CircuitBreakerOpenDurationSeconds}");
        }

        // Validate cache settings
        if (options.EnableRoutingCache)
        {
            if (options.RoutingCacheDurationMinutes < 1)
            {
                return ValidateOptionsResult.Fail(
                    $"RoutingCacheDurationMinutes must be at least 1, got {options.RoutingCacheDurationMinutes}");
            }

            if (options.MaxRoutingCacheSize < 100)
            {
                return ValidateOptionsResult.Fail(
                    $"MaxRoutingCacheSize must be at least 100, got {options.MaxRoutingCacheSize}");
            }
        }

        // Validate task detection confidence
        if (options.MinimumTaskDetectionConfidence < 0.0 || options.MinimumTaskDetectionConfidence > 1.0)
        {
            return ValidateOptionsResult.Fail(
                $"MinimumTaskDetectionConfidence must be between 0.0 and 1.0, got {options.MinimumTaskDetectionConfidence}");
        }

        // Validate cost-optimized routing has cost information
        if (options.RoutingStrategy == "CostOptimized")
        {
            var routesWithoutCost = options.Routes
                .Where(r => !r.Value.CostPer1KTokens.HasValue)
                .Select(r => r.Key)
                .ToList();

            if (routesWithoutCost.Any())
            {
                return ValidateOptionsResult.Fail(
                    $"CostOptimized routing strategy requires CostPer1KTokens for all routes. " +
                    $"Missing cost information for: {string.Join(", ", routesWithoutCost)}");
            }

            if (!options.DefaultRoute.CostPer1KTokens.HasValue)
            {
                return ValidateOptionsResult.Fail(
                    "CostOptimized routing strategy requires CostPer1KTokens for DefaultRoute");
            }
        }

        // Validate performance-optimized routing has latency information
        if (options.RoutingStrategy == "PerformanceOptimized")
        {
            var routesWithoutLatency = options.Routes
                .Where(r => !r.Value.MaxLatencyMs.HasValue)
                .Select(r => r.Key)
                .ToList();

            if (routesWithoutLatency.Any())
            {
                // This is a warning, not a failure - we can still use default latency assumptions
                // But we log it for visibility
            }
        }

        // All validations passed
        return ValidateOptionsResult.Success;
    }
}
