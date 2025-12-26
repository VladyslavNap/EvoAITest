namespace EvoAITest.Core.Models.ErrorRecovery;

/// <summary>
/// Retry strategy configuration with exponential backoff and jitter support
/// </summary>
public sealed class RetryStrategy
{
    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; init; } = 3;
    
    /// <summary>
    /// Initial delay before first retry
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(500);
    
    /// <summary>
    /// Maximum delay between retries
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Use exponential backoff
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;
    
    /// <summary>
    /// Add random jitter to delays
    /// </summary>
    public bool UseJitter { get; init; } = true;
    
    /// <summary>
    /// Backoff multiplier (default: 2x)
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;
    
    /// <summary>
    /// Calculate delay for specific attempt with exponential backoff and jitter
    /// </summary>
    /// <param name="attemptNumber">Current attempt number (1-based)</param>
    /// <returns>Calculated delay time</returns>
    public TimeSpan CalculateDelay(int attemptNumber)
    {
        var delay = InitialDelay;
        
        if (UseExponentialBackoff)
        {
            var multiplier = Math.Pow(BackoffMultiplier, attemptNumber - 1);
            delay = TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * multiplier);
        }
        
        // Cap at max delay
        delay = delay > MaxDelay ? MaxDelay : delay;
        
        // Add jitter to prevent thundering herd
        if (UseJitter)
        {
            var jitter = Random.Shared.Next(0, (int)(delay.TotalMilliseconds * 0.3));
            delay = delay.Add(TimeSpan.FromMilliseconds(jitter));
        }
        
        return delay;
    }
}
