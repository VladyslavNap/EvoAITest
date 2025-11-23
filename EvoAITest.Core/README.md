# BrowserAI Core Library

Production-ready browser automation abstractions for the BrowserAI framework.

## Overview

EvoAITest.Core provides browser-agnostic interfaces and models for web automation. It serves as the foundation layer for AI-powered browser automation.

## Key Components

### Models

#### BrowserAction
Represents browser actions (click, type, navigate, screenshot, etc.)

```csharp
var action = new BrowserAction
{
    Type = ActionType.Click,
    Target = ElementLocator.Css("button.submit"),
    Description = "Click submit button",
    TimeoutMs = 5000
};
```

#### ElementLocator
Flexible element location with multiple strategies:

```csharp
// CSS Selector
var byCSS = ElementLocator.Css("#login-button");

// XPath
var byXPath = ElementLocator.XPath("//button[@type='submit']");

// Text Content
var byText = ElementLocator.Text("Sign In");

// ARIA Role
var byRole = ElementLocator.Role("button");

// Test ID
var byTestId = ElementLocator.TestId("submit-btn");
```

#### ExecutionResult
Rich execution results with diagnostics:

```csharp
var result = await element.ClickAsync();
if (result.Success)
{
    Console.WriteLine($"Completed in {result.DurationMs}ms");
    Console.WriteLine($"Screenshot: {result.Screenshot}");
}
```

### Abstractions

#### IBrowserDriver
Core browser driver interface supporting multiple implementations:

```csharp
public interface IBrowserDriver : IAsyncDisposable
{
    Task LaunchAsync(BrowserOptions options, CancellationToken cancellationToken = default);
    Task<IBrowserContext> CreateContextAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
}
```

#### IPageAnalyzer
AI-powered page analysis and element discovery:

```csharp
var analysis = await pageAnalyzer.AnalyzePageAsync(page);
Console.WriteLine($"Found {analysis.Forms.Count} forms");
Console.WriteLine($"Found {analysis.Navigation.Count} navigation elements");

// Find element by natural language
var locator = await pageAnalyzer.FindElementByDescriptionAsync(
    page, 
    "the blue submit button in the footer"
);
```

## Installation

Add to your project:

```bash
dotnet add reference ../EvoAITest.Core/EvoAITest.Core.csproj
```

Register services:

```csharp
builder.Services.AddEvoAITestCore(builder.Configuration);
```

> `AddEvoAITestCore` binds both `EvoAITest:Core` (Playwright agent + browser knobs) and `EvoAITest:ToolExecutor` (retry/backoff/options) so `IBrowserAgent`, `IBrowserToolRegistry`, and `IToolExecutor` are available via DI.

## Usage Examples

### Basic Page Navigation

```csharp
await using var driver = serviceProvider.GetRequiredService<IBrowserDriver>();
await driver.LaunchAsync(new BrowserOptions { Headless = true });

var context = await driver.CreateContextAsync();
var page = await context.NewPageAsync();

await page.NavigateAsync("https://example.com");
```

### Element Interaction

```csharp
var searchBox = await page.LocateAsync(ElementLocator.Css("input[type='search']"));
await searchBox.FillAsync("BrowserAI");

var searchButton = await page.LocateAsync(ElementLocator.Role("button"));
await searchButton.ClickAsync();
```

### State Capture

```csharp
var state = await page.GetStateAsync();
Console.WriteLine($"URL: {state.Url}");
Console.WriteLine($"Title: {state.Title}");
Console.WriteLine($"Interactive elements: {state.InteractiveElements.Count}");
Console.WriteLine($"Console errors: {state.ConsoleMessages.Count(m => m.Type == ConsoleMessageType.Error)}");
```

## Playwright Browser Agent

Day 6 introduced a production-ready implementation of `IBrowserAgent` powered by Playwright 1.48.0.

- **File**: `EvoAITest.Core/Browser/PlaywrightBrowserAgent.cs`
- **Capabilities**: headless Chromium launch, resilient navigation (retry-on-fail), accessibility tree capture, interactive element harvesting, HTML snapshot, and base64 screenshots.
- **DI Registration**: `AddEvoAITestCore` now registers `IBrowserAgent` → `PlaywrightBrowserAgent`, so any consumer resolving `IBrowserAgent` receives the Playwright-backed instance automatically.

