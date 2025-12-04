# PlannerAgent Implementation - Day 9 Complete ?

## Overview

The `PlannerAgent` is an AI-powered planning agent that converts natural language task descriptions into structured, executable browser automation plans. It leverages Large Language Models (Azure OpenAI GPT-5 in production, Ollama for local development) to intelligently break down complex automation tasks into ordered sequences of browser actions.

## Key Features

? **Natural Language to Execution Plan**: Converts user-friendly task descriptions into concrete browser automation steps  
? **LLM Integration**: Supports Azure OpenAI GPT-5 and Ollama with function calling  
? **Chain-of-Thought Reasoning**: Captures the planner's step-by-step rationale (`ExecutionPlan.ThoughtProcess`) for auditing and explainability  
? **Visualization Output**: Optional Graphviz DOT / JSON graphs (`ExecutionPlan.Visualization`) describing step dependencies for dashboards  
? **Intelligent Tool Selection**: Automatically selects appropriate browser tools from the registry  
? **Plan Validation**: Comprehensive validation of generated plans  
? **Plan Refinement**: Adapts plans based on execution feedback  
? **Error Handling**: Robust error handling with detailed logging  
? **Cancellation Support**: Graceful shutdown with cancellation tokens for Aspire  

## Architecture

### File Location
```
EvoAITest.Agents/
  Agents/
    PlannerAgent.cs       # Main implementation
  Abstractions/
    IPlanner.cs           # Interface definition
  Extensions/
    ServiceCollectionExtensions.cs  # DI registration
```

### Dependencies
- **EvoAITest.LLM**: LLM provider abstraction and implementations
- **EvoAITest.Core**: Browser tool registry and models
- **Microsoft.Extensions.Logging**: Structured logging

## Implementation Details

### Class: `PlannerAgent`

Implements `IPlanner` interface with three main responsibilities:

1. **CreatePlanAsync**: Generate initial execution plan from task description
2. **RefinePlanAsync**: Refine plans based on execution feedback
3. **ValidatePlanAsync**: Validate plan structure and consistency

### Constructor

```csharp
public PlannerAgent(
    ILLMProvider llmProvider,
    IBrowserToolRegistry toolRegistry,
    ILogger<PlannerAgent> logger)
```

**Parameters:**
- `llmProvider`: LLM provider (Azure OpenAI or Ollama)
- `toolRegistry`: Registry of available browser automation tools
- `logger`: Logger for diagnostics and telemetry

## Usage Examples

### 1. Basic Plan Creation

```csharp
// Inject the planner via DI
var planner = serviceProvider.GetRequiredService<IPlanner>();

// Define the task
var task = new AgentTask
{
    Description = "Login to example.com with username 'test@example.com' and password 'SecurePass123'",
    StartUrl = "https://example.com/login",
    Type = TaskType.Authentication,
    Parameters = new Dictionary<string, object>
    {
        ["username"] = "test@example.com",
        ["password"] = "SecurePass123"
    }
};

// Create execution context
var context = new ExecutionContext
{
    SessionId = Guid.NewGuid().ToString()
};

// Generate the plan
var plan = await planner.CreatePlanAsync(task, context, cancellationToken);

// Inspect the generated plan
Console.WriteLine($"Plan ID: {plan.Id}");
Console.WriteLine($"Steps: {plan.Steps.Count}");
Console.WriteLine($"Estimated duration: {plan.EstimatedDurationMs}ms");
Console.WriteLine($"Confidence: {plan.Confidence:P1}");

foreach (var step in plan.Steps)
{
    Console.WriteLine($"  {step.StepNumber}. {step.Action?.Type}");
    Console.WriteLine($"     Reasoning: {step.Reasoning}");
    Console.WriteLine($"     Expected: {step.ExpectedOutcome}");
}
```

