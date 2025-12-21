using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.SelfHealing;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EvoAITest.Core.Services;

/// <summary>
/// Core service for intelligent selector healing with multiple strategies.
/// </summary>
public sealed class SelectorHealingService : ISelectorHealingService
{
    // Confidence threshold constants
    private const double MinimumConfidenceThreshold = 0.7;
    
    // Distance normalization constants (in pixels)
    /// <summary>Maximum visual distance in pixels for normalization (visual similarity strategy).</summary>
    private const double MaxVisualDistancePx = 1000.0;
    
    /// <summary>Maximum position distance in pixels for normalization (position strategy).</summary>
    private const double MaxPositionDistancePx = 500.0;
    
    private readonly VisualElementMatcher _visualMatcher;
    private readonly ISelectorAgent? _selectorAgent;
    private readonly ILogger<SelectorHealingService> _logger;

    public SelectorHealingService(
        VisualElementMatcher visualMatcher,
        ILogger<SelectorHealingService> logger,
        ISelectorAgent? selectorAgent = null)
    {
        _visualMatcher = visualMatcher ?? throw new ArgumentNullException(nameof(visualMatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _selectorAgent = selectorAgent; // Optional for LLM strategy
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

    public Task<List<HealedSelector>> GetHealingHistoryAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement database query when SelectorHealingHistory entity is added
        _logger.LogWarning("GetHealingHistoryAsync not yet implemented - requires database migration");
        return Task.FromResult(new List<HealedSelector>());
    }

    public Task SaveHealingHistoryAsync(
        HealedSelector healedSelector,
        Guid? taskId,
        bool success,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement database save when SelectorHealingHistory entity is added
        _logger.LogInformation(
            "Healing history (not yet persisted): {Original} -> {New}, success: {Success}",
            healedSelector.OriginalSelector, healedSelector.NewSelector, success);
        
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, object>> LearnFromHistoryAsync(
        Guid? taskId = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement learning logic when history data is available
        return Task.FromResult(new Dictionary<string, object>
        {
            ["learned"] = false,
            ["message"] = "Learning not yet implemented - requires healing history data"
        });
    }

    public Task<Dictionary<string, object>> GetHealingStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement statistics when history data is available
        return Task.FromResult(new Dictionary<string, object>
        {
            ["total_healings"] = 0,
            ["success_rate"] = 0.0,
            ["message"] = "Statistics not yet implemented - requires healing history data"
        });
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
            .Select(element => new 
            { 
                Element = element, 
                Similarity = CalculateTextSimilarity(context.ExpectedText, element.Text!) 
            })
            .Where(x => x.Similarity >= MinimumConfidenceThreshold)
            .Select(x => new SelectorCandidate
            {
                Selector = x.Element.Selector!,
                Strategy = HealingStrategy.TextContent,
                BaseConfidence = x.Similarity,
                ElementInfo = x.Element,
                ElementText = x.Element.Text,
                TextSimilarityScore = x.Similarity,
                Reasoning = $"Text match: '{x.Element.Text}' (similarity: {x.Similarity:F2})"
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
            .Where(element => element.Attributes.TryGetValue("aria-label", out var ariaLabel) && 
                            !string.IsNullOrWhiteSpace(ariaLabel) &&
                            element.Selector != null)
            .Select(element =>
            {
                var ariaLabel = element.Attributes["aria-label"];
                var similarity = !string.IsNullOrWhiteSpace(context.ExpectedText)
                    ? CalculateTextSimilarity(context.ExpectedText, ariaLabel)
                    : 0.8; // Default score if no expected text
                
                return new SelectorCandidate
                {
                    Selector = element.Selector!,
                    Strategy = HealingStrategy.AriaLabel,
                    BaseConfidence = similarity,
                    ElementInfo = element,
                    AriaMatchScore = similarity,
                    Reasoning = $"ARIA label match: '{ariaLabel}'"
                };
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
    /// NOTE: Currently uses position-based matching as a proxy since actual visual comparison 
    /// requires element-specific screenshots which are not yet implemented.
    /// </summary>
    private async Task<List<SelectorCandidate>> TryVisualSimilarityStrategyAsync(
        HealingContext context,
        CancellationToken cancellationToken)
    {
        if (context.ExpectedScreenshot == null || context.ExpectedPosition == null)
            return new List<SelectorCandidate>();

        var candidates = context.PageState.InteractiveElements
            .Where(element => element.BoundingBox != null && element.Selector != null)
            .Select(element =>
            {
                var distance = CalculateDistance(
                    element.BoundingBox!.X, element.BoundingBox.Y,
                    context.ExpectedPosition.X, context.ExpectedPosition.Y);
                var normalizedScore = Math.Max(0, 1.0 - (distance / MaxVisualDistancePx));
                
                return new { Element = element, Distance = distance, Score = normalizedScore };
            })
            .Where(x => x.Score >= MinimumConfidenceThreshold)
            .Select(x => new SelectorCandidate
            {
                Selector = x.Element.Selector!,
                Strategy = HealingStrategy.VisualSimilarity,
                BaseConfidence = x.Score,
                ElementInfo = x.Element,
                VisualSimilarityScore = x.Score,
                Reasoning = $"Position-based match (distance: {x.Distance:F0}px) - actual visual comparison not yet implemented"
            })
            .ToList();

        return candidates;
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
                var normalizedScore = Math.Max(0, 1.0 - (distance / MaxPositionDistancePx));
                
                return new { Element = element, Distance = distance, Score = normalizedScore };
            })
            .Where(x => x.Score >= MinimumConfidenceThreshold)
            .Select(x => new SelectorCandidate
            {
                Selector = x.Element.Selector!,
                Strategy = HealingStrategy.Position,
                BaseConfidence = x.Score,
                ElementInfo = x.Element,
                PositionScore = x.Score,
                Reasoning = $"Position proximity (distance: {x.Distance:F0}px)"
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
