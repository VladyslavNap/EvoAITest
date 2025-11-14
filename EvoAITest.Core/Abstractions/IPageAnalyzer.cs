using EvoAITest.Core.Models;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Defines a contract for analyzing and understanding page content.
/// Used by AI agents to extract meaningful information from web pages.
/// </summary>
public interface IPageAnalyzer
{
    /// <summary>
    /// Analyzes a page and extracts its structure and interactive elements.
    /// </summary>
    /// <param name="page">The page to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A structured analysis of the page.</returns>
    Task<PageAnalysis> AnalyzePageAsync(IPage page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies interactive elements that can be automated.
    /// </summary>
    /// <param name="page">The page to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Interactive elements on the page.</returns>
    Task<IReadOnlyList<ElementInfo>> FindInteractiveElementsAsync(IPage page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts structured data from a page based on a schema or pattern.
    /// </summary>
    /// <param name="page">The page to extract data from.</param>
    /// <param name="schema">Optional schema defining what to extract.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted data.</returns>
    Task<Dictionary<string, object>> ExtractDataAsync(IPage page, object? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the best element matching a natural language description.
    /// </summary>
    /// <param name="page">The page to search.</param>
    /// <param name="description">Natural language description of the element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best matching element locator, or null if not found.</returns>
    Task<ElementLocator?> FindElementByDescriptionAsync(IPage page, string description, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a comprehensive analysis of a web page.
/// </summary>
public sealed class PageAnalysis
{
    /// <summary>
    /// Gets or sets the current page state.
    /// </summary>
    public PageState? PageState { get; set; }

    /// <summary>
    /// Gets or sets forms found on the page.
    /// </summary>
    public List<FormInfo> Forms { get; set; } = new();

    /// <summary>
    /// Gets or sets navigation elements (menus, links).
    /// </summary>
    public List<NavigationInfo> Navigation { get; set; } = new();

    /// <summary>
    /// Gets or sets content sections on the page.
    /// </summary>
    public List<ContentSection> ContentSections { get; set; } = new();

    /// <summary>
    /// Gets or sets accessibility information.
    /// </summary>
    public AccessibilityInfo? Accessibility { get; set; }

    /// <summary>
    /// Gets or sets a summary of what the page contains.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets suggested actions that can be performed on this page.
    /// </summary>
    public List<SuggestedAction> SuggestedActions { get; set; } = new();
}

/// <summary>
/// Represents information about a form on the page.
/// </summary>
public sealed class FormInfo
{
    /// <summary>
    /// Gets or sets the form element locator.
    /// </summary>
    public ElementLocator? Locator { get; set; }

    /// <summary>
    /// Gets or sets the form fields.
    /// </summary>
    public List<FormField> Fields { get; set; } = new();

    /// <summary>
    /// Gets or sets the submit button locator.
    /// </summary>
    public ElementLocator? SubmitButton { get; set; }

    /// <summary>
    /// Gets or sets the form action URL.
    /// </summary>
    public string? ActionUrl { get; set; }
}

/// <summary>
/// Represents a form field.
/// </summary>
public sealed class FormField
{
    /// <summary>
    /// Gets or sets the field name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field type (text, email, password, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the field locator.
    /// </summary>
    public ElementLocator? Locator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    public string? Placeholder { get; set; }
}

/// <summary>
/// Represents navigation information on the page.
/// </summary>
public sealed class NavigationInfo
{
    /// <summary>
    /// Gets or sets the navigation type (menu, breadcrumb, pagination, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation items.
    /// </summary>
    public List<NavigationItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a navigation item.
/// </summary>
public sealed class NavigationItem
{
    /// <summary>
    /// Gets or sets the item text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the item locator.
    /// </summary>
    public ElementLocator? Locator { get; set; }
}

/// <summary>
/// Represents a content section on the page.
/// </summary>
public sealed class ContentSection
{
    /// <summary>
    /// Gets or sets the section heading.
    /// </summary>
    public string? Heading { get; set; }

    /// <summary>
    /// Gets or sets the section content.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the section type (article, sidebar, footer, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Represents accessibility information about the page.
/// </summary>
public sealed class AccessibilityInfo
{
    /// <summary>
    /// Gets or sets landmarks found on the page.
    /// </summary>
    public List<string> Landmarks { get; set; } = new();

    /// <summary>
    /// Gets or sets the heading structure.
    /// </summary>
    public List<string> Headings { get; set; } = new();

    /// <summary>
    /// Gets or sets accessibility issues found.
    /// </summary>
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Represents a suggested action that can be performed on the page.
/// </summary>
public sealed class SuggestedAction
{
    /// <summary>
    /// Gets or sets the action type.
    /// </summary>
    public ActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the target element locator.
    /// </summary>
    public ElementLocator? Target { get; set; }

    /// <summary>
    /// Gets or sets a description of the action.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence score (0-1) for this suggestion.
    /// </summary>
    public double Confidence { get; set; }
}
