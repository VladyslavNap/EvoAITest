namespace EvoAITest.Core.Models.Execution;

/// <summary>
/// Represents the result of executing a single test step
/// </summary>
public sealed class TestStepResult
{
    /// <summary>
    /// Step sequence number
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Description of the step
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Execution status of this step
    /// </summary>
    public TestExecutionStatus Status { get; set; }

    /// <summary>
    /// When the step started executing
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the step completed
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs => CompletedAt.HasValue
        ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
        : 0;

    /// <summary>
    /// Error message if step failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if step failed
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Screenshot path if captured
    /// </summary>
    public string? ScreenshotPath { get; set; }

    /// <summary>
    /// Expected result for this step
    /// </summary>
    public string? ExpectedResult { get; set; }

    /// <summary>
    /// Actual result from execution
    /// </summary>
    public string? ActualResult { get; set; }

    /// <summary>
    /// Additional step metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
