using EvoAITest.Core.Models.Execution;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for collecting and processing test execution results
/// </summary>
public interface ITestResultCollector
{
    /// <summary>
    /// Starts a new test execution session
    /// </summary>
    /// <param name="recordingSessionId">Associated recording session ID</param>
    /// <param name="metadata">Additional execution metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created execution session</returns>
    Task<TestExecutionSession> StartSessionAsync(
        Guid recordingSessionId,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a test step result during execution
    /// </summary>
    /// <param name="sessionId">Execution session ID</param>
    /// <param name="stepResult">Step execution result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordStepResultAsync(
        Guid sessionId,
        TestStepResult stepResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a test execution session
    /// </summary>
    /// <param name="sessionId">Execution session ID</param>
    /// <param name="status">Final execution status</param>
    /// <param name="errorMessage">Error message if failed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completed execution result</returns>
    Task<TestExecutionResult> CompleteSessionAsync(
        Guid sessionId,
        TestExecutionStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attaches an artifact (screenshot, log file, etc.) to an execution
    /// </summary>
    /// <param name="sessionId">Execution session ID</param>
    /// <param name="artifactType">Type of artifact</param>
    /// <param name="artifactPath">Path to the artifact</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AttachArtifactAsync(
        Guid sessionId,
        string artifactType,
        string artifactPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records console output from test execution
    /// </summary>
    /// <param name="sessionId">Execution session ID</param>
    /// <param name="output">Console output text</param>
    /// <param name="isError">Whether this is error output</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordConsoleOutputAsync(
        Guid sessionId,
        string output,
        bool isError = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of an execution session
    /// </summary>
    /// <param name="sessionId">Execution session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current execution session state</returns>
    Task<TestExecutionSession?> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
