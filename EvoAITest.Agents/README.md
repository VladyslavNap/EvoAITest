# BrowserAI Agents Library

AI agent orchestration for intelligent browser automation.

> ?? **[Main Documentation](../DOCUMENTATION_INDEX.md)** | ?? **[Quick Start](#quick-start)** | ?? **[Examples](../examples/LoginExample/README.md)**

---

## Overview

EvoAITest.Agents provides AI-powered agents that can plan, execute, and heal browser automation tasks. Agents use LLMs to understand tasks, create execution plans, and recover from failures.

## Quick Start

### Basic Usage

```csharp
// Inject agent via DI
public class MyService
{
    private readonly IAgent _agent;

    public MyService(IAgent agent)
    {
        _agent = agent;
    }

    public async Task AutomateTaskAsync()
    {
        var task = new AgentTask
        {
            Description = "Login and navigate to dashboard",
            StartUrl = "https://app.example.com"
        };

        var result = await _agent.ExecuteTaskAsync(task);
        Console.WriteLine($"Success: {result.Success}");
    }
}
```

---

## Key Components

### Abstractions

#### IAgent
Core agent interface:

```csharp
public interface IAgent
{
    Task<AgentTaskResult> ExecuteTaskAsync(AgentTask task, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentStep>> PlanTaskAsync(AgentTask task, CancellationToken cancellationToken = default);
    Task LearnFromFeedbackAsync(AgentFeedback feedback, CancellationToken cancellationToken = default);
}
```

#### IPlanner
Task planning and decomposition:

```csharp
var plan = await planner.CreatePlanAsync(task, context);
Console.WriteLine($"Created plan with {plan.Steps.Count} steps");
Console.WriteLine($"Estimated duration: {plan.EstimatedDurationMs}ms");
```

#### IExecutor
Step execution management:

```csharp
var result = await executor.ExecutePlanAsync(plan, context);
Console.WriteLine($"Executed {result.StepResults.Count} steps");
Console.WriteLine($"Success rate: {result.Statistics?.SuccessRate:P}");
```

`ExecutorAgent` is the default `IExecutor`. It converts `AgentStep` objects into `ToolCall`s for the resiliency-focused `IToolExecutor`, captures screenshots/validation output through `IBrowserAgent`, tracks execution statistics, and exposes pause/resume/cancel operations for long-running tasks.

#### IHealer
Self-healing capabilities:

```csharp
var healingResult = await healer.HealStepAsync(failedStep, error, context);
if (healingResult.Success)
{
    Console.WriteLine($"Applied healing: {healingResult.Strategy?.Name}");
    await executor.ExecuteStepAsync(healingResult.HealedStep!, context);
}
```

`HealerAgent` is the default `IHealer`. It inspects failed `AgentStepResult` instances, captures page state via `IBrowserAgent`, runs LLM-based diagnostics to classify the failure, then applies adaptive strategies (alternative locators, extended waits, retries, or replanning) while enforcing per-step attempt limits.

---

## Models

### AgentTask
Define high-level automation tasks:

```csharp
var task = new AgentTask
{
    Description = "Login to the application and navigate to the dashboard",
    StartUrl = "https://app.example.com",
    Type = TaskType.Authentication,
    Parameters = new Dictionary<string, object>
    {
        ["username"] = "user@example.com",
        ["password"] = "secure_password"
    },
    Expectations = new TaskExpectations
    {
        ExpectedUrl = "*/dashboard",
        ExpectedElements = new List<string> { "Welcome message", "User menu" }
    }
};
```

### AgentStep
Individual execution steps:

```csharp
var step = new AgentStep
{
    StepNumber = 1,
    Action = new BrowserAction
    {
        Type = ActionType.Fill,
        Target = ElementLocator.Css("#username"),
        Value = "user@example.com"
    },
    Reasoning = "Fill username field with provided credentials",
    ExpectedOutcome = "Username field contains the email address",
    ValidationRules = new List<ValidationRule>
    {
        new()
        {
            Type = ValidationType.ElementValue,
            Target = ElementLocator.Css("#username"),
            Expected = "user@example.com"
        }
    }
};
```

---

## Architecture

```
???????????????????
?   AgentTask     ?
?  (High-level)   ?
???????????????????
         ?
         ?
???????????????????
?    IPlanner     ?  ???? LLM-powered planning
?  (Task ? Steps) ?
???????????????????
         ?
         ?
???????????????????
?   IExecutor     ?  ???? Executes steps
?  (Steps ? Res)  ?       Validates results
???????????????????
         ?
         ?
  ????????????????
  ?              ?
Success      Failure
  ?              ?
  ?              ?
Done      ???????????????
          ?   IHealer   ?  ???? Self-healing
          ? (Diagnose)  ?       Alternative approaches
          ???????????????
```

---

## Features

### ?? AI-Powered Planning
- Natural language task understanding
- Multi-step plan generation
- Context-aware reasoning
- Adaptive re-planning

### ?? Self-Healing
- Automatic error recovery
- Alternative selector discovery
- Dynamic wait optimization
- Learning from failures

### ?? Intelligent Execution
- Screenshot capture
- Result validation
- Progress tracking
- Pause/resume/cancel support

### ?? Observability
- Detailed execution logs
- Performance metrics
- Success/failure tracking
- OpenTelemetry integration

---

## Usage Examples

### Simple Navigation

```csharp
var task = new AgentTask
{
    Description = "Navigate to the homepage and click the login button",
    StartUrl = "https://example.com"
};

var result = await agent.ExecuteTaskAsync(task);
```

### Authentication Flow

```csharp
var task = new AgentTask
{
    Description = "Login with provided credentials",
    StartUrl = "https://app.example.com/login",
    Type = TaskType.Authentication,
    Parameters = new Dictionary<string, object>
    {
        ["username"] = "user@example.com",
        ["password"] = "password123"
    }
};

var result = await agent.ExecuteTaskAsync(task);
```

### Custom Planning

```csharp
var plan = await planner.CreatePlanAsync(task, context);

// Modify plan if needed
plan.Steps.Insert(0, new AgentStep
{
    StepNumber = 0,
    Action = new BrowserAction
    {
        Type = ActionType.Navigate,
        Target = ElementLocator.Url("https://example.com")
    }
});

var result = await executor.ExecutePlanAsync(plan, context);
```

---

## Service Registration

```csharp
// In Program.cs or Startup.cs
builder.Services.AddAgentServices();

// Register implementations
builder.Services.AddSingleton<IAgent, DefaultAgent>();
builder.Services.AddSingleton<IPlanner, PlannerAgent>();
builder.Services.AddSingleton<IExecutor, ExecutorAgent>();
builder.Services.AddSingleton<IHealer, HealerAgent>();
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [Main Documentation](../DOCUMENTATION_INDEX.md) | Central documentation hub |
| [Examples](../examples/LoginExample/README.md) | Working examples |
| [Core Library](../EvoAITest.Core/README.md) | Core services and models |

---

## Dependencies

- **EvoAITest.Core** - Core models and services
- **EvoAITest.LLM** - LLM provider abstraction
- **Microsoft.Playwright** - Browser automation

---

**Version:** 1.0  
**Last Updated:** January 2026
