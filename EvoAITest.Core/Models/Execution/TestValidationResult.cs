namespace EvoAITest.Core.Models.Execution;

/// <summary>
/// Result of test code validation
/// </summary>
public sealed class TestValidationResult
{
    /// <summary>
    /// Whether the test code is valid and can be compiled
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of compilation errors
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// List of compilation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Detected test framework
    /// </summary>
    public string? DetectedFramework { get; set; }

    /// <summary>
    /// Number of test methods found
    /// </summary>
    public int TestMethodCount { get; set; }

    /// <summary>
    /// Additional validation messages
    /// </summary>
    public List<string> Messages { get; set; } = [];
}
