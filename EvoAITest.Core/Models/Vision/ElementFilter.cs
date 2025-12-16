namespace EvoAITest.Core.Models.Vision;

/// <summary>
/// Defines filters for element detection to narrow down results.
/// </summary>
public sealed class ElementFilter
{
    /// <summary>
    /// Gets or sets the element types to detect. Null means detect all types.
    /// </summary>
    public List<ElementType>? Types { get; init; }

    /// <summary>
    /// Gets or sets the minimum confidence threshold (0.0 to 1.0).
    /// </summary>
    public double MinConfidence { get; init; } = 0.5;

    /// <summary>
    /// Gets or sets whether to include only interactable elements.
    /// </summary>
    public bool? InteractableOnly { get; init; }

    /// <summary>
    /// Gets or sets whether to include only visible elements.
    /// </summary>
    public bool VisibleOnly { get; init; } = true;

    /// <summary>
    /// Gets or sets a text pattern to match (contains, regex, etc.).
    /// </summary>
    public string? TextPattern { get; init; }

    /// <summary>
    /// Gets or sets a description pattern to match.
    /// </summary>
    public string? DescriptionPattern { get; init; }

    /// <summary>
    /// Gets or sets the region of interest (bounding box).
    /// Only elements within this region will be included.
    /// </summary>
    public ElementBoundingBox? RegionOfInterest { get; init; }

    /// <summary>
    /// Gets or sets the minimum element size (in pixels).
    /// </summary>
    public double? MinSize { get; init; }

    /// <summary>
    /// Gets or sets the maximum element size (in pixels).
    /// </summary>
    public double? MaxSize { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int? MaxResults { get; init; }

    /// <summary>
    /// Gets or sets whether to include hierarchical information.
    /// </summary>
    public bool IncludeHierarchy { get; init; }

    /// <summary>
    /// Gets or sets additional custom filter criteria.
    /// </summary>
    public Dictionary<string, object>? CustomFilters { get; init; }

    /// <summary>
    /// Determines if an element passes this filter.
    /// </summary>
    public bool Matches(DetectedElement element)
    {
        // Confidence check
        if (element.Confidence < MinConfidence)
            return false;

        // Type check
        if (Types != null && Types.Count > 0 && !Types.Contains(element.Type))
            return false;

        // Interactable check
        if (InteractableOnly.HasValue && element.IsInteractable != InteractableOnly.Value)
            return false;

        // Visible check
        if (VisibleOnly && !element.IsVisible)
            return false;

        // Text pattern check
        if (!string.IsNullOrWhiteSpace(TextPattern) && !string.IsNullOrWhiteSpace(element.Text))
        {
            if (!element.Text.Contains(TextPattern, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Description pattern check
        if (!string.IsNullOrWhiteSpace(DescriptionPattern) && !string.IsNullOrWhiteSpace(element.Description))
        {
            if (!element.Description.Contains(DescriptionPattern, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Region of interest check
        if (RegionOfInterest != null)
        {
            if (!RegionOfInterest.Contains(element.BoundingBox.CenterX, element.BoundingBox.CenterY))
                return false;
        }

        // Size checks
        var area = element.BoundingBox.Area;
        if (MinSize.HasValue && area < MinSize.Value)
            return false;

        if (MaxSize.HasValue && area > MaxSize.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Creates a filter for buttons only.
    /// </summary>
    public static ElementFilter ForButtons(double minConfidence = 0.7)
    {
        return new ElementFilter
        {
            Types = new List<ElementType> { ElementType.Button },
            MinConfidence = minConfidence,
            InteractableOnly = true
        };
    }

    /// <summary>
    /// Creates a filter for input fields.
    /// </summary>
    public static ElementFilter ForInputs(double minConfidence = 0.7)
    {
        return new ElementFilter
        {
            Types = new List<ElementType> { ElementType.Input, ElementType.Textarea, ElementType.SearchBox },
            MinConfidence = minConfidence,
            InteractableOnly = true
        };
    }

    /// <summary>
    /// Creates a filter for clickable elements.
    /// </summary>
    public static ElementFilter ForClickable(double minConfidence = 0.7)
    {
        return new ElementFilter
        {
            Types = new List<ElementType> 
            { 
                ElementType.Button, 
                ElementType.Link, 
                ElementType.Checkbox, 
                ElementType.Radio 
            },
            MinConfidence = minConfidence,
            InteractableOnly = true
        };
    }

    /// <summary>
    /// Creates a filter with text pattern matching.
    /// </summary>
    public static ElementFilter WithText(string textPattern, double minConfidence = 0.7)
    {
        return new ElementFilter
        {
            TextPattern = textPattern,
            MinConfidence = minConfidence
        };
    }
}
