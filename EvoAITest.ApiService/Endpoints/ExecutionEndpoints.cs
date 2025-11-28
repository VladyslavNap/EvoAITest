using System.Collections.Concurrent;
using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Models;
using EvoAITest.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EvoAITest.ApiService.Endpoints;

/// <summary>
/// API endpoints for task execution workflow.
/// Handles plan generation, execution, healing, and execution history.
/// </summary>
public static class ExecutionEndpoints
{
    // In-memory store for background execution status (use Redis/database in production)
    private static readonly ConcurrentDictionary<string, ExecutionStatus> _executionStatuses = new();

    /// <summary>
    /// Registers all execution-related endpoints.
    /// </summary>
    public static void MapExecutionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Execution")
            .WithOpenApi();

        // Execute task synchronously
        group.MapPost("{id:guid}/execute", ExecuteTaskAsync)
            .WithName("ExecuteTask")
            .WithSummary("Execute a task by generating a plan and running it")
            .Produces<AgentTaskResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // Execute task in background
        group.MapPost("{id:guid}/execute/background", ExecuteTaskBackgroundAsync)
            .WithName("ExecuteTaskBackground")
            .WithSummary("Start task execution in background and return immediately")
            .Produces<BackgroundExecutionResponse>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get background execution status
        group.MapGet("{id:guid}/execute/status", GetExecutionStatusAsync)
            .WithName("GetExecutionStatus")
            .WithSummary("Get the status of a background execution")
            .Produces<ExecutionStatus>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get execution history for a task
        group.MapGet("{id:guid}/executions", GetExecutionHistoryAsync)
            .WithName("GetExecutionHistory")
            .WithSummary("Get all execution attempts for a task")
            .Produces<List<ExecutionHistoryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get specific execution details
        app.MapGet("/api/executions/{executionId:guid}", GetExecutionDetailsAsync)
            .WithName("GetExecutionDetails")
            .WithSummary("Get detailed execution result including step-by-step results")
            .WithTags("Execution")
            .Produces<ExecutionDetailsDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        // Retry with healing
        group.MapPost("{id:guid}/execute/heal", ExecuteTaskWithHealingAsync)
            .WithName("ExecuteTaskWithHealing")
            .WithSummary("Retry task execution with healing analysis on last failed execution")
            .Produces<AgentTaskResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Cancel execution
        group.MapPost("{id:guid}/execute/cancel", CancelExecutionAsync)
            .WithName("CancelExecution")
            .WithSummary("Cancel a running task execution")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Executes a task synchronously by generating a plan and running it.
    /// </summary>
    private static async Task<IResult> ExecuteTaskAsync(
        Guid id,
        [FromServices] IPlanner planner,
        [FromServices] IExecutor executor,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting synchronous execution for task {TaskId}", id);

        try
        {
            // 1. Create task (in real scenario, get from repository)
            var task = new AgentTask
            {
                Id = id.ToString(),
                Description = "Sample task", // Would come from database
                Type = TaskType.Custom
            };

            // 2. Create execution context
            var context = new EvoAITest.Agents.Abstractions.ExecutionContext
            {
                SessionId = Guid.NewGuid().ToString()
            };

            // 3. Generate execution plan
            logger.LogInformation("Generating plan for task {TaskId}", id);
            ExecutionPlan plan;
            
            try
            {
                plan = await planner.CreatePlanAsync(task, context, cancellationToken);
                logger.LogInformation(
                    "Plan generated successfully for task {TaskId}: {StepCount} steps, confidence {Confidence:P1}",
                    id, plan.Steps.Count, plan.Confidence);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate plan for task {TaskId}", id);
                return Results.Problem(
                    detail: $"Failed to generate execution plan: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Plan Generation Failed");
            }

            // 4. Validate plan
            var validation = await planner.ValidatePlanAsync(plan, cancellationToken);
            if (!validation.IsValid)
            {
                logger.LogWarning(
                    "Plan validation failed for task {TaskId}: {Errors}",
                    id, string.Join(", ", validation.Errors));
                
                return Results.Problem(
                    detail: $"Plan validation failed: {string.Join("; ", validation.Errors)}",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Plan");
            }

            // 5. Execute plan
            logger.LogInformation("Executing plan {PlanId} for task {TaskId}", plan.Id, id);
            AgentTaskResult result;
            
            try
            {
                result = await executor.ExecutePlanAsync(plan, context, cancellationToken);
                
                logger.LogInformation(
                    "Plan execution completed for task {TaskId}: Status={Status}, Success={Success}, Duration={Duration}ms",
                    id, result.Status, result.Success, result.DurationMs);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Execution cancelled for task {TaskId}", id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Execution failed for task {TaskId}", id);
                return Results.Problem(
                    detail: $"Execution failed: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Execution Failed");
            }

            // 6. Save execution history (TODO: implement repository)
            // await SaveExecutionHistoryAsync(task.Id, plan, result, cancellationToken);

            // 7. Return result
            return Results.Ok(result);
        }
        catch (OperationCanceledException)
        {
            return Results.Problem(
                detail: "Execution was cancelled",
                statusCode: StatusCodes.Status499ClientClosedRequest,
                title: "Execution Cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during task {TaskId} execution", id);
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error");
        }
    }

