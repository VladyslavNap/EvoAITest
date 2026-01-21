namespace EvoAITest.Core.Models.Execution;

/// <summary>
/// Represents the execution status of a test
/// </summary>
public enum TestExecutionStatus
{
    /// <summary>
    /// Test execution is pending
    /// </summary>
    Pending,

    /// <summary>
    /// Test is currently running
    /// </summary>
    Running,

    /// <summary>
    /// Test passed successfully
    /// </summary>
    Passed,

    /// <summary>
    /// Test failed
    /// </summary>
    Failed,

    /// <summary>
    /// Test was skipped
    /// </summary>
    Skipped,

    /// <summary>
    /// Test execution was cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Test timed out
    /// </summary>
    Timeout,

    /// <summary>
    /// Compilation error occurred
    /// </summary>
    CompilationError
}

/// <summary>
/// Represents the result of a test execution
/// </summary>
public sealed class TestExecutionResult
{
    /// <summary>
    /// Unique identifier for this execution result
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Associated recording session ID
    /// </summary>
    public Guid RecordingSessionId { get; set; }

    /// <summary>
    /// Name of the test that was executed
    /// </summary>
    public required string TestName { get; set; }

    /// <summary>
    /// Test framework used (xUnit, NUnit, MSTest)
    /// </summary>
    public required string TestFramework { get; set; }

    /// <summary>
    /// Execution status
    /// </summary>
    public TestExecutionStatus Status { get; set; }

    /// <summary>
    /// When the test execution started
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the test execution completed
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Total execution duration in milliseconds
    /// </summary>
    public long DurationMs => CompletedAt.HasValue
        ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
        : 0;

    /// <summary>
    /// Error message if test failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if test failed
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Standard output from test execution
    /// </summary>
    public string? StandardOutput { get; set; }

    /// <summary>
    /// Error output from test execution
    /// </summary>
    public string? ErrorOutput { get; set; }

    /// <summary>
    /// Individual test step results
    /// </summary>
    public List<TestStepResult> StepResults { get; set; } = [];

    /// <summary>
    /// Artifacts generated during execution (screenshots, logs, etc.)
    /// </summary>
    public List<TestArtifact> Artifacts { get; set; } = [];

    /// <summary>
    /// Number of passed steps
    /// </summary>
    public int PassedSteps => StepResults.Count(s => s.Status == TestExecutionStatus.Passed);

    /// <summary>
    /// Number of failed steps
    /// </summary>
    public int FailedSteps => StepResults.Count(s => s.Status == TestExecutionStatus.Failed);

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int TotalSteps => StepResults.Count;

    /// <summary>
    /// Additional metadata about the execution
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Environment information (OS, browser version, etc.)
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Compilation errors if test failed to compile
    /// </summary>
    public List<string> CompilationErrors { get; set; } = [];
}
