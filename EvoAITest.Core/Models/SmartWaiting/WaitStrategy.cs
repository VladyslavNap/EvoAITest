namespace EvoAITest.Core.Models.SmartWaiting;

/// <summary>
/// Defines the strategy for determining wait timeouts.
/// </summary>
public enum WaitStrategy
{
    /// <summary>
    /// Use fixed timeout value.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Calculate timeout based on historical data.
    /// </summary>
    Adaptive = 1,

    /// <summary>
    /// Use percentile-based timeout from historical data (e.g., 95th percentile).
    /// </summary>
    Percentile = 2,

    /// <summary>
    /// Use exponential backoff for retries.
    /// </summary>
    ExponentialBackoff = 3,

    /// <summary>
    /// Use linear increase for retries.
    /// </summary>
    LinearBackoff = 4,

    /// <summary>
    /// No wait, proceed immediately.
    /// </summary>
    NoWait = 5
}