    /// <summary>
    /// Starts task execution in background and returns immediately.
    /// </summary>
    private static async Task<IResult> ExecuteTaskBackgroundAsync(
        Guid id,
        [FromServices] IPlanner planner,
        [FromServices] IExecutor executor,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting background execution for task {TaskId}", id);

        var executionId = Guid.NewGuid().ToString();

        // Register initial status
        _executionStatuses[executionId] = new ExecutionStatus
        {
            ExecutionId = executionId,
            TaskId = id.ToString(),
            Status = "Queued",
            StartedAt = DateTimeOffset.UtcNow
        };

        // Start background execution
        _ = Task.Run(async () =>
        {
            try
            {
                _executionStatuses[executionId] = _executionStatuses[executionId] with { Status = "Planning" };

                // Create task and context
                var task = new AgentTask
                {
                    Id = id.ToString(),
                    Description = "Sample task",
                    Type = TaskType.Custom
                };

                var context = new EvoAITest.Agents.Abstractions.ExecutionContext
                {
                    SessionId = Guid.NewGuid().ToString()
                };

                // Generate plan
                var plan = await planner.CreatePlanAsync(task, context, CancellationToken.None);
                
                _executionStatuses[executionId] = _executionStatuses[executionId] with 
                { 
                    Status = "Executing",
                    PlanId = plan.Id
                };

                // Execute plan
                var result = await executor.ExecutePlanAsync(plan, context, CancellationToken.None);

                // Update final status
                _executionStatuses[executionId] = _executionStatuses[executionId] with
                {
                    Status = result.Success ? "Completed" : "Failed",
                    CompletedAt = DateTimeOffset.UtcNow,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage,
                    DurationMs = result.DurationMs
                };

                logger.LogInformation(
                    "Background execution {ExecutionId} completed: Status={Status}",
                    executionId, result.Status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background execution {ExecutionId} failed", executionId);
                
                _executionStatuses[executionId] = _executionStatuses[executionId] with
                {
                    Status = "Failed",
                    CompletedAt = DateTimeOffset.UtcNow,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }, cancellationToken);

        return Results.Accepted(
            $"/api/tasks/{id}/execute/status?executionId={executionId}",
            new BackgroundExecutionResponse
            {
                ExecutionId = executionId,
                TaskId = id.ToString(),
                StatusUrl = $"/api/tasks/{id}/execute/status?executionId={executionId}",
                Message = "Execution started in background"
            });
    }

    /// <summary>
    /// Gets the status of a background execution.
    /// </summary>
    private static Task<IResult> GetExecutionStatusAsync(
        Guid id,
        [FromQuery] string executionId,
        [FromServices] ILogger<Program> logger)
    {
        if (string.IsNullOrEmpty(executionId))
        {
            return Task.FromResult(Results.BadRequest("executionId query parameter is required"));
        }

        if (_executionStatuses.TryGetValue(executionId, out var status))
        {
            return Task.FromResult(Results.Ok(status));
        }

        logger.LogWarning("Execution status not found: {ExecutionId}", executionId);
        return Task.FromResult(Results.NotFound(new ProblemDetails
        {
            Title = "Execution Not Found",
            Detail = $"Execution with ID '{executionId}' was not found",
            Status = StatusCodes.Status404NotFound
        }));
    }

    /// <summary>
    /// Gets execution history for a task.
    /// </summary>
    private static Task<IResult> GetExecutionHistoryAsync(
        Guid id,
        [FromServices] ILogger<Program> logger)
    {
        logger.LogInformation("Retrieving execution history for task {TaskId}", id);

        // TODO: Implement repository call
        // var history = await repository.GetExecutionHistoryAsync(id, cancellationToken);

        // Mock data for now
        var history = new List<ExecutionHistoryDto>
        {
            new()
            {
                ExecutionId = Guid.NewGuid(),
                TaskId = id,
                StartedAt = DateTimeOffset.UtcNow.AddHours(-2),
                CompletedAt = DateTimeOffset.UtcNow.AddHours(-2).AddMinutes(5),
                Status = "Completed",
                Success = true,
                DurationMs = 300000,
                StepsCompleted = 10,
                StepsTotal = 10
            }
        };

        return Task.FromResult(Results.Ok(history));
    }

    /// <summary>
    /// Gets detailed execution result for a specific execution.
    /// </summary>
    private static Task<IResult> GetExecutionDetailsAsync(
        Guid executionId,
        [FromServices] ILogger<Program> logger)
    {
        logger.LogInformation("Retrieving execution details for {ExecutionId}", executionId);

        // TODO: Implement repository call
        // var details = await repository.GetExecutionDetailsAsync(executionId, cancellationToken);

        // Mock data for now
        var details = new ExecutionDetailsDto
        {
            ExecutionId = executionId,
            TaskId = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAt = DateTimeOffset.UtcNow.AddHours(-1).AddMinutes(3),
            Status = "Completed",
            Success = true,
            DurationMs = 180000,
            StepResults = new List<StepResultDto>
            {
                new()
                {
                    StepNumber = 1,
                    Action = "navigate",
                    Success = true,
                    DurationMs = 2000,
                    Output = "Navigated to https://example.com"
                },
                new()
                {
                    StepNumber = 2,
                    Action = "click",
                    Success = true,
                    DurationMs = 500,
                    Output = "Clicked button"
                }
            },
            Screenshots = new List<string>
            {
                "/screenshots/exec-123-final.png"
            }
        };

        return Task.FromResult(Results.Ok(details));
    }

    /// <summary>
    /// Retries task execution with healing analysis on the last failed execution.
    /// </summary>
    private static async Task<IResult> ExecuteTaskWithHealingAsync(
        Guid id,
        [FromServices] IPlanner planner,
        [FromServices] IExecutor executor,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting execution with healing for task {TaskId}", id);

        try
        {
            // 1. Get task and last failed execution
            // TODO: Get from repository
            // var task = await repository.GetByIdAsync(id, cancellationToken);
            // var lastExecution = await repository.GetLastFailedExecutionAsync(id, cancellationToken);

            // 2. Create task and context
            var task = new AgentTask
            {
                Id = id.ToString(),
                Description = "Sample task with healing",
                Type = TaskType.Custom
            };

            var context = new EvoAITest.Agents.Abstractions.ExecutionContext
            {
                SessionId = Guid.NewGuid().ToString()
            };

            // 3. Generate initial plan
            var plan = await planner.CreatePlanAsync(task, context, cancellationToken);

            // 4. First execution attempt
            logger.LogInformation("Executing initial plan for task {TaskId}", id);
            var result = await executor.ExecutePlanAsync(plan, context, cancellationToken);

            // 5. If failed, try healing
            if (!result.Success)
            {
                logger.LogInformation(
                    "Initial execution failed for task {TaskId}, attempting healing",
                    id);

                try
                {
                    // Refine plan based on execution results
                    var refinedPlan = await planner.RefinePlanAsync(
                        plan,
                        result.StepResults,
                        cancellationToken);

                    logger.LogInformation(
                        "Refined plan generated for task {TaskId}: {StepCount} steps",
                        id, refinedPlan.Steps.Count);

                    // Retry with refined plan
                    var healedContext = new EvoAITest.Agents.Abstractions.ExecutionContext
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        PreviousSteps = result.StepResults
                    };

                    result = await executor.ExecutePlanAsync(
                        refinedPlan,
                        healedContext,
                        cancellationToken);

                    result.Metadata["healing_applied"] = true;
                    result.Metadata["original_plan_id"] = plan.Id;
                    result.Metadata["refined_plan_id"] = refinedPlan.Id;

                    logger.LogInformation(
                        "Healed execution completed for task {TaskId}: Success={Success}",
                        id, result.Success);
                }
                catch (Exception healEx)
                {
                    logger.LogError(healEx, "Healing failed for task {TaskId}", id);
                    result.Metadata["healing_error"] = healEx.Message;
                }
            }

            return Results.Ok(result);
        }
        catch (OperationCanceledException)
        {
            return Results.Problem(
                detail: "Execution was cancelled",
                statusCode: StatusCodes.Status499ClientClosedRequest,
                title: "Execution Cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Healing execution failed for task {TaskId}", id);
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Healing Execution Failed");
        }
    }

    /// <summary>
    /// Cancels a running task execution.
    /// </summary>
    private static async Task<IResult> CancelExecutionAsync(
        Guid id,
        [FromServices] IExecutor executor,
        [FromServices] ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Cancelling execution for task {TaskId}", id);

        try
        {
            await executor.CancelExecutionAsync(id.ToString(), cancellationToken);
            
            logger.LogInformation("Execution cancelled successfully for task {TaskId}", id);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot cancel execution for task {TaskId}", id);
            return Results.NotFound(new ProblemDetails
            {
                Title = "Execution Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel execution for task {TaskId}", id);
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Cancellation Failed");
        }
    }
}

#region DTOs

/// <summary>
/// Response for background execution request.
/// </summary>
public record BackgroundExecutionResponse
{
    public required string ExecutionId { get; init; }
    public required string TaskId { get; init; }
    public required string StatusUrl { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Status of a background execution.
/// </summary>
public record ExecutionStatus
{
    public required string ExecutionId { get; init; }
    public required string TaskId { get; init; }
    public required string Status { get; init; } // Queued, Planning, Executing, Completed, Failed
    public string? PlanId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public bool? Success { get; init; }
    public string? ErrorMessage { get; init; }
    public long? DurationMs { get; init; }
}

/// <summary>
/// Execution history entry.
/// </summary>
public record ExecutionHistoryDto
{
    public required Guid ExecutionId { get; init; }
    public required Guid TaskId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public required string Status { get; init; }
    public required bool Success { get; init; }
    public long DurationMs { get; init; }
    public int StepsCompleted { get; init; }
    public int StepsTotal { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Detailed execution result.
/// </summary>
public record ExecutionDetailsDto
{
    public required Guid ExecutionId { get; init; }
    public required Guid TaskId { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public required string Status { get; init; }
    public required bool Success { get; init; }
    public long DurationMs { get; init; }
    public string? ErrorMessage { get; init; }
    public required List<StepResultDto> StepResults { get; init; }
    public required List<string> Screenshots { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Step execution result.
/// </summary>
public record StepResultDto
{
    public required int StepNumber { get; init; }
    public required string Action { get; init; }
    public required bool Success { get; init; }
    public long DurationMs { get; init; }
    public string? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ScreenshotUrl { get; init; }
}

#endregion