### Usage

```csharp
// In Program.cs
builder.Services.AddEvoAITestCore(builder.Configuration);

// In a component/service
public class BrowserAutomationService
{
    private readonly IBrowserAgent _browser;

    public BrowserAutomationService(IBrowserAgent browser)
    {
        _browser = browser;
    }

    public async Task<PageState> CaptureAsync(string url)
    {
        await _browser.InitializeAsync();
        await _browser.NavigateAsync(url);
        var state = await _browser.GetPageStateAsync();
        await _browser.DisposeAsync();
        return state;
    }
}
```

## Tool Executor (Day 8)

Day 8 introduced a first-party tool executor that bridges the `BrowserToolRegistry` with `IBrowserAgent`, adding production-grade resiliency around every automation step.

- **Interfaces & Options**: `EvoAITest.Core/Abstractions/IToolExecutor.cs`, `EvoAITest.Core/Options/ToolExecutorOptions.cs`
- **Implementation**: `EvoAITest.Core/Services/DefaultToolExecutor.cs` + `ToolExecutorLog.cs`
- **Features**: registry validation, parameter checking, exponential backoff with jitter, transient vs terminal error classification, per-attempt timeouts, in-memory execution history, and OpenTelemetry-friendly tracing/meters.

### Usage

```csharp
public class ToolRunner
{
    private readonly IToolExecutor _toolExecutor;

    public ToolRunner(IToolExecutor toolExecutor)
    {
        _toolExecutor = toolExecutor;
    }

    public async Task<ToolExecutionResult> NavigateAsync(string url, string correlationId, CancellationToken ct)
    {
        var toolCall = new ToolCall(
            ToolName: "navigate",
            Parameters: new Dictionary<string, object> { ["url"] = url },
            Reasoning: "Open target page",
            CorrelationId: correlationId);

        return await _toolExecutor.ExecuteToolAsync(toolCall, ct);
    }
}
```

## Data Persistence (Day 12)

The core library now ships with an EF Core-backed persistence layer that stores automation tasks and their execution history for observability, auditing, and resume scenarios.

- **DbContext**: `EvoAITest.Core/Data/EvoAIDbContext.cs`
- **Entities**:
  - `AutomationTask` – includes user metadata, natural language prompt, serialized execution plan, correlation IDs, timestamps, and navigation property to executions.
  - `ExecutionHistory` – captures per-run status, duration, serialized step results, screenshots, metadata, and links back to the originating task.
- **Indexes & JSON columns**: Both entities configure indexes for high-frequency queries and store plan/step data as JSON (`nvarchar(max)`).
- **Automatic timestamps**: `SaveChangesAsync` updates `UpdatedAt` for any modified `AutomationTask`.

### Configuration

Add a connection string named `EvoAIDatabase` to your host application. `AddEvoAITestCore` detects the connection string and registers `EvoAIDbContext` (with SQL Server retries suitable for Azure SQL and LocalDB).

```json
{
  "ConnectionStrings": {
    "EvoAIDatabase": "Server=(localdb)\\mssqllocaldb;Database=EvoAITest;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### Usage

```csharp
// Program.cs
builder.Services.AddEvoAITestCore(builder.Configuration);

// In an application service
public sealed class TaskRepository
{
    private readonly EvoAIDbContext _db;

    public TaskRepository(EvoAIDbContext db) => _db = db;

    public async Task<IReadOnlyList<AutomationTask>> GetPendingTasksAsync(CancellationToken ct)
        => await _db.AutomationTasks
            .Include(t => t.Executions)
            .Where(t => t.Status == TaskStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
}
```

### Testing

`EvoAITest.Tests/Data/EvoAIDbContextTests.cs` contains 12 in-memory EF tests that verify entity configuration, cascade deletes, JSON column persistence, composite indexes, and automatic timestamp updates.

## Features

- ? Browser-agnostic design
- ? Multiple element locator strategies
- ? Rich execution diagnostics
- ? Screenshot capture
- ? DOM state extraction
- ? Console message monitoring
- ? Network request tracking
- ? Cookie management
- ? OpenTelemetry ready

## Next Steps

- Implement `IBrowserDriver` for your browser automation library (Playwright, Selenium, etc.)
- Implement `IPageAnalyzer` for intelligent page analysis
- Use with EvoAITest.Agents for AI-powered automation
