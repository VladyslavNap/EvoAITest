namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Stability classification for tests
/// </summary>
public enum StabilityClass
{
    /// <summary>
    /// Test is highly stable (>95% pass rate, low variance)
    /// </summary>
    Stable,

    /// <summary>
    /// Test is mostly stable (85-95% pass rate)
    /// </summary>
    MostlyStable,

    /// <summary>
    /// Test is unstable (70-85% pass rate)
    /// </summary>
    Unstable,

    /// <summary>
    /// Test is highly unstable (<70% pass rate)
    /// </summary>
    HighlyUnstable,

    /// <summary>
    /// Insufficient data to determine stability
    /// </summary>
    Unknown
}

/// <summary>
/// Comprehensive metrics for test stability analysis
/// </summary>
public sealed class TestStabilityMetrics
{
    /// <summary>
    /// Recording session ID these metrics apply to
    /// </summary>
    public Guid RecordingSessionId { get; set; }

    /// <summary>
    /// Test name
    /// </summary>
    public required string TestName { get; set; }

    /// <summary>
    /// Overall stability classification
    /// </summary>
    public StabilityClass StabilityClass { get; set; }

    /// <summary>
    /// Stability score (0-100, where 100 is most stable)
    /// </summary>
    public double StabilityScore { get; set; }

    /// <summary>
    /// Number of consecutive passes before any failure
    /// </summary>
    public int ConsecutivePassStreak { get; set; }

    /// <summary>
    /// Longest streak of consecutive passes
    /// </summary>
    public int LongestPassStreak { get; set; }

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int ConsecutiveFailureStreak { get; set; }

    /// <summary>
    /// Mean time between failures (in hours)
    /// </summary>
    public double? MeanTimeBetweenFailures { get; set; }

    /// <summary>
    /// Mean time to recovery after failure (in hours)
    /// </summary>
    public double? MeanTimeToRecovery { get; set; }

    /// <summary>
    /// Standard deviation of pass rate over rolling windows
    /// </summary>
    public double PassRateStandardDeviation { get; set; }

    /// <summary>
    /// Trend direction (-1: degrading, 0: stable, 1: improving)
    /// </summary>
    public int TrendDirection { get; set; }

    /// <summary>
    /// Rate of change in stability (percentage points per week)
    /// </summary>
    public double StabilityChangeRate { get; set; }

    /// <summary>
    /// Average execution duration in milliseconds
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// Duration variance (coefficient of variation)
    /// </summary>
    public double DurationVariance { get; set; }

    /// <summary>
    /// Percentage of runs that needed retry to pass
    /// </summary>
    public double RetryRate { get; set; }

    /// <summary>
    /// Total number of executions analyzed
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of executions in last 7 days
    /// </summary>
    public int ExecutionsLast7Days { get; set; }

    /// <summary>
    /// Number of executions in last 30 days
    /// </summary>
    public int ExecutionsLast30Days { get; set; }

    /// <summary>
    /// Pass rate for last 7 days
    /// </summary>
    public double PassRateLast7Days { get; set; }

    /// <summary>
    /// Pass rate for last 30 days
    /// </summary>
    public double PassRateLast30Days { get; set; }

    /// <summary>
    /// Time window start for these metrics
    /// </summary>
    public DateTimeOffset WindowStart { get; set; }

    /// <summary>
    /// Time window end for these metrics
    /// </summary>
    public DateTimeOffset WindowEnd { get; set; }

    /// <summary>
    /// When these metrics were calculated
    /// </summary>
    public DateTimeOffset CalculatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Confidence in the stability assessment (0-100)
    /// </summary>
    public double AssessmentConfidence { get; set; }

    /// <summary>
    /// Whether the test is considered reliable for production use
    /// </summary>
    public bool IsProductionReady => StabilityScore >= 85 && PassRateLast7Days >= 90;

    /// <summary>
    /// Additional metrics as key-value pairs
    /// </summary>
    public Dictionary<string, double> ExtendedMetrics { get; set; } = [];
}
