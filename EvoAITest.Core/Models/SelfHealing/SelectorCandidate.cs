namespace EvoAITest.Core.Models.SelfHealing;

/// <summary>
/// Represents a candidate selector that could potentially replace a failed selector.
/// </summary>
public sealed record SelectorCandidate
{
    /// <summary>
    /// Gets the candidate selector.
    /// </summary>
    public required string Selector { get; init; }

    /// <summary>
    /// Gets the healing strategy used to generate this candidate.
    /// </summary>
    public required HealingStrategy Strategy { get; init; }

    /// <summary>
    /// Gets the base confidence score (0.0 to 1.0) for this candidate.
    /// </summary>
    public double BaseConfidence { get; init; }

    /// <summary>
    /// Gets metrics used to calculate the confidence score.
    /// </summary>
    public ConfidenceMetrics? Metrics { get; init; }

    /// <summary>
    /// Gets the element information if the selector was validated.
    /// </summary>
    public ElementInfo? ElementInfo { get; init; }

    /// <summary>
    /// Gets the text content of the element (if available).
    /// </summary>
    public string? ElementText { get; init; }

    /// <summary>
    /// Gets the visual similarity score (for visual matching strategy).
    /// </summary>
    public double? VisualSimilarityScore { get; init; }

    /// <summary>
    /// Gets the text similarity score (for text content matching).
    /// </summary>
    public double? TextSimilarityScore { get; init; }

    /// <summary>
    /// Gets the ARIA attribute match score.
    /// </summary>
    public double? AriaMatchScore { get; init; }

    /// <summary>
    /// Gets the position proximity score.
    /// </summary>
    public double? PositionScore { get; init; }

    /// <summary>
    /// Gets the attribute fuzzy match score.
    /// </summary>
    public double? AttributeMatchScore { get; init; }

    /// <summary>
    /// Gets the reasoning for why this candidate was selected (from LLM).
    /// </summary>
    public string? Reasoning { get; init; }

    /// <summary>
    /// Gets additional context about this candidate.
    /// </summary>
    public Dictionary<string, object>? Context { get; init; }

    /// <summary>
    /// Calculates the final confidence score based on all available metrics.
    /// </summary>
    public double CalculateFinalConfidence()
    {
        if (Metrics != null)
        {
            return Metrics.CalculateWeightedScore();
        }

        // Use individual scores if metrics not available
        var scores = new List<double> { BaseConfidence };
        
        if (VisualSimilarityScore.HasValue) scores.Add(VisualSimilarityScore.Value);
        if (TextSimilarityScore.HasValue) scores.Add(TextSimilarityScore.Value);
        if (AriaMatchScore.HasValue) scores.Add(AriaMatchScore.Value);
        if (PositionScore.HasValue) scores.Add(PositionScore.Value);
        if (AttributeMatchScore.HasValue) scores.Add(AttributeMatchScore.Value);

        return scores.Count > 0 ? scores.Average() : BaseConfidence;
    }
}
