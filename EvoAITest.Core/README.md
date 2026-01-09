# BrowserAI Core Library

Production-ready browser automation abstractions for the BrowserAI framework.

> 📚 **[Main Documentation](../DOCUMENTATION_INDEX.md)** | 🎬 **[Recording Feature](../docs/RECORDING_FEATURE.md)** | 🤖 **[Agents](../EvoAITest.Agents/README.md)**

---

## Overview

EvoAITest.Core provides browser-agnostic interfaces and models for web automation. It serves as the foundation layer for AI-powered browser automation, including the test recording and generation feature.

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

---

## Features

### 🎬 Test Recording & Generation

The Core library provides foundational models and services for test recording:

**Models** (`EvoAITest.Core/Models/Recording`):
- `RecordingSession` - Recording session management
- `UserInteraction` - Captured user actions
- `GeneratedTest` - AI-generated test code
- `RecordingConfiguration` - Recording settings
- `TestGenerationOptions` - Code generation options

**Services** (`EvoAITest.Core/Services/Recording`):
- `BrowserRecordingService` - Orchestrates recording lifecycle
- `InteractionNormalizer` - Cleans and standardizes interactions
- `PlaywrightEventListener` - Captures browser events

**Repository** (`EvoAITest.Core/Repositories`):
- `RecordingRepository` - Database persistence with EF Core

📖 **[Complete Recording Documentation](../docs/RECORDING_FEATURE.md)**

---

## Installation

Add to your project:

```bash
dotnet add reference ../EvoAITest.Core/EvoAITest.Core.csproj
```

Register services:

```csharp
builder.Services.AddEvoAITestCore(builder.Configuration);
```

---

## Database

The Core library includes Entity Framework Core configuration for:

- **Recording Sessions** - Store and manage test recordings
- **Recorded Interactions** - Captured user actions
- **Error Recovery History** - Self-healing tracking
- **Visual Regression Baselines** - Screenshot comparisons

### Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName --project EvoAITest.Core

# Update database
dotnet ef database update --project EvoAITest.Core
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [Main Documentation](../DOCUMENTATION_INDEX.md) | Central documentation hub |
| [Recording Feature](../docs/RECORDING_FEATURE.md) | Test recording and generation |
| [Architecture](../docs/ARCHITECTURE.md) | Technical architecture details |
| [API Reference](../docs/API_REFERENCE.md) | REST API documentation |

---

## Dependencies

- **Entity Framework Core** - Database access
- **Microsoft.Playwright** - Browser automation
- **System.Text.Json** - JSON serialization

---

**Version:** 1.0  
**Last Updated:** January 2026
