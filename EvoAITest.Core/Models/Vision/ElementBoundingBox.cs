namespace EvoAITest.Core.Models.Vision;

/// <summary>
/// Represents the bounding box coordinates of a detected element.
/// </summary>
public sealed record ElementBoundingBox
{
    /// <summary>
    /// Gets the X coordinate of the top-left corner (in pixels).
    /// </summary>
    public required double X { get; init; }

    /// <summary>
    /// Gets the Y coordinate of the top-left corner (in pixels).
    /// </summary>
    public required double Y { get; init; }

    /// <summary>
    /// Gets the width of the element (in pixels).
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// Gets the height of the element (in pixels).
    /// </summary>
    public required double Height { get; init; }

    /// <summary>
    /// Gets the X coordinate of the center point.
    /// </summary>
    public double CenterX => X + (Width / 2);

    /// <summary>
    /// Gets the Y coordinate of the center point.
    /// </summary>
    public double CenterY => Y + (Height / 2);

    /// <summary>
    /// Gets the X coordinate of the right edge.
    /// </summary>
    public double Right => X + Width;

    /// <summary>
    /// Gets the Y coordinate of the bottom edge.
    /// </summary>
    public double Bottom => Y + Height;

    /// <summary>
    /// Gets the area of the bounding box in square pixels.
    /// </summary>
    public double Area => Width * Height;

    /// <summary>
    /// Determines if this bounding box contains a point.
    /// </summary>
    public bool Contains(double pointX, double pointY)
    {
        return pointX >= X && pointX <= Right &&
               pointY >= Y && pointY <= Bottom;
    }

    /// <summary>
    /// Determines if this bounding box overlaps with another.
    /// </summary>
    public bool Overlaps(ElementBoundingBox other)
    {
        return X < other.Right && Right > other.X &&
               Y < other.Bottom && Bottom > other.Y;
    }

    /// <summary>
    /// Calculates the intersection area with another bounding box.
    /// </summary>
    public double IntersectionArea(ElementBoundingBox other)
    {
        if (!Overlaps(other))
            return 0;

        var intersectX = Math.Max(X, other.X);
        var intersectY = Math.Max(Y, other.Y);
        var intersectRight = Math.Min(Right, other.Right);
        var intersectBottom = Math.Min(Bottom, other.Bottom);

        var intersectWidth = intersectRight - intersectX;
        var intersectHeight = intersectBottom - intersectY;

        return intersectWidth * intersectHeight;
    }

    /// <summary>
    /// Calculates the IoU (Intersection over Union) with another bounding box.
    /// </summary>
    public double IoU(ElementBoundingBox other)
    {
        var intersectionArea = IntersectionArea(other);
        if (intersectionArea == 0)
            return 0;

        var unionArea = Area + other.Area - intersectionArea;
        return unionArea > 0 ? intersectionArea / unionArea : 0;
    }

    /// <summary>
    /// Creates a bounding box from center coordinates and dimensions.
    /// </summary>
    public static ElementBoundingBox FromCenter(double centerX, double centerY, double width, double height)
    {
        return new ElementBoundingBox
        {
            X = centerX - (width / 2),
            Y = centerY - (height / 2),
            Width = width,
            Height = height
        };
    }

    /// <summary>
    /// Creates a bounding box from two corner points.
    /// </summary>
    public static ElementBoundingBox FromCorners(double x1, double y1, double x2, double y2)
    {
        var minX = Math.Min(x1, x2);
        var minY = Math.Min(y1, y2);
        var maxX = Math.Max(x1, x2);
        var maxY = Math.Max(y1, y2);

        return new ElementBoundingBox
        {
            X = minX,
            Y = minY,
            Width = maxX - minX,
            Height = maxY - minY
        };
    }
}
