using EvoAITest.Core.Models.Execution;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for executing generated test code
/// </summary>
public interface ITestExecutor
{
    /// <summary>
    /// Executes a generated test and returns the results
    /// </summary>
    /// <param name="testCode">The test code to execute</param>
    /// <param name="options">Options for test execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test execution result</returns>
    Task<TestExecutionResult> ExecuteTestAsync(
        string testCode,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a test from a recording session ID
    /// </summary>
    /// <param name="recordingSessionId">The recording session ID</param>
    /// <param name="options">Options for test execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test execution result</returns>
    Task<TestExecutionResult> ExecuteFromRecordingAsync(
        Guid recordingSessionId,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that test code can be compiled
    /// </summary>
    /// <param name="testCode">The test code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any compilation errors</returns>
    Task<TestValidationResult> ValidateTestAsync(
        string testCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple tests in a batch
    /// </summary>
    /// <param name="testCodes">Collection of test codes to execute</param>
    /// <param name="options">Options for test execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of test execution results</returns>
    Task<IEnumerable<TestExecutionResult>> ExecuteBatchAsync(
        IEnumerable<string> testCodes,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supported test frameworks
    /// </summary>
    /// <returns>List of supported test framework names</returns>
    IEnumerable<string> GetSupportedFrameworks();
}
