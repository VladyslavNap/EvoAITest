# BrowserAI Agents Library

AI agent orchestration for intelligent browser automation.

## Overview

EvoAITest.Agents provides AI-powered agents that can plan, execute, and heal browser automation tasks. Agents use LLMs to understand tasks, create execution plans, and recover from failures.

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

### Models

#### AgentTask
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

#### AgentStep
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
            Type = ValidationType.ElementText,
            ExpectedValue = "user@example.com"
        }
    }
};
```

#### HealingStrategy
Define error recovery strategies:

```csharp
var strategy = new HealingStrategy
{
    Name = "Alternative Locator",
    Type = HealingStrategyType.AlternativeLocator,
    Description = "Try finding the element using text content instead",
    Confidence = 0.8,
    Parameters = new Dictionary<string, object>
    {
        ["locator"] = ElementLocator.Text("Submit")
    }
};
```

## Installation

Add to your project:

```bash
dotnet add reference ../EvoAITest.Agents/EvoAITest.Agents.csproj
```

Register services:

```csharp
builder.Services.AddAgentServices();
builder.Services.AddPlanner<PlannerAgent>();   // optional override
builder.Services.AddExecutor<ExecutorAgent>(); // optional override
builder.Services.AddHealer<HealerAgent>();     // optional override
```

`AddAgentServices()` wires up the PlannerAgent + ExecutorAgent + HealerAgent trio out of the box, so applications immediately benefit from planning, execution, and self-healing orchestration.

### Chain-of-Thought & Visualization

- Planner emits `ExecutionPlan.ThoughtProcess` (list of reasoning strings) plus optional `PlanVisualization` metadata (`dot`, `json`, etc.).
- `PlanVisualizationService` (in `EvoAITest.Agents/Services`) can render Graphviz/Mermaid/JSON graphs for dashboards or docs.
- Chain-of-thought includes goals, requirements, ordered reasoning steps, risks, and dependencies so operators understand why each action exists.

```json
{
  "thought_process": [
    "Identify goal: authenticate user and confirm dashboard.",
    "Need to navigate, wait for login form, submit credentials, verify KPI widgets."
  ],
  "plan": [
    { "order": 1, "action": "navigate", "value": "https://example.com/login" },
    { "order": 2, "action": "wait_for_element", "selector": "#username" }
  ],
  "visualization": {
    "format": "dot",
    "content": "digraph Plan { 1 -> 2; }"
  }
}
```

Plan consumers (Executor, UI, docs) can persist both the structured steps and the reasoning to give engineers full traceability.

## Usage Examples

### Execute a Task

```csharp
var agent = serviceProvider.GetRequiredService<IAgent>();

var task = new AgentTask
{
    Description = "Search for 'AI automation' and extract first 5 results",
    StartUrl = "https://www.google.com",
    Type = TaskType.Search,
    Parameters = new Dictionary<string, object>
    {
        ["query"] = "AI automation",
        ["maxResults"] = 5
    }
};

var result = await agent.ExecuteTaskAsync(task);

if (result.Success)
{
    Console.WriteLine("Task completed successfully!");
    foreach (var (key, value) in result.ExtractedData)
    {
        Console.WriteLine($"{key}: {value}");
    }
}
else
{
    Console.WriteLine($"Task failed: {result.ErrorMessage}");
}
```

### Plan Without Execution

```csharp
var steps = await agent.PlanTaskAsync(task);

Console.WriteLine("Execution Plan:");
foreach (var step in steps)
{
    Console.WriteLine($"{step.StepNumber}. {step.Action?.Type}");
    Console.WriteLine($"   Reasoning: {step.Reasoning}");
}
```

### Manual Step Execution with Healing

```csharp
var executor = serviceProvider.GetRequiredService<IExecutor>();
var healer = serviceProvider.GetRequiredService<IHealer>();

