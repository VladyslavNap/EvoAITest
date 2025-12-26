namespace EvoAITest.Core.Services.ErrorRecovery;

using EvoAITest.Core.Models.ErrorRecovery;
using EvoAITest.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Pattern-based error classifier that analyzes exceptions and assigns error types with confidence scores
/// </summary>
public sealed class ErrorClassifier : IErrorClassifier
{
    private readonly ILogger<ErrorClassifier> _logger;
    
    /// <summary>
    /// Maps error types to suggested recovery actions in priority order
    /// </summary>
    private static readonly Dictionary<ErrorType, List<RecoveryActionType>> _actionMap = new()
    {
        [ErrorType.Transient] = new() 
        { 
            RecoveryActionType.WaitAndRetry 
        },
        [ErrorType.SelectorNotFound] = new() 
        { 
            RecoveryActionType.AlternativeSelector,
            RecoveryActionType.WaitForStability,
            RecoveryActionType.PageRefresh
        },
        [ErrorType.NavigationTimeout] = new() 
        { 
            RecoveryActionType.NavigationRetry,
            RecoveryActionType.WaitAndRetry
        },
        [ErrorType.TimingIssue] = new() 
        { 
            RecoveryActionType.WaitForStability,
            RecoveryActionType.WaitAndRetry
        },
        [ErrorType.ElementNotInteractable] = new() 
        { 
            RecoveryActionType.WaitForStability,
            RecoveryActionType.AlternativeSelector
        },
        [ErrorType.NetworkError] = new() 
        { 
            RecoveryActionType.WaitAndRetry,
            RecoveryActionType.NavigationRetry
        },
        [ErrorType.PageCrash] = new() 
        { 
            RecoveryActionType.RestartContext,
            RecoveryActionType.NavigationRetry
        },
        [ErrorType.JavaScriptError] = new()
        {
            RecoveryActionType.PageRefresh,
            RecoveryActionType.WaitAndRetry
        },
        [ErrorType.PermissionDenied] = new()
        {
            RecoveryActionType.ClearCookies,
            RecoveryActionType.PageRefresh
        }
    };
    
    public ErrorClassifier(ILogger<ErrorClassifier> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public Task<ErrorClassification> ClassifyAsync(
        Exception exception,
        ExecutionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var (errorType, confidence) = ClassifyException(exception);
        
        var classification = new ErrorClassification
        {
            ErrorType = errorType,
            Confidence = confidence,
            Exception = exception,
            Message = exception.Message,
            SuggestedActions = GetSuggestedActions(errorType),
            Context = BuildContext(exception, context)
        };
        
        _logger.LogInformation(
            "Classified error as {ErrorType} with confidence {Confidence:P}",
            errorType, confidence);
        
        return Task.FromResult(classification);
    }
    
    /// <summary>
    /// Classify exception based on message patterns and exception type
    /// </summary>
    private (ErrorType ErrorType, double Confidence) ClassifyException(Exception exception)
    {
        var message = exception.Message.ToLowerInvariant();
        var exceptionType = exception.GetType().Name;
        
        // Playwright-specific timeout exceptions
        if (exceptionType.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            if (message.Contains("navigate") || message.Contains("navigation"))
                return (ErrorType.NavigationTimeout, 0.95);
            
            if (message.Contains("selector"))
                return (ErrorType.TimingIssue, 0.85);
            
            return (ErrorType.Transient, 0.75);
        }
        
        // Selector-related errors
        if (message.Contains("selector") && (message.Contains("not found") || message.Contains("cannot find")))
            return (ErrorType.SelectorNotFound, 0.9);
        
        // Element interaction errors
        if (message.Contains("not visible") || message.Contains("not interactable") || message.Contains("obscured"))
            return (ErrorType.ElementNotInteractable, 0.9);
        
        // Network errors
        if (message.Contains("network") || message.Contains("connection") || message.Contains("net::err"))
            return (ErrorType.NetworkError, 0.85);
        
        // Browser/page crash
        if (message.Contains("crash") || message.Contains("closed") || message.Contains("disconnected"))
            return (ErrorType.PageCrash, 0.9);
        
        // JavaScript execution errors
        if (message.Contains("javascript") || message.Contains("evaluation failed") || message.Contains("js error"))
            return (ErrorType.JavaScriptError, 0.85);
        
        // Permission errors
        if (message.Contains("permission") || message.Contains("denied") || message.Contains("forbidden"))
            return (ErrorType.PermissionDenied, 0.9);
        
        // HTTP error codes
        if (message.Contains("404") || message.Contains("500") || message.Contains("503"))
            return (ErrorType.NetworkError, 0.8);
        
        // Stale element reference
        if (message.Contains("stale") && message.Contains("element"))
            return (ErrorType.SelectorNotFound, 0.85);
        
        // Unknown error type
        _logger.LogWarning(
            "Unable to classify error with high confidence: {ExceptionType} - {Message}",
            exceptionType, exception.Message);
        
        return (ErrorType.Unknown, 0.5);
    }
    
    /// <inheritdoc/>
    public bool IsTransient(ErrorType errorType)
    {
        return errorType is ErrorType.Transient 
            or ErrorType.NetworkError 
            or ErrorType.TimingIssue;
    }
    
    /// <inheritdoc/>
    public List<RecoveryActionType> GetSuggestedActions(ErrorType errorType)
    {
        if (_actionMap.TryGetValue(errorType, out var actions))
        {
            return new List<RecoveryActionType>(actions);
        }
        
        _logger.LogWarning(
            "No recovery actions mapped for error type {ErrorType}, returning None",
            errorType);
        
        return new List<RecoveryActionType> { RecoveryActionType.None };
    }
    
    /// <summary>
    /// Build context dictionary from exception and execution context
    /// </summary>
    private Dictionary<string, object> BuildContext(
        Exception exception,
        ExecutionContext? context)
    {
        var dict = new Dictionary<string, object>
        {
            ["ExceptionType"] = exception.GetType().Name,
            ["StackTrace"] = exception.StackTrace ?? ""
        };
        
        if (exception.InnerException != null)
        {
            dict["InnerExceptionType"] = exception.InnerException.GetType().Name;
            dict["InnerExceptionMessage"] = exception.InnerException.Message;
        }
        
        if (context != null)
        {
            dict["PageUrl"] = context.PageUrl ?? "";
            dict["Action"] = context.Action ?? "";
            dict["Selector"] = context.Selector ?? "";
            
            if (context.TaskId.HasValue)
            {
                dict["TaskId"] = context.TaskId.Value;
            }
            
            if (!string.IsNullOrEmpty(context.ExpectedText))
            {
                dict["ExpectedText"] = context.ExpectedText;
            }
        }
        
        return dict;
    }
}
