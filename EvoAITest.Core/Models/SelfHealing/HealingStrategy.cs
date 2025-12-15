namespace EvoAITest.Core.Models.SelfHealing;

/// <summary>
/// Defines the strategy used to heal a failed selector.
/// </summary>
public enum HealingStrategy
{
    /// <summary>
    /// No healing strategy applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Uses visual similarity between element screenshots to find the target element.
    /// Compares current page elements with the expected visual appearance.
    /// </summary>
    VisualSimilarity = 1,

    /// <summary>
    /// Matches elements based on their text content.
    /// Useful when the element's text is stable but structure changes.
    /// </summary>
    TextContent = 2,

    /// <summary>
    /// Matches elements using ARIA labels and accessibility attributes.
    /// Highly reliable for accessible web applications.
    /// </summary>
    AriaLabel = 3,

    /// <summary>
    /// Matches elements based on their position on the page.
    /// Uses relative positioning and proximity to other elements.
    /// </summary>
    Position = 4,

    /// <summary>
    /// Uses fuzzy matching on element attributes (id, class, name, etc.).
    /// Tolerates minor changes in attribute values.
    /// </summary>
    FuzzyAttributes = 5,

    /// <summary>
    /// Combines multiple strategies for higher confidence.
    /// Uses weighted scoring across different matching methods.
    /// </summary>
    Composite = 6,

    /// <summary>
    /// Uses LLM to analyze the page and generate a new selector.
    /// Most powerful but slower and may incur API costs.
    /// </summary>
    LLMGenerated = 7
}
