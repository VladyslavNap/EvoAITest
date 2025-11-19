# Tool Executor - Quick DI Reference

## Service Registration

### Automatic (Recommended)
```csharp
// Program.cs
builder.Services.AddEvoAITestCore(builder.Configuration);
```

This registers:
- ? `IBrowserAgent` ? `PlaywrightBrowserAgent` (scoped)
- ? `IBrowserToolRegistry` ? `DefaultBrowserToolRegistry` (singleton)
- ? `IToolExecutor` ? `DefaultToolExecutor` (scoped)

### Manual (Custom Implementation)
```csharp
builder.Services
    .AddBrowserAgent<PlaywrightBrowserAgent>()
    .AddToolExecutor<MyCustomToolExecutor>();
```

## Configuration

### appsettings.json (Production)
```json
{
  "EvoAITest": {
    "ToolExecutor": {
      "MaxRetries": 3,
      "InitialRetryDelayMs": 500,
      "MaxRetryDelayMs": 10000,
      "UseExponentialBackoff": true,
      "TimeoutPerToolMs": 30000,
      "EnableDetailedLogging": false
    }
  }
}
```

### appsettings.Development.json
```json
{
  "EvoAITest": {
    "ToolExecutor": {
      "MaxRetries": 1,
      "InitialRetryDelayMs": 200,
      "TimeoutPerToolMs": 10000,
      "EnableDetailedLogging": true
    }
  }
}
```

### Environment Variables
```bash
EVOAITEST__TOOLEXECUTOR__MAXRETRIES=5
EVOAITEST__TOOLEXECUTOR__ENABLEDETAILEDLOGGING=false
```

## Dependency Injection

### API Controller
```csharp
public class AutomationController : ControllerBase
{
    private readonly IToolExecutor _toolExecutor;
    
    public AutomationController(IToolExecutor toolExecutor)
    {
        _toolExecutor = toolExecutor;
    }
}
```

### Blazor Component
```razor
@inject IToolExecutor ToolExecutor

@code {
    private async Task ExecuteTool()
    {
        var result = await ToolExecutor.ExecuteToolAsync(toolCall);
    }
}
```

### Background Service
```csharp
public class MyBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IToolExecutor>();
        
        // Use executor...
    }
}
```

## Common Usage Patterns

### Execute Single Tool
```csharp
var toolCall = new ToolCall(
    ToolName: "navigate",
    Parameters: new Dictionary<string, object> { ["url"] = url },
    Reasoning: "Navigate to page",
    CorrelationId: correlationId
);

var result = await _toolExecutor.ExecuteToolAsync(toolCall, ct);

if (result.Success)
{
    // Success
}
```

### Execute Sequence
```csharp
var tools = new[]
{
    new ToolCall("navigate", new { url = "..." }, "", id),
    new ToolCall("click", new { selector = "#btn" }, "", id)
};

var results = await _toolExecutor.ExecuteSequenceAsync(tools, ct);
var allSucceeded = results.All(r => r.Success);
```

### With Fallbacks
```csharp
var primary = new ToolCall("click", new { selector = "#btn" }, "", id);
var fallbacks = new[]
{
    new ToolCall("click", new { selector = ".btn" }, "", id),
    new ToolCall("click", new { force = true, selector = "#btn" }, "", id)
};

var result = await _toolExecutor.ExecuteWithFallbackAsync(primary, fallbacks, ct);
```

## Lifetime Scopes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IToolExecutor` | Scoped | Per-request/workflow history |
| `IBrowserAgent` | Scoped | Browser instance per session |
| `IBrowserToolRegistry` | Singleton | Static tool metadata |

## Project References Required

### API Service
```xml
<ProjectReference Include="..\EvoAITest.Core\EvoAITest.Core.csproj" />
```

### Web (Blazor)
```xml
<ProjectReference Include="..\EvoAITest.Core\EvoAITest.Core.csproj" />
```

## Verification

### Check DI Registration
```csharp
[HttpGet("health/executor")]
public IActionResult CheckExecutor([FromServices] IToolExecutor executor)
{
    return Ok(new { registered = executor != null });
}
```

### Check Configuration
```csharp
[HttpGet("config/executor")]
public IActionResult GetConfig([FromServices] IOptions<ToolExecutorOptions> options)
{
    var config = options.Value;
    return Ok(config);
}
```

## Troubleshooting

**Error:** `IToolExecutor could not be found`  
**Fix:** Add `using EvoAITest.Core.Extensions;` and ensure project reference exists

**Error:** `Configuration section not found`  
**Fix:** Verify structure: `EvoAITest:ToolExecutor` (not `EvoAITest:Core:ToolExecutor`)

**Error:** `Validation failed on startup`  
**Fix:** Check configuration values meet minimums (MaxRetries ? 0, TimeoutPerToolMs ? 5000)

## Files Modified

- ? `EvoAITest.Core/Extensions/ServiceCollectionExtensions.cs`
- ? `EvoAITest.ApiService/Program.cs`
- ? `EvoAITest.Web/Program.cs`
- ? `EvoAITest.ApiService/appsettings.json`
- ? `EvoAITest.ApiService/appsettings.Development.json`
- ? `EvoAITest.Web/appsettings.json`
- ? `EvoAITest.Web/appsettings.Development.json`
- ? `EvoAITest.ApiService/EvoAITest.ApiService.csproj`
- ? `EvoAITest.Web/EvoAITest.Web.csproj`

## Next Steps

1. Create API endpoints using `IToolExecutor`
2. Create Blazor components for browser automation UI
3. Add integration tests for DI resolution
4. Deploy to Aspire and test in containers
