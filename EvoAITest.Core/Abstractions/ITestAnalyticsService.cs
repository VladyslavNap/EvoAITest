using EvoAITest.Core.Models.Analytics;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for analyzing test execution data and generating analytics
/// </summary>
public interface ITestAnalyticsService
{
    /// <summary>
    /// Generates comprehensive dashboard statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics</returns>
    Task<DashboardStatistics> GetDashboardStatisticsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates trends for a specific time period
    /// </summary>
    /// <param name="interval">Trend interval (hourly, daily, weekly, monthly)</param>
    /// <param name="startDate">Start date for trend analysis</param>
    /// <param name="endDate">End date for trend analysis</param>
    /// <param name="recordingSessionId">Optional recording session filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of trend data points</returns>
    Task<List<TestTrend>> CalculateTrendsAsync(
        TrendInterval interval,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid? recordingSessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trends for a recording session
    /// </summary>
    /// <param name="recordingSessionId">Recording session ID</param>
    /// <param name="interval">Trend interval</param>
    /// <param name="days">Number of days to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of trends</returns>
    Task<List<TestTrend>> GetRecordingTrendsAsync(
        Guid recordingSessionId,
        TrendInterval interval,
        int days = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates insights for a specific recording
    /// </summary>
    /// <param name="recordingSessionId">Recording session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recording-specific insights</returns>
    Task<RecordingInsights> GetRecordingInsightsAsync(
        Guid recordingSessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top failing tests across all recordings
    /// </summary>
    /// <param name="count">Number of tests to return</param>
    /// <param name="days">Days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of test execution summaries</returns>
    Task<List<TestExecutionSummary>> GetTopFailingTestsAsync(
        int count = 10,
        int days = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets slowest tests across all recordings
    /// </summary>
    /// <param name="count">Number of tests to return</param>
    /// <param name="days">Days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of test execution summaries</returns>
    Task<List<TestExecutionSummary>> GetSlowestTestsAsync(
        int count = 10,
        int days = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets most executed tests
    /// </summary>
    /// <param name="count">Number of tests to return</param>
    /// <param name="days">Days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of test execution summaries</returns>
    Task<List<TestExecutionSummary>> GetMostExecutedTestsAsync(
        int count = 10,
        int days = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists calculated trends to database
    /// </summary>
    /// <param name="trends">Trends to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveTrendsAsync(
        IEnumerable<TestTrend> trends,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists flaky test analysis to database
    /// </summary>
    /// <param name="analysis">Analysis to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveFlakyTestAnalysisAsync(
        FlakyTestAnalysis analysis,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical trend data from database
    /// </summary>
    /// <param name="recordingSessionId">Optional recording filter</param>
    /// <param name="interval">Trend interval</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical trends</returns>
    Task<List<TestTrend>> GetHistoricalTrendsAsync(
        Guid? recordingSessionId,
        TrendInterval interval,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines overall test suite health
    /// </summary>
    /// <param name="statistics">Dashboard statistics</param>
    /// <returns>Health status</returns>
    TestSuiteHealth DetermineHealth(DashboardStatistics statistics);
}

/// <summary>
/// Insights for a specific recording
/// </summary>
public sealed class RecordingInsights
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
    /// Overall pass rate
    /// </summary>
    public double PassRate { get; set; }

    /// <summary>
    /// Flaky test analysis if test is flaky
    /// </summary>
    public FlakyTestAnalysis? FlakyAnalysis { get; set; }

    /// <summary>
    /// Stability metrics
    /// </summary>
    public TestStabilityMetrics? StabilityMetrics { get; set; }

    /// <summary>
    /// Recent trend (last 30 days)
    /// </summary>
    public List<TestTrend> RecentTrends { get; set; } = [];

    /// <summary>
    /// Performance baseline (average duration)
    /// </summary>
    public long BaselineDurationMs { get; set; }

    /// <summary>
    /// Whether performance is degrading
    /// </summary>
    public bool PerformanceDegrading { get; set; }

    /// <summary>
    /// Key recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = [];

    /// <summary>
    /// Identified issues
    /// </summary>
    public List<string> Issues { get; set; } = [];

    /// <summary>
    /// Execution statistics
    /// </summary>
    public ExecutionStatisticsSummary Statistics { get; set; } = new();
}

/// <summary>
/// Summary of execution statistics
/// </summary>
public sealed class ExecutionStatisticsSummary
{
    /// <summary>
    /// Total number of executions
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Last 7 days execution count
    /// </summary>
    public int Last7DaysExecutions { get; set; }

    /// <summary>
    /// Last 30 days execution count
    /// </summary>
    public int Last30DaysExecutions { get; set; }

    /// <summary>
    /// Average duration in milliseconds
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// Fastest execution time
    /// </summary>
    public long FastestDurationMs { get; set; }

    /// <summary>
    /// Slowest execution time
    /// </summary>
    public long SlowestDurationMs { get; set; }
}
