namespace EvoAITest.Core.Options;

/// <summary>
/// Options for launching and configuring a browser instance.
/// </summary>
public sealed class BrowserOptions
{
    /// <summary>
    /// Gets or sets the browser type to use.
    /// </summary>
    public BrowserType BrowserType { get; set; } = BrowserType.Chromium;

    /// <summary>
    /// Gets or sets a value indicating whether to run the browser in headless mode.
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Gets or sets the viewport width.
    /// </summary>
    public int ViewportWidth { get; set; } = 1920;

    /// <summary>
    /// Gets or sets the viewport height.
    /// </summary>
    public int ViewportHeight { get; set; } = 1080;

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore HTTPS errors.
    /// </summary>
    public bool IgnoreHttpsErrors { get; set; }

    /// <summary>
    /// Gets or sets the default timeout in milliseconds.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the default navigation timeout in milliseconds.
    /// </summary>
    public int DefaultNavigationTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets additional browser arguments.
    /// </summary>
    public List<string> Args { get; set; } = new();

    /// <summary>
    /// Gets or sets the path to the browser executable (optional).
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable downloads.
    /// </summary>
    public bool EnableDownloads { get; set; }

    /// <summary>
    /// Gets or sets the downloads path.
    /// </summary>
    public string? DownloadsPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to record video.
    /// </summary>
    public bool RecordVideo { get; set; }

    /// <summary>
    /// Gets or sets the video recording path.
    /// </summary>
    public string? VideoPath { get; set; }

    /// <summary>
    /// Gets or sets proxy server settings.
    /// </summary>
    public ProxySettings? Proxy { get; set; }

    /// <summary>
    /// Gets or sets geolocation settings.
    /// </summary>
    public GeolocationSettings? Geolocation { get; set; }

    /// <summary>
    /// Gets or sets permissions to grant.
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable JavaScript.
    /// </summary>
    public bool JavaScriptEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets locale settings.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets timezone settings.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Gets or sets device scale factor.
    /// </summary>
    public double DeviceScaleFactor { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets a value indicating whether to emulate mobile.
    /// </summary>
    public bool IsMobile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable touch events.
    /// </summary>
    public bool HasTouch { get; set; }

    /// <summary>
    /// Gets or sets custom HTTP headers to include with every request.
    /// </summary>
    public Dictionary<string, string> ExtraHttpHeaders { get; set; } = new();
}

/// <summary>
/// Defines supported browser types.
/// </summary>
public enum BrowserType
{
    /// <summary>Chromium-based browsers (Chrome, Edge).</summary>
    Chromium,
    
    /// <summary>Firefox.</summary>
    Firefox,
    
    /// <summary>WebKit (Safari).</summary>
    WebKit,
    
    /// <summary>Chrome specifically.</summary>
    Chrome,
    
    /// <summary>Microsoft Edge.</summary>
    Edge
}

/// <summary>
/// Proxy server settings.
/// </summary>
public sealed class ProxySettings
{
    /// <summary>
    /// Gets or sets the proxy server address.
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the proxy bypass list.
    /// </summary>
    public string? Bypass { get; set; }

    /// <summary>
    /// Gets or sets the proxy username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the proxy password.
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Geolocation settings.
/// </summary>
public sealed class GeolocationSettings
{
    /// <summary>
    /// Gets or sets the latitude.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the accuracy in meters.
    /// </summary>
    public double Accuracy { get; set; } = 0;
}
