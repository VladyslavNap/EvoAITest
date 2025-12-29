# Phase 3 Feature 4: Error Recovery and Retry Logic - Implementation Plan

## Status: ?? READY TO IMPLEMENT

**Priority:** Medium  
**Estimated Time:** 12-15 hours  
**Complexity:** Medium  
**Dependencies:** Self-Healing (Feature 1), Smart Waiting (Feature 3)

---

## Overview

Implement intelligent error recovery system that automatically classifies errors, selects appropriate recovery strategies, and learns from past recoveries to improve success rates.

**Target:** 85%+ automatic recovery success rate

---

## Architecture

```
???????????????????????????????????????????????
?         DefaultToolExecutor                 ?
?  (wraps all tool execution)                 ?
???????????????????????????????????????????????
              ?
              ?
    ???????????????????????
    ?  Try Tool Execution ?
    ???????????????????????
              ?
              ? Exception?
    ???????????????????????
    ?   ErrorClassifier   ?
    ?  - Analyze error    ?
    ?  - Score confidence ?
    ???????????????????????
              ?
              ? ErrorType + Confidence
    ???????????????????????????????
    ?  ErrorRecoveryService       ?
    ?  - Select strategy          ?
    ?  - Check history            ?
    ?  - Apply recovery actions   ?
    ???????????????????????????????
              ?
    ????????????????????????????????????????????
    ?                    ?          ?          ?
??????????    ????????????????  ???????  ??????????
?Refresh ?    ?AlternativeSelec? ?Wait ?  ?Clear   ?
?        ?    ?(SelectorHealing)? ?     ?  ?Cookies ?
??????????    ????????????????  ???????  ??????????
              ?
              ? Retry Tool
    ???????????????????????
    ?   RecoveryHistory   ?
    ?  (save result)      ?
    ???????????????????????
```

---

## Step 1: Create Error Classification Models (2 hours)

### 1.1 ErrorType Enum

**Location:** `EvoAITest.Core/Models/ErrorRecovery/ErrorType.cs`

```csharp
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
```

### 1.2 ErrorClassification Record

**Location:** `EvoAITest.Core/Models/ErrorRecovery/ErrorClassification.cs`

```csharp
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
```

### 1.3 RecoveryActionType Enum

**Location:** `EvoAITest.Core/Models/ErrorRecovery/RecoveryActionType.cs`

```csharp
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
```

### 1.4 RecoveryResult Record

**Location:** `EvoAITest.Core/Models/ErrorRecovery/RecoveryResult.cs`

```csharp
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
```

### 1.5 RetryStrategy Class

**Location:** `EvoAITest.Core/Models/ErrorRecovery/RetryStrategy.cs`

```csharp
namespace EvoAITest.Core.Models.ErrorRecovery;

/// <summary>
/// Retry strategy configuration
/// </summary>
public sealed class RetryStrategy
{
    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; init; } = 3;
    
    /// <summary>
    /// Initial delay before first retry
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMilliseconds(500);
    
    /// <summary>
    /// Maximum delay between retries
    /// </summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Use exponential backoff
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;
    
    /// <summary>
    /// Add random jitter to delays
    /// </summary>
    public bool UseJitter { get; init; } = true;
    
    /// <summary>
    /// Backoff multiplier (default: 2x)
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;
    
    /// <summary>
    /// Calculate delay for specific attempt
    /// </summary>
    public TimeSpan CalculateDelay(int attemptNumber)
    {
        var delay = InitialDelay;
        
        if (UseExponentialBackoff)
        {
            var multiplier = Math.Pow(BackoffMultiplier, attemptNumber - 1);
            delay = TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * multiplier);
        }
        
        delay = delay > MaxDelay ? MaxDelay : delay;
        
        if (UseJitter)
        {
            var jitter = Random.Shared.Next(0, (int)(delay.TotalMilliseconds * 0.3));
            delay = delay.Add(TimeSpan.FromMilliseconds(jitter));
        }
        
        return delay;
    }
}
```

---

## Step 2: Implement ErrorClassifier Service (3 hours)

### 2.1 IErrorClassifier Interface

**Location:** `EvoAITest.Core/Services/ErrorRecovery/IErrorClassifier.cs`

