namespace EvoAITest.Agents.Models;

/// <summary>
/// Represents a strategy for healing failed steps or tasks.
/// </summary>
public sealed class HealingStrategy
{
    /// <summary>
    /// Gets or sets the unique strategy identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the strategy name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the strategy type.
    /// </summary>
    public HealingStrategyType Type { get; set; }

    /// <summary>
    /// Gets or sets a description of what this strategy does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence level (0-1) in this strategy.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets parameters for the healing strategy.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the priority of this strategy (higher is tried first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets applicable error types for this strategy.
    /// </summary>
    public List<string> ApplicableErrorTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the estimated time to apply this strategy in milliseconds.
    /// </summary>
    public int EstimatedTimeMs { get; set; }
}

/// <summary>
/// Defines healing strategy types.
/// </summary>
public enum HealingStrategyType
{
    /// <summary>Retry with the same locator after a delay.</summary>
    RetryWithDelay,
    
    /// <summary>Try alternative element locators.</summary>
    AlternativeLocator,
    
    /// <summary>Wait longer for the element to appear.</summary>
    ExtendedWait,
    
    /// <summary>Scroll to make the element visible.</summary>
    ScrollToElement,
    
    /// <summary>Refresh the page and retry.</summary>
    PageRefresh,
    
    /// <summary>Use AI to find a similar element.</summary>
    AIElementDiscovery,
    
    /// <summary>Adjust element interaction method.</summary>
    InteractionMethodChange,
    
    /// <summary>Handle unexpected popup or dialog.</summary>
    PopupHandling,
    
    /// <summary>Replan the entire task.</summary>
    TaskReplanning,
    
    /// <summary>Fall back to a simpler approach.</summary>
    SimpleFallback,
    
    // ===== Visual Regression Healing Strategies =====
    
    /// <summary>Adjust visual comparison tolerance.</summary>
    AdjustVisualTolerance,
    
    /// <summary>Add ignore regions for dynamic content.</summary>
    AddIgnoreRegions,
    
    /// <summary>Wait for page to stabilize before screenshot.</summary>
    WaitForStability,
    
    /// <summary>Flag for manual baseline approval.</summary>
    ManualBaselineApproval,
    
    /// <summary>Custom healing logic.</summary>
    Custom
}
