using System.ComponentModel.DataAnnotations;
using EvoAITest.Core.Models;
using TaskStatus = EvoAITest.Core.Models.TaskStatus;

namespace EvoAITest.ApiService.Models;

/// <summary>
/// Request model for creating a new automation task.
/// </summary>
public sealed record CreateTaskRequest
{
    /// <summary>
    /// Gets the name of the task.
    /// </summary>
    [Required(ErrorMessage = "Task name is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Task name must be between 1 and 500 characters")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the task.
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the natural language prompt describing what the task should do.
    /// </summary>
    [Required(ErrorMessage = "Natural language prompt is required")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Prompt must be between 10 and 5000 characters")]
    public required string NaturalLanguagePrompt { get; init; }
}

/// <summary>
/// Request model for updating an existing automation task.
/// </summary>
public sealed record UpdateTaskRequest
{
    /// <summary>
    /// Gets the updated name of the task.
    /// </summary>
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Task name must be between 1 and 500 characters")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the updated description of the task.
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the updated status of the task.
    /// </summary>
    public TaskStatus? Status { get; init; }
}

/// <summary>
/// Response model for automation task details.
/// </summary>
public sealed record TaskResponse
{
    /// <summary>
    /// Gets the unique identifier of the task.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the user identifier who owns the task.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the name of the task.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the task.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the natural language prompt.
    /// </summary>
    public required string NaturalLanguagePrompt { get; init; }

    /// <summary>
    /// Gets the current status of the task.
    /// </summary>
    public required TaskStatus Status { get; init; }

    /// <summary>
    /// Gets the number of execution steps in the plan.
    /// </summary>
    public int PlanStepCount { get; init; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Gets the timestamp when the task was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the task was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the task was completed (if completed).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Gets the execution history count.
    /// </summary>
    public int ExecutionCount { get; init; }

    /// <summary>
    /// Creates a TaskResponse from an AutomationTask entity.
    /// </summary>
    public static TaskResponse FromEntity(AutomationTask task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            UserId = task.UserId,
            Name = task.Name,
            Description = task.Description,
            NaturalLanguagePrompt = task.NaturalLanguagePrompt,
            Status = task.Status,
            PlanStepCount = task.Plan?.Count ?? 0,
            CorrelationId = task.CorrelationId,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            CompletedAt = task.CompletedAt,
            ExecutionCount = task.Executions?.Count ?? 0
        };
    }
}

/// <summary>
/// Response model for execution history.
/// </summary>
public sealed record ExecutionHistoryResponse
{
    /// <summary>
    /// Gets the unique identifier of the execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the task identifier.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// Gets the execution status.
    /// </summary>
    public required ExecutionStatus ExecutionStatus { get; init; }

    /// <summary>
    /// Gets when the execution started.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Gets when the execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Gets the execution duration in milliseconds.
    /// </summary>
    public required int DurationMs { get; init; }

    /// <summary>
    /// Gets the final output.
    /// </summary>
    public string? FinalOutput { get; init; }

    /// <summary>
    /// Gets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Creates an ExecutionHistoryResponse from an ExecutionHistory entity.
    /// </summary>
    public static ExecutionHistoryResponse FromEntity(ExecutionHistory history)
    {
        return new ExecutionHistoryResponse
        {
            Id = history.Id,
            TaskId = history.TaskId,
            ExecutionStatus = history.ExecutionStatus,
            StartedAt = history.StartedAt,
            CompletedAt = history.CompletedAt,
            DurationMs = history.DurationMs,
            FinalOutput = history.FinalOutput,
            ErrorMessage = history.ErrorMessage,
            CorrelationId = history.CorrelationId
        };
    }
}

/// <summary>
/// Standard error response model.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets additional error details.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; init; }
}
