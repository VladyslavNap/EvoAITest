namespace EvoAITest.Core.Models.SelfHealing;

/// <summary>
/// Contains detailed metrics used to calculate confidence scores for selector healing.
/// </summary>
public sealed class ConfidenceMetrics
{
    /// <summary>
    /// Gets or sets the visual similarity score (0.0 to 1.0).
    /// Measures how similar the element looks to the expected appearance.
    /// </summary>
    public double VisualSimilarity { get; init; }

    /// <summary>
    /// Gets or sets the weight for visual similarity in the final score.
    /// </summary>
    public double VisualSimilarityWeight { get; init; } = 0.25;

    /// <summary>
    /// Gets or sets the text match score (0.0 to 1.0).
    /// Measures how well the text content matches the expected text.
    /// </summary>
    public double TextMatch { get; init; }

    /// <summary>
    /// Gets or sets the weight for text matching in the final score.
    /// </summary>
    public double TextMatchWeight { get; init; } = 0.30;

    /// <summary>
    /// Gets or sets the ARIA/accessibility attribute match score (0.0 to 1.0).
    /// </summary>
    public double AriaMatch { get; init; }

    /// <summary>
    /// Gets or sets the weight for ARIA matching in the final score.
    /// </summary>
    public double AriaMatchWeight { get; init; } = 0.20;

    /// <summary>
    /// Gets or sets the position similarity score (0.0 to 1.0).
    /// Measures how close the element is to the expected position.
    /// </summary>
    public double PositionSimilarity { get; init; }

    /// <summary>
    /// Gets or sets the weight for position matching in the final score.
    /// </summary>
    public double PositionSimilarityWeight { get; init; } = 0.15;

    /// <summary>
    /// Gets or sets the attribute fuzzy match score (0.0 to 1.0).
    /// </summary>
    public double AttributeMatch { get; init; }

    /// <summary>
    /// Gets or sets the weight for attribute matching in the final score.
    /// </summary>
    public double AttributeMatchWeight { get; init; } = 0.10;

    /// <summary>
    /// Gets or sets the selector specificity score (0.0 to 1.0).
    /// More specific selectors get higher scores.
    /// </summary>
    public double SelectorSpecificity { get; init; }

    /// <summary>
    /// Gets or sets whether the element is currently visible.
    /// </summary>
    public bool IsVisible { get; init; }

    /// <summary>
    /// Gets or sets whether the element is currently interactable.
    /// </summary>
    public bool IsInteractable { get; init; }

    /// <summary>
    /// Gets or sets the number of elements matching this selector.
    /// Ideally should be 1 for best confidence.
    /// </summary>
    public int MatchCount { get; init; }

    /// <summary>
    /// Calculates the weighted confidence score based on all metrics.
    /// </summary>
    public double CalculateWeightedScore()
    {
        var score = 
            (VisualSimilarity * VisualSimilarityWeight) +
            (TextMatch * TextMatchWeight) +
            (AriaMatch * AriaMatchWeight) +
            (PositionSimilarity * PositionSimilarityWeight) +
            (AttributeMatch * AttributeMatchWeight);

        // Apply penalties
        if (!IsVisible) score *= 0.5;
        if (!IsInteractable) score *= 0.8;
        if (MatchCount > 1) score *= (1.0 / MatchCount); // Penalty for ambiguous selectors
        if (MatchCount == 0) score = 0;

        // Bonus for high specificity
        score += (SelectorSpecificity * 0.1);

        return Math.Clamp(score, 0.0, 1.0);
    }

    /// <summary>
    /// Creates confidence metrics with equal weights.
    /// </summary>
    public static ConfidenceMetrics CreateBalanced(
        double visualSimilarity,
        double textMatch,
        double ariaMatch,
        double positionSimilarity,
        double attributeMatch)
    {
        return new ConfidenceMetrics
        {
            VisualSimilarity = visualSimilarity,
            VisualSimilarityWeight = 0.20,
            TextMatch = textMatch,
            TextMatchWeight = 0.20,
            AriaMatch = ariaMatch,
            AriaMatchWeight = 0.20,
            PositionSimilarity = positionSimilarity,
            PositionSimilarityWeight = 0.20,
            AttributeMatch = attributeMatch,
            AttributeMatchWeight = 0.20
        };
    }

    /// <summary>
    /// Creates confidence metrics optimized for accessible web applications.
    /// </summary>
    public static ConfidenceMetrics CreateAccessibilityFocused(
        double visualSimilarity,
        double textMatch,
        double ariaMatch,
        double positionSimilarity,
        double attributeMatch)
    {
        return new ConfidenceMetrics
        {
            VisualSimilarity = visualSimilarity,
            VisualSimilarityWeight = 0.10,
            TextMatch = textMatch,
            TextMatchWeight = 0.25,
            AriaMatch = ariaMatch,
            AriaMatchWeight = 0.40, // Heavily weighted
            PositionSimilarity = positionSimilarity,
            PositionSimilarityWeight = 0.10,
            AttributeMatch = attributeMatch,
            AttributeMatchWeight = 0.15
        };
    }
}
