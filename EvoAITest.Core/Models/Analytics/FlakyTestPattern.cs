namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Type of failure pattern detected in flaky tests
/// </summary>
public enum PatternType
{
    /// <summary>
    /// Test fails intermittently with no clear pattern
    /// </summary>
    Intermittent,

    /// <summary>
    /// Test fails at specific times (time-based)
    /// </summary>
    Temporal,

    /// <summary>
    /// Test fails based on execution order or dependencies
    /// </summary>
    Sequential,

    /// <summary>
    /// Test fails due to timing/race conditions
    /// </summary>
    TimingDependent,

    /// <summary>
    /// Test fails due to external resource availability
    /// </summary>
    ResourceDependent,

    /// <summary>
    /// Test fails on specific environments or browsers
    /// </summary>
    EnvironmentSpecific,

    /// <summary>
    /// Test fails after certain number of consecutive runs
    /// </summary>
    RunCount,

    /// <summary>
    /// Test has increasing failure rate over time
    /// </summary>
    Degrading
}

/// <summary>
/// Represents a detected pattern in flaky test behavior
/// </summary>
public sealed class FlakyTestPattern
{
    /// <summary>
    /// Unique identifier for this pattern
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Type of pattern detected
    /// </summary>
    public PatternType Type { get; set; }

    /// <summary>
    /// Human-readable description of the pattern
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Confidence level that this pattern is accurate (0-100)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Number of occurrences of this pattern
    /// </summary>
    public int Occurrences { get; set; }

    /// <summary>
    /// Error messages associated with this pattern
    /// </summary>
    public List<string> ErrorMessages { get; set; } = [];

    /// <summary>
    /// Steps that typically fail when this pattern occurs
    /// </summary>
    public List<int> AffectedStepNumbers { get; set; } = [];

    /// <summary>
    /// Time range when this pattern occurs (for temporal patterns)
    /// </summary>
    public (TimeSpan Start, TimeSpan End)? TimeWindow { get; set; }

    /// <summary>
    /// Environmental factors associated with this pattern
    /// </summary>
    public Dictionary<string, string> EnvironmentalFactors { get; set; } = [];

    /// <summary>
    /// When this pattern was first detected
    /// </summary>
    public DateTimeOffset FirstDetectedAt { get; set; }

    /// <summary>
    /// When this pattern was last observed
    /// </summary>
    public DateTimeOffset LastObservedAt { get; set; }

    /// <summary>
    /// Suggested fix or remediation for this pattern
    /// </summary>
    public string? SuggestedFix { get; set; }

    /// <summary>
    /// Related patterns that might be connected
    /// </summary>
    public List<Guid> RelatedPatternIds { get; set; } = [];

    /// <summary>
    /// Statistical correlation coefficient (-1 to 1)
    /// </summary>
    public double? CorrelationStrength { get; set; }
}
