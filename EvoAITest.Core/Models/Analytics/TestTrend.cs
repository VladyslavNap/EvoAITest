namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Time interval for trend aggregation
/// </summary>
public enum TrendInterval
{
    /// <summary>
    /// Hourly aggregation
    /// </summary>
    Hourly,

    /// <summary>
    /// Daily aggregation
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly aggregation
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly aggregation
    /// </summary>
    Monthly
}

/// <summary>
/// Represents a test execution trend data point
/// </summary>
public sealed class TestTrend
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Recording session ID (null for overall trends)
    /// </summary>
    public Guid? RecordingSessionId { get; set; }

    /// <summary>
    /// Test name (null for aggregated trends)
    /// </summary>
    public string? TestName { get; set; }

    /// <summary>
    /// Time period this data point represents
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Interval type for this trend
    /// </summary>
    public TrendInterval Interval { get; set; }

    /// <summary>
    /// Total number of test executions in this period
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
    /// Number of skipped executions
    /// </summary>
    public int SkippedExecutions { get; set; }

    /// <summary>
    /// Pass rate for this period (0-100)
    /// </summary>
    public double PassRate { get; set; }

    /// <summary>
    /// Average execution duration in milliseconds
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// Minimum execution duration in this period
    /// </summary>
    public long MinDurationMs { get; set; }

    /// <summary>
    /// Maximum execution duration in this period
    /// </summary>
    public long MaxDurationMs { get; set; }

    /// <summary>
    /// Standard deviation of execution duration
    /// </summary>
    public long DurationStdDev { get; set; }

    /// <summary>
    /// Number of flaky test occurrences detected
    /// </summary>
    public int FlakyTestCount { get; set; }

    /// <summary>
    /// Number of unique tests executed
    /// </summary>
    public int UniqueTestCount { get; set; }

    /// <summary>
    /// Number of compilation errors
    /// </summary>
    public int CompilationErrors { get; set; }

    /// <summary>
    /// Number of tests that needed retry
    /// </summary>
    public int RetriedTests { get; set; }

    /// <summary>
    /// Average number of steps per test
    /// </summary>
    public double AverageStepsPerTest { get; set; }

    /// <summary>
    /// When this trend data was calculated
    /// </summary>
    public DateTimeOffset CalculatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional metrics
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = [];
}
