namespace EvoAITest.Core.Models;

/// <summary>
/// Represents the current state of a browser page.
/// </summary>
public sealed class PageState
{
    /// <summary>
    /// Gets or sets the current URL of the page.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page load state.
    /// </summary>
    public LoadState LoadState { get; set; }

    /// <summary>
    /// Gets or sets the visible elements on the page.
    /// </summary>
    public List<ElementInfo> VisibleElements { get; set; } = new();

    /// <summary>
    /// Gets or sets the interactive elements (buttons, links, inputs) on the page.
    /// </summary>
    public List<ElementInfo> InteractiveElements { get; set; } = new();

    /// <summary>
    /// Gets or sets the page dimensions.
    /// </summary>
    public PageDimensions? Dimensions { get; set; }

    /// <summary>
    /// Gets or sets the viewport dimensions.
    /// </summary>
    public PageDimensions? ViewportDimensions { get; set; }

    /// <summary>
    /// Gets or sets any console messages or errors.
    /// </summary>
    public List<ConsoleMessage> ConsoleMessages { get; set; } = new();

    /// <summary>
    /// Gets or sets network requests made by the page.
    /// </summary>
    public List<NetworkRequest> NetworkRequests { get; set; } = new();

    /// <summary>
    /// Gets or sets custom metadata about the page state.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when this state was captured.
    /// </summary>
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents information about a DOM element.
/// </summary>
public sealed class ElementInfo
{
    /// <summary>
    /// Gets or sets the tag name of the element.
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the element's CSS selector.
    /// </summary>
    public string? Selector { get; set; }

    /// <summary>
    /// Gets or sets the element's XPath.
    /// </summary>
    public string? XPath { get; set; }

    /// <summary>
    /// Gets or sets the visible text content.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the element's attributes.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();

    /// <summary>
    /// Gets or sets the element's bounding box.
    /// </summary>
    public BoundingBox? BoundingBox { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the element is visible.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the element is interactable.
    /// </summary>
    public bool IsInteractable { get; set; }
}

/// <summary>
/// Represents the bounding box of an element.
/// </summary>
public sealed class BoundingBox
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public double Height { get; set; }
}

/// <summary>
/// Represents page dimensions.
/// </summary>
public sealed class PageDimensions
{
    /// <summary>
    /// Gets or sets the width in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height in pixels.
    /// </summary>
    public int Height { get; set; }
}

/// <summary>
/// Represents a console message from the browser.
/// </summary>
public sealed class ConsoleMessage
{
    /// <summary>
    /// Gets or sets the message type.
    /// </summary>
    public ConsoleMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the message text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the location where the message originated.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Defines console message types.
/// </summary>
public enum ConsoleMessageType
{
    /// <summary>Log message.</summary>
    Log,
    
    /// <summary>Debug message.</summary>
    Debug,
    
    /// <summary>Info message.</summary>
    Info,
    
    /// <summary>Warning message.</summary>
    Warning,
    
    /// <summary>Error message.</summary>
    Error
}

/// <summary>
/// Represents a network request made by the page.
/// </summary>
public sealed class NetworkRequest
{
    /// <summary>
    /// Gets or sets the request URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the request timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Defines page load states.
/// </summary>
public enum LoadState
{
    /// <summary>Page is loading.</summary>
    Loading,
    
    /// <summary>DOM content is loaded.</summary>
    DomContentLoaded,
    
    /// <summary>Page is fully loaded.</summary>
    Load,
    
    /// <summary>Network is idle.</summary>
    NetworkIdle
}
