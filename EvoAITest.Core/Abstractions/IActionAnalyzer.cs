using EvoAITest.Core.Models.Recording;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for analyzing user interactions and detecting intent using AI
/// </summary>
public interface IActionAnalyzer
{
    /// <summary>
    /// Analyzes a single user interaction to detect intent and generate description
    /// </summary>
    /// <param name="interaction">The user interaction to analyze</param>
    /// <param name="context">Additional context from the recording session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated interaction with intent and description</returns>
    Task<UserInteraction> AnalyzeInteractionAsync(
        UserInteraction interaction,
        RecordingSession context,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Analyzes multiple interactions to detect patterns and group related actions
    /// </summary>
    /// <param name="interactions">List of interactions to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Grouped and analyzed interactions</returns>
    Task<List<InteractionGroup>> AnalyzeInteractionPatternsAsync(
        List<UserInteraction> interactions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a natural language description for an action
    /// </summary>
    /// <param name="interaction">The interaction to describe</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Natural language description</returns>
    Task<string> GenerateDescriptionAsync(
        UserInteraction interaction,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Detects the intent of an action based on context
    /// </summary>
    /// <param name="interaction">The interaction to analyze</param>
    /// <param name="previousInteractions">Previous interactions for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detected intent and confidence score</returns>
    Task<(ActionIntent Intent, double Confidence)> DetectIntentAsync(
        UserInteraction interaction,
        List<UserInteraction> previousInteractions,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the accuracy of action recognition
    /// </summary>
    /// <param name="session">The recording session to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation metrics including accuracy percentage</returns>
    Task<ActionRecognitionMetrics> ValidateActionRecognitionAsync(
        RecordingSession session,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a group of related interactions
/// </summary>
public sealed class InteractionGroup
{
    /// <summary>
    /// Name or purpose of this interaction group
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Description of what this group accomplishes
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Detected intent for the entire group
    /// </summary>
    public ActionIntent GroupIntent { get; init; }
    
    /// <summary>
    /// Interactions in this group
    /// </summary>
    public required List<UserInteraction> Interactions { get; init; }
    
    /// <summary>
    /// Suggested test method name
    /// </summary>
    public string? SuggestedTestName { get; init; }
}

/// <summary>
/// Metrics for action recognition accuracy
/// </summary>
public sealed class ActionRecognitionMetrics
{
    /// <summary>
    /// Overall accuracy percentage (0-100)
    /// </summary>
    public double AccuracyPercentage { get; init; }
    
    /// <summary>
    /// Number of correctly recognized actions
    /// </summary>
    public int CorrectRecognitions { get; init; }
    
    /// <summary>
    /// Total number of actions evaluated
    /// </summary>
    public int TotalActions { get; init; }
    
    /// <summary>
    /// Average confidence score
    /// </summary>
    public double AverageConfidence { get; init; }
    
    /// <summary>
    /// Actions with low confidence (< 0.7)
    /// </summary>
    public List<UserInteraction> LowConfidenceActions { get; init; } = [];
    
    /// <summary>
    /// Breakdown by action type
    /// </summary>
    public Dictionary<ActionType, double> AccuracyByActionType { get; init; } = [];
}
