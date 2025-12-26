namespace EvoAITest.Core.Services.ErrorRecovery;

using EvoAITest.Core.Models;
using EvoAITest.Core.Models.ErrorRecovery;

/// <summary>
/// Service for recovering from errors with intelligent action selection and learning
/// </summary>
public interface IErrorRecoveryService
{
    /// <summary>
    /// Attempt to recover from an error using adaptive strategies
    /// </summary>
    /// <param name="error">Exception that occurred</param>
    /// <param name="context">Execution context when error occurred</param>
    /// <param name="strategy">Optional retry strategy (uses defaults if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovery result with success status and actions attempted</returns>
    Task<RecoveryResult> RecoverAsync(
        Exception error,
        ExecutionContext context,
        RetryStrategy? strategy = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get recovery statistics for analytics and monitoring
    /// </summary>
    /// <param name="taskId">Optional task ID to filter statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with recovery metrics (success rate, duration, by error type)</returns>
    Task<Dictionary<string, object>> GetRecoveryStatisticsAsync(
        Guid? taskId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determine best recovery actions based on error type and historical data
    /// </summary>
    /// <param name="errorType">Type of error</param>
    /// <param name="context">Execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suggested actions in priority order</returns>
    Task<List<RecoveryActionType>> SuggestActionsAsync(
        ErrorType errorType,
        ExecutionContext context,
        CancellationToken cancellationToken = default);
}
