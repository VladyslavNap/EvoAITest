namespace EvoAITest.Core.Models.Vision;

/// <summary>
/// Represents a UI element detected in a screenshot through vision analysis.
/// </summary>
public sealed record DetectedElement
{
    /// <summary>
    /// Gets the type of element detected.
    /// </summary>
    public required ElementType Type { get; init; }

    /// <summary>
    /// Gets the bounding box coordinates of the element.
    /// </summary>
    public required ElementBoundingBox BoundingBox { get; init; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0) that this detection is correct.
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// Gets the text content of the element (if any).
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets a description of the element (from vision model).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the suggested CSS selector for this element (if generated).
    /// </summary>
    public string? SuggestedSelector { get; init; }

    /// <summary>
    /// Gets additional attributes detected (e.g., aria-label, placeholder).
    /// </summary>
    public Dictionary<string, string>? Attributes { get; init; }

    /// <summary>
    /// Gets whether this element appears to be interactable.
    /// </summary>
    public bool IsInteractable { get; init; }

    /// <summary>
    /// Gets whether this element is visible in the screenshot.
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Gets the visual properties of the element (color, style hints).
    /// </summary>
    public Dictionary<string, object>? VisualProperties { get; init; }

    /// <summary>
    /// Gets the hierarchical level of this element (0 = top level).
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// Gets the parent element (if detected as part of hierarchy).
    /// </summary>
    public DetectedElement? Parent { get; init; }

    /// <summary>
    /// Gets child elements (if detected as part of hierarchy).
    /// </summary>
    public List<DetectedElement>? Children { get; init; }

    /// <summary>
    /// Gets when this element was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets additional metadata about the detection.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a detected element with basic information.
    /// </summary>
    public static DetectedElement Create(
        ElementType type,
        ElementBoundingBox boundingBox,
        double confidence,
        string? text = null)
    {
        return new DetectedElement
        {
            Type = type,
            BoundingBox = boundingBox,
            Confidence = confidence,
            Text = text,
            IsInteractable = type is ElementType.Button or ElementType.Input or ElementType.Link or 
                           ElementType.Checkbox or ElementType.Radio or ElementType.Select
        };
    }

    /// <summary>
    /// Determines if this element is reliable enough to use (based on confidence).
    /// </summary>
    public bool IsReliable(double threshold = 0.7)
    {
        return Confidence >= threshold && IsVisible;
    }

    /// <summary>
    /// Gets a human-readable description of this element.
    /// </summary>
    public string GetDisplayDescription()
    {
        var parts = new List<string> { Type.ToString() };
        
        if (!string.IsNullOrWhiteSpace(Text))
            parts.Add($"'{Text}'");
        
        if (!string.IsNullOrWhiteSpace(Description))
            parts.Add(Description);

        parts.Add($"at ({BoundingBox.X:F0}, {BoundingBox.Y:F0})");
        parts.Add($"confidence: {Confidence:P0}");

        return string.Join(" ", parts);
    }
}
