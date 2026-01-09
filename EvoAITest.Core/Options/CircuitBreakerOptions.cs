using System.ComponentModel.DataAnnotations;

namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for circuit breaker pattern implementation.
/// Controls how the system detects and responds to failing LLM providers.
/// </summary>
/// <remarks>
/// Circuit breaker pattern helps prevent cascading failures by:
/// - Detecting when a provider is unhealthy (consecutive failures)
/// - Stopping requests to the unhealthy provider (circuit open)
/// - Routing to fallback provider immediately
/// - Periodically testing if the provider has recovered (half-open)
/// - Resuming normal operation when healthy (circuit closed)
/// </remarks>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the number of consecutive failures before opening the circuit.
    /// </summary>
    /// <remarks>
    /// Default: 5 failures
    /// Lower values = more sensitive to failures (faster failover)
    /// Higher values = more tolerant to failures (less frequent failover)
    /// 
    /// Recommended:
    /// - Development: 10 (more lenient)
    /// - Staging: 5 (balanced)
    /// - Production: 3 (conservative)
    /// </remarks>
    [Range(1, 100)]
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration to keep the circuit open before attempting recovery.
    /// </summary>
    /// <remarks>
    /// Default: 30 seconds
    /// Time to wait before testing if the provider has recovered.
    /// 
    /// After this duration:
    /// - Circuit enters half-open state
    /// - One test request is sent to primary provider
    /// - If successful, circuit closes
    /// - If fails, circuit stays open for another duration
    /// </remarks>
    [Range(5, 300)]
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the timeout for individual provider requests.
    /// Requests exceeding this timeout are counted as failures.
    /// </summary>
    /// <remarks>
    /// Default: 30 seconds
    /// Should be higher than typical provider response time.
    /// 
    /// Recommended by provider:
    /// - Azure OpenAI: 30-60s
    /// - Ollama (local): 10-20s
    /// - Claude: 30-60s
    /// </remarks>
    [Range(5, 300)]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to count timeouts as failures.
    /// </summary>
    /// <remarks>
    /// Default: true
    /// When true, requests that exceed RequestTimeout count toward FailureThreshold.
    /// When false, only explicit errors (exceptions) count as failures.
    /// </remarks>
    public bool CountTimeoutsAsFailures { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to count rate limit errors (429) as failures.
    /// </summary>
    /// <remarks>
    /// Default: false
    /// Rate limiting is temporary and usually resolves quickly.
    /// Setting to false prevents unnecessary circuit breaker activation.
    /// However, set to true if rate limiting persists longer than OpenDuration.
    /// </remarks>
    public bool CountRateLimitsAsFailures { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum time between state changes.
    /// Prevents rapid oscillation between states.
    /// </summary>
    /// <remarks>
    /// Default: 5 seconds
    /// Circuit breaker state can only change after this duration has passed
    /// since the last state change.
    /// </remarks>
    [Range(1, 60)]
    public TimeSpan MinimumStateDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the number of successful requests required in half-open state
    /// before closing the circuit completely.
    /// </summary>
    /// <remarks>
    /// Default: 1
    /// Higher values require more proof of recovery before resuming normal operation.
    /// 
    /// Recommended:
    /// - Conservative (production): 3-5
    /// - Balanced: 2
    /// - Aggressive: 1
    /// </remarks>
    [Range(1, 10)]
    public int SuccessThresholdInHalfOpen { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether to emit telemetry events for state changes.
    /// </summary>
    /// <remarks>
    /// Default: true
    /// Emits events when circuit opens, closes, or enters half-open state.
    /// Useful for monitoring and alerting.
    /// </remarks>
    public bool EmitTelemetryEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to reset failure count on any success.
    /// </summary>
    /// <remarks>
    /// Default: true
    /// When true, any successful request resets the failure counter to 0.
    /// When false, only closing the circuit resets the counter.
    /// 
    /// True is recommended for most scenarios as it prevents circuit breaker
    /// from opening due to intermittent failures.
    /// </remarks>
    public bool ResetCounterOnSuccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of concurrent requests allowed in half-open state.
    /// </summary>
    /// <remarks>
    /// Default: 1
    /// Limits the number of test requests during recovery testing.
    /// Higher values provide faster validation but risk overwhelming the recovering provider.
    /// </remarks>
    [Range(1, 10)]
    public int MaxConcurrentRequestsInHalfOpen { get; set; } = 1;

    /// <summary>
    /// Validates the circuit breaker options configuration.
    /// </summary>
    /// <returns>A tuple indicating if configuration is valid and any error messages.</returns>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // Validate failure threshold
        if (FailureThreshold < 1)
        {
            errors.Add("FailureThreshold must be at least 1");
        }

        if (FailureThreshold > 100)
        {
            errors.Add("FailureThreshold cannot exceed 100");
        }

        // Validate open duration
        if (OpenDuration < TimeSpan.FromSeconds(5))
        {
            errors.Add("OpenDuration must be at least 5 seconds");
        }

        if (OpenDuration > TimeSpan.FromMinutes(5))
        {
            errors.Add("OpenDuration cannot exceed 5 minutes");
        }

        // Validate request timeout
        if (RequestTimeout < TimeSpan.FromSeconds(5))
        {
            errors.Add("RequestTimeout must be at least 5 seconds");
        }

        if (RequestTimeout > TimeSpan.FromMinutes(5))
        {
            errors.Add("RequestTimeout cannot exceed 5 minutes");
        }

        // Validate minimum state duration
        if (MinimumStateDuration < TimeSpan.FromSeconds(1))
        {
            errors.Add("MinimumStateDuration must be at least 1 second");
        }

        if (MinimumStateDuration >= OpenDuration)
        {
            errors.Add("MinimumStateDuration must be less than OpenDuration");
        }

        // Validate success threshold
        if (SuccessThresholdInHalfOpen < 1)
        {
            errors.Add("SuccessThresholdInHalfOpen must be at least 1");
        }

        // Validate concurrent requests
        if (MaxConcurrentRequestsInHalfOpen < 1)
        {
            errors.Add("MaxConcurrentRequestsInHalfOpen must be at least 1");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Creates a string representation of the circuit breaker configuration.
    /// </summary>
    /// <returns>A human-readable description of the configuration.</returns>
    public override string ToString()
    {
        return $"Circuit Breaker: {FailureThreshold} failures ? Open for {OpenDuration.TotalSeconds}s " +
               $"(Timeout: {RequestTimeout.TotalSeconds}s, Success threshold: {SuccessThresholdInHalfOpen})";
    }
}
