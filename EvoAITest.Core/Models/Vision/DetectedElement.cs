namespace EvoAITest.Core.Models.Vision;

/// <summary>
/// Represents a UI element detected in a screenshot using vision analysis.
/// </summary>
public sealed class DetectedElement
{
    /// <summary>
    /// Gets or sets the type of element (button, input, link, etc.).
    /// </summary>
    public required string ElementType { get; set; }

    /// <summary>
    /// Gets or sets the visible text or label of the element.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the bounding box coordinates of the element.
    /// </summary>
    public required ElementBoundingBox BoundingBox { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0) of the detection.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets whether the element is currently visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the element is interactable.
    /// </summary>
    public bool IsInteractable { get; set; } = true;

    /// <summary>
    /// Gets or sets a description of the element (for vision-based detection).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets additional attributes detected.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();
}
