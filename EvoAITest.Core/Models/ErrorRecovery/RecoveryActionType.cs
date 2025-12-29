namespace EvoAITest.Core.Models.ErrorRecovery;

/// <summary>
/// Types of recovery actions available
/// </summary>
public enum RecoveryActionType
{
    /// <summary>
    /// No action (fail immediately)
    /// </summary>
    None,
    
    /// <summary>
    /// Wait and retry with same parameters
    /// </summary>
    WaitAndRetry,
    
    /// <summary>
    /// Refresh the page
    /// </summary>
    PageRefresh,
    
    /// <summary>
    /// Try alternative selector (uses SelectorHealingService)
    /// </summary>
    AlternativeSelector,
    
    /// <summary>
    /// Retry navigation
    /// </summary>
    NavigationRetry,
    
    /// <summary>
    /// Clear cookies and retry
    /// </summary>
    ClearCookies,
    
    /// <summary>
    /// Clear browser cache
    /// </summary>
    ClearCache,
    
    /// <summary>
    /// Wait for page stability (uses SmartWaitService)
    /// </summary>
    WaitForStability,
    
    /// <summary>
    /// Restart browser context
    /// </summary>
    RestartContext
}
