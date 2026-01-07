namespace EvoAITest.LLM.CircuitBreaker;

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
/// <remarks>
/// The circuit breaker pattern prevents cascading failures by monitoring provider health
/// and automatically switching to fallback providers when failures exceed thresholds.
/// 
/// State transitions:
/// - Closed ? Open: When failure count exceeds threshold
/// - Open ? Half-Open: After timeout period expires
/// - Half-Open ? Closed: When test request succeeds
/// - Half-Open ? Open: When test request fails
/// </remarks>
public enum CircuitBreakerState
{
    /// <summary>
    /// Normal operation state. Requests flow through to the primary provider.
    /// </summary>
    /// <remarks>
    /// The circuit breaker monitors failures but doesn't block requests.
    /// If failures exceed the threshold, transitions to Open state.
    /// </remarks>
    Closed = 0,

    /// <summary>
    /// Failure state. All requests immediately use the fallback provider.
    /// </summary>
    /// <remarks>
    /// The primary provider is considered unhealthy and is not called.
    /// After a timeout period, transitions to Half-Open to test recovery.
    /// </remarks>
    Open = 1,

    /// <summary>
    /// Testing state. Allows limited requests through to test provider recovery.
    /// </summary>
    /// <remarks>
    /// A small number of test requests are sent to the primary provider.
    /// - If successful: Transitions to Closed (provider recovered)
    /// - If fails: Transitions back to Open (provider still unhealthy)
    /// </remarks>
    HalfOpen = 2
}

/// <summary>
/// Contains the current status and metrics of a circuit breaker.
/// Thread-safe immutable snapshot of circuit breaker state.
/// </summary>
public sealed record CircuitBreakerStatus
{
    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public required CircuitBreakerState State { get; init; }

    /// <summary>
    /// Gets the number of consecutive failures recorded.
    /// </summary>
    /// <remarks>
    /// Reset to 0 when:
    /// - A request succeeds in Closed state
    /// - Circuit transitions to Closed from Half-Open
    /// </remarks>
    public required int FailureCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the last failure.
    /// </summary>
    /// <remarks>
    /// Used to determine when to transition from Open to Half-Open.
    /// </remarks>
    public DateTimeOffset? LastFailureTime { get; init; }

    /// <summary>
    /// Gets the timestamp of the last state change.
    /// </summary>
    /// <remarks>
    /// Used for telemetry and preventing rapid state oscillation.
    /// </remarks>
    public DateTimeOffset LastStateChange { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the number of successful requests since last failure.
    /// </summary>
    /// <remarks>
    /// Used in Half-Open state to determine when to close the circuit.
    /// Typically requires 1-3 successful requests before closing.
    /// </remarks>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Gets the total number of requests attempted through this circuit breaker.
    /// </summary>
    /// <remarks>
    /// Includes both successful and failed requests.
    /// Useful for calculating failure rates and reliability metrics.
    /// </remarks>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Gets the total number of requests that used the fallback provider.
    /// </summary>
    /// <remarks>
    /// Includes:
    /// - Requests in Open state (forced fallback)
    /// - Requests that failed primary and used fallback
    /// Used for cost tracking and provider health metrics.
    /// </remarks>
    public long FallbackRequests { get; init; }

    /// <summary>
    /// Gets a value indicating whether the circuit breaker is currently allowing requests
    /// to the primary provider.
    /// </summary>
    /// <remarks>
    /// Returns true when:
    /// - State is Closed (normal operation)
    /// - State is Half-Open (testing recovery)
    /// Returns false when:
    /// - State is Open (using fallback only)
    /// </remarks>
    public bool IsAllowingRequests => State is CircuitBreakerState.Closed or CircuitBreakerState.HalfOpen;

    /// <summary>
    /// Gets a value indicating whether the circuit breaker is using the fallback provider.
    /// </summary>
    public bool IsUsingFallback => State == CircuitBreakerState.Open;

    /// <summary>
    /// Gets the failure rate as a percentage (0.0 - 1.0).
    /// </summary>
    /// <remarks>
    /// Calculated as: FailureCount / TotalRequests
    /// Returns 0 if no requests have been made.
    /// </remarks>
    public double FailureRate
    {
        get
        {
            if (TotalRequests == 0) return 0.0;
            return FailureCount / (double)TotalRequests;
        }
    }

    /// <summary>
    /// Gets the fallback usage rate as a percentage (0.0 - 1.0).
    /// </summary>
    /// <remarks>
    /// Calculated as: FallbackRequests / TotalRequests
    /// Useful for cost analysis and provider health monitoring.
    /// </remarks>
    public double FallbackRate
    {
        get
        {
            if (TotalRequests == 0) return 0.0;
            return FallbackRequests / (double)TotalRequests;
        }
    }

    /// <summary>
    /// Creates a string representation of the circuit breaker status.
    /// </summary>
    /// <returns>A human-readable status string.</returns>
    public override string ToString()
    {
        var stateDescription = State switch
        {
            CircuitBreakerState.Closed => "Closed (Normal)",
            CircuitBreakerState.Open => "Open (Using Fallback)",
            CircuitBreakerState.HalfOpen => "Half-Open (Testing)",
            _ => "Unknown"
        };

        return $"{stateDescription}: {FailureCount} failures, " +
               $"{TotalRequests} total requests, " +
               $"{FallbackRequests} fallback requests " +
               $"({FallbackRate:P0} fallback rate)";
    }

    /// <summary>
    /// Creates a default CircuitBreakerStatus representing a newly created circuit breaker.
    /// </summary>
    /// <returns>A new status in Closed state with zero counters.</returns>
    public static CircuitBreakerStatus CreateDefault()
    {
        return new CircuitBreakerStatus
        {
            State = CircuitBreakerState.Closed,
            FailureCount = 0,
            SuccessCount = 0,
            TotalRequests = 0,
            FallbackRequests = 0,
            LastStateChange = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a copy of this status with an incremented failure count.
    /// </summary>
    /// <returns>A new status with failure count incremented.</returns>
    public CircuitBreakerStatus WithFailure()
    {
        return this with
        {
            FailureCount = FailureCount + 1,
            LastFailureTime = DateTimeOffset.UtcNow,
            TotalRequests = TotalRequests + 1,
            SuccessCount = 0 // Reset success count on failure
        };
    }

    /// <summary>
    /// Creates a copy of this status with an incremented success count and reset failure count.
    /// </summary>
    /// <returns>A new status with success count incremented and failures reset.</returns>
    public CircuitBreakerStatus WithSuccess()
    {
        return this with
        {
            FailureCount = 0,
            SuccessCount = SuccessCount + 1,
            TotalRequests = TotalRequests + 1
        };
    }

    /// <summary>
    /// Creates a copy of this status with state changed and counters reset.
    /// </summary>
    /// <param name="newState">The new circuit breaker state.</param>
    /// <returns>A new status with the specified state.</returns>
    public CircuitBreakerStatus WithState(CircuitBreakerState newState)
    {
        return this with
        {
            State = newState,
            LastStateChange = DateTimeOffset.UtcNow,
            SuccessCount = 0 // Reset success count on state change
        };
    }

    /// <summary>
    /// Creates a copy of this status with fallback counter incremented.
    /// </summary>
    /// <returns>A new status with fallback requests incremented.</returns>
    public CircuitBreakerStatus WithFallback()
    {
        return this with
        {
            FallbackRequests = FallbackRequests + 1,
            TotalRequests = TotalRequests + 1
        };
    }
}
