namespace EvoAITest.Core.Services.ErrorRecovery;

using EvoAITest.Core.Models.ErrorRecovery;
using EvoAITest.Core.Models;

/// <summary>
/// Service for classifying exceptions into recoverable error types
/// </summary>
public interface IErrorClassifier
{
    /// <summary>
    /// Classify an exception into an error type with confidence score
    /// </summary>
    /// <param name="exception">Exception to classify</param>
    /// <param name="context">Optional execution context for additional classification hints</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error classification with type, confidence, and suggested recovery actions</returns>
    Task<ErrorClassification> ClassifyAsync(
        Exception exception,
        ExecutionContext? context = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if error type is transient (temporary/retryable)
    /// </summary>
    /// <param name="errorType">Error type to check</param>
    /// <returns>True if error is transient</returns>
    bool IsTransient(ErrorType errorType);
    
    /// <summary>
    /// Get suggested recovery actions for error type
    /// </summary>
    /// <param name="errorType">Error type</param>
    /// <returns>List of suggested recovery actions in priority order</returns>
    List<RecoveryActionType> GetSuggestedActions(ErrorType errorType);
}
