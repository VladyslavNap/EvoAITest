namespace EvoAITest.Core.Models;

/// <summary>
/// Represents a strategy for locating elements in the browser DOM.
/// </summary>
public sealed class ElementLocator
{
    /// <summary>
    /// Gets or sets the locator strategy to use.
    /// </summary>
    public LocatorStrategy Strategy { get; set; }

    /// <summary>
    /// Gets or sets the selector value (CSS selector, XPath, text content, etc.).
    /// </summary>
    public string Selector { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets alternative selectors to try if the primary fails.
    /// </summary>
    public List<ElementLocator> Fallbacks { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to locate all matching elements.
    /// </summary>
    public bool FindAll { get; set; }

    /// <summary>
    /// Gets or sets the timeout for element location in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets a human-readable description of the element being located.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Creates a CSS selector locator.
    /// </summary>
    /// <param name="selector">The CSS selector.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A new ElementLocator instance.</returns>
    public static ElementLocator Css(string selector, string? description = null) =>
        new() { Strategy = LocatorStrategy.Css, Selector = selector, Description = description };

    /// <summary>
    /// Creates an XPath locator.
    /// </summary>
    /// <param name="xpath">The XPath expression.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A new ElementLocator instance.</returns>
    public static ElementLocator XPath(string xpath, string? description = null) =>
        new() { Strategy = LocatorStrategy.XPath, Selector = xpath, Description = description };

    /// <summary>
    /// Creates a text content locator.
    /// </summary>
    /// <param name="text">The text to find.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A new ElementLocator instance.</returns>
    public static ElementLocator Text(string text, string? description = null) =>
        new() { Strategy = LocatorStrategy.Text, Selector = text, Description = description };

    /// <summary>
    /// Creates an ARIA role locator.
    /// </summary>
    /// <param name="role">The ARIA role.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A new ElementLocator instance.</returns>
    public static ElementLocator Role(string role, string? description = null) =>
        new() { Strategy = LocatorStrategy.Role, Selector = role, Description = description };

    /// <summary>
    /// Creates a test ID locator.
    /// </summary>
    /// <param name="testId">The test ID attribute value.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A new ElementLocator instance.</returns>
    public static ElementLocator TestId(string testId, string? description = null) =>
        new() { Strategy = LocatorStrategy.TestId, Selector = testId, Description = description };
}

/// <summary>
/// Defines strategies for locating elements in the DOM.
/// </summary>
public enum LocatorStrategy
{
    /// <summary>Locate by CSS selector.</summary>
    Css,
    
    /// <summary>Locate by XPath expression.</summary>
    XPath,
    
    /// <summary>Locate by visible text content.</summary>
    Text,
    
    /// <summary>Locate by element ID attribute.</summary>
    Id,
    
    /// <summary>Locate by ARIA role.</summary>
    Role,
    
    /// <summary>Locate by ARIA label.</summary>
    Label,
    
    /// <summary>Locate by placeholder text.</summary>
    Placeholder,
    
    /// <summary>Locate by test ID attribute (data-testid).</summary>
    TestId,
    
    /// <summary>Locate by title attribute.</summary>
    Title,
    
    /// <summary>Locate by alt text.</summary>
    AltText
}
