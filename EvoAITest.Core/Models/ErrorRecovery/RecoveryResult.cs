namespace EvoAITest.Core.Models.ErrorRecovery;

/// <summary>
/// Result of recovery attempt
/// </summary>
public sealed record RecoveryResult
{
    /// <summary>
    /// Was recovery successful?
    /// </summary>
    public required bool Success { get; init; }
    
    /// <summary>
    /// Actions attempted
    /// </summary>
    public List<RecoveryActionType> ActionsAttempted { get; init; } = new();
    
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public required int AttemptNumber { get; init; }
    
    /// <summary>
    /// Total time spent on recovery
    /// </summary>
    public required TimeSpan Duration { get; init; }
    
    /// <summary>
    /// Original error classification
    /// </summary>
    public required ErrorClassification ErrorClassification { get; init; }
    
    /// <summary>
    /// Final exception if recovery failed
    /// </summary>
    public Exception? FinalException { get; init; }
    
    /// <summary>
    /// Recovery strategy used
    /// </summary>
    public required string Strategy { get; init; }
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
