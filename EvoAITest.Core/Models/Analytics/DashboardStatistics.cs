namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Dashboard summary statistics
/// </summary>
public sealed class DashboardStatistics
{
    /// <summary>
    /// Total number of test executions across all recordings
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Total number of unique tests
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Total number of recording sessions
    /// </summary>
    public int TotalRecordings { get; set; }

    /// <summary>
    /// Overall pass rate (0-100)
    /// </summary>
    public double OverallPassRate { get; set; }

    /// <summary>
    /// Number of flaky tests detected
    /// </summary>
    public int FlakyTestCount { get; set; }

    /// <summary>
    /// Number of stable tests
    /// </summary>
    public int StableTestCount { get; set; }

    /// <summary>
    /// Average test execution duration in milliseconds
    /// </summary>
    public long AverageExecutionDurationMs { get; set; }

    /// <summary>
    /// Total execution time across all tests (in hours)
    /// </summary>
    public double TotalExecutionTimeHours { get; set; }

    /// <summary>
    /// Executions in the last 24 hours
    /// </summary>
    public int ExecutionsLast24Hours { get; set; }

    /// <summary>
    /// Executions in the last 7 days
    /// </summary>
    public int ExecutionsLast7Days { get; set; }

    /// <summary>
    /// Executions in the last 30 days
    /// </summary>
    public int ExecutionsLast30Days { get; set; }

    /// <summary>
    /// Pass rate for last 24 hours
    /// </summary>
    public double PassRateLast24Hours { get; set; }

    /// <summary>
    /// Pass rate for last 7 days
    /// </summary>
    public double PassRateLast7Days { get; set; }

    /// <summary>
    /// Pass rate for last 30 days
    /// </summary>
    public double PassRateLast30Days { get; set; }

    /// <summary>
    /// Top 10 most executed tests
    /// </summary>
    public List<TestExecutionSummary> TopExecutedTests { get; set; } = [];

    /// <summary>
    /// Top 10 most failing tests
    /// </summary>
    public List<TestExecutionSummary> TopFailingTests { get; set; } = [];

    /// <summary>
    /// Top 10 slowest tests
    /// </summary>
    public List<TestExecutionSummary> SlowestTests { get; set; } = [];

    /// <summary>
    /// Recent trend data (last 30 days)
    /// </summary>
    public List<TestTrend> RecentTrends { get; set; } = [];

    /// <summary>
    /// When these statistics were calculated
    /// </summary>
    public DateTimeOffset CalculatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Health status of the test suite
    /// </summary>
    public TestSuiteHealth Health { get; set; }
}

/// <summary>
/// Health status of the test suite
/// </summary>
public enum TestSuiteHealth
{
    /// <summary>
    /// Excellent health (>95% pass rate, <5% flaky)
    /// </summary>
    Excellent,

    /// <summary>
    /// Good health (85-95% pass rate, <10% flaky)
    /// </summary>
    Good,

    /// <summary>
    /// Fair health (70-85% pass rate, <20% flaky)
    /// </summary>
    Fair,

    /// <summary>
    /// Poor health (<70% pass rate or >20% flaky)
    /// </summary>
    Poor,

    /// <summary>
    /// Unknown health (insufficient data)
    /// </summary>
    Unknown
}

/// <summary>
/// Summary of test execution metrics
/// </summary>
public sealed class TestExecutionSummary
{
    /// <summary>
    /// Recording session ID
    /// </summary>
    public Guid RecordingSessionId { get; set; }

    /// <summary>
    /// Test name
    /// </summary>
    public required string TestName { get; set; }

    /// <summary>
    /// Total number of executions
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Number of passed executions
    /// </summary>
    public int PassedCount { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Pass rate (0-100)
    /// </summary>
    public double PassRate { get; set; }

    /// <summary>
    /// Average duration in milliseconds
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// Flakiness score (0-100)
    /// </summary>
    public double FlakinessScore { get; set; }

    /// <summary>
    /// Last execution timestamp
    /// </summary>
    public DateTimeOffset LastExecutedAt { get; set; }

    /// <summary>
    /// Whether this test is flaky
    /// </summary>
    public bool IsFlaky { get; set; }
}
