namespace EvoAITest.Core.Models.Execution;

/// <summary>
/// Statistics for test executions
/// </summary>
public sealed class TestExecutionStatistics
{
    /// <summary>
    /// Recording session ID these statistics are for
    /// </summary>
    public Guid RecordingSessionId { get; set; }

    /// <summary>
    /// Total number of executions
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of passed executions
    /// </summary>
    public int PassedExecutions { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Pass rate percentage (0-100)
    /// </summary>
    public double PassRate => TotalExecutions > 0
        ? (double)PassedExecutions / TotalExecutions * 100
        : 0;

    /// <summary>
    /// Average execution duration in milliseconds
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// Minimum execution duration in milliseconds
    /// </summary>
    public long MinDurationMs { get; set; }

    /// <summary>
    /// Maximum execution duration in milliseconds
    /// </summary>
    public long MaxDurationMs { get; set; }

    /// <summary>
    /// Date of first execution
    /// </summary>
    public DateTimeOffset? FirstExecutionAt { get; set; }

    /// <summary>
    /// Date of last execution
    /// </summary>
    public DateTimeOffset? LastExecutionAt { get; set; }

    /// <summary>
    /// Number of times the test has been flaky (passed after retry)
    /// </summary>
    public int FlakyCount { get; set; }

    /// <summary>
    /// Whether this test is considered flaky
    /// </summary>
    public bool IsFlaky => FlakyCount > 0 && TotalExecutions > 0 
        && (double)FlakyCount / TotalExecutions > 0.1;
}
