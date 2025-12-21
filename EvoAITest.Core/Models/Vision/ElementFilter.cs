namespace EvoAITest.Core.Models.Vision;

/// <summary>
/// Filter for element detection to narrow down search results.
/// </summary>
public sealed class ElementFilter
{
    /// <summary>
    /// Gets or sets the element type to filter by (button, input, link, etc.).
    /// </summary>
    public string? ElementType { get; set; }

    /// <summary>
    /// Gets or sets the minimum confidence score (0.0 to 1.0).
    /// </summary>
    public double? MinConfidence { get; set; }

    /// <summary>
    /// Gets or sets whether to only include visible elements.
    /// </summary>
    public bool? OnlyVisible { get; set; }

    /// <summary>
    /// Gets or sets whether to only include interactable elements.
    /// </summary>
    public bool? OnlyInteractable { get; set; }

    /// <summary>
    /// Gets or sets a text pattern to match against element text.
    /// </summary>
    public string? TextContains { get; set; }

    /// <summary>
    /// Gets or sets a region to limit detection to.
    /// </summary>
    public ElementBoundingBox? Region { get; set; }
}
