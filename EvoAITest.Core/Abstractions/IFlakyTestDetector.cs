using EvoAITest.Core.Models.Analytics;
using EvoAITest.Core.Models.Execution;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for detecting and analyzing flaky test behavior
/// </summary>
public interface IFlakyTestDetector
{
    /// <summary>
    /// Analyzes a recording session's test executions for flakiness
    /// </summary>
    /// <param name="recordingSessionId">Recording session to analyze</param>
    /// <param name="criteria">Detection criteria (null for default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Flaky test analysis</returns>
    Task<FlakyTestAnalysis> AnalyzeRecordingAsync(
        Guid recordingSessionId,
        FlakyCriteria? criteria = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a collection of test execution results for flakiness
    /// </summary>
    /// <param name="results">Test execution results to analyze</param>
    /// <param name="criteria">Detection criteria (null for default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Flaky test analysis</returns>
    Task<FlakyTestAnalysis> AnalyzeExecutionResultsAsync(
        IEnumerable<TestExecutionResult> results,
        FlakyCriteria? criteria = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects patterns in flaky test failures
    /// </summary>
    /// <param name="results">Test execution results to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of detected patterns</returns>
    Task<List<FlakyTestPattern>> DetectPatternsAsync(
        IEnumerable<TestExecutionResult> results,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates stability metrics for a test
    /// </summary>
    /// <param name="recordingSessionId">Recording session ID</param>
    /// <param name="windowDays">Number of days to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stability metrics</returns>
    Task<TestStabilityMetrics> CalculateStabilityMetricsAsync(
        Guid recordingSessionId,
        int windowDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all flaky tests across the system
    /// </summary>
    /// <param name="criteria">Detection criteria (null for default)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of flaky test analyses</returns>
    Task<List<FlakyTestAnalysis>> GetAllFlakyTestsAsync(
        FlakyCriteria? criteria = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates flakiness score for a set of execution results
    /// </summary>
    /// <param name="results">Test execution results</param>
    /// <param name="criteria">Detection criteria</param>
    /// <returns>Flakiness score (0-100)</returns>
    double CalculateFlakinessScore(
        IEnumerable<TestExecutionResult> results,
        FlakyCriteria criteria);

        /// <summary>
        /// Determines severity level based on flakiness score and metrics
        /// </summary>
        /// <param name="score">Flakiness score</param>
        /// <param name="passRate">Pass rate percentage</param>
        /// <param name="retryCount">Number of flaky retries</param>
        /// <returns>Flakiness severity</returns>
        FlakinessSeverity DetermineSeverity(
            double score,
            double passRate,
            int retryCount);

        /// <summary>
        /// Gets recording session IDs that have test executions within the specified time window
        /// </summary>
        /// <param name="days">Number of days to look back</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of recording session IDs</returns>
        Task<List<Guid>> GetRecordingsWithRecentExecutionsAsync(
            int days = 30,
            CancellationToken cancellationToken = default);
    }