```csharp
namespace EvoAITest.Core.Services.ErrorRecovery;

public interface IErrorClassifier
{
    /// <summary>
    /// Classify an exception
    /// </summary>
    Task<ErrorClassification> ClassifyAsync(
        Exception exception,
        ExecutionContext? context = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if error type is transient
    /// </summary>
    bool IsTransient(ErrorType errorType);
    
    /// <summary>
    /// Get suggested recovery actions for error type
    /// </summary>
    List<RecoveryActionType> GetSuggestedActions(ErrorType errorType);
}
```

### 2.2 ErrorClassifier Implementation

**Location:** `EvoAITest.Core/Services/ErrorRecovery/ErrorClassifier.cs`

```csharp
namespace EvoAITest.Core.Services.ErrorRecovery;

public sealed class ErrorClassifier : IErrorClassifier
{
    private readonly ILogger<ErrorClassifier> _logger;
    
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
        }
    };
    
    public ErrorClassifier(ILogger<ErrorClassifier> logger)
    {
        _logger = logger;
    }
    
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
    
    private (ErrorType, double) ClassifyException(Exception exception)
    {
        var message = exception.Message.ToLowerInvariant();
        var exceptionType = exception.GetType().Name;
        
        // Playwright-specific exceptions
        if (exceptionType.Contains("Timeout"))
        {
            if (message.Contains("navigate") || message.Contains("navigation"))
                return (ErrorType.NavigationTimeout, 0.95);
            if (message.Contains("selector"))
                return (ErrorType.TimingIssue, 0.85);
            return (ErrorType.Transient, 0.75);
        }
        
        if (message.Contains("selector") && message.Contains("not found"))
            return (ErrorType.SelectorNotFound, 0.9);
        
        if (message.Contains("not visible") || message.Contains("not interactable"))
            return (ErrorType.ElementNotInteractable, 0.9);
        
        if (message.Contains("network") || message.Contains("connection"))
            return (ErrorType.NetworkError, 0.85);
        
        if (message.Contains("crash") || message.Contains("closed"))
            return (ErrorType.PageCrash, 0.9);
        
        if (message.Contains("javascript") || message.Contains("evaluation failed"))
            return (ErrorType.JavaScriptError, 0.85);
        
        if (message.Contains("permission"))
            return (ErrorType.PermissionDenied, 0.9);
        
        return (ErrorType.Unknown, 0.5);
    }
    
    public bool IsTransient(ErrorType errorType)
    {
        return errorType is ErrorType.Transient 
            or ErrorType.NetworkError 
            or ErrorType.TimingIssue;
    }
    
    public List<RecoveryActionType> GetSuggestedActions(ErrorType errorType)
    {
        return _actionMap.TryGetValue(errorType, out var actions) 
            ? new List<RecoveryActionType>(actions)
            : new List<RecoveryActionType> { RecoveryActionType.None };
    }
    
    private Dictionary<string, object> BuildContext(
        Exception exception,
        ExecutionContext? context)
    {
        var dict = new Dictionary<string, object>
        {
            ["ExceptionType"] = exception.GetType().Name,
            ["StackTrace"] = exception.StackTrace ?? ""
        };
        
        if (context != null)
        {
            dict["PageUrl"] = context.PageUrl ?? "";
            dict["Action"] = context.Action ?? "";
            dict["Selector"] = context.Selector ?? "";
        }
        
        return dict;
    }
}
```

---

## Step 3: Create RecoveryHistory Database Entity (2 hours)

### 3.1 Entity Model

**Location:** `EvoAITest.Core/Data/Models/RecoveryHistory.cs`

```csharp
namespace EvoAITest.Core.Data.Models;

/// <summary>
/// Tracks error recovery attempts for learning
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
    /// Exception type
    /// </summary>
    public string ExceptionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Recovery strategy used
    /// </summary>
    public string RecoveryStrategy { get; set; } = string.Empty;
    
    /// <summary>
    /// Actions attempted (JSON array)
    /// </summary>
    public string RecoveryActions { get; set; } = "[]";
    
    /// <summary>
    /// Was recovery successful?
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Attempt number
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
    /// Action being performed
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
```

### 3.2 EF Core Configuration

**Location:** Update `EvoAITest.Core/Data/EvoAIDbContext.cs`

