using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Real-time execution metrics for dashboard tracking.
/// Captures instantaneous state and performance metrics of task executions.
/// </summary>
[Table("ExecutionMetrics")]
[Index(nameof(TaskId))]
[Index(nameof(Status))]
[Index(nameof(RecordedAt))]
[Index(nameof(IsActive))]
public sealed class ExecutionMetrics
{
    /// <summary>
    /// Gets or sets the unique identifier for this metric record.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the ID of the automation task being executed.
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the execution history ID this metric is associated with.
    /// </summary>
    public Guid? ExecutionHistoryId { get; set; }

    /// <summary>
    /// Gets or sets the current execution status.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public ExecutionStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the name of the task being executed.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public required string TaskName { get; set; }

    /// <summary>
    /// Gets or sets the current step number being executed (0-based).
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets the total number of steps in the execution plan.
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Gets or sets the current action being performed.
    /// </summary>
    [MaxLength(200)]
    public string? CurrentAction { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds at the time of recording.
    /// </summary>
    [Required]
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this metric was recorded.
    /// </summary>
    [Required]
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets whether this execution is currently active.
    /// </summary>
    [Required]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the number of steps completed successfully.
    /// </summary>
    public int StepsCompleted { get; set; }

    /// <summary>
    /// Gets or sets the number of steps that failed.
    /// </summary>
    public int StepsFailed { get; set; }

    /// <summary>
    /// Gets or sets the percentage of completion (0-100).
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the URL being tested (if applicable).
    /// </summary>
    [MaxLength(1000)]
    public string? TargetUrl { get; set; }

    /// <summary>
    /// Gets or sets whether healing was attempted during this execution.
    /// </summary>
    public bool HealingAttempted { get; set; }

    /// <summary>
    /// Gets or sets whether healing was successful.
    /// </summary>
    public bool HealingSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the number of retries attempted.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property to the task.
    /// </summary>
    [ForeignKey(nameof(TaskId))]
    public AutomationTask? Task { get; set; }

    /// <summary>
    /// Navigation property to the execution history.
    /// </summary>
    [ForeignKey(nameof(ExecutionHistoryId))]
    public ExecutionHistory? ExecutionHistory { get; set; }
}
