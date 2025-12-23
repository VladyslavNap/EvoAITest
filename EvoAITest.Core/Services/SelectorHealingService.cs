using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.SelfHealing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services;

/// <summary>
/// Core service for intelligent selector healing with multiple strategies.
/// </summary>
public sealed class SelectorHealingService : ISelectorHealingService
{
    private readonly VisualElementMatcher _visualMatcher;
    private readonly ILogger<SelectorHealingService> _logger;
    private readonly ISelectorAgent? _selectorAgent;
    private readonly EvoAIDbContext _dbContext;

    public SelectorHealingService(
        VisualElementMatcher visualMatcher,
        ILogger<SelectorHealingService> logger,
        EvoAIDbContext dbContext,
        ISelectorAgent? selectorAgent = null)
    {
        _visualMatcher = visualMatcher ?? throw new ArgumentNullException(nameof(visualMatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _selectorAgent = selectorAgent;
    }

    public async Task<HealedSelector?> HealSelectorAsync(
        string failedSelector,
        PageState pageState,
        string? expectedText,
        byte[]? screenshot,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting selector healing for: {Selector} on page: {Url}",
            failedSelector, pageState.Url);

        var context = new HealingContext
        {
            FailedSelector = failedSelector,
            PageState = pageState,
            ExpectedText = expectedText,
            ExpectedScreenshot = screenshot
        };

        // Try each healing strategy in order
        foreach (var strategy in context.StrategiesToTry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Trying healing strategy: {Strategy}", strategy);

            var candidates = strategy switch
            {
                HealingStrategy.TextContent => await TryTextContentStrategyAsync(context, cancellationToken),
                HealingStrategy.AriaLabel => await TryAriaLabelStrategyAsync(context, cancellationToken),
                HealingStrategy.FuzzyAttributes => await TryFuzzyAttributesStrategyAsync(context, cancellationToken),
                HealingStrategy.VisualSimilarity when screenshot != null =>
                    await TryVisualSimilarityStrategyAsync(context, cancellationToken),
                HealingStrategy.Position when context.ExpectedPosition != null =>
                    await TryPositionStrategyAsync(context, cancellationToken),
                HealingStrategy.LLMGenerated => await TryLLMGeneratedStrategyAsync(context, cancellationToken),
                _ => new List<SelectorCandidate>()
            };

            if (candidates.Count == 0)
                continue;

            // Find best candidate
            var bestCandidate = candidates
                .OrderByDescending(c => c.CalculateFinalConfidence())
                .FirstOrDefault();

            if (bestCandidate != null &&
                bestCandidate.CalculateFinalConfidence() >= context.MinConfidenceThreshold)
            {
                _logger.LogInformation(
                    "Found healed selector using {Strategy}: {Selector} (confidence: {Confidence:F3})",
                    strategy, bestCandidate.Selector, bestCandidate.CalculateFinalConfidence());

                return new HealedSelector
                {
                    OriginalSelector = failedSelector,
                    NewSelector = bestCandidate.Selector,
                    Strategy = strategy,
                    ConfidenceScore = bestCandidate.CalculateFinalConfidence(),
                    HealedAt = DateTimeOffset.UtcNow,
                    PageUrl = pageState.Url,
                    Reasoning = bestCandidate.Reasoning ?? $"Healed using {strategy} strategy",
                    Context = bestCandidate.Context
                };
            }
        }

        _logger.LogWarning("Failed to heal selector: {Selector}", failedSelector);
        return null;
    }

    public async Task<HealedSelector?> HealSelectorAsync(
        HealingContext context,
        CancellationToken cancellationToken = default)
    {
        return await HealSelectorAsync(
            context.FailedSelector,
            context.PageState,
            context.ExpectedText,
            context.ExpectedScreenshot,
            cancellationToken);
    }

    public async Task<List<SelectorCandidate>> FindSelectorCandidatesAsync(
        PageState pageState,
        HealingContext context,
        CancellationToken cancellationToken = default)
    {
        var allCandidates = new List<SelectorCandidate>();

        // Try all strategies and collect candidates
        foreach (var strategy in context.StrategiesToTry)
        {
            var candidates = strategy switch
            {
                HealingStrategy.TextContent => await TryTextContentStrategyAsync(context, cancellationToken),
                HealingStrategy.AriaLabel => await TryAriaLabelStrategyAsync(context, cancellationToken),
                HealingStrategy.FuzzyAttributes => await TryFuzzyAttributesStrategyAsync(context, cancellationToken),
                HealingStrategy.VisualSimilarity when context.ExpectedScreenshot != null =>
                    await TryVisualSimilarityStrategyAsync(context, cancellationToken),
                HealingStrategy.Position when context.ExpectedPosition != null =>
                    await TryPositionStrategyAsync(context, cancellationToken),
                HealingStrategy.LLMGenerated => await TryLLMGeneratedStrategyAsync(context, cancellationToken),
                _ => new List<SelectorCandidate>()
            };

            allCandidates.AddRange(candidates);
        }

        return allCandidates
            .OrderByDescending(c => c.CalculateFinalConfidence())
            .Take(10) // Return top 10 candidates
            .ToList();
    }

    public Task<double> CalculateConfidenceScoreAsync(
        SelectorCandidate candidate,
        HealingContext context,
        CancellationToken cancellationToken = default)
    {
        var confidence = candidate.CalculateFinalConfidence();
        return Task.FromResult(confidence);
    }

    public Task<bool> VerifyHealedSelectorAsync(
        HealedSelector healedSelector,
        PageState pageState,
        CancellationToken cancellationToken = default)
    {
        // Check if selector matches exactly one element
        var matches = pageState.VisibleElements
            .Count(e => e.Selector == healedSelector.NewSelector);

        return Task.FromResult(matches == 1);
    }

    public async Task<List<HealedSelector>> GetHealingHistoryAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _dbContext.SelectorHealingHistory
                .Where(h => h.TaskId == taskId)
                .OrderByDescending(h => h.HealedAt)
                .ToListAsync(cancellationToken);

            return history.Select(h => new HealedSelector
            {
                OriginalSelector = h.OriginalSelector,
                NewSelector = h.HealedSelector,
                Strategy = Enum.TryParse<HealingStrategy>(h.HealingStrategy, out var strategy)
                    ? strategy
                    : HealingStrategy.TextContent,
                ConfidenceScore = h.ConfidenceScore,
                HealedAt = h.HealedAt,
                PageUrl = h.PageUrl,
                Reasoning = $"Historical healing using {h.HealingStrategy}",
                Context = string.IsNullOrEmpty(h.Context)
                    ? new Dictionary<string, object>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(h.Context)
                      ?? new Dictionary<string, object>()
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve healing history for task {TaskId}", taskId);
            return new List<HealedSelector>();
        }
    }

    public async Task SaveHealingHistoryAsync(
        HealedSelector healedSelector,
        Guid? taskId,
        bool success,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var historyEntry = new Data.Models.SelectorHealingHistory
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                OriginalSelector = healedSelector.OriginalSelector,
                HealedSelector = healedSelector.NewSelector,
                HealingStrategy = healedSelector.Strategy.ToString(),
                ConfidenceScore = healedSelector.ConfidenceScore,
                Success = success,
                HealedAt = healedSelector.HealedAt,
                PageUrl = healedSelector.PageUrl,
                ExpectedText = healedSelector.Context.TryGetValue("expected_text", out var text)
                    ? text?.ToString()
                    : null,
                Context = System.Text.Json.JsonSerializer.Serialize(healedSelector.Context)
            };

            _dbContext.SelectorHealingHistory.Add(historyEntry);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saved healing history: {Original} -> {Healed} (Strategy: {Strategy}, Success: {Success})",
                healedSelector.OriginalSelector,
                healedSelector.NewSelector,
                healedSelector.Strategy,
                success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save healing history to database");
        }
    }

    public async Task<Dictionary<string, object>> LearnFromHistoryAsync(
        Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.SelectorHealingHistory.AsQueryable();

            if (taskId.HasValue)
            {
                query = query.Where(h => h.TaskId == taskId.Value);
            }

            var totalAttempts = await query.CountAsync(cancellationToken);

            if (totalAttempts == 0)
            {
                return new Dictionary<string, object>
                {
                    ["total_attempts"] = 0,
                    ["message"] = "No healing history available for learning"
                };
            }

            // Calculate success rate by strategy using database-side aggregation
            var strategyStats = await query
                .GroupBy(h => h.HealingStrategy)
                .Select(g => new
                {
                    Strategy = g.Key,
                    TotalAttempts = g.Count(),
                    SuccessCount = g.Count(h => h.Success),
                    SuccessRate = g.Count(h => h.Success) / (double)g.Count(),
                    AvgConfidence = g.Average(h => h.ConfidenceScore)
                })
                .OrderByDescending(s => s.SuccessRate)
                .ToListAsync(cancellationToken);

            var bestStrategy = strategyStats.FirstOrDefault();

            var successfulAttempts = await query.CountAsync(h => h.Success, cancellationToken);
            var averageConfidence = await query.AverageAsync(h => h.ConfidenceScore, cancellationToken);

            var learningResults = new Dictionary<string, object>
            {
                ["total_attempts"] = totalAttempts,
                ["successful_attempts"] = successfulAttempts,
                ["overall_success_rate"] = successfulAttempts / (double)totalAttempts,
                ["average_confidence"] = averageConfidence,
                ["strategy_stats"] = strategyStats,
                ["best_strategy"] = bestStrategy?.Strategy ?? "None",
                ["best_strategy_success_rate"] = bestStrategy?.SuccessRate ?? 0.0
            };

            _logger.LogInformation(
                "Learning analysis complete: {Total} attempts, {Success:P} success rate, best strategy: {Strategy}",
                totalAttempts,
                learningResults["overall_success_rate"],
                learningResults["best_strategy"]);

            return learningResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to learn from healing history");
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message
            };
        }
    }

    public async Task<Dictionary<string, object>> GetHealingStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var totalCount = await _dbContext.SelectorHealingHistory.CountAsync(cancellationToken);

            if (totalCount == 0)
            {
                return new Dictionary<string, object>
                {
                    ["total_healings"] = 0,
                    ["message"] = "No healing data available"
                };
            }

            var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);

            var successCount = await _dbContext.SelectorHealingHistory
                .CountAsync(h => h.Success, cancellationToken);

            var avgConfidence = await _dbContext.SelectorHealingHistory
                .AverageAsync(h => h.ConfidenceScore, cancellationToken);

            var recentCount = await _dbContext.SelectorHealingHistory
                .CountAsync(h => h.HealedAt >= sevenDaysAgo, cancellationToken);

            var recentSuccessCount = await _dbContext.SelectorHealingHistory
                .CountAsync(h => h.HealedAt >= sevenDaysAgo && h.Success, cancellationToken);

            var uniquePages = await _dbContext.SelectorHealingHistory
                .Select(h => h.PageUrl)
                .Distinct()
                .CountAsync(cancellationToken);

            var mostCommonStrategy = await _dbContext.SelectorHealingHistory
                .GroupBy(h => h.HealingStrategy)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync(cancellationToken);

            var stats = new Dictionary<string, object>
            {
                ["total_healings"] = totalCount,
                ["successful_healings"] = successCount,
                ["success_rate"] = successCount / (double)totalCount,
                ["average_confidence"] = avgConfidence,
                ["recent_healings_7days"] = recentCount,
                ["recent_success_rate"] = recentCount > 0
                    ? recentSuccessCount / (double)recentCount
                    : 0.0,
                ["unique_pages_healed"] = uniquePages,
                ["most_common_strategy"] = mostCommonStrategy ?? "None"
            };

            _logger.LogInformation(
                "Healing statistics: {Total} total, {Success:P} success rate",
                stats["total_healings"],
                stats["success_rate"]);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get healing statistics");
            return new Dictionary<string, object>
            {
                ["error"] = ex.Message
            };
        }
    }

    /// <summary>
    /// Tries to find elements by text content matching.
    /// </summary>
    private Task<List<SelectorCandidate>> TryTextContentStrategyAsync(
        HealingContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.ExpectedText))
            return Task.FromResult(new List<SelectorCandidate>());

        var candidates = context.PageState.InteractiveElements
            .Where(element => element.Text != null && element.Selector != null)
            .Select(element =>
            {
                var similarity = CalculateTextSimilarity(context.ExpectedText, element.Text!);
                return new { element, similarity };
            })
            .Where(x => x.similarity >= 0.7)
            .Select(x => new SelectorCandidate
            {
                Selector = x.element.Selector!,
                Strategy = HealingStrategy.TextContent,
                BaseConfidence = x.similarity,
                ElementInfo = x.element,
                ElementText = x.element.Text,
                TextSimilarityScore = x.similarity,
                Reasoning = $"Text match: '{x.element.Text}' (similarity: {x.similarity:F2})"
            })
            .ToList();

        return Task.FromResult(candidates);
    }

    /// <summary>
    /// Tries to find elements by ARIA label matching.
    /// </summary>
    private Task<List<SelectorCandidate>> TryAriaLabelStrategyAsync(
        HealingContext context,
        CancellationToken cancellationToken)
    {
        var candidates = context.PageState.InteractiveElements
            .Where(element => element.Selector != null &&
                            element.Attributes.TryGetValue("aria-label", out var ariaLabel) &&
                            !string.IsNullOrWhiteSpace(ariaLabel))
            .Select(element =>
            {
                var ariaLabel = element.Attributes["aria-label"];
                var similarity = !string.IsNullOrWhiteSpace(context.ExpectedText)
                    ? CalculateTextSimilarity(context.ExpectedText, ariaLabel)
                    : 0.8; // Default score if no expected text
                return new { element, ariaLabel, similarity };
            })
            .Where(x => x.similarity >= 0.7)
            .Select(x => new SelectorCandidate
            {
                Selector = x.element.Selector!,
                Strategy = HealingStrategy.AriaLabel,
                BaseConfidence = x.similarity,
                ElementInfo = x.element,
                AriaMatchScore = x.similarity,
                Reasoning = $"ARIA label match: '{x.ariaLabel}'"
            })
            .ToList();

        return Task.FromResult(candidates);
    }

    /// <summary>
    /// Tries to find elements by fuzzy attribute matching.
    /// </summary>
    private Task<List<SelectorCandidate>> TryFuzzyAttributesStrategyAsync(
        HealingContext context,
        CancellationToken cancellationToken)
    {
        if (context.ExpectedAttributes == null || context.ExpectedAttributes.Count == 0)
            return Task.FromResult(new List<SelectorCandidate>());

        var candidates = new List<SelectorCandidate>();

        foreach (var element in context.PageState.InteractiveElements)
        {
            var matchScore = CalculateAttributeMatchScore(context.ExpectedAttributes, element.Attributes);

            if (matchScore >= 0.6 && element.Selector != null)
            {
                candidates.Add(new SelectorCandidate
                {
                    Selector = element.Selector,
                    Strategy = HealingStrategy.FuzzyAttributes,
                    BaseConfidence = matchScore,
                    ElementInfo = element,
                    AttributeMatchScore = matchScore,
                    Reasoning = $"Attribute match score: {matchScore:F2}"
                });
            }
        }

        return Task.FromResult(candidates);
    }

    /// <summary>
    /// Tries to find elements by visual similarity.
    /// </summary>
    private Task<List<SelectorCandidate>> TryVisualSimilarityStrategyAsync(
        HealingContext context,
        CancellationToken cancellationToken)
    {
        if (context.ExpectedScreenshot == null)
            return Task.FromResult(new List<SelectorCandidate>());

        var candidates = context.PageState.InteractiveElements
            .Where(element => element.BoundingBox != null &&
                            element.Selector != null &&
                            context.ExpectedPosition != null)
            .Select(element =>
            {
                var distance = CalculateDistance(
                    element.BoundingBox!.X, element.BoundingBox.Y,
                    context.ExpectedPosition!.X, context.ExpectedPosition.Y);

                var normalizedScore = Math.Max(0, 1.0 - (distance / 1000.0)); // Normalize to 0-1
                return new { element, distance, normalizedScore };
            })
            .Where(x => x.normalizedScore >= 0.7)
            .Select(x => new SelectorCandidate
            {
                Selector = x.element.Selector!,
                Strategy = HealingStrategy.VisualSimilarity,
                BaseConfidence = x.normalizedScore,
                ElementInfo = x.element,
                VisualSimilarityScore = x.normalizedScore,
                Reasoning = $"Visual/position match (distance: {x.distance:F0}px)"
            })
            .ToList();

        return Task.FromResult(candidates);
    }

    /// <summary>
    /// Tries to find elements by position proximity.
    /// </summary>
    private Task<List<SelectorCandidate>> TryPositionStrategyAsync(
        HealingContext context,
        CancellationToken cancellationToken)
    {
        if (context.ExpectedPosition == null)
            return Task.FromResult(new List<SelectorCandidate>());

        var candidates = context.PageState.InteractiveElements
            .Where(element => element.BoundingBox != null && element.Selector != null)
            .Select(element =>
            {
                var distance = CalculateDistance(
                    element.BoundingBox!.X, element.BoundingBox.Y,
                    context.ExpectedPosition.X, context.ExpectedPosition.Y);

                // Closer is better - normalize to 0-1 score
                var normalizedScore = Math.Max(0, 1.0 - (distance / 500.0));
                return new { element, distance, normalizedScore };
            })
            .Where(x => x.normalizedScore >= 0.7)
            .Select(x => new SelectorCandidate
            {
                Selector = x.element.Selector!,
                Strategy = HealingStrategy.Position,
                BaseConfidence = x.normalizedScore,
                ElementInfo = x.element,
                PositionScore = x.normalizedScore,
                Reasoning = $"Position proximity (distance: {x.distance:F0}px)"
            })
            .ToList();

        return Task.FromResult(candidates);
    }

    /// <summary>
    /// Uses LLM to generate selector candidates.
    /// </summary>
    private async Task<List<SelectorCandidate>> TryLLMGeneratedStrategyAsync(
        HealingContext context,
        CancellationToken cancellationToken)
    {
        if (_selectorAgent == null)
        {
            _logger.LogDebug("LLM strategy skipped: ISelectorAgent not available");
            return new List<SelectorCandidate>();
        }

        try
        {
            _logger.LogInformation("Using LLM to generate selector candidates");

            var candidates = await _selectorAgent.GenerateSelectorCandidatesAsync(
                context.PageState,
                context.FailedSelector,
                context.ExpectedText,
                cancellationToken);

            _logger.LogInformation("LLM generated {Count} selector candidates", candidates.Count);

            return candidates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM selector generation failed");
            return new List<SelectorCandidate>();
        }
    }

    /// <summary>
    /// Calculates text similarity using Levenshtein distance.
    /// </summary>
    private double CalculateTextSimilarity(string expected, string actual)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            return 0;

        var maxLength = Math.Max(expected.Length, actual.Length);
        var distance = LevenshteinDistance(expected.ToLower(), actual.ToLower());

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    private int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return target?.Length ?? 0;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var distance = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            distance[i, 0] = i;

        for (int j = 0; j <= target.Length; j++)
            distance[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                var cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[source.Length, target.Length];
    }

    /// <summary>
    /// Calculates attribute match score.
    /// </summary>
    private double CalculateAttributeMatchScore(
        Dictionary<string, string> expected,
        Dictionary<string, string> actual)
    {
        if (expected.Count == 0)
            return 0;

        double matchScore = 0;
        int totalCount = expected.Count;

        foreach (var (key, expectedValue) in expected)
        {
            if (actual.TryGetValue(key, out var actualValue))
            {
                if (expectedValue.Equals(actualValue, StringComparison.OrdinalIgnoreCase))
                {
                    matchScore += 1.0;
                }
                else
                {
                    // Partial credit for similar values
                    var similarity = CalculateTextSimilarity(expectedValue, actualValue);
                    matchScore += similarity * 0.5;
                }
            }
        }

        return matchScore / totalCount;
    }

    /// <summary>
    /// Calculates Euclidean distance between two points.
    /// </summary>
    private double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
