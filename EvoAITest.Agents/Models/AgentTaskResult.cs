namespace EvoAITest.Agents.Models;

/// <summary>
/// Represents the result of executing an agent task.
/// </summary>
public sealed class AgentTaskResult
{
    /// <summary>
    /// Gets or sets the task ID.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the task completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the final task status.
    /// </summary>
    public TaskStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the results of each step execution.
    /// </summary>
    public List<AgentStepResult> StepResults { get; set; } = new();

    /// <summary>
    /// Gets or sets data extracted during task execution.
    /// </summary>
    public Dictionary<string, object> ExtractedData { get; set; } = new();

    /// <summary>
    /// Gets or sets the error if the task failed.
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets screenshots captured during execution.
    /// </summary>
    public List<string> Screenshots { get; set; } = new();

    /// <summary>
    /// Gets or sets execution statistics.
    /// </summary>
    public ExecutionStatistics? Statistics { get; set; }

    /// <summary>
    /// Gets or sets metadata about the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when execution completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the total execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }
}

/// <summary>
/// Represents execution statistics for a task.
/// </summary>
public sealed class ExecutionStatistics
{
    /// <summary>
    /// Gets or sets the total number of steps executed.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of successful steps.
    /// </summary>
    public int SuccessfulSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of failed steps.
    /// </summary>
    public int FailedSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of retried steps.
    /// </summary>
    public int RetriedSteps { get; set; }

    /// <summary>
    /// Gets or sets the number of healed steps.
    /// </summary>
    public int HealedSteps { get; set; }

    /// <summary>
    /// Gets or sets the total retry attempts.
    /// </summary>
    public int TotalRetries { get; set; }

    /// <summary>
    /// Gets or sets the average step duration in milliseconds.
    /// </summary>
    public double AverageStepDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the total time spent waiting in milliseconds.
    /// </summary>
    public long TotalWaitTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the success rate (0-1).
    /// </summary>
    public double SuccessRate => TotalSteps > 0 ? (double)SuccessfulSteps / TotalSteps : 0;
}
