namespace EvoAITest.Core.Models;

/// <summary>
/// Represents viewport dimensions for browser or device emulation.
/// </summary>
/// <param name="Width">Viewport width in pixels.</param>
/// <param name="Height">Viewport height in pixels.</param>
public sealed record ViewportSize(int Width, int Height)
{
    /// <summary>
    /// Gets a string representation of the viewport size.
    /// </summary>
    /// <returns>String in format "WidthxHeight" (e.g., "1920x1080").</returns>
    public override string ToString() => $"{Width}x{Height}";

    /// <summary>
    /// Parses a viewport size string in format "WidthxHeight".
    /// </summary>
    /// <param name="value">String to parse (e.g., "1920x1080").</param>
    /// <returns>ViewportSize instance.</returns>
    /// <exception cref="FormatException">If the string format is invalid.</exception>
    public static ViewportSize Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('x', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid viewport size format: '{value}'. Expected format: 'WidthxHeight' (e.g., '1920x1080').");
        }

        if (!int.TryParse(parts[0], out var width) || width <= 0)
        {
            throw new FormatException($"Invalid width value: '{parts[0]}'. Width must be a positive integer.");
        }

        if (!int.TryParse(parts[1], out var height) || height <= 0)
        {
            throw new FormatException($"Invalid height value: '{parts[1]}'. Height must be a positive integer.");
        }

        return new ViewportSize(width, height);
    }

    /// <summary>
    /// Tries to parse a viewport size string.
    /// </summary>
    /// <param name="value">String to parse.</param>
    /// <param name="result">Parsed ViewportSize if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string? value, out ViewportSize? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            result = Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Common desktop viewport.
    /// </summary>
    public static ViewportSize Desktop => new(1920, 1080);

    /// <summary>
    /// Common laptop viewport.
    /// </summary>
    public static ViewportSize Laptop => new(1366, 768);

    /// <summary>
    /// Common tablet viewport (portrait).
    /// </summary>
    public static ViewportSize TabletPortrait => new(768, 1024);

    /// <summary>
    /// Common tablet viewport (landscape).
    /// </summary>
    public static ViewportSize TabletLandscape => new(1024, 768);

    /// <summary>
    /// Common mobile viewport (portrait).
    /// </summary>
    public static ViewportSize MobilePortrait => new(375, 667);

    /// <summary>
    /// Common mobile viewport (landscape).
    /// </summary>
    public static ViewportSize MobileLandscape => new(667, 375);
}
