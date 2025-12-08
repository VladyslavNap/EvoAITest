using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EvoAITest.Core.Models;

/// <summary>
/// Represents a record of an automation task execution.
/// Tracks execution history, results, and diagnostics for observability and debugging.
/// </summary>
[Table("ExecutionHistory")]
[Index(nameof(TaskId))]
[Index(nameof(ExecutionStatus))]
[Index(nameof(StartedAt))]
public sealed class ExecutionHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for this execution record.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the ID of the automation task that was executed.
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the execution status (Success, Failed, PartialSuccess).
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public ExecutionStatus ExecutionStatus { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when execution started.
    /// </summary>
    [Required]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when execution completed (nullable).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the total execution duration in milliseconds.
    /// </summary>
    [Required]
    public int DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized step results.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string StepResults { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the final output or result data.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string FinalOutput { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if execution failed (nullable).
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the JSON array of screenshot URLs.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ScreenshotUrls { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Navigation property to the parent AutomationTask.
    /// </summary>
    [ForeignKey(nameof(TaskId))]
    public AutomationTask? Task { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the execution.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string Metadata { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the JSON-serialized visual comparison results.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string VisualComparisonResults { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the count of visual checkpoints that passed.
    /// </summary>
    public int VisualCheckpointsPassed { get; set; }

    /// <summary>
    /// Gets or sets the count of visual checkpoints that failed.
    /// </summary>
    public int VisualCheckpointsFailed { get; set; }

    /// <summary>
    /// Gets or sets the overall visual regression status.
    /// </summary>
    [Column(TypeName = "nvarchar(50)")]
    public VisualRegressionStatus VisualStatus { get; set; } = VisualRegressionStatus.NotApplicable;

    /// <summary>
    /// Gets or sets the timestamp when this record was created.
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
