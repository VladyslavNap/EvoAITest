namespace EvoAITest.Core.Models;

/// <summary>
/// Represents screen dimensions for device emulation.
/// May differ from viewport due to browser chrome, status bars, etc.
/// </summary>
/// <param name="Width">Screen width in pixels.</param>
/// <param name="Height">Screen height in pixels.</param>
public sealed record ScreenSize(int Width, int Height)
{
    /// <summary>
    /// Gets a string representation of the screen size.
    /// </summary>
    /// <returns>String in format "WidthxHeight" (e.g., "1920x1080").</returns>
    public override string ToString() => $"{Width}x{Height}";

    /// <summary>
    /// Parses a screen size string in format "WidthxHeight".
    /// </summary>
    /// <param name="value">String to parse (e.g., "1920x1080").</param>
    /// <returns>ScreenSize instance.</returns>
    /// <exception cref="FormatException">If the string format is invalid.</exception>
    public static ScreenSize Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('x', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid screen size format: '{value}'. Expected format: 'WidthxHeight' (e.g., '1920x1080').");
        }

        if (!int.TryParse(parts[0], out var width) || width <= 0)
        {
            throw new FormatException($"Invalid width value: '{parts[0]}'. Width must be a positive integer.");
        }

        if (!int.TryParse(parts[1], out var height) || height <= 0)
        {
            throw new FormatException($"Invalid height value: '{parts[1]}'. Height must be a positive integer.");
        }

        return new ScreenSize(width, height);
    }

    /// <summary>
    /// Tries to parse a screen size string.
    /// </summary>
    /// <param name="value">String to parse.</param>
    /// <param name="result">Parsed ScreenSize if successful.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string? value, out ScreenSize? result)
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
}
