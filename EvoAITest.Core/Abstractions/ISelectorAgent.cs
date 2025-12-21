using EvoAITest.Core.Models;
using EvoAITest.Core.Models.SelfHealing;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Interface for LLM-powered selector generation agent.
/// Provides AI-driven selector generation as a healing strategy.
/// </summary>
public interface ISelectorAgent
{
    /// <summary>
    /// Generates selector candidates using LLM analysis of page structure.
    /// </summary>
    /// <param name="pageState">Current page state with DOM information.</param>
    /// <param name="failedSelector">The selector that failed.</param>
    /// <param name="expectedText">Optional expected text for the target element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of selector candidates with reasoning and confidence scores.</returns>
    Task<List<SelectorCandidate>> GenerateSelectorCandidatesAsync(
        PageState pageState,
        string failedSelector,
        string? expectedText = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a single best selector using LLM analysis.
    /// </summary>
    /// <param name="pageState">Current page state with DOM information.</param>
    /// <param name="failedSelector">The selector that failed.</param>
    /// <param name="expectedText">Optional expected text for the target element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best selector candidate or null if generation failed.</returns>
    Task<SelectorCandidate?> GenerateBestSelectorAsync(
        PageState pageState,
        string failedSelector,
        string? expectedText = null,
        CancellationToken cancellationToken = default);
}
