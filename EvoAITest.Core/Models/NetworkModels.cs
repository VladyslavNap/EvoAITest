namespace EvoAITest.Core.Models;

/// <summary>
/// Represents an intercepted HTTP request.
/// </summary>
public sealed record InterceptedRequest
{
    /// <summary>Gets the request URL.</summary>
    public required string Url { get; init; }

    /// <summary>Gets the HTTP method (GET, POST, etc.).</summary>
    public required string Method { get; init; }

    /// <summary>Gets the request headers.</summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>Gets the POST data if available.</summary>
    public string? PostData { get; init; }

    /// <summary>Gets the resource type (document, stylesheet, image, etc.).</summary>
    public string? ResourceType { get; init; }

    /// <summary>Gets whether this is a navigation request.</summary>
    public bool IsNavigationRequest { get; init; }
}

/// <summary>
/// Represents a response to be returned for an intercepted request.
/// </summary>
public sealed record InterceptedResponse
{
    /// <summary>Gets the HTTP status code.</summary>
    public int Status { get; init; } = 200;

    /// <summary>Gets the response headers.</summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>Gets the response body as string.</summary>
    public string? Body { get; init; }

    /// <summary>Gets the response body as bytes.</summary>
    public byte[]? BodyBytes { get; init; }

    /// <summary>Gets the content type.</summary>
    public string? ContentType { get; init; }
}

/// <summary>
/// Represents a mock response configuration.
/// </summary>
public sealed record MockResponse
{
    /// <summary>Gets the HTTP status code.</summary>
    public int Status { get; init; } = 200;

    /// <summary>Gets the response body.</summary>
    public string? Body { get; init; }

    /// <summary>Gets the response headers.</summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>Gets the delay before responding in milliseconds.</summary>
    public int? DelayMs { get; init; }

    /// <summary>Gets the content type.</summary>
    public string? ContentType { get; init; }
}

/// <summary>
/// Represents a logged network request/response.
/// </summary>
public sealed record NetworkLog
{
    /// <summary>Gets the log ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets the request URL.</summary>
    public required string Url { get; init; }

    /// <summary>Gets the HTTP method.</summary>
    public required string Method { get; init; }

    /// <summary>Gets the resource type.</summary>
    public string? ResourceType { get; init; }

    /// <summary>Gets the HTTP status code.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Gets the timestamp when request was made.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Gets the request duration in milliseconds.</summary>
    public double? DurationMs { get; init; }

    /// <summary>Gets whether the request was blocked.</summary>
    public bool WasBlocked { get; init; }

    /// <summary>Gets whether the request was mocked.</summary>
    public bool WasMocked { get; init; }

    /// <summary>Gets the request headers.</summary>
    public Dictionary<string, string>? RequestHeaders { get; init; }

    /// <summary>Gets the response headers.</summary>
    public Dictionary<string, string>? ResponseHeaders { get; init; }
}

/// <summary>
/// Represents a route pattern for network interception.
/// </summary>
public sealed record RoutePattern
{
    /// <summary>Gets the URL pattern (glob or regex).</summary>
    public required string Pattern { get; init; }

    /// <summary>Gets the pattern type.</summary>
    public PatternType Type { get; init; } = PatternType.Glob;

    /// <summary>Gets the resource types to match.</summary>
    public List<string>? ResourceTypes { get; init; }
}

/// <summary>
/// Pattern matching type for routes.
/// </summary>
public enum PatternType
{
    /// <summary>Glob pattern matching (e.g., "*.jpg").</summary>
    Glob,

    /// <summary>Regular expression matching.</summary>
    Regex
}
