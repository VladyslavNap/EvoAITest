namespace EvoAITest.Core.Models.Execution;

/// <summary>
/// Represents an active test execution session
/// </summary>
public sealed class TestExecutionSession
{
    /// <summary>
    /// Unique identifier for this execution session
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Associated recording session ID
    /// </summary>
    public Guid RecordingSessionId { get; set; }

    /// <summary>
    /// Current execution status
    /// </summary>
    public TestExecutionStatus Status { get; set; } = TestExecutionStatus.Pending;

    /// <summary>
    /// When the session started
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the session completed
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Test code being executed
    /// </summary>
    public string? TestCode { get; set; }

    /// <summary>
    /// Test framework being used
    /// </summary>
    public required string TestFramework { get; set; }

    /// <summary>
    /// Current step being executed
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Step results collected so far
    /// </summary>
    public List<TestStepResult> StepResults { get; set; } = [];

    /// <summary>
    /// Artifacts collected during execution
    /// </summary>
    public List<TestArtifact> Artifacts { get; set; } = [];

    /// <summary>
    /// Console output buffer
    /// </summary>
    public List<string> ConsoleOutput { get; set; } = [];

    /// <summary>
    /// Error output buffer
    /// </summary>
    public List<string> ErrorOutput { get; set; } = [];

    /// <summary>
    /// Additional execution metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double ProgressPercentage => TotalSteps > 0
        ? (double)CurrentStep / TotalSteps * 100
        : 0;

    /// <summary>
    /// Converts the session to a final result
    /// </summary>
    public TestExecutionResult ToResult(string testName)
    {
        return new TestExecutionResult
        {
            Id = Id,
            RecordingSessionId = RecordingSessionId,
            TestName = testName,
            TestFramework = TestFramework,
            Status = Status,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            ErrorMessage = ErrorMessage,
            StandardOutput = string.Join(Environment.NewLine, ConsoleOutput),
            ErrorOutput = string.Join(Environment.NewLine, ErrorOutput),
            StepResults = StepResults,
            Artifacts = Artifacts,
            Metadata = Metadata
        };
    }
}