```csharp
// Add DbSet
public DbSet<RecoveryHistory> RecoveryHistory { get; set; } = null!;

// Add to OnModelCreating
modelBuilder.Entity<RecoveryHistory>(entity =>
{
    entity.ToTable("RecoveryHistory");
    entity.HasKey(e => e.Id);
    
    entity.Property(e => e.ErrorType)
        .HasMaxLength(100)
        .IsRequired();
    
    entity.Property(e => e.RecoveryStrategy)
        .HasMaxLength(50)
        .IsRequired();
    
    entity.Property(e => e.ErrorMessage)
        .IsRequired();
    
    entity.Property(e => e.RecoveryActions)
        .IsRequired();
    
    entity.HasIndex(e => e.TaskId);
    entity.HasIndex(e => e.ErrorType);
    entity.HasIndex(e => e.Success);
    entity.HasIndex(e => e.RecoveredAt);
    
    entity.HasOne(e => e.Task)
        .WithMany()
        .HasForeignKey(e => e.TaskId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

### 3.3 Migration

```bash
dotnet ef migrations add AddRecoveryHistory -p EvoAITest.Core -s EvoAITest.ApiService
```

---

## Step 4: Implement ErrorRecoveryService (4 hours)

### 4.1 IErrorRecoveryService Interface

**Location:** `EvoAITest.Core/Services/ErrorRecovery/IErrorRecoveryService.cs`

```csharp
namespace EvoAITest.Core.Services.ErrorRecovery;

public interface IErrorRecoveryService
{
    /// <summary>
    /// Attempt to recover from an error
    /// </summary>
    Task<RecoveryResult> RecoverAsync(
        Exception error,
        ExecutionContext context,
        RetryStrategy? strategy = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get recovery statistics for learning
    /// </summary>
    Task<Dictionary<string, object>> GetRecoveryStatisticsAsync(
        Guid? taskId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determine best recovery strategy based on history
    /// </summary>
    Task<List<RecoveryActionType>> SuggestActionsAsync(
        ErrorType errorType,
        ExecutionContext context,
        CancellationToken cancellationToken = default);
}
```

### 4.2 ErrorRecoveryService Implementation

**Location:** `EvoAITest.Core/Services/ErrorRecovery/ErrorRecoveryService.cs`

```csharp
namespace EvoAITest.Core.Services.ErrorRecovery;

public sealed class ErrorRecoveryService : IErrorRecoveryService
{
    private readonly IErrorClassifier _classifier;
    private readonly ISelectorHealingService _selectorHealing;
    private readonly ISmartWaitService _smartWait;
    private readonly EvoAIDbContext _dbContext;
    private readonly ILogger<ErrorRecoveryService> _logger;
    private readonly IBrowserAgent _browserAgent;
    
    public ErrorRecoveryService(
        IErrorClassifier classifier,
        ISelectorHealingService selectorHealing,
        ISmartWaitService smartWait,
        EvoAIDbContext dbContext,
        ILogger<ErrorRecoveryService> logger,
        IBrowserAgent browserAgent)
    {
        _classifier = classifier;
        _selectorHealing = selectorHealing;
        _smartWait = smartWait;
        _dbContext = dbContext;
        _logger = logger;
        _browserAgent = browserAgent;
    }
    
    public async Task<RecoveryResult> RecoverAsync(
        Exception error,
        ExecutionContext context,
        RetryStrategy? strategy = null,
        CancellationToken cancellationToken = default)
    {
        strategy ??= new RetryStrategy();
        var startTime = DateTimeOffset.UtcNow;
        var classification = await _classifier.ClassifyAsync(error, context, cancellationToken);
        
        if (!classification.IsRecoverable)
        {
            _logger.LogWarning("Error is not recoverable: {ErrorType}", classification.ErrorType);
            return CreateFailureResult(classification, 0, TimeSpan.Zero, error);
        }
        
        var actionsAttempted = new List<RecoveryActionType>();
        var suggestedActions = await SuggestActionsAsync(
            classification.ErrorType, context, cancellationToken);
        
        for (int attempt = 1; attempt <= strategy.MaxRetries; attempt++)
        {
            var delay = strategy.CalculateDelay(attempt);
            _logger.LogInformation(
                "Recovery attempt {Attempt}/{Max}, waiting {Delay}ms",
                attempt, strategy.MaxRetries, delay.TotalMilliseconds);
            
            await Task.Delay(delay, cancellationToken);
            
            foreach (var action in suggestedActions)
            {
                if (actionsAttempted.Contains(action))
                    continue;
                
                actionsAttempted.Add(action);
                
                var success = await ExecuteRecoveryActionAsync(
                    action, context, cancellationToken);
                
                if (success)
                {
                    var duration = DateTimeOffset.UtcNow - startTime;
                    var result = CreateSuccessResult(
                        classification, attempt, duration, actionsAttempted);
                    
                    await SaveRecoveryHistoryAsync(result, context, cancellationToken);
                    return result;
                }
            }
        }
        
        var failDuration = DateTimeOffset.UtcNow - startTime;
        var failResult = CreateFailureResult(
            classification, strategy.MaxRetries, failDuration, error, actionsAttempted);
        
        await SaveRecoveryHistoryAsync(failResult, context, cancellationToken);
        return failResult;
    }
    
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
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    return true;
                
                case RecoveryActionType.PageRefresh:
                    await _browserAgent.RefreshPageAsync(cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    return true;
                
                case RecoveryActionType.WaitForStability:
                    await _smartWait.WaitForStableStateAsync(
                        new WaitConditions { WaitForDOMStability = true },
                        10000,
                        cancellationToken);
                    return true;
                
                case RecoveryActionType.AlternativeSelector:
                    if (string.IsNullOrEmpty(context.Selector))
                        return false;
                    
                    var pageState = await _browserAgent.GetPageStateAsync(cancellationToken);
                    var screenshot = await _browserAgent.TakeScreenshotAsync(cancellationToken);
                    
                    var healed = await _selectorHealing.HealSelectorAsync(
                        context.Selector,
                        pageState,
                        context.ExpectedText,
                        screenshot,
                        cancellationToken);
                    
                    return healed != null;
                
                case RecoveryActionType.ClearCookies:
                    await _browserAgent.ClearCookiesAsync(cancellationToken);
                    return true;
                
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recovery action {Action} failed", action);
            return false;
        }
    }
    
    public async Task<List<RecoveryActionType>> SuggestActionsAsync(
        ErrorType errorType,
        ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var baseActions = _classifier.GetSuggestedActions(errorType);
        
        // Learn from history
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
            var learned = successfulActions
                .SelectMany(x => JsonSerializer.Deserialize<List<RecoveryActionType>>(x.Actions) ?? new())
                .Distinct()
                .ToList();
            
            return learned.Concat(baseActions).Distinct().ToList();
        }
        
        return baseActions;
    }
    
