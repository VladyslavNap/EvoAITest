using EvoAITest.Agents.Abstractions;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Examples;

/// <summary>
/// Complete example demonstrating login automation for example.com using EvoAITest framework.
/// This example shows the full workflow: task creation, planning, execution, and result storage.
/// </summary>
public sealed class LoginAutomationExample
{
    private readonly IAutomationTaskRepository _repository;
    private readonly IPlanner _planner;
    private readonly IExecutor _executor;
    private readonly ILogger<LoginAutomationExample> _logger;

    public LoginAutomationExample(
        IAutomationTaskRepository repository,
        IPlanner planner,
        IExecutor executor,
        ILogger<LoginAutomationExample> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs the complete login automation example.
    /// </summary>
    /// <param name="targetUrl">The URL to navigate to (default: https://example.com).</param>
    /// <param name="username">Username for login (optional).</param>
    /// <param name="password">Password for login (optional).</param>
    /// <returns>The execution result.</returns>
    public async Task<ExecutionResult> RunLoginExampleAsync(
        string targetUrl = "https://example.com",
        string? username = null,
        string? password = null)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("Starting Login Automation Example");
        _logger.LogInformation("========================================");
        _logger.LogInformation("Target URL: {Url}", targetUrl);
        _logger.LogInformation("");

        try
        {
            // Step 1: Create automation task
            _logger.LogInformation("Step 1: Creating automation task...");
            var task = await CreateTaskAsync(targetUrl, username, password);
            _logger.LogInformation("? Task created with ID: {TaskId}", task.Id);
            _logger.LogInformation("");

            // Step 2: Generate execution plan using AI
            _logger.LogInformation("Step 2: Planning execution with AI...");
            var plan = await PlanExecutionAsync(task);
            _logger.LogInformation("? Plan generated: {StepCount} steps", plan.Steps.Count);
            
            // Display the plan
            DisplayPlan(plan);
            _logger.LogInformation("");

            // Step 3: Execute the plan
            _logger.LogInformation("Step 3: Executing automation steps...");
            var result = await ExecutePlanAsync(plan);
            _logger.LogInformation("? Execution completed: {Status}", result.Status);
            _logger.LogInformation("");

            // Step 4: Save execution results
            _logger.LogInformation("Step 4: Saving execution results...");
            await SaveResultsAsync(task, result);
            _logger.LogInformation("? Results saved to database");
            _logger.LogInformation("");

            // Step 5: Display results
            DisplayResults(result);
            _logger.LogInformation("");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Login automation example failed");
            throw;
        }
    }

    /// <summary>
    /// Creates an automation task for login.
    /// </summary>
    private async Task<AutomationTask> CreateTaskAsync(string targetUrl, string? username, string? password)
    {
        var prompt = BuildPrompt(targetUrl, username, password);

        var task = new AutomationTask
        {
            Id = Guid.NewGuid(),
            UserId = "example-user",
            Name = $"Login to {new Uri(targetUrl).Host}",
            Description = "Automated login test using AI-powered browser automation",
            NaturalLanguagePrompt = prompt,
            Status = TaskStatus.Pending,
            CreatedBy = "LoginExample"
        };

        await _repository.CreateAsync(task);
        return task;
    }

