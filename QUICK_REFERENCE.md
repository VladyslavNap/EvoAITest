# BrowserAI Quick Reference

## ?? Quick Start

### 1. Register Services

```csharp
// Program.cs or Startup.cs
builder.Services.AddBrowserAICore(options =>
{
    options.Headless = true;
    options.ViewportWidth = 1920;
    options.ViewportHeight = 1080;
});

builder.Services.AddLLMServices();
builder.Services.AddAgentServices();
```

### 2. Create a Task

```csharp
var task = new AgentTask
{
    Description = "Login and navigate to dashboard",
    StartUrl = "https://app.example.com/login",
    Type = TaskType.Authentication,
    Parameters = new Dictionary<string, object>
    {
        ["username"] = "user@example.com",
        ["password"] = "password123"
    }
};
```

### 3. Execute with Agent

```csharp
var agent = serviceProvider.GetRequiredService<IAgent>();
var result = await agent.ExecuteTaskAsync(task);

if (result.Success)
{
    Console.WriteLine($"Completed in {result.DurationMs}ms");
}
```

## ?? Core Types

### BrowserAction
```csharp
new BrowserAction
{
    Type = ActionType.Click,
    Target = ElementLocator.Css("button.submit"),
    TimeoutMs = 5000
}
```

### ElementLocator
```csharp
ElementLocator.Css("#login-button")
ElementLocator.XPath("//button[@type='submit']")
ElementLocator.Text("Sign In")
ElementLocator.Role("button")
ElementLocator.TestId("submit-btn")
```

### AgentStep
```csharp
new AgentStep
{
    StepNumber = 1,
    Action = new BrowserAction { /* ... */ },
    Reasoning = "Why this step",
    ExpectedOutcome = "What should happen",
    ValidationRules = new List<ValidationRule> { /* ... */ }
}
```

## ?? Common Patterns

### Navigate and Interact
```csharp
await page.NavigateAsync("https://example.com");
var element = await page.LocateAsync(ElementLocator.Css("input"));
await element.FillAsync("text");
await element.GetAttributeAsync("value");
```

### Capture State
```csharp
var state = await page.GetStateAsync();
var screenshot = await page.ScreenshotAsync(fullPage: true);
```

### Handle Errors with Healing
```csharp
try
{
    await executor.ExecuteStepAsync(step, context);
}
catch (Exception ex)
{
    var healing = await healer.HealStepAsync(step, ex, context);
    if (healing.Success)
    {
        await executor.ExecuteStepAsync(healing.HealedStep!, context);
    }
}
```

### Stream LLM Responses
```csharp
await foreach (var chunk in llmProvider.StreamCompleteAsync(request))
{
    Console.Write(chunk.Delta);
}
```

## ?? Action Types

- `Navigate` - Go to URL
- `Click` - Click element
- `Type` - Type text
- `Fill` - Fill input (clears first)
- `Select` - Select dropdown option
- `Check/Uncheck` - Checkbox control
- `Hover` - Mouse hover
- `WaitForElement` - Wait for element
- `Wait` - Fixed delay
- `Screenshot` - Capture screen
- `ExecuteScript` - Run JavaScript
- `Scroll` - Scroll page/element
- `Press` - Keyboard key
- `ExtractText` - Get text content
- `Verify` - Validate state

## ?? Locator Strategies

- `Css` - CSS selectors
- `XPath` - XPath expressions
- `Text` - Visible text content
- `Id` - Element ID
- `Role` - ARIA role
- `Label` - ARIA label
- `Placeholder` - Placeholder text
- `TestId` - data-testid attribute
- `Title` - Title attribute
- `AltText` - Image alt text

## ?? Healing Strategies

- `RetryWithDelay` - Simple retry
- `AlternativeLocator` - Try different selector
- `ExtendedWait` - Wait longer
- `ScrollToElement` - Make visible
- `PageRefresh` - Reload page
- `AIElementDiscovery` - AI finds element
- `InteractionMethodChange` - Try different method
- `PopupHandling` - Handle popups
- `TaskReplanning` - Replan with AI
- `SimpleFallback` - Simpler approach
- `Custom` - Your strategy

## ?? Task Status

- `Pending` - Waiting to start
- `Planning` - Creating plan
- `Executing` - Running steps
- `Paused` - Temporarily stopped
- `Completed` - Finished successfully
- `Failed` - Error occurred
- `Cancelled` - User cancelled

## ?? Validation Types

- `UrlPattern` - Check URL
- `ElementExists` - Element present
- `ElementText` - Text content
- `PageTitle` - Page title
- `DataExtracted` - Data captured
- `Custom` - Your validation

## ?? Statistics

```csharp
var stats = result.Statistics;
stats.TotalSteps          // Number of steps
stats.SuccessfulSteps     // Successful count
stats.FailedSteps         // Failed count
stats.HealedSteps         // Healed count
stats.SuccessRate         // 0.0 to 1.0
stats.AverageStepDurationMs  // Avg time per step
```

## ?? Best Practices

### ? DO
- Use descriptive locators
- Add validation rules
- Enable healing
- Set reasonable timeouts
- Capture screenshots on failure
- Use structured logging
- Handle cleanup in finally blocks
- Test with different browsers

### ? DON'T
- Hardcode credentials
- Use brittle selectors (nth-child)
- Ignore timeouts
- Skip error handling
- Forget to dispose resources
- Block the UI thread
- Ignore retry limits

## ?? Package References

```xml
<!-- EvoAITest.Core -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="9.0.0" />

<!-- EvoAITest.LLM -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />

<!-- EvoAITest.Agents -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
<ProjectReference Include="..\EvoAITest.Core\EvoAITest.Core.csproj" />
<ProjectReference Include="..\EvoAITest.LLM\EvoAITest.LLM.csproj" />
```

## ?? Aspire Integration

```csharp
// AppHost Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.EvoAITest_ApiService>("apiservice");

builder.AddProject<Projects.EvoAITest_Web>("web")
    .WithReference(apiService);

builder.Build().Run();
```

## ?? Docker Support

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "EvoAITest.ApiService.dll"]
```

## ?? Support

- GitHub: https://github.com/VladyslavNap/EvoAITest
- Issues: https://github.com/VladyslavNap/EvoAITest/issues
- Docs: See individual project README.md files