    public async Task<Dictionary<string, object>> GetRecoveryStatisticsAsync(
        Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RecoveryHistory.AsQueryable();
        
        if (taskId.HasValue)
            query = query.Where(h => h.TaskId == taskId.Value);
        
        var total = await query.CountAsync(cancellationToken);
        var successful = await query.CountAsync(h => h.Success, cancellationToken);
        var avgDuration = await query.AverageAsync(h => h.DurationMs, cancellationToken);
        
        var byErrorType = await query
            .GroupBy(h => h.ErrorType)
            .Select(g => new
            {
                ErrorType = g.Key,
                Total = g.Count(),
                Successful = g.Count(h => h.Success),
                SuccessRate = g.Count(h => h.Success) / (double)g.Count()
            })
            .ToListAsync(cancellationToken);
        
        return new Dictionary<string, object>
        {
            ["total_recoveries"] = total,
            ["successful_recoveries"] = successful,
            ["success_rate"] = total > 0 ? successful / (double)total : 0,
            ["average_duration_ms"] = avgDuration,
            ["by_error_type"] = byErrorType
        };
    }
    
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
    }
    
    private RecoveryResult CreateSuccessResult(
        ErrorClassification classification,
        int attempt,
        TimeSpan duration,
        List<RecoveryActionType> actions)
    {
        return new RecoveryResult
        {
            Success = true,
            ActionsAttempted = actions,
            AttemptNumber = attempt,
            Duration = duration,
            ErrorClassification = classification,
            Strategy = "Adaptive"
        };
    }
    
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
            ActionsAttempted = actions ?? new(),
            AttemptNumber = attempts,
            Duration = duration,
            ErrorClassification = classification,
            FinalException = finalException,
            Strategy = "Adaptive"
        };
    }
}
```

---

## Step 5: Integrate with DefaultToolExecutor (2 hours)

**Location:** Update `EvoAITest.Core/Services/DefaultToolExecutor.cs`

```csharp
public class DefaultToolExecutor : IToolExecutor
{
    // ... existing fields ...
    private readonly IErrorRecoveryService _errorRecovery;
    
