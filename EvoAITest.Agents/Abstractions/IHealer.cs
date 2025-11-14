using EvoAITest.Agents.Models;

namespace EvoAITest.Agents.Abstractions;

/// <summary>
/// Defines a contract for self-healing capabilities in agents.
/// Heals errors and adapts to changing page structures.
/// </summary>
public interface IHealer
{
    /// <summary>
    /// Attempts to heal a failed step.
    /// </summary>
    /// <param name="failedStep">The step that failed.</param>
    /// <param name="error">The error that occurred.</param>
    /// <param name="context">Execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A healing result with suggested remediation.</returns>
    Task<HealingResult> HealStepAsync(AgentStep failedStep, Exception error, ExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes an error and determines if it's healable.
    /// </summary>
    /// <param name="error">The error to analyze.</param>
    /// <param name="context">Execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis of whether the error can be healed.</returns>
    Task<ErrorAnalysis> AnalyzeErrorAsync(Exception error, ExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests alternative strategies when an approach fails repeatedly.
    /// </summary>
    /// <param name="failedAttempts">Previous failed attempts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Alternative healing strategies.</returns>
    Task<IReadOnlyList<HealingStrategy>> SuggestAlternativesAsync(IReadOnlyList<AgentStepResult> failedAttempts, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a healing attempt.
/// </summary>
public sealed class HealingResult
{
    /// <summary>
    /// Gets or sets a value indicating whether healing was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the healed step (if successful).
    /// </summary>
    public AgentStep? HealedStep { get; set; }

    /// <summary>
    /// Gets or sets the healing strategy that was applied.
    /// </summary>
    public HealingStrategy? Strategy { get; set; }

    /// <summary>
    /// Gets or sets an explanation of what was changed.
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// Gets or sets the confidence level (0-1) in this healing.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets additional healing attempts to try if this one fails.
    /// </summary>
    public List<HealingResult> Alternatives { get; set; } = new();
}

/// <summary>
/// Represents analysis of an error.
/// </summary>
public sealed class ErrorAnalysis
{
    /// <summary>
    /// Gets or sets the error type.
    /// </summary>
    public ErrorType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the error is healable.
    /// </summary>
    public bool IsHealable { get; set; }

    /// <summary>
    /// Gets or sets the root cause of the error.
    /// </summary>
    public string? RootCause { get; set; }

    /// <summary>
    /// Gets or sets suggested healing strategies.
    /// </summary>
    public List<HealingStrategy> SuggestedStrategies { get; set; } = new();

    /// <summary>
    /// Gets or sets the severity of the error.
    /// </summary>
    public ErrorSeverity Severity { get; set; }
}

/// <summary>
/// Defines error types.
/// </summary>
public enum ErrorType
{
    /// <summary>Element not found.</summary>
    ElementNotFound,
    
    /// <summary>Timeout waiting for element.</summary>
    Timeout,
    
    /// <summary>Navigation failed.</summary>
    NavigationFailure,
    
    /// <summary>Element not interactable.</summary>
    ElementNotInteractable,
    
    /// <summary>JavaScript execution error.</summary>
    JavaScriptError,
    
    /// <summary>Network error.</summary>
    NetworkError,
    
    /// <summary>Authentication required.</summary>
    AuthenticationRequired,
    
    /// <summary>Page structure changed.</summary>
    PageStructureChanged,
    
    /// <summary>Unknown error.</summary>
    Unknown
}

/// <summary>
/// Defines error severity levels.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>Low severity, easily recoverable.</summary>
    Low,
    
    /// <summary>Medium severity, may require strategy change.</summary>
    Medium,
    
    /// <summary>High severity, may require task replanning.</summary>
    High,
    
    /// <summary>Critical severity, task cannot continue.</summary>
    Critical
}