**Example Output:**
```
Plan ID: plan-abc123
Steps: 5
Estimated duration: 8500ms
Confidence: 92.0%
  1. Navigate
     Reasoning: Navigate to the login page
     Expected: Login page loads successfully
  2. WaitForElement
     Reasoning: Wait for username field to be visible
     Expected: Username field is ready for input
  3. Type
     Reasoning: Enter the username
     Expected: Username is filled in the field
  4. Type
     Reasoning: Enter the password
     Expected: Password is masked in the field
  5. Click
    Reasoning: Submit the login form
     Expected: User is logged in and redirected to dashboard

### Chain-of-Thought Response Format

Every LLM response now includes a reasoning section plus the structured plan and optional visualization:

```json
{
  "thought_process": [
    "Goal: authenticate user and verify dashboard KPIs.",
    "Need to navigate, wait for form, submit credentials, confirm widgets."
  ],
  "plan": [
    {
      "order": 1,
      "action": "navigate",
      "value": "https://example.com/login",
      "reasoning": "Start on the login page",
      "expected_result": "Login form visible",
      "depends_on": []
    }
  ],
  "visualization": {
    "format": "dot",
    "content": "digraph Plan { step1 -> step2; }"
  }
}
```

The PlannerAgent stores `thought_process` entries in `ExecutionPlan.ThoughtProcess` and visualization metadata in `ExecutionPlan.Visualization`. Downstream services (Executor, dashboards) can persist both for explainability or diagrams.

#### Rendering Visualizations

`EvoAITest.Agents/Services/PlanVisualizationService.cs` can transform plans + chain-of-thought metadata into Graphviz, Mermaid, PlantUML, JSON, or D3-friendly graphs:

```csharp
var vizService = new PlanVisualizationService();
var graph = vizService.GenerateGraph(plan, GraphFormat.Mermaid);
Console.WriteLine(graph.Content);
```

### 2. Complex Task with Expectations

```csharp
var task = new AgentTask
{
    Description = "Search for 'AI automation' on Google and extract first 5 results",
    StartUrl = "https://www.google.com",
    Type = TaskType.Search,
    Parameters = new Dictionary<string, object>
    {
        ["query"] = "AI automation",
        ["max_results"] = 5
    },
    Expectations = new TaskExpectations
    {
        ExpectedUrl = "*/search?q=*",
        ExpectedDataFields = new List<string> { "title", "url", "snippet" },
        ExpectedElements = new List<string> { "search results", "result links" }
    }
};

var plan = await planner.CreatePlanAsync(task, context);
```

### 3. Plan Validation

```csharp
// Generate plan
var plan = await planner.CreatePlanAsync(task, context);

// Validate the plan before execution
var validation = await planner.ValidatePlanAsync(plan);

if (!validation.IsValid)
{
    Console.WriteLine("Plan validation failed!");
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"  ERROR: {error}");
    }
}

// Check warnings
if (validation.Warnings.Count > 0)
{
    Console.WriteLine("Plan warnings:");
    foreach (var warning in validation.Warnings)
    {
        Console.WriteLine($"  WARNING: {warning}");
    }
}

// Review suggestions
if (validation.Suggestions.Count > 0)
{
    Console.WriteLine("Suggestions for improvement:");
    foreach (var suggestion in validation.Suggestions)
    {
        Console.WriteLine($"  ?? {suggestion}");
    }
}
```

### 4. Plan Refinement After Failure

```csharp
// Execute the plan
var result = await executor.ExecutePlanAsync(plan, context);

// If execution failed, refine the plan
if (!result.Success)
{
    Console.WriteLine("Execution failed, refining plan...");
    
    var refinedPlan = await planner.RefinePlanAsync(
        plan,
        result.StepResults,
        cancellationToken);
    
    Console.WriteLine($"Original steps: {plan.Steps.Count}");
    Console.WriteLine($"Refined steps: {refinedPlan.Steps.Count}");
    
    // Retry with refined plan
    result = await executor.ExecutePlanAsync(refinedPlan, context);
}
```

### 5. Monitoring and Telemetry

```csharp
var plan = await planner.CreatePlanAsync(task, context);

