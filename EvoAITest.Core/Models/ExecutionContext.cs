namespace EvoAITest.Core.Models;

/// <summary>
/// Execution context for error recovery, capturing state when an error occurs
/// </summary>
public sealed class ExecutionContext
{
    /// <summary>
    /// Task ID if execution is part of a task
    /// </summary>
    public Guid? TaskId { get; set; }
    
    /// <summary>
    /// Current page URL when error occurred
    /// </summary>
    public string? PageUrl { get; set; }
    
    /// <summary>
    /// Action being performed when error occurred
    /// </summary>
    public string? Action { get; set; }
    
    /// <summary>
    /// Selector being used when error occurred
    /// </summary>
    public string? Selector { get; set; }
    
    /// <summary>
    /// Expected text for element (if applicable)
    /// </summary>
    public string? ExpectedText { get; set; }
    
    /// <summary>
    /// Additional metadata about the execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
