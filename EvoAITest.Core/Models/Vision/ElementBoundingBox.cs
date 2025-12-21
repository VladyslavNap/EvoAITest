namespace EvoAITest.Core.Models.Vision;

/// <summary>
/// Represents the bounding box coordinates of a detected element.
/// Design note: Properties use 'init' setters for immutability after construction.
/// Coordinate types are int (not double) as browser coordinates are typically pixel-perfect integers.
/// For sub-pixel precision scenarios (high-DPI, zoom), consider rounding at the capture point.
/// </summary>
public sealed class ElementBoundingBox
{
    /// <summary>
    /// Gets or sets the X coordinate (left position) in pixels.
    /// </summary>
    public int X { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate (top position) in pixels.
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    /// Gets or sets the width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Gets or sets the height in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Gets the center X coordinate.
    /// </summary>
    public int CenterX => X + (Width / 2);

    /// <summary>
    /// Gets the center Y coordinate.
    /// </summary>
    public int CenterY => Y + (Height / 2);

    /// <summary>
    /// Gets the area of the bounding box.
    /// </summary>
    public int Area => Width * Height;
}
