using EvoAITest.Core.Models.Execution;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for persisting and retrieving test execution results
/// </summary>
public interface ITestResultStorage
{
    /// <summary>
    /// Saves a test execution result to storage
    /// </summary>
    /// <param name="result">The execution result to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved result with generated ID</returns>
    Task<TestExecutionResult> SaveResultAsync(
        TestExecutionResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a test execution result by ID
    /// </summary>
    /// <param name="resultId">The execution result ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result or null if not found</returns>
    Task<TestExecutionResult?> GetResultAsync(
        Guid resultId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all execution results for a recording session
    /// </summary>
    /// <param name="recordingSessionId">The recording session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of execution results</returns>
    Task<IEnumerable<TestExecutionResult>> GetResultsByRecordingAsync(
        Guid recordingSessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution history with pagination
    /// </summary>
    /// <param name="skip">Number of results to skip</param>
    /// <param name="take">Number of results to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of execution results</returns>
    Task<IEnumerable<TestExecutionResult>> GetExecutionHistoryAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution statistics for a recording session
    /// </summary>
    /// <param name="recordingSessionId">The recording session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution statistics</returns>
    Task<TestExecutionStatistics> GetStatisticsAsync(
        Guid recordingSessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes execution results older than specified date
    /// </summary>
    /// <param name="olderThan">Delete results older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of results deleted</returns>
    Task<int> DeleteOldResultsAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches execution results by status, date range, or recording ID
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching execution results</returns>
    Task<IEnumerable<TestExecutionResult>> SearchResultsAsync(
        TestExecutionSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an existing execution result
    /// </summary>
    /// <param name="resultId">The execution result ID</param>
    /// <param name="status">New status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateStatusAsync(
        Guid resultId,
        TestExecutionStatus status,
        CancellationToken cancellationToken = default);
}
