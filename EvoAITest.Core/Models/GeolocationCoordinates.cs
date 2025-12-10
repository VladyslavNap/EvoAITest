namespace EvoAITest.Core.Models;

/// <summary>
/// Represents geolocation coordinates for device emulation.
/// </summary>
public sealed record GeolocationCoordinates
{
    /// <summary>
    /// Latitude in decimal degrees (-90 to 90).
    /// </summary>
    public required double Latitude { get; init; }

    /// <summary>
    /// Longitude in decimal degrees (-180 to 180).
    /// </summary>
    public required double Longitude { get; init; }

    /// <summary>
    /// Optional accuracy in meters. If null, defaults to high accuracy.
    /// </summary>
    public double? Accuracy { get; init; }

    /// <summary>
    /// Validates coordinates after initialization.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If coordinates are out of valid range.</exception>
    public void Validate()
    {
        if (Latitude < -90 || Latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(Latitude), Latitude, "Latitude must be between -90 and 90 degrees.");
        }

        if (Longitude < -180 || Longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(Longitude), Longitude, "Longitude must be between -180 and 180 degrees.");
        }

        if (Accuracy.HasValue && Accuracy.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Accuracy), Accuracy.Value, "Accuracy must be non-negative.");
        }
    }

    /// <summary>
    /// Gets a string representation of the coordinates.
    /// </summary>
    /// <returns>String in format "Latitude, Longitude" or with accuracy if specified.</returns>
    public override string ToString()
    {
        return Accuracy.HasValue
            ? $"{Latitude:F6}, {Longitude:F6} (±{Accuracy:F0}m)"
            : $"{Latitude:F6}, {Longitude:F6}";
    }

    // Common locations for testing

    /// <summary>
    /// San Francisco, CA, USA (37.7749° N, 122.4194° W).
    /// </summary>
    public static GeolocationCoordinates SanFrancisco => new() { Latitude = 37.7749, Longitude = -122.4194 };

    /// <summary>
    /// New York City, NY, USA (40.7128° N, 74.0060° W).
    /// </summary>
    public static GeolocationCoordinates NewYork => new() { Latitude = 40.7128, Longitude = -74.0060 };

    /// <summary>
    /// London, UK (51.5074° N, 0.1278° W).
    /// </summary>
    public static GeolocationCoordinates London => new() { Latitude = 51.5074, Longitude = -0.1278 };

    /// <summary>
    /// Tokyo, Japan (35.6762° N, 139.6503° E).
    /// </summary>
    public static GeolocationCoordinates Tokyo => new() { Latitude = 35.6762, Longitude = 139.6503 };

    /// <summary>
    /// Sydney, Australia (33.8688° S, 151.2093° E).
    /// </summary>
    public static GeolocationCoordinates Sydney => new() { Latitude = -33.8688, Longitude = 151.2093 };

    /// <summary>
    /// Paris, France (48.8566° N, 2.3522° E).
    /// </summary>
    public static GeolocationCoordinates Paris => new() { Latitude = 48.8566, Longitude = 2.3522 };
}
