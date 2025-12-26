namespace EvoAITest.Core.Services.ErrorRecovery;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Data.Models;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.ErrorRecovery;
using EvoAITest.Core.Models.SmartWaiting;

/// <summary>
/// Error recovery service with intelligent action selection and learning from history
/// </summary>
public sealed class ErrorRecoveryService : IErrorRecoveryService
{
    private readonly IErrorClassifier _classifier;
    private readonly ISelectorHealingService? _selectorHealing;
    private readonly ISmartWaitService? _smartWait;
    private readonly EvoAIDbContext _dbContext;
    private readonly ILogger<ErrorRecoveryService> _logger;
    private readonly IBrowserAgent _browserAgent;
    
    public ErrorRecoveryService(
        IErrorClassifier classifier,
        EvoAIDbContext dbContext,
        ILogger<ErrorRecoveryService> logger,
        IBrowserAgent browserAgent,
        ISelectorHealingService? selectorHealing = null,
        ISmartWaitService? smartWait = null)
    {
        _classifier = classifier;
        _selectorHealing = selectorHealing;
        _smartWait = smartWait;
        _dbContext = dbContext;
        _logger = logger;
        _browserAgent = browserAgent;
    }
    
    /// <inheritdoc/>
    public async Task<RecoveryResult> RecoverAsync(
        Exception error,
        ExecutionContext context,
        RetryStrategy? strategy = null,
        CancellationToken cancellationToken = default)
    {
        strategy ??= new RetryStrategy();
        var startTime = DateTimeOffset.UtcNow;
        var classification = await _classifier.ClassifyAsync(error, context, cancellationToken);
        
        _logger.LogInformation(
            "Starting recovery for {ErrorType} error (confidence: {Confidence:P})",
            classification.ErrorType, classification.Confidence);
        
        if (!classification.IsRecoverable)
        {
            _logger.LogWarning(
                "Error is not recoverable: {ErrorType} (confidence: {Confidence:P})",
                classification.ErrorType, classification.Confidence);
            
            return CreateFailureResult(classification, 0, TimeSpan.Zero, error);
        }
        
        var actionsAttempted = new List<RecoveryActionType>();
        var suggestedActions = await SuggestActionsAsync(
            classification.ErrorType, context, cancellationToken);
        
        _logger.LogInformation(
            "Suggested recovery actions: {Actions}",
            string.Join(", ", suggestedActions));
        
        // Try recovery with exponential backoff
        for (int attempt = 1; attempt <= strategy.MaxRetries; attempt++)
        {
            var delay = strategy.CalculateDelay(attempt);
            _logger.LogInformation(
                "Recovery attempt {Attempt}/{Max}, waiting {Delay:F0}ms",
                attempt, strategy.MaxRetries, delay.TotalMilliseconds);
            
            await Task.Delay(delay, cancellationToken);
            
            // Try each suggested action
            foreach (var action in suggestedActions)
            {
                if (actionsAttempted.Contains(action))
                {
                    _logger.LogDebug("Skipping already attempted action: {Action}", action);
                    continue;
                }
                
                actionsAttempted.Add(action);
                
                var success = await ExecuteRecoveryActionAsync(
                    action, context, cancellationToken);
                
                if (success)
                {
                    var duration = DateTimeOffset.UtcNow - startTime;
                    _logger.LogInformation(
                        "Recovery successful after {Attempt} attempts using {Action} (duration: {Duration:F0}ms)",
                        attempt, action, duration.TotalMilliseconds);
                    
                    var result = CreateSuccessResult(
                        classification, attempt, duration, actionsAttempted);
                    
                    await SaveRecoveryHistoryAsync(result, context, cancellationToken);
                    return result;
                }
            }
        }
        
        // All recovery attempts failed
        var failDuration = DateTimeOffset.UtcNow - startTime;
        _logger.LogError(
            "Recovery failed after {Attempts} attempts and {Duration:F0}ms. Actions tried: {Actions}",
            strategy.MaxRetries, failDuration.TotalMilliseconds, string.Join(", ", actionsAttempted));
        
        var failResult = CreateFailureResult(
            classification, strategy.MaxRetries, failDuration, error, actionsAttempted);
        
        await SaveRecoveryHistoryAsync(failResult, context, cancellationToken);
        return failResult;
    }
    
