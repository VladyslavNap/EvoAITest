using System.Text.Json;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.SelfHealing;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Agents.Agents;

/// <summary>
/// AI-powered selector generation agent that uses LLM to analyze page structure
/// and generate robust CSS selectors with multiple alternatives.
/// </summary>
public sealed class SelectorAgent : ISelectorAgent
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<SelectorAgent> _logger;

    public SelectorAgent(
        ILLMProvider llmProvider,
        ILogger<SelectorAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates multiple selector candidates for a failed selector using LLM analysis.
    /// </summary>
    /// <param name="context">The healing context with page state and failure information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of selector candidates with confidence scores and reasoning.</returns>
    public async Task<List<SelectorCandidate>> GenerateSelectorCandidatesAsync(
        HealingContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating selector candidates for failed selector: {Selector}",
            context.FailedSelector);

        try
        {
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(context);

            var llmRequest = new LLMRequest
            {
                Model = _llmProvider.GetModelName(),
                Messages = new List<Message>
                {
                    new() { Role = MessageRole.System, Content = systemPrompt },
                    new() { Role = MessageRole.User, Content = userPrompt }
                },
                Temperature = 0.3, // Lower temperature for more deterministic selector generation
                MaxTokens = 2000,
                ResponseFormat = new ResponseFormat { Type = "json_object" }
            };

            var response = await _llmProvider.CompleteAsync(llmRequest, cancellationToken);

            if (response.Choices.Count == 0 || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("LLM returned empty response for selector generation");
                return new List<SelectorCandidate>();
            }

            var candidates = ParseLLMResponse(response.Content, context);

            _logger.LogInformation(
                "Generated {Count} selector candidates from LLM",
                candidates.Count);

            return candidates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating selector candidates with LLM");
            return new List<SelectorCandidate>();
        }
    }

    /// <summary>
    /// Validates a selector candidate against the page state.
    /// </summary>
    public async Task<bool> ValidateSelectorAsync(
        string selector,
        PageState pageState,
        CancellationToken cancellationToken = default)
    {
        // Check if selector matches exactly one element
        var matchingElements = pageState.VisibleElements
            .Where(e => e.Selector == selector || MatchesSelector(e, selector))
            .ToList();

        return matchingElements.Count == 1;
    }

    /// <summary>
    /// Builds the system prompt for selector generation.
    /// </summary>
    private string BuildSystemPrompt()
    {
        return @"You are an expert CSS selector generator for web automation.

Your task is to analyze a failed CSS selector and the current page structure, then generate robust alternative selectors that will reliably locate the intended element.

## Guidelines:
1. Generate 3-5 alternative selectors in order of reliability
2. Prefer unique attributes (id, data-*, aria-*, name) over generic classes
3. Use attribute selectors [attr=""value""] when possible
4. Avoid overly specific selectors that break on minor DOM changes
5. Consider text content matching using :contains() or :has-text() (Playwright-specific pseudo-classes)
6. Include reasoning for each selector explaining why it should work

## Selector Priority (from best to worst):
1. Unique ID: #unique-id
2. Unique data attributes: [data-testid=""value""]
3. Unique ARIA labels: [aria-label=""value""]
4. Unique name attributes: [name=""value""]
5. Combination of tag + attributes: button[type=""submit""]
6. Text content (Playwright syntax): button:has-text(""Submit"")
7. Position-based (least reliable): .parent > .child:nth-child(2)

Note: :contains() and :has-text() are Playwright-specific extensions and not standard CSS.
If standard CSS is required, use attribute selectors instead.

## Output Format:
Return a JSON object with this structure:
{
  ""candidates"": [
    {
      ""selector"": ""#submit-button"",
      ""strategy"": ""UniqueId"",
      ""confidence"": 0.95,
      ""reasoning"": ""Unique ID selector is most reliable"",
      ""isValidCss"": true
    }
  ]
}

Return ONLY valid JSON. Do not include any explanation outside the JSON structure.";
    }

    /// <summary>
    /// Builds the user prompt with context about the failed selector and page state.
    /// </summary>
    private string BuildUserPrompt(HealingContext context)
    {
        var prompt = $@"## Failed Selector
{context.FailedSelector}

## Expected Element Information
";

        if (!string.IsNullOrWhiteSpace(context.ExpectedText))
        {
            prompt += $"Expected Text: {context.ExpectedText}\n";
        }

        if (context.ExpectedPosition != null)
        {
            prompt += $"Expected Position: X={context.ExpectedPosition.X}, Y={context.ExpectedPosition.Y}\n";
        }

        prompt += $@"

## Current Page Information
URL: {context.PageState.Url}
Title: {context.PageState.Title}

## Available Interactive Elements (potential matches)
";

        // Include up to 20 interactive elements that might match
        var potentialMatches = context.PageState.InteractiveElements
            .Take(20)
            .Select((e, index) => FormatElementInfo(e, index + 1));

        prompt += string.Join("\n\n", potentialMatches);

        prompt += @"

## Task
Analyze the failed selector and current page structure. Generate 3-5 alternative CSS selectors that could locate the intended element. 
Prioritize selectors that use unique attributes and are resilient to minor DOM changes.

Consider:
1. Elements with matching text content
2. Elements with similar attributes
3. Elements in similar positions
4. Elements with similar roles/purposes

Return your response as JSON following the specified format.";

        return prompt;
    }

    /// <summary>
    /// Formats element information for the LLM prompt.
    /// </summary>
    private string FormatElementInfo(ElementInfo element, int number)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Element {number}:");
        sb.AppendLine($"  Tag: {element.TagName}");

        if (!string.IsNullOrWhiteSpace(element.Selector))
        {
            sb.AppendLine($"  Selector: {element.Selector}");
        }

        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            var truncatedText = element.Text.Length > 100 
                ? element.Text.Substring(0, 100) + "..." 
                : element.Text;
            sb.AppendLine($"  Text: {truncatedText}");
        }

        // Include relevant attributes
        var relevantAttrs = element.Attributes
            .Where(a => IsRelevantAttribute(a.Key))
            .Take(5);

        foreach (var attr in relevantAttrs)
        {
            sb.AppendLine($"  @{attr.Key}: {attr.Value}");
        }

        if (element.BoundingBox != null)
        {
            sb.AppendLine($"  Position: ({element.BoundingBox.X}, {element.BoundingBox.Y})");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines if an attribute is relevant for selector generation.
    /// </summary>
    private bool IsRelevantAttribute(string attributeName)
    {
        var relevantAttrs = new[]
        {
            "id", "class", "name", "type", "value", "placeholder",
            "aria-label", "aria-describedby", "role", "title",
            "data-testid", "data-test", "data-cy", "data-qa"
        };

        return relevantAttrs.Contains(attributeName.ToLower()) ||
               attributeName.StartsWith("data-", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses the LLM JSON response into selector candidates.
    /// </summary>
    private List<SelectorCandidate> ParseLLMResponse(string jsonResponse, HealingContext context)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<SelectorGenerationResponse>(jsonResponse, options);

            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                _logger.LogWarning("LLM response contained no candidates");
                return new List<SelectorCandidate>();
            }

            var candidates = response.Candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Selector))
                .Select(candidate =>
                {
                    // Parse strategy enum
                    if (!Enum.TryParse<HealingStrategy>(candidate.Strategy, true, out var strategy))
                    {
                        strategy = HealingStrategy.LLMGenerated;
                    }

                    return new SelectorCandidate
                    {
                        Selector = candidate.Selector,
                        Strategy = strategy,
                        BaseConfidence = Math.Clamp(candidate.Confidence, 0.0, 1.0),
                        Reasoning = candidate.Reasoning ?? "LLM-generated selector",
                        Context = new Dictionary<string, object>
                        {
                            ["llm_generated"] = true,
                            ["is_valid_css"] = candidate.IsValidCss
                        }
                    };
                })
                .ToList();

            return candidates;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response as JSON: {Response}", jsonResponse);
            return new List<SelectorCandidate>();
        }
    }

    /// <summary>
    /// Checks if an element matches a CSS selector (simple implementation).
    /// </summary>
    private bool MatchesSelector(ElementInfo element, string selector)
    {
        // Simple matching - in production would use proper CSS selector matching
        if (selector.StartsWith("#"))
        {
            var id = selector.Substring(1);
            return element.Attributes.TryGetValue("id", out var elementId) && elementId == id;
        }

        if (selector.StartsWith("."))
        {
            var className = selector.Substring(1);
            return element.Attributes.TryGetValue("class", out var classes) && 
                   classes.Split(' ').Contains(className);
        }

        if (selector.StartsWith("[") && selector.EndsWith("]"))
        {
            // Attribute selector [attr="value"]
            var attrMatch = System.Text.RegularExpressions.Regex.Match(
                selector, @"\[([^\]]+)=""([^""]+)""\]");
            
            if (attrMatch.Success)
            {
                var attrName = attrMatch.Groups[1].Value;
                var attrValue = attrMatch.Groups[2].Value;
                return element.Attributes.TryGetValue(attrName, out var value) && value == attrValue;
            }
        }

        return false;
    }

    #region ISelectorAgent Implementation

    /// <summary>
    /// Generates selector candidates using LLM analysis (interface implementation).
    /// </summary>
    Task<List<SelectorCandidate>> ISelectorAgent.GenerateSelectorCandidatesAsync(
        PageState pageState,
        string failedSelector,
        string? expectedText,
        CancellationToken cancellationToken)
    {
        // Create healing context from parameters
        var context = new HealingContext
        {
            FailedSelector = failedSelector,
            PageState = pageState,
            ExpectedText = expectedText
        };

        // Call existing implementation
        return GenerateSelectorCandidatesAsync(context, cancellationToken);
    }

    /// <summary>
    /// Generates the best selector using LLM analysis (interface implementation).
    /// </summary>
    async Task<SelectorCandidate?> ISelectorAgent.GenerateBestSelectorAsync(
        PageState pageState,
        string failedSelector,
        string? expectedText,
        CancellationToken cancellationToken)
    {
        var candidates = await ((ISelectorAgent)this).GenerateSelectorCandidatesAsync(
            pageState, failedSelector, expectedText, cancellationToken);

        // Return the candidate with highest confidence
        return candidates
            .OrderByDescending(c => c.BaseConfidence)
            .FirstOrDefault();
    }

    #endregion

    #region Response Models

    private class CandidateDto
    {
        public string Selector { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string? Reasoning { get; set; }
        public bool IsValidCss { get; set; } = true;
    }

    private class SelectorGenerationResponse
    {
        public List<CandidateDto> Candidates { get; set; } = new();
    }

    #endregion
}