    /// <summary>
    /// Builds the natural language prompt based on the scenario.
    /// </summary>
    private string BuildPrompt(string targetUrl, string? username, string? password)
    {
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            return $@"
Navigate to {targetUrl} and perform the following actions:
1. Wait for the page to load completely
2. Find the username input field and enter '{username}'
3. Find the password input field and enter '{password}'
4. Click the login or submit button
5. Wait for navigation to complete
6. Verify successful login by checking for welcome message or user profile

Take a screenshot after completing the login.
";
        }
        else
        {
            return $@"
Navigate to {targetUrl} and analyze the page structure:
1. Wait for the page to load completely
2. Extract the main heading text
3. Find all interactive elements (buttons, links, inputs)
4. Take a screenshot of the page
5. Extract the page title

This is a demonstration of browser automation capabilities.
";
        }
    }

    /// <summary>
    /// Generates an execution plan using the AI Planner agent.
    /// </summary>
    private async Task<ExecutionPlan> PlanExecutionAsync(AutomationTask task)
    {
        var startTime = DateTimeOffset.UtcNow;

        var plan = await _planner.PlanAsync(
            task.NaturalLanguagePrompt,
            metadata: new Dictionary<string, string>
            {
                ["TaskId"] = task.Id.ToString(),
                ["TaskName"] = task.Name,
                ["CreatedAt"] = startTime.ToString("O")
            });

        // Update task with the generated plan
        task.Plan = plan.Steps.Select(s => new ExecutionStep(
            Order: s.Order,
            Action: s.Action,
            Selector: s.Selector ?? string.Empty,
            Value: s.Value ?? string.Empty,
            Reasoning: s.Reasoning ?? string.Empty,
            ExpectedResult: s.ExpectedResult ?? "Step completed successfully",
            IsOptional: false,
            RetryCount: 0,
            MaxRetries: 3,
            TimeoutMs: 30000
        )).ToList();

        await _repository.UpdateAsync(task);

        return plan;
    }

    /// <summary>
    /// Executes the automation plan using the Executor agent.
    /// </summary>
    private async Task<ExecutionResult> ExecutePlanAsync(ExecutionPlan plan)
    {
        var result = await _executor.ExecuteAsync(
            plan.Steps,
            metadata: new Dictionary<string, string>
            {
                ["Example"] = "LoginAutomation",
                ["ExecutionId"] = Guid.NewGuid().ToString()
            });

        return result;
    }

    /// <summary>
    /// Saves execution results to the database.
    /// </summary>
    private async Task SaveResultsAsync(AutomationTask task, ExecutionResult result)
    {
        var history = new ExecutionHistory
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ExecutionStatus = result.Status,
            StartedAt = result.StartedAt,
            CompletedAt = result.CompletedAt,
            DurationMs = result.TotalDurationMs,
            StepResults = System.Text.Json.JsonSerializer.Serialize(result.Steps),
            FinalOutput = result.FinalOutput ?? string.Empty,
            ErrorMessage = result.ErrorMessage,
            ScreenshotUrls = System.Text.Json.JsonSerializer.Serialize(result.Screenshots ?? new List<string>()),
            CorrelationId = Guid.NewGuid().ToString(),
            Metadata = System.Text.Json.JsonSerializer.Serialize(result.Metadata ?? new Dictionary<string, string>())
        };

        await _repository.AddExecutionHistoryAsync(history);

        // Update task status
        task.Status = result.Status switch
        {
            ExecutionStatus.Success => TaskStatus.Completed,
            ExecutionStatus.Failed => TaskStatus.Failed,
            ExecutionStatus.PartialSuccess => TaskStatus.Completed,
            ExecutionStatus.Cancelled => TaskStatus.Cancelled,
            _ => TaskStatus.Failed
        };

        if (result.Status == ExecutionStatus.Success)
        {
            task.CompletedAt = result.CompletedAt;
        }

        await _repository.UpdateAsync(task);
    }

    /// <summary>
    /// Displays the generated execution plan.
    /// </summary>
    private void DisplayPlan(ExecutionPlan plan)
    {
        _logger.LogInformation("?? Execution Plan:");
        _logger.LogInformation("   Confidence: {Confidence:P1}", plan.Confidence);
        _logger.LogInformation("   Estimated Duration: {Duration}ms", plan.EstimatedDurationMs);
        _logger.LogInformation("");

        foreach (var step in plan.Steps.OrderBy(s => s.Order))
        {
            _logger.LogInformation("   {Order}. {Action}", step.Order, step.Action);
            if (!string.IsNullOrWhiteSpace(step.Selector))
            {
                _logger.LogInformation("      Selector: {Selector}", step.Selector);
            }
            if (!string.IsNullOrWhiteSpace(step.Value))
            {
                _logger.LogInformation("      Value: {Value}", MaskSensitiveValue(step.Value));
            }
            _logger.LogInformation("      Reasoning: {Reasoning}", step.Reasoning);
        }
    }

    /// <summary>
    /// Displays the execution results.
    /// </summary>
    private void DisplayResults(ExecutionResult result)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("Execution Results");
        _logger.LogInformation("========================================");
        _logger.LogInformation("Status: {Status}", result.Status);
        _logger.LogInformation("Duration: {Duration}ms ({Seconds:F2}s)", result.TotalDurationMs, result.TotalDurationMs / 1000.0);
        _logger.LogInformation("Steps Executed: {StepCount}", result.Steps.Count);
        _logger.LogInformation("Successful Steps: {SuccessCount}", result.Steps.Count(s => s.Success));
        _logger.LogInformation("Failed Steps: {FailureCount}", result.Steps.Count(s => !s.Success));
        _logger.LogInformation("");

        _logger.LogInformation("?? Step-by-Step Results:");
        foreach (var step in result.Steps)
        {
            var icon = step.Success ? "?" : "?";
            _logger.LogInformation("   {Icon} Step {Number}: {Action} ({Duration}ms)",
                icon, step.StepNumber, step.Action, step.DurationMs);

            if (!step.Success && !string.IsNullOrWhiteSpace(step.ErrorMessage))
            {
                _logger.LogWarning("      Error: {Error}", step.ErrorMessage);
            }
        }

        _logger.LogInformation("");

        if (!string.IsNullOrWhiteSpace(result.FinalOutput))
        {
            _logger.LogInformation("?? Final Output:");
            _logger.LogInformation("   {Output}", result.FinalOutput);
            _logger.LogInformation("");
        }

        if (result.Screenshots?.Count > 0)
        {
            _logger.LogInformation("?? Screenshots: {Count} captured", result.Screenshots.Count);
            _logger.LogInformation("");
        }

        if (result.Status == ExecutionStatus.Success)
        {
            _logger.LogInformation("?? Automation completed successfully!");
        }
        else if (result.Status == ExecutionStatus.PartialSuccess)
        {
            _logger.LogWarning("??  Automation partially completed");
        }
        else
        {
            _logger.LogError("? Automation failed: {Error}", result.ErrorMessage);
        }

        _logger.LogInformation("========================================");
    }

    /// <summary>
    /// Masks sensitive values (passwords) in output.
    /// </summary>
    private string MaskSensitiveValue(string value)
    {
        if (value.Length > 50)
        {
            return value.Substring(0, 50) + "...";
        }

        // Check if it looks like a password (heuristic)
        if (value.Length >= 8 && value.Length <= 20 && !value.Contains(" "))
        {
            return "********";
        }

        return value;
    }
}
