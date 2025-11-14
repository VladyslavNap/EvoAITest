namespace EvoAITest.Core.Models;

/// <summary>
/// Represents an interactive element on a web page (immutable).
/// </summary>
/// <param name="Id">Unique identifier for the element.</param>
/// <param name="Tag">HTML tag name (button, input, a, etc.).</param>
/// <param name="ClassName">CSS classes applied to the element.</param>
/// <param name="Text">Visible text content (maximum 100 characters).</param>
/// <param name="Selector">CSS selector to locate this element.</param>
/// <param name="Type">Element type (button, input, link, checkbox, etc.).</param>
/// <param name="IsVisible">Indicates whether the element is visible in the current viewport.</param>
/// <param name="IsEnabled">Indicates whether the element is enabled and can be interacted with.</param>
public sealed record InteractiveElement(
    string Id,
    string Tag,
    string ClassName,
    string Text,
    string Selector,
    string Type,
    bool IsVisible,
    bool IsEnabled
);

/// <summary>
/// Represents the current state of a browser page (immutable).
/// </summary>
/// <param name="Url">Current page URL.</param>
/// <param name="Title">Page title.</param>
/// <param name="AccessibilityTree">Text-based tree representation of page structure for AI analysis.</param>
/// <param name="Elements">Collection of all interactive elements on the page.</param>
/// <param name="PageMetadata">Additional page metadata as key-value pairs.</param>
/// <param name="ExtractedAt">Timestamp when this page state was captured.</param>
public sealed record PageStateRecord(
    string Url,
    string Title,
    string AccessibilityTree,
    List<InteractiveElement> Elements,
    Dictionary<string, string> PageMetadata,
    DateTime ExtractedAt
);

/// <summary>
/// Represents a browser automation tool definition that can be called by an LLM (immutable).
/// </summary>
/// <param name="Name">The name of the tool (e.g., "click", "type", "navigate").</param>
/// <param name="Description">Human-readable description of what this tool does.</param>
/// <param name="Parameters">JSON schema describing the parameters this tool accepts.</param>
/// <remarks>
/// This is a simple tool representation. For full tool definitions with parameter metadata,
/// see <see cref="BrowserToolDefinition"/> and <see cref="BrowserToolRegistry"/>.
/// </remarks>
public sealed record BrowserTool(
    string Name,
    string Description,
    Dictionary<string, object> Parameters
);

/// <summary>
/// Represents a tool call request from an LLM for browser automation (immutable).
/// </summary>
/// <param name="ToolName">Name of the tool/function to execute.</param>
/// <param name="Parameters">Dictionary of parameter names and values for the tool.</param>
/// <param name="Reasoning">Explanation of why the LLM chose this tool and these parameters.</param>
/// <param name="CorrelationId">Trace correlation ID for distributed tracing and debugging.</param>
public sealed record ToolCall(
    string ToolName,
    Dictionary<string, object> Parameters,
    string Reasoning,
    string CorrelationId
);

/// <summary>
/// Represents a single execution step in a browser automation workflow (immutable).
/// </summary>
/// <param name="Order">Sequential order number of this step in the workflow.</param>
/// <param name="Action">Name of the action/tool to execute.</param>
/// <param name="Selector">CSS selector for the target element (empty string if not applicable).</param>
/// <param name="Value">Input value for the action (empty string if not applicable).</param>
/// <param name="Reasoning">Explanation of why this step is necessary.</param>
/// <param name="ExpectedResult">Description of the expected outcome after executing this step.</param>
public sealed record ExecutionStep(
    int Order,
    string Action,
    string Selector,
    string Value,
    string Reasoning,
    string ExpectedResult
);
