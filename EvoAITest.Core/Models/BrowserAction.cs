namespace EvoAITest.Core.Models;

/// <summary>
/// Represents a browser action to be executed by the automation framework.
/// </summary>
public sealed class BrowserAction
{
    /// <summary>
    /// Gets or sets the unique identifier for this action.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the type of action to perform.
    /// </summary>
    public ActionType Type { get; set; }

    /// <summary>
    /// Gets or sets the target element locator for this action.
    /// </summary>
    public ElementLocator? Target { get; set; }

    /// <summary>
    /// Gets or sets the input value for actions that require data (e.g., type, fill).
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets additional options for this action.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout for this action in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets a value indicating whether to wait for navigation after this action.
    /// </summary>
    public bool WaitForNavigation { get; set; }

    /// <summary>
    /// Gets or sets the description of what this action does (for logging and AI context).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this action was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Defines the types of browser actions that can be performed.
/// </summary>
public enum ActionType
{
    /// <summary>Navigate to a URL.</summary>
    Navigate,
    
    /// <summary>Click on an element.</summary>
    Click,
    
    /// <summary>Type text into an input field.</summary>
    Type,
    
    /// <summary>Fill an input field with text (clears first).</summary>
    Fill,
    
    /// <summary>Select an option from a dropdown.</summary>
    Select,
    
    /// <summary>Check a checkbox.</summary>
    Check,
    
    /// <summary>Uncheck a checkbox.</summary>
    Uncheck,
    
    /// <summary>Hover over an element.</summary>
    Hover,
    
    /// <summary>Wait for an element to be visible.</summary>
    WaitForElement,
    
    /// <summary>Wait for a specific time period.</summary>
    Wait,
    
    /// <summary>Take a screenshot.</summary>
    Screenshot,
    
    /// <summary>Execute custom JavaScript.</summary>
    ExecuteScript,
    
    /// <summary>Scroll to an element or position.</summary>
    Scroll,
    
    /// <summary>Press a keyboard key.</summary>
    Press,
    
    /// <summary>Extract text from an element.</summary>
    ExtractText,
    
    /// <summary>Verify element state or content.</summary>
    Verify
}
