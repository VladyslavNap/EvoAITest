using EvoAITest.Core.Models;

namespace EvoAITest.Core.Data.Models;

/// <summary>
/// Tracks error recovery attempts for learning and analytics
/// </summary>
public sealed class RecoveryHistory
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Associated task (nullable for non-task recoveries)
    /// </summary>
    public Guid? TaskId { get; set; }
    
    /// <summary>
    /// Error type that occurred
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;
    
    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Exception type name
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Recovery strategy used
    /// </summary>
    public string RecoveryStrategy { get; set; } = string.Empty;
    
    /// <summary>
    /// Actions attempted (JSON array of RecoveryActionType)
    /// </summary>
    public string RecoveryActions { get; set; } = "[]";
    
    /// <summary>
    /// Was recovery successful?
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Attempt number when recovery succeeded/failed
    /// </summary>
    public int AttemptNumber { get; set; }
    
    /// <summary>
    /// Recovery duration in milliseconds
    /// </summary>
    public int DurationMs { get; set; }
    
    /// <summary>
    /// When recovery occurred
    /// </summary>
    public DateTimeOffset RecoveredAt { get; set; }
    
    /// <summary>
    /// Page URL when error occurred
    /// </summary>
    public string? PageUrl { get; set; }
    
    /// <summary>
    /// Action being performed when error occurred
    /// </summary>
    public string? Action { get; set; }
    
    /// <summary>
    /// Selector involved (if applicable)
    /// </summary>
    public string? Selector { get; set; }
    
    /// <summary>
    /// Additional context (JSON)
    /// </summary>
    public string? Context { get; set; }
    
    /// <summary>
    /// Navigation to associated task
    /// </summary>
    public AutomationTask? Task { get; set; }
}
