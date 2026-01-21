namespace EvoAITest.Core.Models.Execution;

/// <summary>
/// Criteria for searching test execution results
/// </summary>
public sealed class TestExecutionSearchCriteria
{
    /// <summary>
    /// Filter by recording session ID
    /// </summary>
    public Guid? RecordingSessionId { get; set; }

    /// <summary>
    /// Filter by execution status
    /// </summary>
    public TestExecutionStatus? Status { get; set; }

    /// <summary>
    /// Filter by test framework
    /// </summary>
    public string? TestFramework { get; set; }

    /// <summary>
    /// Filter by test name (partial match)
    /// </summary>
    public string? TestNameContains { get; set; }

    /// <summary>
    /// Filter by start date (results after this date)
    /// </summary>
    public DateTimeOffset? StartedAfter { get; set; }

    /// <summary>
    /// Filter by start date (results before this date)
    /// </summary>
    public DateTimeOffset? StartedBefore { get; set; }

    /// <summary>
    /// Filter by minimum duration in milliseconds
    /// </summary>
    public long? MinDurationMs { get; set; }

    /// <summary>
    /// Filter by maximum duration in milliseconds
    /// </summary>
    public long? MaxDurationMs { get; set; }

    /// <summary>
    /// Number of results to skip (for pagination)
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of results to take (for pagination)
    /// </summary>
    public int Take { get; set; } = 50;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "StartedAt";

    /// <summary>
    /// Sort direction (true = descending, false = ascending)
    /// </summary>
    public bool SortDescending { get; set; } = true;
}
