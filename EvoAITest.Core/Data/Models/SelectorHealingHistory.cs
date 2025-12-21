using EvoAITest.Core.Models;

namespace EvoAITest.Core.Data.Models;

/// <summary>
/// Entity for tracking selector healing history for learning and analytics.
/// </summary>
public sealed class SelectorHealingHistory
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the task ID this healing is associated with.
    /// Can be null if healing occurs outside of a specific task context.
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Gets or sets the original selector that failed.
    /// </summary>
    public required string OriginalSelector { get; set; }

    /// <summary>
    /// Gets or sets the healed selector that replaced the failed one.
    /// </summary>
    public required string HealedSelector { get; set; }

    /// <summary>
    /// Gets or sets the healing strategy that was used.
    /// </summary>
    public required string HealingStrategy { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0) of the healing.
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets whether the healing was successful when verified.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets when this healing occurred.
    /// </summary>
    public DateTimeOffset HealedAt { get; set; }

    /// <summary>
    /// Gets or sets the page URL where the healing occurred.
    /// </summary>
    public string? PageUrl { get; set; }

    /// <summary>
    /// Gets or sets the expected text that was used for matching.
    /// </summary>
    public string? ExpectedText { get; set; }

    /// <summary>
    /// Gets or sets additional context about the healing (stored as JSON).
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets the automation task this healing belongs to.
    /// </summary>
    public AutomationTask? Task { get; set; }
}
