namespace EvoAITest.Core.Models;

/// <summary>
/// Defines the status of an automation task.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task has been created but not yet started.
    /// </summary>
    Pending,

    /// <summary>
    /// Task is being planned by the AI agent.
    /// </summary>
    Planning,

    /// <summary>
    /// Task plan is being executed.
    /// </summary>
    Executing,

    /// <summary>
    /// Task has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Task execution failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Task was cancelled by the user or system.
    /// </summary>
    Cancelled
}

/// <summary>
/// Defines the execution status of a completed task.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    /// All steps completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Some steps completed successfully, but not all.
    /// </summary>
    PartialSuccess,

    /// <summary>
    /// Execution failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Execution timed out before completion.
    /// </summary>
    Timeout
}

/// <summary>
/// Represents an automation task that can be persisted to a database.
/// This is a mutable class designed for Entity Framework Core compatibility.
/// </summary>
public sealed class AutomationTask
{
    /// <summary>
    /// Gets or sets the unique identifier for this task (Primary Key).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the user ID who created this task.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the task.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of what this task does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original natural language prompt from the user describing their intent.
    /// </summary>
    public string NaturalLanguagePrompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the task.
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// Gets or sets the execution plan as a list of steps.
    /// </summary>
    /// <remarks>
    /// This will be serialized to JSON for EF Core storage.
    /// </remarks>
    public List<ExecutionStep> Plan { get; set; } = new();

    /// <summary>
    /// Gets or sets the task context as JSON-serialized data.
    /// </summary>
    /// <remarks>
    /// This can store additional context like browser state, session data, etc.
    /// </remarks>
    public string Context { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    /// <remarks>
    /// Used to correlate logs, traces, and metrics across the system.
    /// Compatible with OpenTelemetry and Aspire observability.
    /// </remarks>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the timestamp when this task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this task was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of who created this task (for audit purposes).
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this task was completed (null if not completed).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Updates the task status and sets the UpdatedAt timestamp.
    /// </summary>
    /// <param name="newStatus">The new status to set.</param>
    /// <remarks>
    /// This method automatically updates the UpdatedAt timestamp and sets
    /// CompletedAt when transitioning to a terminal state (Completed, Failed, Cancelled).
    /// </remarks>
    public void UpdateStatus(TaskStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Set CompletedAt when reaching a terminal state
        if (newStatus is TaskStatus.Completed or TaskStatus.Failed or TaskStatus.Cancelled)
        {
            CompletedAt ??= DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Sets the execution plan for this task.
    /// </summary>
    /// <param name="steps">The list of execution steps that make up the plan.</param>
    /// <remarks>
    /// This method replaces the existing plan and updates the UpdatedAt timestamp.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="steps"/> is null.
    /// </exception>
    public void SetPlan(List<ExecutionStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Plan = steps;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents the result of executing an automation task (immutable).
/// </summary>
/// <param name="TaskId">The unique identifier of the task that was executed.</param>
/// <param name="Status">The overall execution status.</param>
/// <param name="Steps">The results of each execution step.</param>
/// <param name="FinalOutput">The final output or result data from the task.</param>
/// <param name="ErrorMessage">Error message if the execution failed (null if successful).</param>
/// <param name="TotalDurationMs">Total duration of the execution in milliseconds.</param>
public sealed record TaskExecutionResult(
    Guid TaskId,
    ExecutionStatus Status,
    List<StepResult> Steps,
    string FinalOutput,
    string? ErrorMessage,
    int TotalDurationMs
)
{
    /// <summary>
    /// Gets a value indicating whether the execution was successful.
    /// </summary>
    public bool IsSuccess => Status is ExecutionStatus.Success or ExecutionStatus.PartialSuccess;
}

/// <summary>
/// Represents the result of executing a single step (immutable).
/// </summary>
/// <param name="StepNumber">The sequential number of this step.</param>
/// <param name="Action">The action that was executed.</param>
/// <param name="Success">Indicates whether the step completed successfully.</param>
/// <param name="Output">The output or result data from this step.</param>
/// <param name="ErrorMessage">Error message if the step failed (null if successful).</param>
/// <param name="DurationMs">Duration of this step execution in milliseconds.</param>
/// <param name="ScreenshotUrl">Optional URL to a screenshot captured during this step (null if not captured).</param>
public sealed record StepResult(
    int StepNumber,
    string Action,
    bool Success,
    string Output,
    string? ErrorMessage,
    int DurationMs,
    string? ScreenshotUrl
);
