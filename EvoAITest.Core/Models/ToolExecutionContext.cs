using EvoAITest.Core.Models;

namespace EvoAITest.Core.Models;

/// <summary>
/// Context for tool execution, tracking state and results across multiple tool calls.
/// </summary>
public sealed class ToolExecutionContext
{
    /// <summary>
    /// Gets or sets the task ID this execution context belongs to.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the execution history ID for this execution.
    /// </summary>
    public Guid ExecutionHistoryId { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracking related operations.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets the visual comparison results collected during execution.
    /// </summary>
    public List<VisualComparisonResult> VisualComparisonResults { get; init; } = new();

    /// <summary>
    /// Gets or sets the environment for visual regression testing (e.g., "dev", "staging", "prod").
    /// </summary>
    public string Environment { get; set; } = "dev";

    /// <summary>
    /// Gets or sets the browser being used (e.g., "chromium", "firefox", "webkit").
    /// </summary>
    public string Browser { get; set; } = "chromium";

    /// <summary>
    /// Gets or sets the viewport size for visual regression (e.g., "1920x1080").
    /// </summary>
    public string Viewport { get; set; } = "1920x1080";

    /// <summary>
    /// Gets or sets additional metadata for this execution context.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
