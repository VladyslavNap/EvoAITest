namespace EvoAITest.Core.Models.Recording;

/// <summary>
/// Represents contextual information about the page state when an action occurred
/// </summary>
public sealed class ActionContext
{
    /// <summary>
    /// URL of the page when the action occurred
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// Page title
    /// </summary>
    public string? PageTitle { get; init; }
    
    /// <summary>
    /// Viewport width in pixels
    /// </summary>
    public int ViewportWidth { get; init; }
    
    /// <summary>
    /// Viewport height in pixels
    /// </summary>
    public int ViewportHeight { get; init; }
    
    /// <summary>
    /// CSS selector of the target element
    /// </summary>
    public string? TargetSelector { get; init; }
    
    /// <summary>
    /// XPath of the target element
    /// </summary>
    public string? TargetXPath { get; init; }
    
    /// <summary>
    /// Text content of the target element
    /// </summary>
    public string? ElementText { get; init; }
    
    /// <summary>
    /// HTML tag name of the target element
    /// </summary>
    public string? ElementTag { get; init; }
    
    /// <summary>
    /// Element attributes as key-value pairs
    /// </summary>
    public Dictionary<string, string> ElementAttributes { get; init; } = [];
    
    /// <summary>
    /// Scroll position (Y-axis) when action occurred
    /// </summary>
    public int ScrollY { get; init; }
    
    /// <summary>
    /// Scroll position (X-axis) when action occurred
    /// </summary>
    public int ScrollX { get; init; }
    
    /// <summary>
    /// Screenshot data (base64) at the time of action
    /// </summary>
    public string? ScreenshotData { get; init; }
    
    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = [];
}
