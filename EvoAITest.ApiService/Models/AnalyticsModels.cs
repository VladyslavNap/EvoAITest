using EvoAITest.Core.Models.Analytics;

namespace EvoAITest.ApiService.Models;

/// <summary>
/// Health score response with detailed metrics
/// </summary>
public sealed class HealthScoreResponse
{
    /// <summary>
    /// Overall health status
    /// </summary>
    public TestSuiteHealth Health { get; set; }

    /// <summary>
    /// Numeric health score (0-100)
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Overall pass rate percentage
    /// </summary>
    public double PassRate { get; set; }

    /// <summary>
    /// Percentage of flaky tests
    /// </summary>
    public double FlakyTestPercentage { get; set; }

    /// <summary>
    /// Total number of tests
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Total number of executions
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// When this health score was calculated
    /// </summary>
    public DateTimeOffset CalculatedAt { get; set; }

    /// <summary>
    /// Health trend indicators
    /// </summary>
    public required HealthTrends Trends { get; set; }
}

/// <summary>
/// Health trend indicators
/// </summary>
public sealed class HealthTrends
{
    /// <summary>
    /// Pass rate trend (improving, stable, degrading)
    /// </summary>
    public required string PassRateTrend { get; set; }

    /// <summary>
    /// Flaky test trend (increasing, stable, decreasing)
    /// </summary>
    public required string FlakyTestTrend { get; set; }
}

/// <summary>
/// Request for comparing two time periods
/// </summary>
public sealed class PeriodComparisonRequest
{
    /// <summary>
    /// Start of current period
    /// </summary>
    public DateTimeOffset CurrentPeriodStart { get; set; }

    /// <summary>
    /// End of current period
    /// </summary>
    public DateTimeOffset CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Start of previous period (for comparison)
    /// </summary>
    public DateTimeOffset PreviousPeriodStart { get; set; }

    /// <summary>
    /// End of previous period (for comparison)
    /// </summary>
    public DateTimeOffset PreviousPeriodEnd { get; set; }

    /// <summary>
    /// Optional recording session filter
    /// </summary>
    public Guid? RecordingSessionId { get; set; }
}

/// <summary>
/// Response with period comparison results
/// </summary>
public sealed class PeriodComparisonResponse
{
    /// <summary>
    /// Summary of current period
    /// </summary>
    public required PeriodSummary CurrentPeriod { get; set; }

    /// <summary>
    /// Summary of previous period
    /// </summary>
    public required PeriodSummary PreviousPeriod { get; set; }

    /// <summary>
    /// Comparison metrics between periods
    /// </summary>
    public required ComparisonMetrics Comparison { get; set; }
}

/// <summary>
/// Summary statistics for a time period
/// </summary>
public sealed class PeriodSummary
{
    /// <summary>
    /// Total test executions
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
    /// Pass rate percentage
    /// </summary>
    public double PassRate { get; set; }

    /// <summary>
    /// Average execution duration in milliseconds
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// Number of flaky tests detected
    /// </summary>
    public int FlakyTestCount { get; set; }

    /// <summary>
    /// Number of unique tests executed
    /// </summary>
    public int UniqueTestCount { get; set; }

    /// <summary>
    /// Number of data points in this summary
    /// </summary>
    public int DataPoints { get; set; }
}

/// <summary>
/// Metrics comparing two periods
/// </summary>
public sealed class ComparisonMetrics
{
    /// <summary>
    /// Change in pass rate (percentage points)
    /// </summary>
    public double PassRateChange { get; set; }

    /// <summary>
    /// Change in execution count
    /// </summary>
    public int ExecutionCountChange { get; set; }

    /// <summary>
    /// Change in flaky test count
    /// </summary>
    public int FlakyTestCountChange { get; set; }

    /// <summary>
    /// Change in average duration (milliseconds)
    /// </summary>
    public long AvgDurationChange { get; set; }

    /// <summary>
    /// Pass rate change as percentage (e.g., +10% means 10% improvement)
    /// </summary>
    public double PassRateChangePercent { get; set; }

    /// <summary>
    /// Overall verdict (significantly_improved, improved, stable, slightly_degraded, degraded)
    /// </summary>
    public required string Verdict { get; set; }
}
