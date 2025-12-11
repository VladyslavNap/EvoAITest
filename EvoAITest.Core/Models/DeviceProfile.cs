namespace EvoAITest.Core.Models;

/// <summary>
/// Represents a mobile device configuration for browser emulation.
/// Includes viewport, user agent, device capabilities, and optional settings.
/// </summary>
public sealed record DeviceProfile
{
    /// <summary>
    /// Device name (e.g., "iPhone 14 Pro", "Galaxy S23").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// User agent string for the device.
    /// </summary>
    public required string UserAgent { get; init; }

    /// <summary>
    /// Viewport dimensions.
    /// </summary>
    public required ViewportSize Viewport { get; init; }

    /// <summary>
    /// Device pixel ratio (e.g., 2 for Retina, 3 for high-DPI displays).
    /// Default: 1.0 (standard DPI).
    /// </summary>
    public double DeviceScaleFactor { get; init; } = 1.0;

    /// <summary>
    /// Whether the device has touch support.
    /// Default: true (most mobile devices have touch).
    /// </summary>
    public bool HasTouch { get; init; } = true;

    /// <summary>
    /// Whether the device is in landscape orientation.
    /// Default: false (portrait mode).
    /// </summary>
    public bool IsLandscape { get; init; } = false;

    /// <summary>
    /// Whether the device is mobile (affects meta viewport and touch events).
    /// Default: true.
    /// </summary>
    public bool IsMobile { get; init; } = true;

    /// <summary>
    /// Platform name (e.g., "iOS", "Android", "Windows").
    /// Optional - can be null for generic devices.
    /// </summary>
    public string? Platform { get; init; }

    /// <summary>
    /// Optional screen dimensions (may differ from viewport due to browser chrome, status bars).
    /// If not specified, defaults to viewport size.
    /// </summary>
    public ScreenSize? Screen { get; init; }

    /// <summary>
    /// Optional geolocation coordinates for the device.
    /// If specified, geolocation API will return these coordinates.
    /// </summary>
    public GeolocationCoordinates? Geolocation { get; init; }

    /// <summary>
    /// Optional timezone ID (e.g., "America/New_York", "Europe/London").
    /// If not specified, uses system timezone.
    /// </summary>
    public string? TimezoneId { get; init; }

    /// <summary>
    /// Optional locale (e.g., "en-US", "ja-JP", "fr-FR").
    /// Affects language and regional formatting.
    /// </summary>
    public string? Locale { get; init; }

    /// <summary>
    /// Optional permissions to grant automatically (e.g., "geolocation", "notifications", "camera").
    /// If not specified, permissions require user interaction.
    /// </summary>
    public List<string>? Permissions { get; init; }

    /// <summary>
    /// Optional color scheme preference ("light", "dark", "no-preference").
    /// Default: null (uses system preference).
    /// </summary>
    public string? ColorScheme { get; init; }

    /// <summary>
    /// Optional reduced motion preference.
    /// Default: null (uses system preference).
    /// </summary>
    public bool? ReducedMotion { get; init; }

    /// <summary>
    /// Gets the effective screen size (uses Screen if specified, otherwise Viewport).
    /// </summary>
    public ScreenSize EffectiveScreen => Screen ?? new ScreenSize(Viewport.Width, Viewport.Height);

    /// <summary>
    /// Creates a landscape version of this device profile.
    /// Swaps width and height of viewport and screen.
    /// </summary>
    /// <returns>New DeviceProfile instance in landscape orientation.</returns>
    public DeviceProfile ToLandscape()
    {
        if (IsLandscape)
        {
            return this; // Already landscape
        }

        return this with
        {
            Viewport = new ViewportSize(Viewport.Height, Viewport.Width),
            Screen = Screen != null ? new ScreenSize(Screen.Height, Screen.Width) : null,
            IsLandscape = true
        };
    }

    /// <summary>
    /// Creates a portrait version of this device profile.
    /// Swaps width and height of viewport and screen if currently landscape.
    /// </summary>
    /// <returns>New DeviceProfile instance in portrait orientation.</returns>
    public DeviceProfile ToPortrait()
    {
        if (!IsLandscape)
        {
            return this; // Already portrait
        }

        return this with
        {
            Viewport = new ViewportSize(Viewport.Height, Viewport.Width),
            Screen = Screen != null ? new ScreenSize(Screen.Height, Screen.Width) : null,
            IsLandscape = false
        };
    }

    /// <summary>
    /// Creates a copy of this device with different geolocation.
    /// </summary>
    /// <param name="coordinates">New geolocation coordinates.</param>
    /// <returns>New DeviceProfile instance with updated geolocation.</returns>
    public DeviceProfile WithGeolocation(GeolocationCoordinates coordinates)
    {
        return this with { Geolocation = coordinates };
    }

    /// <summary>
    /// Creates a copy of this device with different timezone.
    /// </summary>
    /// <param name="timezoneId">New timezone ID.</param>
    /// <returns>New DeviceProfile instance with updated timezone.</returns>
    public DeviceProfile WithTimezone(string timezoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timezoneId);
        return this with { TimezoneId = timezoneId };
    }

    /// <summary>
    /// Creates a copy of this device with additional permissions.
    /// </summary>
    /// <param name="permissions">Permissions to add.</param>
    /// <returns>New DeviceProfile instance with updated permissions.</returns>
    public DeviceProfile WithPermissions(params string[] permissions)
    {
        var allPermissions = new List<string>(Permissions ?? new List<string>());
        allPermissions.AddRange(permissions);
        return this with { Permissions = allPermissions.Distinct().ToList() };
    }

    /// <summary>
    /// Validates the device profile configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">If configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Device name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(UserAgent))
        {
            throw new InvalidOperationException("User agent cannot be empty.");
        }

        if (Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            throw new InvalidOperationException($"Invalid viewport size: {Viewport}. Width and height must be positive.");
        }

        if (DeviceScaleFactor <= 0)
        {
            throw new InvalidOperationException($"Invalid device scale factor: {DeviceScaleFactor}. Must be positive.");
        }

        if (Screen != null && (Screen.Width <= 0 || Screen.Height <= 0))
        {
            throw new InvalidOperationException($"Invalid screen size: {Screen}. Width and height must be positive.");
        }

        if (!string.IsNullOrWhiteSpace(TimezoneId))
        {
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                throw new InvalidOperationException($"Invalid timezone ID: '{TimezoneId}'.", ex);
            }
        }
    }

    /// <summary>
    /// Gets a string representation of the device profile.
    /// </summary>
    /// <returns>Device name and viewport information.</returns>
    public override string ToString()
    {
        var orientation = IsLandscape ? "landscape" : "portrait";
        return $"{Name} ({Viewport}, {orientation}, {DeviceScaleFactor}x)";
    }
}
