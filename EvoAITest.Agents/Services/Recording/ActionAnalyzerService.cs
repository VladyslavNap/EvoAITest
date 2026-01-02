using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Recording;
using EvoAITest.Core.Options;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EvoAITest.Agents.Services.Recording;

/// <summary>
/// AI-powered service for analyzing user interactions and detecting intent
/// </summary>
public sealed class ActionAnalyzerService : IActionAnalyzer
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<ActionAnalyzerService> _logger;
    private readonly RecordingOptions _options;

    public ActionAnalyzerService(
        ILLMProvider llmProvider,
        ILogger<ActionAnalyzerService> logger,
        IOptions<RecordingOptions> options)
    {
        _llmProvider = llmProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<UserInteraction> AnalyzeInteractionAsync(
        UserInteraction interaction,
        RecordingSession context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analyzing interaction {Id} of type {Type}", 
            interaction.Id, interaction.ActionType);

        try
        {
            // Detect intent
            var (intent, confidence) = await DetectIntentAsync(
                interaction,
                context.Interactions.TakeLast(5).ToList(),
                cancellationToken);

            // Generate description
            var description = await GenerateDescriptionAsync(interaction, cancellationToken);

            // Update interaction with analysis results
            interaction.Intent = intent;
            interaction.IntentConfidence = confidence;
            interaction.Description = description;

            _logger.LogInformation(
                "Analyzed interaction: {Type} -> {Intent} (confidence: {Confidence:P0})",
                interaction.ActionType,
                intent,
                confidence);

            return interaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze interaction {Id}", interaction.Id);
            
            // Return interaction with Unknown intent on failure
            interaction.Intent = ActionIntent.Unknown;
            interaction.IntentConfidence = 0;
            interaction.Description = $"{interaction.ActionType} action";
            
            return interaction;
        }
    }

    public async Task<List<InteractionGroup>> AnalyzeInteractionPatternsAsync(
        List<UserInteraction> interactions,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing patterns in {Count} interactions", interactions.Count);

        if (interactions.Count == 0)
        {
            return [];
        }

        var prompt = BuildPatternAnalysisPrompt(interactions);

        var request = new LLMRequest
        {
            Model = _options.AnalysisModel,
            Temperature = _options.LlmTemperature,
            MaxTokens = _options.MaxLlmTokens,
            Messages =
            [
                new Message
                {
                    Role = MessageRole.System,
                    Content = "You are an expert test automation engineer analyzing user interaction patterns to group related actions into logical test scenarios."
                },
                new Message
                {
                    Role = MessageRole.User,
                    Content = prompt
                }
            ]
        };

        try
        {
            var response = await _llmProvider.CompleteAsync(request, cancellationToken);
            var groups = ParseInteractionGroups(response.Content, interactions);

            _logger.LogInformation("Identified {Count} interaction groups", groups.Count);
            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze interaction patterns");
            
            // Fallback: Create a single group with all interactions
            return
            [
                new InteractionGroup
                {
                    Name = "User Workflow",
                    Description = "All recorded user interactions",
                    GroupIntent = ActionIntent.Unknown,
                    Interactions = interactions,
                    SuggestedTestName = "UserWorkflowTest"
                }
            ];
        }
    }

    public async Task<string> GenerateDescriptionAsync(
        UserInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildDescriptionPrompt(interaction);

        var request = new LLMRequest
        {
            Model = _options.AnalysisModel,
            Temperature = _options.LlmTemperature,
            MaxTokens = 100,
            Messages =
            [
                new Message
                {
                    Role = MessageRole.System,
                    Content = "You are an expert at creating clear, concise descriptions of user actions for test documentation. Generate descriptions in past tense, third person, suitable for test step documentation."
                },
                new Message
                {
                    Role = MessageRole.User,
                    Content = prompt
                }
            ]
        };

        try
        {
            var response = await _llmProvider.CompleteAsync(request, cancellationToken);
            return response.Content.Trim().Trim('"');
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate description for interaction {Id}", interaction.Id);
            return GenerateFallbackDescription(interaction);
        }
    }

    public async Task<(ActionIntent Intent, double Confidence)> DetectIntentAsync(
        UserInteraction interaction,
        List<UserInteraction> previousInteractions,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildIntentDetectionPrompt(interaction, previousInteractions);

        var request = new LLMRequest
        {
            Model = _options.AnalysisModel,
            Temperature = 0.1, // Lower temperature for more deterministic intent classification
            MaxTokens = 150,
            Messages =
            [
                new Message
                {
                    Role = MessageRole.System,
                    Content = @"You are an expert at analyzing user interactions and detecting their intent. 
Respond with a JSON object containing 'intent' (one of: Authentication, Search, Navigation, FormSubmission, DataEntry, Selection, Validation, Creation, Update, Deletion, Download, Upload, DialogInteraction, Waiting, ErrorVerification) and 'confidence' (0.0-1.0).

Example response:
{
  ""intent"": ""Authentication"",
  ""confidence"": 0.95,
  ""reasoning"": ""User entered credentials in login form""
}"
                },
                new Message
                {
                    Role = MessageRole.User,
                    Content = prompt
                }
            ]
        };

        try
        {
            var response = await _llmProvider.CompleteAsync(request, cancellationToken);
            return ParseIntentResponse(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect intent for interaction {Id}", interaction.Id);
            return (InferIntentFromAction(interaction), 0.5);
        }
    }

    public async Task<ActionRecognitionMetrics> ValidateActionRecognitionAsync(
        RecordingSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating action recognition for session {SessionId}", session.Id);

        var interactionsWithIntent = session.Interactions
            .Where(i => i.Intent != ActionIntent.Unknown)
            .ToList();

        var totalActions = session.Interactions.Count;
        var correctRecognitions = interactionsWithIntent
            .Count(i => i.IntentConfidence >= _options.MinimumConfidenceThreshold);

        var averageConfidence = interactionsWithIntent.Any()
            ? interactionsWithIntent.Average(i => i.IntentConfidence)
            : 0.0;

        var accuracyPercentage = totalActions > 0
            ? (double)correctRecognitions / totalActions * 100
            : 0.0;

        var lowConfidenceActions = session.Interactions
            .Where(i => i.IntentConfidence < _options.MinimumConfidenceThreshold)
            .ToList();

        var accuracyByType = session.Interactions
            .GroupBy(i => i.ActionType)
            .ToDictionary(
                g => g.Key,
                g => g.Count(i => i.IntentConfidence >= _options.MinimumConfidenceThreshold) / (double)g.Count() * 100
            );

        var metrics = new ActionRecognitionMetrics
        {
            AccuracyPercentage = accuracyPercentage,
            CorrectRecognitions = correctRecognitions,
            TotalActions = totalActions,
            AverageConfidence = averageConfidence,
            LowConfidenceActions = lowConfidenceActions,
            AccuracyByActionType = accuracyByType
        };

        _logger.LogInformation(
            "Action recognition metrics - Accuracy: {Accuracy:P0}, Avg Confidence: {Confidence:P0}",
            metrics.AccuracyPercentage / 100,
            metrics.AverageConfidence);

        return metrics;
    }

    #region Helper Methods

    private string BuildIntentDetectionPrompt(
        UserInteraction interaction,
        List<UserInteraction> previousInteractions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following user interaction and determine its intent:");
        sb.AppendLine();
        sb.AppendLine($"Action Type: {interaction.ActionType}");
        sb.AppendLine($"URL: {interaction.Context.Url}");
        sb.AppendLine($"Element: {interaction.Context.ElementTag}");
        
        if (!string.IsNullOrEmpty(interaction.Context.ElementText))
        {
            sb.AppendLine($"Element Text: {interaction.Context.ElementText}");
        }

        if (!string.IsNullOrEmpty(interaction.InputValue))
        {
            sb.AppendLine($"Input Value: {MaskSensitiveData(interaction.InputValue)}");
        }

        if (!string.IsNullOrEmpty(interaction.Context.TargetSelector))
        {
            sb.AppendLine($"Selector: {interaction.Context.TargetSelector}");
        }

        if (previousInteractions.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Previous actions (for context):");
            foreach (var prev in previousInteractions.TakeLast(3))
            {
                sb.AppendLine($"- {prev.ActionType} on {prev.Context.ElementTag}");
            }
        }

        return sb.ToString();
    }

    private string BuildDescriptionPrompt(UserInteraction interaction)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate a clear, concise test step description for this user action:");
        sb.AppendLine();
        sb.AppendLine($"Action: {interaction.ActionType}");
        sb.AppendLine($"Page: {interaction.Context.Url}");
        sb.AppendLine($"Element: {interaction.Context.ElementTag}");
        
        if (!string.IsNullOrEmpty(interaction.Context.ElementText))
        {
            sb.AppendLine($"Text: {interaction.Context.ElementText}");
        }

        if (!string.IsNullOrEmpty(interaction.InputValue))
        {
            sb.AppendLine($"Value: {MaskSensitiveData(interaction.InputValue)}");
        }

        sb.AppendLine();
        sb.AppendLine("Provide a single sentence description suitable for test documentation.");

        return sb.ToString();
    }

    private string BuildPatternAnalysisPrompt(List<UserInteraction> interactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze these user interactions and group them into logical test scenarios:");
        sb.AppendLine();

        foreach (var interaction in interactions)
        {
            sb.AppendLine($"{interaction.SequenceNumber}. {interaction.ActionType} - {interaction.Context.Url}");
            if (!string.IsNullOrEmpty(interaction.Context.ElementText))
            {
                sb.AppendLine($"   Text: {interaction.Context.ElementText}");
            }
        }

        sb.AppendLine();
        sb.AppendLine(@"Respond with JSON array of groups:
[
  {
    ""name"": ""Group Name"",
    ""description"": ""What this group accomplishes"",
    ""intent"": ""PrimaryIntent"",
    ""interactionNumbers"": [1, 2, 3],
    ""suggestedTestName"": ""TestMethodName""
  }
]");

        return sb.ToString();
    }

    private (ActionIntent Intent, double Confidence) ParseIntentResponse(string responseContent)
    {
        try
        {
            // Try to extract JSON from response
            var jsonStart = responseContent.IndexOf('{');
            var jsonEnd = responseContent.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = responseContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("intent", out var intentProp) &&
                    root.TryGetProperty("confidence", out var confidenceProp))
                {
                    var intentStr = intentProp.GetString();
                    var confidence = confidenceProp.GetDouble();

                    if (Enum.TryParse<ActionIntent>(intentStr, out var intent))
                    {
                        return (intent, confidence);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse intent response: {Response}", responseContent);
        }

        return (ActionIntent.Unknown, 0.5);
    }

    private List<InteractionGroup> ParseInteractionGroups(
        string responseContent,
        List<UserInteraction> allInteractions)
    {
        try
        {
            var jsonStart = responseContent.IndexOf('[');
            var jsonEnd = responseContent.LastIndexOf(']');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = responseContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var doc = JsonDocument.Parse(json);
                var groups = new List<InteractionGroup>();

                foreach (var groupElement in doc.RootElement.EnumerateArray())
                {
                    var name = groupElement.GetProperty("name").GetString() ?? "Unnamed Group";
                    var description = groupElement.TryGetProperty("description", out var descProp)
                        ? descProp.GetString()
                        : null;
                    var intentStr = groupElement.TryGetProperty("intent", out var intentProp)
                        ? intentProp.GetString()
                        : "Unknown";
                    var testName = groupElement.TryGetProperty("suggestedTestName", out var testProp)
                        ? testProp.GetString()
                        : null;

                    var interactionNumbers = new List<int>();
                    if (groupElement.TryGetProperty("interactionNumbers", out var numbersProp))
                    {
                        foreach (var num in numbersProp.EnumerateArray())
                        {
                            interactionNumbers.Add(num.GetInt32());
                        }
                    }

                    var interactions = allInteractions
                        .Where(i => interactionNumbers.Contains(i.SequenceNumber))
                        .ToList();

                    if (interactions.Any())
                    {
                        groups.Add(new InteractionGroup
                        {
                            Name = name,
                            Description = description,
                            GroupIntent = Enum.TryParse<ActionIntent>(intentStr, out var intent)
                                ? intent
                                : ActionIntent.Unknown,
                            Interactions = interactions,
                            SuggestedTestName = testName
                        });
                    }
                }

                return groups;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse interaction groups");
        }

        return [];
    }

    private ActionIntent InferIntentFromAction(UserInteraction interaction)
    {
        // Fallback rule-based intent inference
        return interaction.ActionType switch
        {
            ActionType.Navigation => ActionIntent.Navigation,
            ActionType.Submit => ActionIntent.FormSubmission,
            ActionType.Input => ActionIntent.DataEntry,
            ActionType.Select => ActionIntent.Selection,
            ActionType.Click when interaction.Context.ElementText?.Contains("login", StringComparison.OrdinalIgnoreCase) == true
                => ActionIntent.Authentication,
            ActionType.Click when interaction.Context.ElementText?.Contains("search", StringComparison.OrdinalIgnoreCase) == true
                => ActionIntent.Search,
            ActionType.Click when interaction.Context.ElementText?.Contains("delete", StringComparison.OrdinalIgnoreCase) == true
                => ActionIntent.Deletion,
            ActionType.FileUpload => ActionIntent.Upload,
            _ => ActionIntent.Unknown
        };
    }

    private string GenerateFallbackDescription(UserInteraction interaction)
    {
        var element = !string.IsNullOrEmpty(interaction.Context.ElementText)
            ? $" '{interaction.Context.ElementText}'"
            : "";

        return interaction.ActionType switch
        {
            ActionType.Click => $"Clicked{element} button",
            ActionType.Input => $"Entered text into{element} field",
            ActionType.Select => $"Selected option from{element} dropdown",
            ActionType.Submit => $"Submitted{element} form",
            ActionType.Navigation => $"Navigated to {interaction.Context.Url}",
            _ => $"Performed {interaction.ActionType} action"
        };
    }

    private string MaskSensitiveData(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Mask passwords
        if (value.All(c => c == '*'))
        {
            return "[PASSWORD]";
        }

        // Mask emails
        if (value.Contains('@') && value.Contains('.'))
        {
            var parts = value.Split('@');
            if (parts.Length == 2)
            {
                return $"{parts[0][0]}***@{parts[1]}";
            }
        }

        return value;
    }

    #endregion
}
