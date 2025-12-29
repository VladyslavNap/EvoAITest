using EvoAITest.Core.Models.Recording;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for generating test code from recorded user interactions
/// </summary>
public interface ITestGenerator
{
    /// <summary>
    /// Generates complete test code from a recording session
    /// </summary>
    /// <param name="session">The recording session to generate tests from</param>
    /// <param name="options">Options for test generation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated test code</returns>
    Task<GeneratedTest> GenerateTestAsync(
        RecordingSession session,
        TestGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates test code for a single interaction
    /// </summary>
    /// <param name="interaction">The interaction to generate code for</param>
    /// <param name="options">Options for code generation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated code snippet</returns>
    Task<string> GenerateCodeForInteractionAsync(
        UserInteraction interaction,
        TestGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates test code for a group of related interactions
    /// </summary>
    /// <param name="group">The interaction group to generate code for</param>
    /// <param name="options">Options for test generation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated test method code</returns>
    Task<string> GenerateTestMethodAsync(
        InteractionGroup group,
        TestGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates smart assertions based on page state
    /// </summary>
    /// <param name="interaction">The interaction to create assertions for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated assertions</returns>
    Task<List<ActionAssertion>> CreateAssertionsAsync(
        UserInteraction interaction,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the generated test code
    /// </summary>
    /// <param name="generatedTest">The generated test to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any issues found</returns>
    Task<TestValidationResult> ValidateGeneratedTestAsync(
        GeneratedTest generatedTest,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for test generation
/// </summary>
public sealed class TestGenerationOptions
{
    /// <summary>
    /// Test framework to generate code for
    /// </summary>
    public string TestFramework { get; init; } = "xUnit";
    
    /// <summary>
    /// Programming language for generated tests
    /// </summary>
    public string Language { get; init; } = "C#";
    
    /// <summary>
    /// Browser automation library
    /// </summary>
    public string AutomationLibrary { get; init; } = "Playwright";
    
    /// <summary>
    /// Whether to include comments in generated code
    /// </summary>
    public bool IncludeComments { get; init; } = true;
    
    /// <summary>
    /// Whether to use async/await patterns
    /// </summary>
    public bool UseAsyncAwait { get; init; } = true;
    
    /// <summary>
    /// Whether to generate page object model classes
    /// </summary>
    public bool GeneratePageObjects { get; init; } = false;
    
    /// <summary>
    /// Namespace for generated test classes
    /// </summary>
    public string Namespace { get; init; } = "EvoAITest.Generated";
    
    /// <summary>
    /// Class name for generated test
    /// </summary>
    public string? ClassName { get; init; }
    
    /// <summary>
    /// Whether to include setup and teardown methods
    /// </summary>
    public bool IncludeSetupTeardown { get; init; } = true;
    
    /// <summary>
    /// Whether to add assertions automatically
    /// </summary>
    public bool AutoGenerateAssertions { get; init; } = true;
    
    /// <summary>
    /// Minimum confidence threshold for including actions
    /// </summary>
    public double MinimumConfidenceThreshold { get; init; } = 0.7;
}

/// <summary>
/// Represents a complete generated test
/// </summary>
public sealed class GeneratedTest
{
    /// <summary>
    /// ID of the recording session this test was generated from
    /// </summary>
    public required Guid SessionId { get; init; }
    
    /// <summary>
    /// Complete test class code
    /// </summary>
    public required string Code { get; init; }
    
    /// <summary>
    /// Name of the test class
    /// </summary>
    public required string ClassName { get; init; }
    
    /// <summary>
    /// Namespace of the test class
    /// </summary>
    public required string Namespace { get; init; }
    
    /// <summary>
    /// Individual test methods generated
    /// </summary>
    public List<GeneratedTestMethod> Methods { get; init; } = [];
    
    /// <summary>
    /// Page object classes generated (if applicable)
    /// </summary>
    public List<GeneratedPageObject> PageObjects { get; init; } = [];
    
    /// <summary>
    /// Required using statements/imports
    /// </summary>
    public List<string> Imports { get; init; } = [];
    
    /// <summary>
    /// Generation timestamp
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Quality metrics for the generated test
    /// </summary>
    public TestQualityMetrics Metrics { get; init; } = new();
}

/// <summary>
/// Represents a generated test method
/// </summary>
public sealed class GeneratedTestMethod
{
    /// <summary>
    /// Method name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Method code
    /// </summary>
    public required string Code { get; init; }
    
    /// <summary>
    /// Description of what the test does
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Interactions this method was generated from
    /// </summary>
    public List<Guid> InteractionIds { get; init; } = [];
}

/// <summary>
/// Represents a generated page object class
/// </summary>
public sealed class GeneratedPageObject
{
    /// <summary>
    /// Class name
    /// </summary>
    public required string ClassName { get; init; }
    
    /// <summary>
    /// Complete class code
    /// </summary>
    public required string Code { get; init; }
    
    /// <summary>
    /// URL pattern this page object represents
    /// </summary>
    public string? UrlPattern { get; init; }
}

/// <summary>
/// Quality metrics for generated tests
/// </summary>
public sealed class TestQualityMetrics
{
    /// <summary>
    /// Number of test methods generated
    /// </summary>
    public int TestMethodCount { get; set; }
    
    /// <summary>
    /// Number of assertions generated
    /// </summary>
    public int AssertionCount { get; set; }
    
    /// <summary>
    /// Code coverage estimate (0-100)
    /// </summary>
    public double EstimatedCoverage { get; set; }
    
    /// <summary>
    /// Test maintainability score (0-100)
    /// </summary>
    public double MaintainabilityScore { get; set; }
    
    /// <summary>
    /// Lines of code generated
    /// </summary>
    public int LinesOfCode { get; set; }
}

/// <summary>
/// Result of test validation
/// </summary>
public sealed class TestValidationResult
{
    /// <summary>
    /// Whether the test passed validation
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Validation errors found
    /// </summary>
    public List<string> Errors { get; init; } = [];
    
    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; init; } = [];
    
    /// <summary>
    /// Suggestions for improvement
    /// </summary>
    public List<string> Suggestions { get; init; } = [];
}