// Access plan metadata
var metadata = plan.Metadata;
Console.WriteLine($"Correlation ID: {metadata["correlation_id"]}");
Console.WriteLine($"LLM Model: {metadata["llm_model"]}");
Console.WriteLine($"Input Tokens: {metadata["llm_tokens_input"]}");
Console.WriteLine($"Output Tokens: {metadata["llm_tokens_output"]}");
Console.WriteLine($"Cost: ${metadata["llm_cost_usd"]}");
Console.WriteLine($"Step Count: {metadata["step_count"]}");
```

## LLM Integration

### System Prompt

The PlannerAgent uses a comprehensive system prompt that:
- Defines the agent's role as a browser automation expert
- Lists available tool categories
- Provides guidelines for step generation
- Specifies response format (JSON)

### Function Calling

The agent leverages LLM function calling to:
- Present available browser tools as callable functions
- Receive structured tool calls in responses
- Map tool calls to `BrowserAction` objects

### Response Parsing

The agent parses LLM responses expecting JSON format:

```json
{
  "steps": [
    {
      "order": 1,
      "action": "navigate",
      "selector": "",
      "value": "https://example.com",
      "reasoning": "Navigate to the target website",
      "expected_result": "Page loads successfully"
    },
    {
      "order": 2,
      "action": "click",
      "selector": "#button",
      "value": "",
      "reasoning": "Click the button",
      "expected_result": "Action completes"
    }
  ]
}
```

## Plan Structure

### ExecutionPlan

```csharp
public sealed class ExecutionPlan
{
    public string Id { get; set; }                      // Unique plan identifier
    public string TaskId { get; set; }                  // Associated task ID
    public List<AgentStep> Steps { get; set; }          // Ordered execution steps
    public long EstimatedDurationMs { get; set; }       // Estimated execution time
    public double Confidence { get; set; }              // Plan confidence (0-1)
    public List<ExecutionPlan> Alternatives { get; set; } // Alternative plans
    public Dictionary<string, object> Metadata { get; set; } // Telemetry data
    public DateTimeOffset CreatedAt { get; set; }       // Creation timestamp
}
```

### AgentStep

```csharp
public sealed class AgentStep
{
    public string Id { get; set; }                      // Step identifier
    public int StepNumber { get; set; }                 // Sequence number
    public BrowserAction? Action { get; set; }          // Browser action to execute
    public string? Reasoning { get; set; }              // Why this step
    public string? ExpectedOutcome { get; set; }        // What should happen
    public List<string> Dependencies { get; set; }      // Step dependencies
    public int TimeoutMs { get; set; }                  // Step timeout
    public bool IsOptional { get; set; }                // Optional flag
    public RetryConfiguration? RetryConfig { get; set; } // Retry settings
    public List<ValidationRule> ValidationRules { get; set; } // Validation rules
}
```

## Validation Rules

The `ValidatePlanAsync` method checks:

1. **Plan Structure**
   - Plan must contain at least one step
   - Warns if plan has > 100 steps (excessive)

2. **Step Sequence**
   - Step numbers must be sequential (1, 2, 3...)
   - Each step must have an action

3. **Tool Existence**
   - All referenced tools must exist in the registry

4. **Required Parameters**
   - Navigate actions must have a URL
   - Click/Type actions must have a target selector

5. **Timeouts**
   - Warns if timeouts exceed 5 minutes

6. **Confidence**
   - Suggests improvements if confidence < 0.7

## Error Handling

### Handled Errors

1. **TaskCanceledException**: Graceful cancellation support
2. **JsonException**: Malformed LLM responses
3. **InvalidOperationException**: Various validation failures
4. **LLM Failures**: Timeout, rate limits, invalid responses

### Error Logging

All errors are logged with:
- Correlation IDs for tracing
- Full exception details
- Context information (task ID, plan ID, etc.)

Example log output:
```
[Error] Failed to create plan for task task-123: LLM returned malformed JSON
  Correlation ID: corr-456
  Task Description: Login to example.com
  LLM Model: gpt-5
  Error: Unexpected character at position 42
```

## Performance Considerations

### Duration Estimation

The planner estimates execution time based on action types:
- Navigate: ~3000ms
- Click: ~500ms
- Type: ~1000ms
- WaitForElement: ~2000ms
- Screenshot: ~800ms
- ExtractText: ~500ms
- Default: ~1000ms

### Confidence Calculation

Confidence is calculated based on:
- Plan length (very short or very long reduces confidence)
- Presence of reasoning for each step
- Presence of expected outcomes
- Result: Score between 0.0 and 1.0

### Token Usage Tracking

Every LLM call tracks:
- Input tokens
- Output tokens
- Estimated cost (USD)
- Model used

## Configuration

### Dependency Injection

Register the PlannerAgent in `Program.cs` or `Startup.cs`:

```csharp
// Register all agent services (includes PlannerAgent)
builder.Services.AddAgentServices();

// Or register individually
builder.Services.AddPlanner<PlannerAgent>();
```

### LLM Provider Configuration

```json
{
  "EvoAITest": {
    "LLM": {
      "Provider": "AzureOpenAI",  // or "Ollama"
      "AzureOpenAI": {
        "Endpoint": "https://your-endpoint.openai.azure.com",
        "DeploymentName": "gpt-5",
        "ApiKey": "your-api-key"
      },
      "Ollama": {
        "BaseUrl": "http://localhost:11434",
        "Model": "qwen2.5-7b"
      }
    }
  }
}
```

## Testing

### Unit Test Structure

```csharp
[Fact]
public async Task CreatePlanAsync_WithValidTask_GeneratesExecutionPlan()
{
    // Arrange: Setup mocks for ILLMProvider and IBrowserToolRegistry
    var mockLLM = new Mock<ILLMProvider>();
    var mockRegistry = new Mock<IBrowserToolRegistry>();
    var planner = new PlannerAgent(mockLLM.Object, mockRegistry.Object, logger);
    
    // Act: Call CreatePlanAsync
    var plan = await planner.CreatePlanAsync(task, context);
    
    // Assert: Verify plan structure
    Assert.NotNull(plan);
    Assert.Equal(5, plan.Steps.Count);
    Assert.True(plan.Confidence > 0.8);
}
```

### Integration Testing

For integration tests with real LLMs:

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task CreatePlanAsync_WithRealLLM_GeneratesValidPlan()
{
    // Use real LLM provider
    var llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();
    var toolRegistry = serviceProvider.GetRequiredService<IBrowserToolRegistry>();
    var planner = new PlannerAgent(llmProvider, toolRegistry, logger);
    
    var task = new AgentTask
    {
        Description = "Login to example.com",
        StartUrl = "https://example.com/login"
    };
    
    var plan = await planner.CreatePlanAsync(task, context);
    
    // Validate plan
    var validation = await planner.ValidatePlanAsync(plan);
    Assert.True(validation.IsValid);
}
```

