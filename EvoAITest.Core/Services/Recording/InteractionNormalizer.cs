using EvoAITest.Core.Models.Recording;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services.Recording;

/// <summary>
/// Service for normalizing and deduplicating recorded interactions
/// </summary>
public sealed class InteractionNormalizer
{
    private readonly ILogger<InteractionNormalizer> _logger;
    private readonly Queue<UserInteraction> _recentInteractions = new();
    private const int RecentInteractionsBufferSize = 10;

    public InteractionNormalizer(ILogger<InteractionNormalizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Normalizes an interaction by cleaning and standardizing data
    /// </summary>
    public UserInteraction Normalize(UserInteraction interaction)
    {
        var needsUpdate = false;
        var updatedContext = interaction.Context;
        var updatedInputValue = interaction.InputValue;

        // Clean selector
        if (!string.IsNullOrEmpty(interaction.Context.TargetSelector))
        {
            var normalizedSelector = NormalizeSelector(interaction.Context.TargetSelector);
            if (normalizedSelector != interaction.Context.TargetSelector)
            {
                updatedContext = new ActionContext
                {
                    Url = interaction.Context.Url,
                    PageTitle = interaction.Context.PageTitle,
                    ViewportWidth = interaction.Context.ViewportWidth,
                    ViewportHeight = interaction.Context.ViewportHeight,
                    TargetSelector = normalizedSelector,
                    TargetXPath = interaction.Context.TargetXPath,
                    ElementText = interaction.Context.ElementText,
                    ElementTag = interaction.Context.ElementTag,
                    ElementAttributes = interaction.Context.ElementAttributes,
                    ScrollX = interaction.Context.ScrollX,
                    ScrollY = interaction.Context.ScrollY,
                    ScreenshotData = interaction.Context.ScreenshotData,
                    Metadata = interaction.Context.Metadata
                };
                needsUpdate = true;
            }
        }

        // Trim text content
        if (!string.IsNullOrEmpty(updatedContext.ElementText))
        {
            var trimmedText = updatedContext.ElementText.Trim();
            if (trimmedText != updatedContext.ElementText)
            {
                updatedContext = new ActionContext
                {
                    Url = updatedContext.Url,
                    PageTitle = updatedContext.PageTitle,
                    ViewportWidth = updatedContext.ViewportWidth,
                    ViewportHeight = updatedContext.ViewportHeight,
                    TargetSelector = updatedContext.TargetSelector,
                    TargetXPath = updatedContext.TargetXPath,
                    ElementText = trimmedText,
                    ElementTag = updatedContext.ElementTag,
                    ElementAttributes = updatedContext.ElementAttributes,
                    ScrollX = updatedContext.ScrollX,
                    ScrollY = updatedContext.ScrollY,
                    ScreenshotData = updatedContext.ScreenshotData,
                    Metadata = updatedContext.Metadata
                };
                needsUpdate = true;
            }
        }

        // Normalize input values
        if (!string.IsNullOrEmpty(interaction.InputValue))
        {
            var normalizedValue = NormalizeInputValue(interaction.InputValue, interaction.ActionType);
            if (normalizedValue != interaction.InputValue)
            {
                updatedInputValue = normalizedValue;
                needsUpdate = true;
            }
        }

        // Return new instance if anything changed
        if (needsUpdate)
        {
            return new UserInteraction
            {
                Id = interaction.Id,
                SessionId = interaction.SessionId,
                SequenceNumber = interaction.SequenceNumber,
                ActionType = interaction.ActionType,
                Intent = interaction.Intent,
                Description = interaction.Description,
                Timestamp = interaction.Timestamp,
                DurationMs = interaction.DurationMs,
                InputValue = updatedInputValue,
                Key = interaction.Key,
                Coordinates = interaction.Coordinates,
                Context = updatedContext,
                IncludeInTest = interaction.IncludeInTest,
                IntentConfidence = interaction.IntentConfidence,
                GeneratedCode = interaction.GeneratedCode,
                Assertions = interaction.Assertions
            };
        }

        return interaction;
    }

    /// <summary>
    /// Checks if an interaction is a duplicate of recent interactions
    /// </summary>
    public bool IsDuplicate(UserInteraction interaction)
    {
        // Check against recent interactions
        var similarInteraction = _recentInteractions
            .Where(recent => AreSimilarInteractions(recent, interaction))
            .FirstOrDefault();

        if (similarInteraction != null)
        {
            _logger.LogTrace(
                "Duplicate interaction detected: {ActionType} on {Selector}",
                interaction.ActionType,
                interaction.Context.TargetSelector);
            return true;
        }

        // Add to recent buffer
        _recentInteractions.Enqueue(interaction);
        if (_recentInteractions.Count > RecentInteractionsBufferSize)
        {
            _recentInteractions.Dequeue();
        }

        return false;
    }

    /// <summary>
    /// Merges consecutive similar interactions into a single interaction
    /// </summary>
    public List<UserInteraction> MergeConsecutiveSimilar(List<UserInteraction> interactions)
    {
        if (interactions.Count <= 1)
        {
            return interactions;
        }

        var merged = new List<UserInteraction>();
        UserInteraction? current = null;

        foreach (var interaction in interactions)
        {
            if (current == null)
            {
                current = interaction;
                continue;
            }

            // Check if should be merged
            if (ShouldMerge(current, interaction))
            {
                current = MergeInteractions(current, interaction);
            }
            else
            {
                merged.Add(current);
                current = interaction;
            }
        }

        if (current != null)
        {
            merged.Add(current);
        }

        if (merged.Count < interactions.Count)
        {
            _logger.LogDebug(
                "Merged {Original} interactions into {Merged}",
                interactions.Count,
                merged.Count);
        }

        return merged;
    }

    /// <summary>
    /// Filters out noise and insignificant interactions
    /// </summary>
    public List<UserInteraction> FilterNoise(List<UserInteraction> interactions)
    {
        var filtered = interactions.Where(i => !IsNoiseInteraction(i)).ToList();

        if (filtered.Count < interactions.Count)
        {
            _logger.LogDebug(
                "Filtered {Count} noise interactions",
                interactions.Count - filtered.Count);
        }

        return filtered;
    }

    private string NormalizeSelector(string selector)
    {
        // Remove dynamic IDs and classes
        selector = System.Text.RegularExpressions.Regex.Replace(
            selector,
            @"\[id[*^$]?=['""][^'""]*\d{4,}[^'""]*['""]",
            "[data-test-id]");

        // Normalize nth-child selectors
        selector = System.Text.RegularExpressions.Regex.Replace(
            selector,
            @":nth-child\(\d+\)",
            ":nth-child(n)");

        return selector.Trim();
    }

    private string NormalizeInputValue(string value, ActionType actionType)
    {
        // Don't normalize password fields
        if (value.Length > 0 && value.All(c => c == '*'))
        {
            return "[PASSWORD]";
        }

        // Normalize email addresses for privacy
        if (value.Contains('@') && value.Contains('.'))
        {
            return "[EMAIL]";
        }

        // Trim and normalize whitespace
        return string.Join(" ", value.Split(
            new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries));
    }

    private bool AreSimilarInteractions(UserInteraction a, UserInteraction b)
    {
        // Must be same action type
        if (a.ActionType != b.ActionType)
        {
            return false;
        }

        // Must be on same element (similar selector)
        if (!AreSimilarSelectors(a.Context.TargetSelector, b.Context.TargetSelector))
        {
            return false;
        }

        // Must be within time threshold (500ms)
        var timeDiff = Math.Abs((b.Timestamp - a.Timestamp).TotalMilliseconds);
        if (timeDiff > 500)
        {
            return false;
        }

        // For input actions, check if values are similar
        if (a.ActionType == ActionType.Input)
        {
            return AreSimilarInputValues(a.InputValue, b.InputValue);
        }

        return true;
    }

    private bool ShouldMerge(UserInteraction a, UserInteraction b)
    {
        // Only merge consecutive input actions on the same element
        if (a.ActionType != ActionType.Input || b.ActionType != ActionType.Input)
        {
            return false;
        }

        if (!AreSimilarSelectors(a.Context.TargetSelector, b.Context.TargetSelector))
        {
            return false;
        }

        // Within 2 seconds
        var timeDiff = (b.Timestamp - a.Timestamp).TotalSeconds;
        return timeDiff <= 2;
    }

    private UserInteraction MergeInteractions(UserInteraction first, UserInteraction second)
    {
        // Create a new interaction with merged data
        return new UserInteraction
        {
            Id = first.Id,
            SessionId = first.SessionId,
            SequenceNumber = first.SequenceNumber,
            ActionType = first.ActionType,
            Intent = first.Intent,
            Description = first.Description,
            Timestamp = second.Timestamp,
            DurationMs = (int)(second.Timestamp - first.Timestamp).TotalMilliseconds,
            InputValue = second.InputValue, // Use the latest value
            Key = second.Key,
            Coordinates = second.Coordinates,
            Context = second.Context,
            IncludeInTest = first.IncludeInTest,
            IntentConfidence = first.IntentConfidence,
            GeneratedCode = first.GeneratedCode,
            Assertions = first.Assertions
        };
    }

    private bool IsNoiseInteraction(UserInteraction interaction)
    {
        // Filter out hover actions if configured
        if (interaction.ActionType == ActionType.Hover)
        {
            return true;
        }

        // Filter out very rapid duplicate clicks
        if (interaction.ActionType == ActionType.Click && interaction.DurationMs < 50)
        {
            return true;
        }

        // Filter out empty inputs
        if (interaction.ActionType == ActionType.Input &&
            string.IsNullOrWhiteSpace(interaction.InputValue))
        {
            return true;
        }

        return false;
    }

    private bool AreSimilarSelectors(string? selector1, string? selector2)
    {
        if (string.IsNullOrEmpty(selector1) || string.IsNullOrEmpty(selector2))
        {
            return false;
        }

        // Exact match
        if (selector1 == selector2)
        {
            return true;
        }

        // Normalize and compare
        var normalized1 = NormalizeSelector(selector1);
        var normalized2 = NormalizeSelector(selector2);

        return normalized1 == normalized2;
    }

    private bool AreSimilarInputValues(string? value1, string? value2)
    {
        if (string.IsNullOrEmpty(value1) || string.IsNullOrEmpty(value2))
        {
            return false;
        }

        // Check if one value is a prefix of the other (typing in progress)
        return value2.StartsWith(value1) || value1.StartsWith(value2);
    }
}
