namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for intelligent error recovery and retry logic
/// </summary>
public sealed class ErrorRecoveryOptions
{
    /// <summary>
    /// Enable or disable error recovery
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Automatically retry after recovery
    /// </summary>
    public bool AutoRetry { get; set; } = true;
    
    /// <summary>
    /// Maximum number of recovery attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Initial delay before first recovery attempt (milliseconds)
    /// </summary>
    public int InitialDelayMs { get; set; } = 500;
    
    /// <summary>
    /// Maximum delay between recovery attempts (milliseconds)
    /// </summary>
    public int MaxDelayMs { get; set; } = 10000;
    
    /// <summary>
    /// Use exponential backoff for retry delays
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
    
    /// <summary>
    /// Add random jitter to delays (prevents thundering herd)
    /// </summary>
    public bool UseJitter { get; set; } = true;
    
    /// <summary>
    /// Backoff multiplier for exponential delays
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// List of enabled recovery actions
    /// </summary>
    public List<string> EnabledActions { get; set; } = new()
    {
        "WaitAndRetry",
        "PageRefresh",
        "AlternativeSelector",
        "WaitForStability",
        "ClearCookies"
    };
    
    /// <summary>
    /// Validate configuration options
    /// </summary>
    public void Validate()
    {
        if (MaxRetries < 0)
            throw new InvalidOperationException("MaxRetries must be non-negative");
        
        if (InitialDelayMs < 0)
            throw new InvalidOperationException("InitialDelayMs must be non-negative");
        
        if (MaxDelayMs < InitialDelayMs)
            throw new InvalidOperationException("MaxDelayMs must be greater than or equal to InitialDelayMs");
        
        if (BackoffMultiplier <= 0)
            throw new InvalidOperationException("BackoffMultiplier must be positive");
    }
}
