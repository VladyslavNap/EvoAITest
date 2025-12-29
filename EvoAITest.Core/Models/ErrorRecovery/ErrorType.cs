namespace EvoAITest.Core.Models.ErrorRecovery;

/// <summary>
/// Types of errors that can be classified for recovery
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Unknown or unclassified error
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Temporary network or service issues
    /// </summary>
    Transient,
    
    /// <summary>
    /// Element selector not found or stale
    /// </summary>
    SelectorNotFound,
    
    /// <summary>
    /// Navigation timeout or failure
    /// </summary>
    NavigationTimeout,
    
    /// <summary>
    /// JavaScript execution error
    /// </summary>
    JavaScriptError,
    
    /// <summary>
    /// Browser permission denied
    /// </summary>
    PermissionDenied,
    
    /// <summary>
    /// Network request failed
    /// </summary>
    NetworkError,
    
    /// <summary>
    /// Page or browser crashed
    /// </summary>
    PageCrash,
    
    /// <summary>
    /// Element exists but not interactable
    /// </summary>
    ElementNotInteractable,
    
    /// <summary>
    /// Timing issue (race condition)
    /// </summary>
    TimingIssue
}