foreach (var step in plan.Steps)
{
    var result = await executor.ExecuteStepAsync(step, context);
    
    if (!result.Success && result.Error != null)
    {
        var analysis = await healer.AnalyzeErrorAsync(result.Error, context);
        
        if (analysis.IsHealable)
        {
            var healingResult = await healer.HealStepAsync(step, result.Error, context);
            
            if (healingResult.Success)
            {
                result = await executor.ExecuteStepAsync(healingResult.HealedStep!, context);
            }
        }
    }
}
```

### Learn from Feedback

```csharp
var feedback = new AgentFeedback
{
    TaskId = result.TaskId,
    Success = false,
    Error = "Element selector became stale",
    Suggestions = new List<string>
    {
        "Use more resilient selectors",
        "Add explicit waits before interaction"
    }
};

await agent.LearnFromFeedbackAsync(feedback);
```

## Healing Strategies

Built-in healing strategies:

1. **RetryWithDelay** - Simple retry with exponential backoff
2. **AlternativeLocator** - Try different element selection strategies
3. **ExtendedWait** - Wait longer for dynamic content
4. **ScrollToElement** - Make elements visible before interaction
5. **PageRefresh** - Reload the page and retry
6. **AIElementDiscovery** - Use LLM to find alternative elements
7. **InteractionMethodChange** - Try JavaScript click instead of native click
8. **PopupHandling** - Detect and dismiss unexpected popups
9. **TaskReplanning** - Create a new plan with LLM
10. **SimpleFallback** - Use simpler, more reliable approach

## Agent Capabilities

Configure what your agent can do:

```csharp
var capabilities = new AgentCapabilities
{
    CanFillForms = true,
    CanNavigate = true,
    CanExtractData = true,
    CanAuthenticate = true,
    CanSelfHeal = true,
    MaxComplexity = TaskComplexity.Advanced,
    SupportedBrowsers = new List<string> { "chromium", "firefox" }
};
```

## Task Constraints

Control task execution:

```csharp
var constraints = new TaskConstraints
{
    MaxRetries = 3,
    MaxSteps = 50,
    AllowedDomains = new List<string> { "example.com", "api.example.com" },
    BlockedDomains = new List<string> { "ads.example.com" },
    AllowExternalLinks = false,
    EnableHealing = true
};
```

## Statistics and Monitoring

Track execution metrics:

```csharp
var stats = result.Statistics;
Console.WriteLine($"Total steps: {stats.TotalSteps}");
Console.WriteLine($"Successful: {stats.SuccessfulSteps}");
Console.WriteLine($"Failed: {stats.FailedSteps}");
Console.WriteLine($"Healed: {stats.HealedSteps}");
Console.WriteLine($"Success rate: {stats.SuccessRate:P}");
Console.WriteLine($"Avg step duration: {stats.AverageStepDurationMs}ms");
```

## Features

- ? AI-powered task planning
- ? Multi-step execution
- ? Self-healing on errors
- ? Learning from feedback
- ? Validation rules
- ? Retry with backoff
- ? Rich execution statistics
- ? Screenshot capture
- ? Comprehensive error handling

### ExecutorAgent Highlights
- Pause/resume/cancel APIs with thread-safe task state tracking.
- Automatic translation from planner actions to registry-backed tool calls (navigate, click, type, wait, verify, extract).
- Evidence collection on every failure plus a final screenshot for the task summary.
- Built-in validation runners (element exists, text, page title, data extracted) with per-rule telemetry.
- Execution statistics surfaced via `ExecutionStatistics` for dashboards and health checks.

### HealerAgent Highlights
- LLM-powered diagnostics classify failure types (selectors, timeouts, navigation, authentication, structure changes) with severity and recommended strategies.
- Captures real-time page state before proposing fixes, enabling accurate locator suggestions or scroll/refresh adjustments.
- Adaptive strategy engine supports retries with backoff, alternative locators, extended waits, replanning hooks, and manual escalation guidance.
- Max-healing-attempt enforcement (default 3 per step) prevents infinite loops while surfacing actionable telemetry.
- 25 unit tests validate error analysis, cancellation, malformed LLM output handling, and alternative strategy suggestions.

## Next Steps

- Implement agents for your specific use cases
- Create custom healing strategies
- Build domain-specific planners
- Integrate with monitoring systems
