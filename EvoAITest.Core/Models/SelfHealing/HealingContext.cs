namespace EvoAITest.Core.Models.SelfHealing;

/// <summary>
/// Provides context for selector healing, including information about the failed selector
/// and the current page state.
/// </summary>
public sealed class HealingContext
{
    /// <summary>
    /// Gets or sets the selector that failed.
    /// </summary>
    public required string FailedSelector { get; init; }

    /// <summary>
    /// Gets or sets the current page state.
    /// </summary>
    public required PageState PageState { get; init; }

    /// <summary>
    /// Gets or sets the expected text content of the target element (if known).
    /// </summary>
    public string? ExpectedText { get; init; }

    /// <summary>
    /// Gets or sets a screenshot of the expected element (for visual matching).
    /// </summary>
    public byte[]? ExpectedScreenshot { get; init; }

    /// <summary>
    /// Gets or sets the expected element attributes (if known).
    /// </summary>
    public Dictionary<string, string>? ExpectedAttributes { get; init; }

    /// <summary>
    /// Gets or sets the expected position of the element (for position-based matching).
    /// </summary>
    public BoundingBox? ExpectedPosition { get; init; }

    /// <summary>
    /// Gets or sets the task ID this healing is associated with.
    /// </summary>
    public Guid? TaskId { get; init; }

    /// <summary>
    /// Gets or sets the action that was being performed when the selector failed.
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// Gets or sets the strategies to try (in order of preference).
    /// </summary>
    public List<HealingStrategy> StrategiesToTry { get; init; } = new()
    {
        HealingStrategy.TextContent,
        HealingStrategy.AriaLabel,
        HealingStrategy.FuzzyAttributes,
        HealingStrategy.VisualSimilarity,
        HealingStrategy.Position,
        HealingStrategy.LLMGenerated
    };

    /// <summary>
    /// Gets or sets the minimum confidence threshold for accepting a healed selector.
    /// </summary>
    public double MinConfidenceThreshold { get; init; } = 0.75;

    /// <summary>
    /// Gets or sets the maximum number of healing attempts.
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Gets or sets whether to save successful healings to the database.
    /// </summary>
    public bool SaveHistory { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to automatically update the task with the healed selector.
    /// </summary>
    public bool AutoUpdateTask { get; init; } = true;

    /// <summary>
    /// Gets or sets additional context as key-value pairs.
    /// </summary>
    public Dictionary<string, object> AdditionalContext { get; init; } = new();

    /// <summary>
    /// Gets or sets when the healing attempt started.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a healing context from a failed selector and page state.
    /// </summary>
    public static HealingContext Create(
        string failedSelector,
        PageState pageState,
        string? expectedText = null)
    {
        return new HealingContext
        {
            FailedSelector = failedSelector,
            PageState = pageState,
            ExpectedText = expectedText
        };
    }

    /// <summary>
    /// Creates a healing context with custom strategies.
    /// </summary>
    public static HealingContext CreateWithStrategies(
        string failedSelector,
        PageState pageState,
        params HealingStrategy[] strategies)
    {
        return new HealingContext
        {
            FailedSelector = failedSelector,
            PageState = pageState,
            StrategiesToTry = strategies.ToList()
        };
    }
}
