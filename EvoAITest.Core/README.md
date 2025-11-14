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
builder.Services.AddBrowserAICore(options =>
{
    options.Headless = true;
    options.ViewportWidth = 1920;
    options.ViewportHeight = 1080;
});
```

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
