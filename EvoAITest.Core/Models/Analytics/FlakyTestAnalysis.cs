namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Represents the severity level of test flakiness
/// </summary>
public enum FlakinessSeverity
{
    /// <summary>
    /// Test is stable with no flakiness detected
    /// </summary>
    None = 0,

    /// <summary>
    /// Minor flakiness detected (1-10% failure rate with retries)
    /// </summary>
    Low = 1,

    /// <summary>
    /// Moderate flakiness detected (11-30% failure rate)
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High flakiness detected (31-60% failure rate)
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical flakiness detected (>60% failure rate or unreliable)
    /// </summary>
    Critical = 4
}

/// <summary>
/// Analysis of flaky test behavior and patterns
/// </summary>
public sealed class FlakyTestAnalysis
{
    /// <summary>
    /// Unique identifier for this analysis
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Recording session ID being analyzed
    /// </summary>
    public Guid RecordingSessionId { get; set; }

    /// <summary>
    /// Test name
    /// </summary>
    public required string TestName { get; set; }

    /// <summary>
    /// Flakiness score (0-100, where 100 is most flaky)
    /// </summary>
    public double FlakinessScore { get; set; }

    /// <summary>
    /// Severity level of flakiness
    /// </summary>
    public FlakinessSeverity Severity { get; set; }

    /// <summary>
    /// Total number of executions analyzed
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of times test failed initially but passed on retry
    /// </summary>
    public int FlakyFailureCount { get; set; }

    /// <summary>
    /// Number of times test passed consistently
    /// </summary>
    public int ConsistentPassCount { get; set; }

    /// <summary>
    /// Number of times test failed consistently
    /// </summary>
    public int ConsistentFailureCount { get; set; }

    /// <summary>
    /// Pass rate percentage (0-100)
    /// </summary>
    public double PassRate { get; set; }

    /// <summary>
    /// Coefficient of variation for execution duration (measure of consistency)
    /// </summary>
    public double DurationVariability { get; set; }

    /// <summary>
    /// Detected failure patterns
    /// </summary>
    public List<FlakyTestPattern> Patterns { get; set; } = [];

    /// <summary>
    /// When this analysis was performed
    /// </summary>
    public DateTimeOffset AnalyzedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last execution date included in analysis
    /// </summary>
    public DateTimeOffset? LastExecutionAt { get; set; }

    /// <summary>
    /// Recommended remediation actions
    /// </summary>
    public List<string> Recommendations { get; set; } = [];

    /// <summary>
    /// Root cause categories identified
    /// </summary>
    public List<string> RootCauses { get; set; } = [];

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Whether this test is considered flaky
    /// </summary>
    public bool IsFlaky => FlakinessScore > 20 || FlakyFailureCount > 0;

    /// <summary>
    /// Confidence level of the analysis (0-100)
    /// </summary>
    public double AnalysisConfidence { get; set; }

    /// <summary>
    /// Average time to failure (in milliseconds) when test fails
    /// </summary>
    public long? AverageTimeToFailure { get; set; }

    /// <summary>
    /// Standard deviation of execution duration
    /// </summary>
    public long? ExecutionDurationStdDev { get; set; }
}