## Logging

### Log Levels

- **Information**: Plan creation start/complete, refinement, validation results
- **Debug**: LLM request/response details, token usage, step parsing
- **Warning**: LLM issues, missing steps, validation warnings
- **Error**: Exceptions, failures, JSON parsing errors

### Example Logs

```
[Information] Starting plan creation for task task-abc: Login to example.com [CorrelationId: corr-123]
[Debug] Sending planning request to LLM: gpt-5. Tools available: 13. Prompt length: 542 characters
[Debug] LLM response received. Tokens: 150 in, 287 out, $0.0052 cost
[Debug] Parsed 5 steps from LLM response
[Information] Plan created successfully for task task-abc. Steps: 5, Estimated duration: 8500ms, Confidence: 92.0%, Planning time: 1243ms
```

## Best Practices

### ? DO

1. **Provide Clear Task Descriptions**: The more specific, the better
2. **Use Task Parameters**: Pass structured data in `Parameters` dictionary
3. **Set Expectations**: Define expected outcomes for validation
4. **Validate Plans**: Always validate before execution
5. **Monitor Token Usage**: Track costs via metadata
6. **Handle Cancellation**: Use cancellation tokens properly
7. **Refine on Failure**: Use `RefinePlanAsync` for adaptive behavior

### ? DON'T

1. **Don't Skip Validation**: Always validate generated plans
2. **Don't Ignore Confidence**: Low confidence plans may be unreliable
3. **Don't Execute Without Context**: Provide proper `ExecutionContext`
4. **Don't Ignore Warnings**: Review and address validation warnings
5. **Don't Block**: Use async/await properly
6. **Don't Hardcode Models**: Use configuration for LLM models

## Troubleshooting

### Issue: LLM Returns Malformed JSON

**Symptoms**: `JsonException` thrown during plan creation

**Solutions**:
1. Check LLM response format configuration
2. Verify `ResponseFormat.Type = "json_object"` is set
3. Review system prompt for clarity
4. Try with different temperature settings

### Issue: No Steps Generated

**Symptoms**: Plan contains zero steps

**Solutions**:
1. Make task description more specific
2. Check if LLM is available (`IsAvailableAsync`)
3. Review LLM logs for timeout or rate limit issues
4. Verify tool registry is populated

### Issue: Low Plan Confidence

**Symptoms**: `Confidence < 0.7`

**Solutions**:
1. Provide more detailed task descriptions
2. Add task parameters for context
3. Define clear expectations
4. Review generated steps for completeness

### Issue: Plan Validation Fails

**Symptoms**: `ValidatePlanAsync` returns `IsValid = false`

**Solutions**:
1. Review validation errors
2. Check tool names against registry
3. Ensure required parameters are present
4. Verify step sequence is logical

## Roadmap / Future Enhancements

- [ ] Alternative plan generation (stored in `ExecutionPlan.Alternatives`)
- [ ] Plan caching and reuse for similar tasks
- [ ] Learning from execution history
- [ ] Multi-agent collaboration for complex planning
- [ ] Streaming plan generation for long tasks
- [ ] Visual plan preview/debugging
- [ ] Plan templates for common scenarios

## Contributing

When modifying PlannerAgent:

1. Maintain backward compatibility with `IPlanner` interface
2. Add comprehensive unit tests for new features
3. Update XML documentation
4. Log important events for debugging
5. Handle cancellation tokens properly
6. Consider token costs when changing prompts

## Related Documentation

- [IPlanner Interface](../Abstractions/IPlanner.cs)
- [BrowserToolRegistry](../../EvoAITest.Core/Models/BrowserToolRegistry.cs)
- [LLM Providers](../../EvoAITest.LLM/README.md)
- [Agent Models](../Models/README.md)

## Status: ? COMPLETE

Implementation complete and tested:
- ? Core planning logic
- ? LLM integration with function calling
- ? Plan validation
- ? Plan refinement
- ? Error handling
- ? Logging and telemetry
- ? Cancellation support
- ? Service registration
- ? XML documentation

Build successful with zero errors. Ready for integration with executor and healer agents! ??
