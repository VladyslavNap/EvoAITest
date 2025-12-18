using EvoAITest.Core.Models;

namespace EvoAITest.Core.Data.Models;

/// <summary>
/// Entity for tracking wait operation history for learning and optimization.
/// </summary>
public sealed class WaitHistory
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the task ID this wait is associated with.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Gets or sets the action being performed.
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the selector being waited for (if applicable).
    /// </summary>
    public string? Selector { get; set; }

    /// <summary>
    /// Gets or sets the wait condition type.
    /// </summary>
    public required string WaitCondition { get; set; }

    /// <summary>
    /// Gets or sets the configured timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the actual wait time in milliseconds.
    /// </summary>
    public int ActualWaitMs { get; set; }

    /// <summary>
    /// Gets or sets whether the wait was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the page URL where the wait occurred.
    /// </summary>
    public string? PageUrl { get; set; }

    /// <summary>
    /// Gets or sets when this wait was recorded.
    /// </summary>
    public DateTimeOffset RecordedAt { get; set; }

    /// <summary>
    /// Gets or sets the automation task this wait belongs to.
    /// </summary>
    public AutomationTask? Task { get; set; }
}
