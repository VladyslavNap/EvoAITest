using EvoAITest.Core.Models;
using EvoAITest.Core.Models.SelfHealing;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for healing failed selectors using multiple intelligent strategies.
/// Automatically attempts to fix broken selectors when page structure changes.
/// </summary>
public interface ISelectorHealingService
{
    /// <summary>
    /// Attempts to heal a failed selector using configured strategies.
    /// </summary>
    /// <param name="failedSelector">The selector that failed to find an element.</param>
    /// <param name="pageState">The current state of the page.</param>
    /// <param name="expectedText">Optional expected text content of the target element.</param>
    /// <param name="screenshot">Optional screenshot of the page for visual matching.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A healed selector if successful, null otherwise.</returns>
    /// <example>
    /// <code>
    /// var healed = await healingService.HealSelectorAsync(
    ///     "#submit-button",
    ///     pageState,
    ///     expectedText: "Submit",
    ///     screenshot: null,
    ///     cancellationToken);
    ///     
    /// if (healed != null && healed.IsReliable())
    /// {
    ///     // Use healed.NewSelector
    /// }
    /// </code>
    /// </example>
    Task<HealedSelector?> HealSelectorAsync(
        string failedSelector,
        PageState pageState,
        string? expectedText = null,
        byte[]? screenshot = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to heal a failed selector using a specific context with custom options.
    /// </summary>
    /// <param name="context">The healing context with all necessary information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A healed selector if successful, null otherwise.</returns>
    Task<HealedSelector?> HealSelectorAsync(
        HealingContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all potential candidate selectors that could replace the failed selector.
    /// </summary>
    /// <param name="pageState">The current page state containing interactive elements.</param>
    /// <param name="context">The healing context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of selector candidates ranked by confidence.</returns>
    /// <example>
    /// <code>
    /// var candidates = await healingService.FindSelectorCandidatesAsync(
    ///     pageState,
    ///     context,
    ///     cancellationToken);
    ///     
    /// foreach (var candidate in candidates.Where(c => c.BaseConfidence > 0.7))
    /// {
    ///     // Try candidate.Selector
    /// }
    /// </code>
    /// </example>
    Task<List<SelectorCandidate>> FindSelectorCandidatesAsync(
        PageState pageState,
        HealingContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates a confidence score for a specific selector candidate.
    /// </summary>
    /// <param name="candidate">The candidate to score.</param>
    /// <param name="context">The healing context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confidence score between 0.0 and 1.0.</returns>
    Task<double> CalculateConfidenceScoreAsync(
        SelectorCandidate candidate,
        HealingContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that a healed selector actually works on the current page.
    /// </summary>
    /// <param name="healedSelector">The healed selector to verify.</param>
    /// <param name="pageState">The current page state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the selector works, false otherwise.</returns>
    Task<bool> VerifyHealedSelectorAsync(
        HealedSelector healedSelector,
        PageState pageState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the healing history for a specific task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of past healing attempts for this task.</returns>
    Task<List<HealedSelector>> GetHealingHistoryAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a successful healing to the database for future learning.
    /// </summary>
    /// <param name="healedSelector">The healed selector to save.</param>
    /// <param name="taskId">The task ID this healing is associated with (can be null if outside task context).</param>
    /// <param name="success">Whether the healing was successful.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveHealingHistoryAsync(
        HealedSelector healedSelector,
        Guid? taskId,
        bool success,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Learns from past successful healings to improve future healing accuracy.
    /// Analyzes patterns in healing history to optimize strategy selection.
    /// </summary>
    /// <param name="taskId">Optional task ID to learn from specific task history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Learning statistics (e.g., most successful strategies).</returns>
    Task<Dictionary<string, object>> LearnFromHistoryAsync(
        Guid? taskId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about healing success rates and performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics dictionary with success rates, average confidence, etc.</returns>
    Task<Dictionary<string, object>> GetHealingStatisticsAsync(
        CancellationToken cancellationToken = default);
}
