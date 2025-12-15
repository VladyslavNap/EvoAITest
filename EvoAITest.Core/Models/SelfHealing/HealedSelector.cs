namespace EvoAITest.Core.Models.SelfHealing;

/// <summary>
/// Represents a healed selector along with metadata about the healing process.
/// </summary>
public sealed record HealedSelector
{
    /// <summary>
    /// Gets the original selector that failed.
    /// </summary>
    public required string OriginalSelector { get; init; }

    /// <summary>
    /// Gets the new healed selector that should work.
    /// </summary>
    public required string NewSelector { get; init; }

    /// <summary>
    /// Gets the healing strategy that was used.
    /// </summary>
    public required HealingStrategy Strategy { get; init; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0) indicating how confident we are in this healing.
    /// Higher values indicate greater confidence.
    /// </summary>
    public required double ConfidenceScore { get; init; }

    /// <summary>
    /// Gets the reasoning behind why this selector was chosen (for LLM-based healing).
    /// </summary>
    public string? Reasoning { get; init; }

    /// <summary>
    /// Gets the expected text that should be present in the element (if known).
    /// </summary>
    public string? ExpectedText { get; init; }

    /// <summary>
    /// Gets the page URL where the healing occurred.
    /// </summary>
    public string? PageUrl { get; init; }

    /// <summary>
    /// Gets when this selector was healed.
    /// </summary>
    public DateTimeOffset HealedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets additional context about the healing (stored as JSON).
    /// </summary>
    public Dictionary<string, object>? Context { get; init; }

    /// <summary>
    /// Gets whether this healing was successful when verified.
    /// </summary>
    public bool? Verified { get; init; }

    /// <summary>
    /// Creates a healed selector with high confidence.
    /// </summary>
    public static HealedSelector CreateWithHighConfidence(
        string originalSelector,
        string newSelector,
        HealingStrategy strategy,
        double confidenceScore,
        string? reasoning = null)
    {
        return new HealedSelector
        {
            OriginalSelector = originalSelector,
            NewSelector = newSelector,
            Strategy = strategy,
            ConfidenceScore = confidenceScore,
            Reasoning = reasoning
        };
    }

    /// <summary>
    /// Determines if this healing is reliable enough to use automatically.
    /// </summary>
    public bool IsReliable(double threshold = 0.75)
    {
        return ConfidenceScore >= threshold;
    }
}
