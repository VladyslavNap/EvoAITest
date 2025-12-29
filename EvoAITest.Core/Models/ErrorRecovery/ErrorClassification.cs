namespace EvoAITest.Core.Models.ErrorRecovery;

/// <summary>
/// Result of error classification with confidence score
/// </summary>
public sealed record ErrorClassification
{
    /// <summary>
    /// Classified error type
    /// </summary>
    public required ErrorType ErrorType { get; init; }
    
    /// <summary>
    /// Confidence score (0.0 - 1.0)
    /// </summary>
    public required double Confidence { get; init; }
    
    /// <summary>
    /// Original exception
    /// </summary>
    public required Exception Exception { get; init; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// Suggested recovery actions
    /// </summary>
    public List<RecoveryActionType> SuggestedActions { get; init; } = new();
    
    /// <summary>
    /// Is this error recoverable?
    /// </summary>
    public bool IsRecoverable => ErrorType != ErrorType.Unknown && Confidence >= 0.7;
    
    /// <summary>
    /// Additional context about the error
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}
