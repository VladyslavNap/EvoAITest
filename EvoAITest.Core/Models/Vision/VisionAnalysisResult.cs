namespace EvoAITest.Core.Models.Vision;

/// <summary>
/// Represents the result of a vision analysis operation.
/// </summary>
public sealed class VisionAnalysisResult
{
    /// <summary>
    /// Gets the detected elements in the screenshot.
    /// </summary>
    public required List<DetectedElement> Elements { get; init; }

    /// <summary>
    /// Gets the overall success status of the analysis.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets any error message if the analysis failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the provider used for analysis (GPT4Vision, AzureCV, etc.).
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Gets the duration of the analysis operation.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets the raw response from the vision model (for debugging).
    /// </summary>
    public string? RawResponse { get; init; }

    /// <summary>
    /// Gets general observations about the screenshot.
    /// </summary>
    public string? GeneralObservations { get; init; }

    /// <summary>
    /// Gets detected text content (OCR results).
    /// </summary>
    public List<string>? DetectedText { get; init; }

    /// <summary>
    /// Gets the screenshot dimensions.
    /// </summary>
    public (int Width, int Height)? ScreenshotDimensions { get; init; }

    /// <summary>
    /// Gets additional metadata about the analysis.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets when the analysis was performed.
    /// </summary>
    public DateTimeOffset AnalyzedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the confidence score for the overall analysis.
    /// </summary>
    public double? OverallConfidence { get; init; }

    /// <summary>
    /// Gets whether any high-confidence elements were detected.
    /// </summary>
    public bool HasHighConfidenceElements => 
        Elements.Any(e => e.Confidence >= 0.8);

    /// <summary>
    /// Gets the number of interactable elements detected.
    /// </summary>
    public int InteractableElementCount => 
        Elements.Count(e => e.IsInteractable);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static VisionAnalysisResult CreateSuccess(
        List<DetectedElement> elements,
        string? provider = null,
        TimeSpan? duration = null)
    {
        return new VisionAnalysisResult
        {
            Success = true,
            Elements = elements,
            Provider = provider,
            Duration = duration,
            OverallConfidence = elements.Any() ? elements.Average(e => e.Confidence) : 0
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static VisionAnalysisResult CreateFailure(string errorMessage, string? provider = null)
    {
        return new VisionAnalysisResult
        {
            Success = false,
            Elements = new List<DetectedElement>(),
            ErrorMessage = errorMessage,
            Provider = provider
        };
    }

    /// <summary>
    /// Gets a summary of the analysis results.
    /// </summary>
    public string GetSummary()
    {
        if (!Success)
            return $"Analysis failed: {ErrorMessage}";

        var summary = $"Found {Elements.Count} elements";
        
        if (InteractableElementCount > 0)
            summary += $" ({InteractableElementCount} interactable)";

        if (OverallConfidence.HasValue)
            summary += $" with {OverallConfidence.Value:P0} average confidence";

        if (Duration.HasValue)
            summary += $" in {Duration.Value.TotalMilliseconds:F0}ms";

        return summary;
    }
}
