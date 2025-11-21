using System.Text.Json;
using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Models;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;
using AgentExecutionContext = EvoAITest.Agents.Abstractions.ExecutionContext;

namespace EvoAITest.Agents.Agents;

/// <summary>
/// AI-powered planner agent that converts natural language tasks into structured execution plans.
/// Uses LLM (GPT-5 or Ollama) with function calling to generate browser automation steps.
/// </summary>
/// <remarks>
/// <para>
/// The PlannerAgent is responsible for the first phase of the agent lifecycle: planning.
/// It takes a natural language task description and converts it into a concrete, executable
/// plan consisting of ordered browser automation steps.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var planner = serviceProvider.GetRequiredService&lt;IPlanner&gt;();
/// 
/// var task = new AgentTask
/// {
///     Description = "Login to example.com with username 'test@example.com' and password 'SecurePass123'",
///     StartUrl = "https://example.com/login"
/// };
/// 
/// var plan = await planner.CreatePlanAsync(task, context);
/// 
/// foreach (var step in plan.Steps)
/// {
///     Console.WriteLine($"Step {step.StepNumber}: {step.Action?.Type} - {step.Reasoning}");
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class PlannerAgent : IPlanner
{
    private readonly ILLMProvider _llmProvider;
    private readonly IBrowserToolRegistry _toolRegistry;
    private readonly ILogger<PlannerAgent> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlannerAgent"/> class.
    /// </summary>
    /// <param name="llmProvider">The LLM provider for generating plans (Azure OpenAI GPT-5 or Ollama).</param>
    /// <param name="toolRegistry">The browser tool registry containing available automation tools.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public PlannerAgent(
        ILLMProvider llmProvider,
        IBrowserToolRegistry toolRegistry,
        ILogger<PlannerAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ExecutionPlan> CreatePlanAsync(
        AgentTask task,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = Guid.NewGuid().ToString();
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Starting plan creation for task {TaskId}: {TaskDescription} [CorrelationId: {CorrelationId}]",
            task.Id, task.Description, correlationId);

        try
        {
            // Build the system prompt that defines the agent's role
            var systemPrompt = BuildSystemPrompt();

            // Build the user prompt with task details and available tools
            var userPrompt = BuildUserPrompt(task, context);

            // Get available tools in LLM-compatible format
            var availableTools = ConvertToolsForLLM();

            _logger.LogDebug(
                "Sending planning request to LLM: {Model}. Tools available: {ToolCount}. Prompt length: {PromptLength} characters",
                _llmProvider.GetModelName(), availableTools.Count, userPrompt.Length);

            // Create LLM request with function calling support
            var llmRequest = new LLMRequest
            {
                Model = _llmProvider.GetModelName(),
                Messages = new List<Message>
                {
                    new() { Role = MessageRole.System, Content = systemPrompt },
                    new() { Role = MessageRole.User, Content = userPrompt }
                },
                Temperature = 0.7,
                MaxTokens = 4000,
                Functions = availableTools,
                ResponseFormat = new ResponseFormat { Type = "json_object" }
            };

            // Call LLM to generate the plan
            var response = await _llmProvider.CompleteAsync(llmRequest, cancellationToken);

            if (response.Choices.Count == 0)
            {
                throw new InvalidOperationException("LLM returned no choices in response");
            }

            var tokenUsage = _llmProvider.GetLastTokenUsage();
            _logger.LogDebug(
                "LLM response received. Tokens: {InputTokens} in, {OutputTokens} out, ${Cost:F4} cost",
                tokenUsage.InputTokens, tokenUsage.OutputTokens, tokenUsage.EstimatedCostUSD);

            // Parse the LLM response into ExecutionStep objects
            var steps = ParseLLMResponseToSteps(response.Content, correlationId);

            if (steps.Count == 0)
            {
                _logger.LogWarning("LLM returned no execution steps. Response: {Response}", response.Content);
                throw new InvalidOperationException("Failed to generate execution plan: LLM returned no steps");
            }

            // Build the execution plan
            var plan = new ExecutionPlan
            {
                TaskId = task.Id,
                Steps = steps,
                EstimatedDurationMs = EstimatePlanDuration(steps),
                Confidence = CalculatePlanConfidence(steps, response),
                CreatedAt = startTime,
                Metadata = new Dictionary<string, object>
                {
                    ["correlation_id"] = correlationId,
                    ["llm_model"] = _llmProvider.GetModelName(),
                    ["llm_tokens_input"] = tokenUsage.InputTokens,
                    ["llm_tokens_output"] = tokenUsage.OutputTokens,
                    ["llm_cost_usd"] = tokenUsage.EstimatedCostUSD,
                    ["task_type"] = task.Type.ToString(),
                    ["step_count"] = steps.Count
                }
            };

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "Plan created successfully for task {TaskId}. Steps: {StepCount}, Estimated duration: {EstimatedMs}ms, Confidence: {Confidence:P1}, Planning time: {PlanningMs}ms",
                task.Id, steps.Count, plan.EstimatedDurationMs, plan.Confidence, duration);

            return plan;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Plan creation cancelled for task {TaskId}", task.Id);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to parse LLM response as JSON for task {TaskId}. This usually indicates the LLM returned malformed JSON.",
                task.Id);
            throw new InvalidOperationException(
                "Failed to parse LLM response. The response was not valid JSON. Please try again.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create plan for task {TaskId}: {ErrorMessage}", task.Id, ex.Message);
            throw new InvalidOperationException($"Failed to create execution plan: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<ExecutionPlan> RefinePlanAsync(
        ExecutionPlan plan,
        IReadOnlyList<AgentStepResult> executionResults,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(executionResults);

        _logger.LogInformation(
            "Refining plan {PlanId} based on {ResultCount} execution results",
            plan.Id, executionResults.Count);

        try
        {
            var systemPrompt = BuildSystemPrompt();
            var refinementPrompt = BuildRefinementPrompt(plan, executionResults);
            var availableTools = ConvertToolsForLLM();

            var llmRequest = new LLMRequest
            {
                Model = _llmProvider.GetModelName(),
                Messages = new List<Message>
                {
                    new() { Role = MessageRole.System, Content = systemPrompt },
                    new() { Role = MessageRole.User, Content = refinementPrompt }
                },
                Temperature = 0.7,
                MaxTokens = 4000,
                Functions = availableTools,
                ResponseFormat = new ResponseFormat { Type = "json_object" }
            };

            var response = await _llmProvider.CompleteAsync(llmRequest, cancellationToken);
            var steps = ParseLLMResponseToSteps(response.Content, plan.Id);

            var refinedPlan = new ExecutionPlan
            {
                Id = Guid.NewGuid().ToString(),
                TaskId = plan.TaskId,
                Steps = steps,
                EstimatedDurationMs = EstimatePlanDuration(steps),
                Confidence = CalculatePlanConfidence(steps, response),
                CreatedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["original_plan_id"] = plan.Id,
                    ["refinement_reason"] = "Based on execution feedback",
                    ["llm_model"] = _llmProvider.GetModelName(),
                    ["step_count"] = steps.Count
                }
            };

            _logger.LogInformation(
                "Plan refined successfully. New plan {NewPlanId} has {StepCount} steps (original: {OriginalStepCount})",
                refinedPlan.Id, steps.Count, plan.Steps.Count);

            return refinedPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refine plan {PlanId}", plan.Id);
            throw new InvalidOperationException($"Failed to refine execution plan: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<PlanValidation> ValidatePlanAsync(
        ExecutionPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        _logger.LogDebug("Validating plan {PlanId} with {StepCount} steps", plan.Id, plan.Steps.Count);

        var validation = new PlanValidation { IsValid = true };

        try
        {
            // Validate step count
            if (plan.Steps.Count == 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("Plan contains no steps");
                return validation;
            }

            if (plan.Steps.Count > 100)
            {
                validation.Warnings.Add($"Plan has {plan.Steps.Count} steps, which may be excessive. Consider breaking into smaller tasks.");
            }

            // Validate each step
            for (int i = 0; i < plan.Steps.Count; i++)
            {
                var step = plan.Steps[i];

                // Check step number sequence
                if (step.StepNumber != i + 1)
                {
                    validation.Warnings.Add($"Step number mismatch at index {i}: expected {i + 1}, got {step.StepNumber}");
                }

                // Validate browser action
                if (step.Action == null)
                {
                    validation.IsValid = false;
                    validation.Errors.Add($"Step {step.StepNumber} has no action defined");
                    continue;
                }

                // Validate tool exists
                var toolName = MapActionTypeToToolName(step.Action.Type);
                if (!_toolRegistry.ToolExists(toolName))
                {
                    validation.IsValid = false;
                    validation.Errors.Add($"Step {step.StepNumber} references unknown tool: {toolName}");
                }

                // Validate required parameters
                if (step.Action.Type == ActionType.Navigate && string.IsNullOrEmpty(step.Action.Value))
                {
                    validation.IsValid = false;
                    validation.Errors.Add($"Step {step.StepNumber}: Navigate action requires a URL");
                }

                if ((step.Action.Type == ActionType.Click || step.Action.Type == ActionType.Type) &&
                    step.Action.Target == null)
                {
                    validation.IsValid = false;
                    validation.Errors.Add($"Step {step.StepNumber}: {step.Action.Type} action requires a target selector");
                }

                // Check timeouts
                if (step.TimeoutMs > 300000) // 5 minutes
                {
                    validation.Warnings.Add($"Step {step.StepNumber} has a very long timeout: {step.TimeoutMs}ms");
                }
            }

            // Suggest improvements
            if (plan.Confidence < 0.7)
            {
                validation.Suggestions.Add("Plan confidence is low. Consider providing more detailed task instructions.");
            }

            if (plan.EstimatedDurationMs > 600000) // 10 minutes
            {
                validation.Suggestions.Add($"Plan has long estimated duration ({plan.EstimatedDurationMs}ms). Consider splitting into smaller tasks.");
            }

            _logger.LogDebug(
                "Plan validation complete. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                validation.IsValid, validation.Errors.Count, validation.Warnings.Count);

            return validation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during plan validation for plan {PlanId}", plan.Id);
            validation.IsValid = false;
            validation.Errors.Add($"Validation error: {ex.Message}");
            return validation;
        }
    }

    // ============================================================
    // Private Helper Methods
    // ============================================================

    /// <summary>
    /// Builds the system prompt that defines the agent's role and capabilities.
    /// </summary>
    private string BuildSystemPrompt()
    {
        return """
            You are an expert browser automation planner. Your role is to convert natural language task descriptions
            into structured, executable browser automation plans.
            
            Key responsibilities:
            - Analyze the user's task description and break it down into sequential steps
            - Select appropriate browser automation tools for each step
            - Provide clear reasoning for each step's purpose
            - Ensure steps are ordered logically and efficiently
            - Include proper waits and validation where needed
            
            Available tool categories:
            - Navigation: navigate, wait_for_url_change
            - Interaction: click, type, clear_input, select_option, submit_form
            - Extraction: extract_text, extract_table, get_page_state
            - Verification: wait_for_element, verify_element_exists
            - Utilities: take_screenshot, get_page_html
            
            Guidelines:
            - Start with navigation if a URL is provided
            - Always wait for elements before interacting with them
            - Use descriptive CSS selectors (prefer IDs, then classes, then semantic selectors)
            - Include reasoning that explains the "why" for each step
            - Specify expected outcomes to enable validation
            - Keep plans concise but complete
            - Consider error cases (e.g., popups, slow loading)
            
            Response format:
            Return a JSON object with a "steps" array. Each step must have:
            - order: Sequential step number (1, 2, 3...)
            - action: Tool name (e.g., "navigate", "click", "type")
            - selector: CSS selector (empty string if not applicable)
            - value: Input value (empty string if not applicable)
            - reasoning: Why this step is necessary
            - expected_result: What should happen after this step
            
            Example response:
            {
              "steps": [
                {
                  "order": 1,
                  "action": "navigate",
                  "selector": "",
                  "value": "https://example.com",
                  "reasoning": "Navigate to the target website to begin the automation workflow",
                  "expected_result": "Page loads successfully and displays the homepage"
                },
                {
                  "order": 2,
                  "action": "wait_for_element",
                  "selector": "#login-button",
                  "value": "",
                  "reasoning": "Ensure the login button is visible before attempting to click it",
                  "expected_result": "Login button is visible and ready for interaction"
                },
                {
                  "order": 3,
                  "action": "click",
                  "selector": "#login-button",
                  "value": "",
                  "reasoning": "Click the login button to proceed to the authentication form",
                  "expected_result": "Login form appears with username and password fields"
                }
              ]
            }
            """;
    }

    /// <summary>
    /// Builds the user prompt with task details and available tools.
    /// </summary>
    private string BuildUserPrompt(AgentTask task, AgentExecutionContext context)
    {
        var availableToolsList = string.Join(", ", _toolRegistry.GetToolNames());
        
        var promptBuilder = new System.Text.StringBuilder();
        promptBuilder.AppendLine("Task to automate:");
        promptBuilder.AppendLine($"Description: {task.Description}");
        
        if (!string.IsNullOrEmpty(task.StartUrl))
        {
            promptBuilder.AppendLine($"Starting URL: {task.StartUrl}");
        }

        promptBuilder.AppendLine($"Task Type: {task.Type}");

        if (task.Parameters.Count > 0)
        {
            promptBuilder.AppendLine("\nTask Parameters:");
            foreach (var (key, value) in task.Parameters)
            {
                promptBuilder.AppendLine($"  - {key}: {value}");
            }
        }

        if (task.Expectations != null)
        {
            promptBuilder.AppendLine("\nExpected Outcomes:");
            if (!string.IsNullOrEmpty(task.Expectations.ExpectedUrl))
            {
                promptBuilder.AppendLine($"  - Final URL should match: {task.Expectations.ExpectedUrl}");
            }
            if (task.Expectations.ExpectedElements.Count > 0)
            {
                promptBuilder.AppendLine("  - Expected elements:");
                foreach (var element in task.Expectations.ExpectedElements)
                {
                    promptBuilder.AppendLine($"    • {element}");
                }
            }
        }

        promptBuilder.AppendLine($"\nAvailable browser automation tools: {availableToolsList}");
        
        promptBuilder.AppendLine("\nPlease generate a detailed execution plan with ordered steps.");
        promptBuilder.AppendLine("Return your response as a JSON object with a 'steps' array.");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Builds a prompt for refining an existing plan based on execution results.
    /// </summary>
    private string BuildRefinementPrompt(ExecutionPlan plan, IReadOnlyList<AgentStepResult> executionResults)
    {
        var promptBuilder = new System.Text.StringBuilder();
        promptBuilder.AppendLine("The following execution plan encountered issues and needs refinement:");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("Original Plan Steps:");
        foreach (var step in plan.Steps)
        {
            promptBuilder.AppendLine($"  {step.StepNumber}. {step.Action?.Type} - {step.Reasoning}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Execution Results:");
        foreach (var result in executionResults)
        {
            var stepNum = plan.Steps.FindIndex(s => s.Id == result.StepId) + 1;
            var status = result.Success ? "? SUCCESS" : "? FAILED";
            promptBuilder.AppendLine($"  Step {stepNum}: {status}");
            
            if (!result.Success && result.Error != null)
            {
                promptBuilder.AppendLine($"    Error: {result.Error.Message}");
            }
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Please generate a refined execution plan that addresses the failures.");
        promptBuilder.AppendLine("Consider alternative selectors, additional waits, or different approaches.");
        promptBuilder.AppendLine("Return your response as a JSON object with a 'steps' array.");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Converts browser tools to LLM-compatible function definitions.
    /// </summary>
    private List<FunctionDefinition> ConvertToolsForLLM()
    {
        var tools = _toolRegistry.GetAllTools();
        var functions = new List<FunctionDefinition>();

        foreach (var tool in tools)
        {
            var parameters = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = tool.Parameters.ToDictionary(
                    p => p.Key,
                    p => new Dictionary<string, object>
                    {
                        ["type"] = p.Value.Type,
                        ["description"] = p.Value.Description
                    }
                ),
                ["required"] = tool.Parameters
                    .Where(p => p.Value.Required)
                    .Select(p => p.Key)
                    .ToArray()
            };

            functions.Add(new FunctionDefinition
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = parameters
            });
        }

        return functions;
    }

    /// <summary>
    /// Parses the LLM response into a list of AgentStep objects.
    /// </summary>
    private List<AgentStep> ParseLLMResponseToSteps(string responseContent, string correlationId)
    {
        var steps = new List<AgentStep>();

        try
        {
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            if (!root.TryGetProperty("steps", out var stepsElement))
            {
                _logger.LogWarning("LLM response missing 'steps' property. Response: {Response}", responseContent);
                return steps;
            }

            if (stepsElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("LLM response 'steps' is not an array. Response: {Response}", responseContent);
                return steps;
            }

            foreach (var stepElement in stepsElement.EnumerateArray())
            {
                var step = ParseSingleStep(stepElement);
                if (step != null)
                {
                    steps.Add(step);
                }
            }

            _logger.LogDebug("Parsed {StepCount} steps from LLM response", steps.Count);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response as JSON: {Response}", responseContent);
            throw;
        }

        return steps;
    }

    /// <summary>
    /// Parses a single step from JSON.
    /// </summary>
    private AgentStep? ParseSingleStep(JsonElement stepElement)
    {
        try
        {
            var order = stepElement.TryGetProperty("order", out var orderProp) ? orderProp.GetInt32() : 0;
            var action = stepElement.TryGetProperty("action", out var actionProp) ? actionProp.GetString() ?? "" : "";
            var selector = stepElement.TryGetProperty("selector", out var selectorProp) ? selectorProp.GetString() ?? "" : "";
            var value = stepElement.TryGetProperty("value", out var valueProp) ? valueProp.GetString() ?? "" : "";
            var reasoning = stepElement.TryGetProperty("reasoning", out var reasoningProp) ? reasoningProp.GetString() ?? "" : "";
            var expectedResult = stepElement.TryGetProperty("expected_result", out var expectedProp) ? expectedProp.GetString() ?? "" : "";

            if (string.IsNullOrEmpty(action))
            {
                _logger.LogWarning("Step with order {Order} has no action, skipping", order);
                return null;
            }

            // Map tool name to ActionType
            var actionType = MapToolNameToActionType(action);

            // Create BrowserAction
            var browserAction = new BrowserAction
            {
                Type = actionType,
                Value = string.IsNullOrEmpty(value) ? null : value,
                Description = reasoning
            };

            // Set target selector if provided
            if (!string.IsNullOrEmpty(selector))
            {
                browserAction.Target = ElementLocator.Css(selector);
            }

            return new AgentStep
            {
                StepNumber = order,
                Action = browserAction,
                Reasoning = reasoning,
                ExpectedOutcome = expectedResult,
                TimeoutMs = 30000,
                IsOptional = false
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse step from JSON element (JSON error)");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to parse step from JSON element (invalid operation)");
            return null;
        }
    }

    /// <summary>
    /// Maps tool name to ActionType enum.
    /// </summary>
    private ActionType MapToolNameToActionType(string toolName)
    {
        return toolName.ToLowerInvariant() switch
        {
            "navigate" => ActionType.Navigate,
            "click" => ActionType.Click,
            "type" => ActionType.Type,
            "clear_input" => ActionType.Fill, // Use Fill for clearing
            "select_option" => ActionType.Select,
            "submit_form" => ActionType.Click,
            "wait_for_element" => ActionType.WaitForElement,
            "take_screenshot" => ActionType.Screenshot,
            "extract_text" => ActionType.ExtractText,
            "verify_element_exists" => ActionType.Verify,
            "get_page_state" => ActionType.ExtractText,
            "get_page_html" => ActionType.ExtractText,
            _ => ActionType.ExtractText // Default fallback
        };
    }

    /// <summary>
    /// Maps ActionType to tool name.
    /// </summary>
    private string MapActionTypeToToolName(ActionType actionType)
    {
        return actionType switch
        {
            ActionType.Navigate => "navigate",
            ActionType.Click => "click",
            ActionType.Type => "type",
            ActionType.Fill => "type",
            ActionType.Select => "select_option",
            ActionType.WaitForElement => "wait_for_element",
            ActionType.Screenshot => "take_screenshot",
            ActionType.ExtractText => "extract_text",
            ActionType.Verify => "verify_element_exists",
            _ => "extract_text"
        };
    }

    /// <summary>
    /// Estimates the total duration for executing the plan.
    /// </summary>
    private long EstimatePlanDuration(List<AgentStep> steps)
    {
        long totalMs = 0;

        foreach (var step in steps)
        {
            if (step.Action == null) continue;

            // Estimate based on action type
            totalMs += step.Action.Type switch
            {
                ActionType.Navigate => 3000,      // Navigation typically takes ~3 seconds
                ActionType.Click => 500,          // Clicks are fast
                ActionType.Type => 1000,          // Typing takes ~1 second per field
                ActionType.WaitForElement => 2000, // Waits average ~2 seconds
                ActionType.Screenshot => 800,     // Screenshots take ~800ms
                ActionType.ExtractText => 500,    // Extraction is fast
                _ => 1000                         // Default estimate
            };
        }

        return totalMs;
    }

    /// <summary>
    /// Calculates confidence in the plan based on various factors.
    /// </summary>
    private double CalculatePlanConfidence(List<AgentStep> steps, LLMResponse response)
    {
        double confidence = 1.0;

        // Reduce confidence if plan is very short (might be incomplete)
        if (steps.Count < 2)
        {
            confidence -= 0.2;
        }

        // Reduce confidence if plan is very long (might be complex/risky)
        if (steps.Count > 20)
        {
            confidence -= 0.1;
        }

        // Check if all steps have reasoning
        var stepsWithoutReasoning = steps.Count(s => string.IsNullOrEmpty(s.Reasoning));
        if (stepsWithoutReasoning > 0)
        {
            confidence -= 0.1 * (stepsWithoutReasoning / (double)steps.Count);
        }

        // Check if all steps have expected outcomes
        var stepsWithoutOutcomes = steps.Count(s => string.IsNullOrEmpty(s.ExpectedOutcome));
        if (stepsWithoutOutcomes > 0)
        {
            confidence -= 0.1 * (stepsWithoutOutcomes / (double)steps.Count);
        }

        return Math.Max(0.0, Math.Min(1.0, confidence));
    }
}