    /// <summary>
    /// Execute a specific recovery action
    /// </summary>
    private async Task<bool> ExecuteRecoveryActionAsync(
        RecoveryActionType action,
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Executing recovery action: {Action}", action);
            
            switch (action)
            {
                case RecoveryActionType.WaitAndRetry:
                    // Simple wait to let transient issues resolve
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    _logger.LogDebug("WaitAndRetry: waited 2 seconds");
                    return true;
                
                case RecoveryActionType.PageRefresh:
                    // Refresh the page to reset state
                    var pageState = await _browserAgent.GetPageStateAsync(cancellationToken);
                    await _browserAgent.NavigateAsync(pageState.Url, cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    _logger.LogDebug("PageRefresh: reloaded page and waited 1 second");
                    return true;
                
                case RecoveryActionType.WaitForStability:
                    // Wait for page to stabilize using smart wait if available
                    if (_smartWait != null)
                    {
                        await _smartWait.WaitForStableStateAsync(
                            new WaitConditions 
                            { 
                                Conditions = new List<WaitConditionType> 
                                { 
                                    WaitConditionType.DomStable 
                                } 
                            },
                            10000,
                            cancellationToken);
                        _logger.LogDebug("WaitForStability: page is now stable");
                    }
                    else
                    {
                        // Fallback to simple delay
                        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                        _logger.LogDebug("WaitForStability: waited 3 seconds (fallback)");
                    }
                    return true;
                
                case RecoveryActionType.AlternativeSelector:
                    // Try to heal the selector if service is available
                    if (_selectorHealing == null || string.IsNullOrEmpty(context.Selector))
                    {
                        _logger.LogWarning("AlternativeSelector: healing service not available or no selector");
                        return false;
                    }
                    
                    var currentState = await _browserAgent.GetPageStateAsync(cancellationToken);
                    var screenshot = await _browserAgent.TakeFullPageScreenshotBytesAsync(cancellationToken);
                    
                    var healed = await _selectorHealing.HealSelectorAsync(
                        context.Selector,
                        currentState,
                        context.ExpectedText,
                        screenshot,
                        cancellationToken);
                    
                    if (healed != null)
                    {
                        _logger.LogInformation(
                            "AlternativeSelector: healed {Original} -> {Healed} (confidence: {Confidence:P})",
                            context.Selector, healed.NewSelector, healed.ConfidenceScore);
                        
                        // Update context with healed selector for retry
                        context.Selector = healed.NewSelector;
                        return true;
                    }
                    
                    _logger.LogWarning("AlternativeSelector: healing failed");
                    return false;
                
                case RecoveryActionType.ClearCookies:
                    // Clear cookies by navigating away and back
                    var originalUrl = (await _browserAgent.GetPageStateAsync(cancellationToken)).Url;
                    await _browserAgent.NavigateAsync("about:blank", cancellationToken);
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                    await _browserAgent.NavigateAsync(originalUrl, cancellationToken);
                    _logger.LogDebug("ClearCookies: cleared session by navigation");
                    return true;
                
                default:
                    _logger.LogWarning("Recovery action {Action} not implemented", action);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recovery action {Action} failed with exception", action);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<List<RecoveryActionType>> SuggestActionsAsync(
        ErrorType errorType,
        ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        // Start with base actions from classifier
        var baseActions = _classifier.GetSuggestedActions(errorType);
        
        // Learn from historical success
        var successfulActions = await _dbContext.RecoveryHistory
            .Where(h => h.ErrorType == errorType.ToString() && h.Success)
            .GroupBy(h => h.RecoveryActions)
            .Select(g => new { Actions = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToListAsync(cancellationToken);
        
        // Prioritize historically successful actions
        if (successfulActions.Any())
        {
            _logger.LogDebug(
                "Found {Count} successful recovery patterns for {ErrorType}",
                successfulActions.Count, errorType);
            
            var learned = successfulActions
                .SelectMany(x => JsonSerializer.Deserialize<List<RecoveryActionType>>(x.Actions) ?? new())
                .Distinct()
                .ToList();
            
            // Combine learned actions with base actions (learned first)
            var combined = learned.Concat(baseActions).Distinct().ToList();
            
            _logger.LogDebug(
                "Prioritized actions based on history: {Actions}",
                string.Join(", ", combined));
            
            return combined;
        }
        
        return baseActions;
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> GetRecoveryStatisticsAsync(
        Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RecoveryHistory.AsQueryable();
        
        if (taskId.HasValue)
        {
            query = query.Where(h => h.TaskId == taskId.Value);
        }
        
        var total = await query.CountAsync(cancellationToken);
        
        if (total == 0)
        {
            return new Dictionary<string, object>
            {
                ["total_recoveries"] = 0,
                ["successful_recoveries"] = 0,
                ["success_rate"] = 0.0,
                ["average_duration_ms"] = 0.0,
                ["by_error_type"] = Array.Empty<object>()
            };
        }
        
        var successful = await query.CountAsync(h => h.Success, cancellationToken);
        var avgDuration = await query.AverageAsync(h => h.DurationMs, cancellationToken);
        
        var byErrorType = await query
            .GroupBy(h => h.ErrorType)
            .Select(g => new
            {
                ErrorType = g.Key,
                Total = g.Count(),
                Successful = g.Count(h => h.Success),
                SuccessRate = g.Count(h => h.Success) / (double)g.Count(),
                AvgDurationMs = g.Average(h => h.DurationMs)
            })
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation(
            "Recovery statistics: {Total} total, {Successful} successful ({SuccessRate:P})",
            total, successful, total > 0 ? successful / (double)total : 0);
        
        return new Dictionary<string, object>
        {
            ["total_recoveries"] = total,
            ["successful_recoveries"] = successful,
            ["success_rate"] = successful / (double)total,
            ["average_duration_ms"] = avgDuration,
            ["by_error_type"] = byErrorType
        };
    }
    
    /// <summary>
    /// Save recovery result to history for learning
    /// </summary>
    private async Task SaveRecoveryHistoryAsync(
        RecoveryResult result,
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        var history = new RecoveryHistory
        {
            Id = Guid.NewGuid(),
            TaskId = context.TaskId,
            ErrorType = result.ErrorClassification.ErrorType.ToString(),
            ErrorMessage = result.ErrorClassification.Message,
            ExceptionType = result.ErrorClassification.Exception.GetType().Name,
            RecoveryStrategy = result.Strategy,
            RecoveryActions = JsonSerializer.Serialize(result.ActionsAttempted),
            Success = result.Success,
            AttemptNumber = result.AttemptNumber,
            DurationMs = (int)result.Duration.TotalMilliseconds,
            RecoveredAt = DateTimeOffset.UtcNow,
            PageUrl = context.PageUrl,
            Action = context.Action,
            Selector = context.Selector,
            Context = JsonSerializer.Serialize(result.Metadata)
        };
        
        _dbContext.RecoveryHistory.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug(
            "Saved recovery history: {Success} after {Attempts} attempts",
            result.Success ? "Success" : "Failure", result.AttemptNumber);
    }
    
    /// <summary>
    /// Create success result
    /// </summary>
    private RecoveryResult CreateSuccessResult(
        ErrorClassification classification,
        int attempt,
        TimeSpan duration,
        List<RecoveryActionType> actions)
    {
        return new RecoveryResult
        {
            Success = true,
            ActionsAttempted = new List<RecoveryActionType>(actions),
            AttemptNumber = attempt,
            Duration = duration,
            ErrorClassification = classification,
            Strategy = "Adaptive"
        };
    }
    
    /// <summary>
    /// Create failure result
    /// </summary>
    private RecoveryResult CreateFailureResult(
        ErrorClassification classification,
        int attempts,
        TimeSpan duration,
        Exception? finalException,
        List<RecoveryActionType>? actions = null)
    {
        return new RecoveryResult
        {
            Success = false,
            ActionsAttempted = actions ?? new List<RecoveryActionType>(),
            AttemptNumber = attempts,
            Duration = duration,
            ErrorClassification = classification,
            FinalException = finalException,
            Strategy = "Adaptive"
        };
    }
}