    public DefaultToolExecutor(
        // ... existing parameters ...
        IErrorRecoveryService errorRecovery)
    {
        // ... existing assignments ...
        _errorRecovery = errorRecovery;
    }
    
    public async Task<ToolResult> ExecuteToolAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        var context = new ExecutionContext
        {
            Action = toolCall.ToolName,
            Selector = toolCall.Parameters.TryGetValue("selector", out var sel) 
                ? sel?.ToString() : null,
            PageUrl = await _browserAgent.GetCurrentUrlAsync(cancellationToken)
        };
        
        var strategy = new RetryStrategy
        {
            MaxRetries = _options.MaxRetries,
            InitialDelay = TimeSpan.FromMilliseconds(_options.InitialRetryDelayMs),
            MaxDelay = TimeSpan.FromMilliseconds(_options.MaxRetryDelayMs),
            UseExponentialBackoff = _options.UseExponentialBackoff
        };
        
        for (int attempt = 0; attempt <= strategy.MaxRetries; attempt++)
        {
            try
            {
                return await ExecuteToolInternalAsync(toolCall, cancellationToken);
            }
            catch (Exception ex) when (attempt < strategy.MaxRetries)
            {
                _logger.LogWarning(
                    ex,
                    "Tool execution failed (attempt {Attempt}/{Max}): {Tool}",
                    attempt + 1, strategy.MaxRetries + 1, toolCall.ToolName);
                
                var recovery = await _errorRecovery.RecoverAsync(
                    ex, context, strategy, cancellationToken);
                
                if (!recovery.Success)
                {
                    _logger.LogError("Recovery failed after {Attempts} attempts", recovery.AttemptNumber);
                    throw;
                }
                
                _logger.LogInformation(
                    "Recovery successful using {Actions}",
                    string.Join(", ", recovery.ActionsAttempted));
                
                // Retry the tool after successful recovery
                continue;
            }
        }
        
        throw new InvalidOperationException("Should not reach here");
    }
}
```

---

## Step 6: Configuration (1 hour)

### 6.1 ErrorRecoveryOptions

**Location:** `EvoAITest.Core/Options/ErrorRecoveryOptions.cs`

```csharp
namespace EvoAITest.Core.Options;

public sealed class ErrorRecoveryOptions
{
    public bool Enabled { get; set; } = true;
    public bool AutoRetry { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 500;
    public int MaxDelayMs { get; set; } = 10000;
    public bool UseExponentialBackoff { get; set; } = true;
    public bool UseJitter { get; set; } = true;
    public double BackoffMultiplier { get; set; } = 2.0;
    public List<string> EnabledActions { get; set; } = new()
    {
        "WaitAndRetry",
        "PageRefresh",
        "AlternativeSelector",
        "WaitForStability",
        "ClearCookies"
    };
}
```

### 6.2 appsettings.json

```json
{
  "EvoAITest": {
    "Core": {
      "ErrorRecovery": {
        "Enabled": true,
        "AutoRetry": true,
        "MaxRetries": 3,
        "InitialDelayMs": 500,
        "MaxDelayMs": 10000,
        "UseExponentialBackoff": true,
        "UseJitter": true,
        "BackoffMultiplier": 2.0,
        "EnabledActions": [
          "WaitAndRetry",
          "PageRefresh",
          "AlternativeSelector",
          "WaitForStability",
          "ClearCookies"
        ]
      }
    }
  }
}
```

### 6.3 DI Registration

**Location:** Update `EvoAITest.Core/ServiceConfiguration.cs`

```csharp
services.AddScoped<IErrorClassifier, ErrorClassifier>();
services.AddScoped<IErrorRecoveryService, ErrorRecoveryService>();
services.Configure<ErrorRecoveryOptions>(
    configuration.GetSection("EvoAITest:Core:ErrorRecovery"));
```

---

## Step 7: Testing (2-3 hours)

### 7.1 Unit Tests

**Location:** `EvoAITest.Tests/ErrorRecovery/ErrorClassifierTests.cs`

```csharp
public class ErrorClassifierTests
{
    [Fact]
    public async Task ClassifyAsync_SelectorNotFound_ReturnsCorrectType()
    {
        // Arrange
        var logger = new NullLogger<ErrorClassifier>();
        var classifier = new ErrorClassifier(logger);
        var exception = new Exception("Selector not found: #button");
        
        // Act
        var result = await classifier.ClassifyAsync(exception);
        
        // Assert
        result.ErrorType.Should().Be(ErrorType.SelectorNotFound);
        result.Confidence.Should().BeGreaterThan(0.8);
        result.IsRecoverable.Should().BeTrue();
        result.SuggestedActions.Should().Contain(RecoveryActionType.AlternativeSelector);
    }
    
