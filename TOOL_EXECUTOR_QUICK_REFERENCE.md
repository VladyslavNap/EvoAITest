# Tool Executor Service - Quick Reference

## Interface Methods

### ExecuteToolAsync
Execute a single tool with retry:
```csharp
var result = await executor.ExecuteToolAsync(toolCall, cancellationToken);
```

### ExecuteSequenceAsync
Execute tools in order:
```csharp
var results = await executor.ExecuteSequenceAsync(toolCalls, cancellationToken);
```

### ExecuteWithFallbackAsync
Execute with fallback strategies:
```csharp
var result = await executor.ExecuteWithFallbackAsync(primary, fallbacks, cancellationToken);
```

### ValidateToolCallAsync
Validate without executing:
```csharp
var isValid = await executor.ValidateToolCallAsync(toolCall);
```

### GetExecutionHistoryAsync
Get execution history:
```csharp
var history = await executor.GetExecutionHistoryAsync(correlationId);
```

## ToolExecutionResult

### Properties
- `Success` (bool) - Execution outcome
- `ToolName` (string) - Tool executed
- `Result` (object?) - Return data
- `Error` (Exception?) - Error if failed
- `ExecutionDuration` (TimeSpan) - Total time
- `AttemptCount` (int) - Number of attempts
- `Metadata` (Dictionary) - Additional context

### Convenience Properties
- `IsFailure` - Inverse of Success
- `WasRetried` - True if retried
- `Summary` - Human-readable summary

### Factory Methods
```csharp
ToolExecutionResult.Succeeded(toolName, result, duration, attempts);
ToolExecutionResult.Failed(toolName, error, duration, attempts);
```

### Metadata Enrichment
```csharp
result.WithMetadata(key, value);
result.WithMetadata(dictionary);
```

## Configuration (appsettings.json)

```json
{
  "EvoAITest": {
    "ToolExecutor": {
      "MaxRetries": 3,
      "InitialRetryDelayMs": 500,
      "MaxRetryDelayMs": 10000,
      "UseExponentialBackoff": true,
      "MaxConcurrentTools": 1,
      "TimeoutPerToolMs": 30000,
      "EnableDetailedLogging": true,
      "MaxHistorySize": 100
    }
  }
}
```

## Common Patterns

### Basic Execution
```csharp
var toolCall = new ToolCall(
    ToolName: "navigate",
    Parameters: new Dictionary<string, object> { ["url"] = url },
    Reasoning: "Navigate to target",
    CorrelationId: correlationId
);

var result = await executor.ExecuteToolAsync(toolCall, ct);
```

### Sequential Workflow
```csharp
var tools = new[]
{
    new ToolCall("navigate", new { url = "..." }, ...),
    new ToolCall("type", new { selector = "#username", text = "..." }, ...),
    new ToolCall("click", new { selector = "#login" }, ...)
};

var results = await executor.ExecuteSequenceAsync(tools, ct);
var allSucceeded = results.All(r => r.Success);
```

### With Fallbacks
```csharp
var primary = new ToolCall("click", new { selector = "#btn" }, ...);
var fallbacks = new[]
{
    new ToolCall("click", new { selector = ".btn-primary" }, ...),
    new ToolCall("click", new { force = true, selector = "#btn" }, ...)
};

var result = await executor.ExecuteWithFallbackAsync(primary, fallbacks, ct);
```

### Error Handling
```csharp
try
{
    var result = await executor.ExecuteToolAsync(toolCall, ct);
    
    if (!result.Success)
    {
        _logger.LogWarning("Tool failed: {Error}", result.Error?.Message);
        
        if (result.WasRetried)
        {
            _logger.LogInformation("Failed after {Attempts} attempts", result.AttemptCount);
        }
    }
}
catch (OperationCanceledException)
{
    _logger.LogInformation("Execution canceled");
    throw;
}
catch (InvalidOperationException ex)
{
    _logger.LogError(ex, "Invalid tool or configuration");
}
```

## Retry Behavior

### Exponential Backoff (Default)
| Attempt | Delay |
|---------|-------|
| 1 | 0ms |
| 2 | ~500ms |
| 3 | ~1000ms |
| 4 | ~2000ms |

### Fixed Delay
All retries use `InitialRetryDelayMs` (e.g., 500ms each)

## Common Metadata Keys

| Key | Type | Description |
|-----|------|-------------|
| `correlation_id` | string | Tracing ID |
| `selector` | string | Element selector |
| `url` | string | Current URL |
| `retry_reasons` | string[] | Why retried |
| `fallback_used` | bool | Used fallback |
| `primary_error` | string | Primary error |
| `screenshots` | string[] | Error screenshots |

## File Locations

- **Interface**: `EvoAITest.Core/Abstractions/IToolExecutor.cs`
- **Result**: `EvoAITest.Core/Models/ToolExecutionResult.cs`
- **Options**: `EvoAITest.Core/Options/ToolExecutorOptions.cs`

## Next Implementation Steps

1. Create `EvoAITest.Core/Services/ToolExecutor.cs`
2. Implement `IToolExecutor` interface
3. Add DI registration in `ServiceCollectionExtensions.cs`
4. Create unit tests in `EvoAITest.Tests/Services/ToolExecutorTests.cs`
5. Create integration tests

## References

- Full documentation: `TOOL_EXECUTOR_SERVICE_SUMMARY.md`
- Microsoft retry patterns: https://learn.microsoft.com/en-us/azure/architecture/patterns/retry