    [Theory]
    [InlineData("Timeout exceeded", ErrorType.Transient)]
    [InlineData("Navigation timeout", ErrorType.NavigationTimeout)]
    [InlineData("Element not visible", ErrorType.ElementNotInteractable)]
    public async Task ClassifyAsync_VariousErrors_ReturnsExpectedTypes(
        string message, ErrorType expected)
    {
        // Test implementation
    }
}
```

### 7.2 Integration Tests

**Location:** `EvoAITest.Tests/ErrorRecovery/ErrorRecoveryIntegrationTests.cs`

```csharp
[Trait("Category", "Integration")]
public class ErrorRecoveryIntegrationTests : IAsyncLifetime
{
    [Fact]
    public async Task RecoverAsync_SelectorFailure_HealsAndRetries()
    {
        // Test with real browser
    }
    
    [Fact]
    public async Task RecoverAsync_TransientError_RetriesSuccessfully()
    {
        // Test implementation
    }
}
```

---

## Step 8: Documentation (1 hour)

### 8.1 User Guide

**Location:** `docs/ErrorRecoveryGuide.md`

Topics:
- Overview of error recovery
- Supported error types
- Recovery actions explained
- Configuration options
- Monitoring and metrics
- Troubleshooting

### 8.2 API Documentation

- XML documentation on all public APIs
- Code examples
- Configuration samples

---

## Timeline

| Step | Task | Hours | Dependencies |
|------|------|-------|--------------|
| 1 | Models & Enums | 2 | None |
| 2 | ErrorClassifier | 3 | Step 1 |
| 3 | Database Entity | 2 | Step 1 |
| 4 | ErrorRecoveryService | 4 | Steps 1-3, Phase 3 Features 1&3 |
| 5 | DefaultToolExecutor Integration | 2 | Step 4 |
| 6 | Configuration | 1 | Steps 1-5 |
| 7 | Testing | 3 | Steps 1-6 |
| 8 | Documentation | 1 | All steps |
| **Total** | | **18** | |

**Note:** Estimated 18 hours total (slight increase from 12-15 due to thoroughness)

---

## Success Criteria

- ? 85%+ automatic recovery success rate
- ? Correct error classification in 90%+ cases
- ? Average recovery time < 5 seconds
- ? Learning improves success rate over time
- ? No infinite retry loops
- ? All unit tests passing
- ? Integration tests with real browser scenarios passing

---

## Dependencies

**Required:**
- Phase 3 Feature 1: SelectorHealingService (for AlternativeSelector action)
- Phase 3 Feature 3: SmartWaitService (for WaitForStability action)
- EvoAIDbContext (for RecoveryHistory persistence)
- DefaultToolExecutor (for integration point)

**Optional:**
- Telemetry/metrics infrastructure
- Dashboard for monitoring

---

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Incorrect classification | Medium | Conservative confidence thresholds, learning from history |
| Infinite retry loops | High | Hard retry limits, circuit breaker pattern |
| Performance overhead | Low | Async operations, configurable timeouts |
| Action conflicts | Medium | Action ordering, mutual exclusion checks |

---

## Future Enhancements

- Machine learning-based classification
- Custom recovery action plugins
- A/B testing of recovery strategies
- Recovery action chaining optimization
- Distributed recovery history (across multiple instances)

---

**Status:** Ready to implement  
**Priority:** Medium  
**Estimated Completion:** 18 hours  
**Dependencies:** Features 1 & 3 complete ?

---

*This plan provides a complete, production-ready error recovery system with intelligent classification, adaptive strategies, and continuous learning.*
