using System.Text.Json;
using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Models;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;
using AgentExecutionContext = EvoAITest.Agents.Abstractions.ExecutionContext;

namespace EvoAITest.Agents.Agents;

/// <summary>
/// AI-powered healer agent that analyzes execution failures and suggests alternative approaches.
/// Uses LLM to diagnose root causes and adaptively replan or modify failed steps.
/// </summary>
/// <remarks>
/// <para>
/// The HealerAgent is responsible for the self-healing phase of the agent lifecycle:
/// - Analyzes execution failures from ExecutorAgent
/// - Uses LLM to diagnose root cause of errors
/// - Suggests alternative locators, longer waits, or different approaches
/// - Can optionally trigger replanning via PlannerAgent
/// - Tracks healing attempts to prevent infinite loops
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var healer = serviceProvider.GetRequiredService&lt;IHealer&gt;();
/// 
/// if (!stepResult.Success &amp;&amp; stepResult.Error != null)
/// {
///     var analysis = await healer.AnalyzeErrorAsync(stepResult.Error, context);
///     
///     if (analysis.IsHealable)
///     {
///         var healingResult = await healer.HealStepAsync(failedStep, stepResult.Error, context);
///         
///         if (healingResult.Success)
///         {
///             Console.WriteLine($"Applied healing: {healingResult.Strategy?.Name}");
///             await executor.ExecuteStepAsync(healingResult.HealedStep!, context);
///         }
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public sealed class HealerAgent : IHealer
{
    private readonly ILLMProvider _llmProvider;
    private readonly IBrowserAgent _browserAgent;
    private readonly ILogger<HealerAgent> _logger;
    
    // Track healing attempts per step to prevent infinite loops
    private readonly Dictionary<string, int> _healingAttempts = new();
    private const int MaxHealingAttempts = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealerAgent"/> class.
    /// </summary>
    /// <param name="llmProvider">The LLM provider for diagnostic analysis.</param>
    /// <param name="browserAgent">The browser agent for page state inspection.</param>
    /// <param name="logger">Logger for diagnostics and telemetry.</param>
    public HealerAgent(
        ILLMProvider llmProvider,
        IBrowserAgent browserAgent,
        ILogger<HealerAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _browserAgent = browserAgent ?? throw new ArgumentNullException(nameof(browserAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<HealingResult> HealStepAsync(
        AgentStep failedStep,
        Exception error,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failedStep);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Starting healing attempt for step {StepNumber}: {Action} [Error: {ErrorType}, CorrelationId: {CorrelationId}]",
            failedStep.StepNumber,
            failedStep.Action?.Type.ToString() ?? "Unknown",
            error.GetType().Name,
            correlationId);

        // Check if we've exceeded max healing attempts for this step
        if (_healingAttempts.TryGetValue(failedStep.Id, out var attempts) && attempts >= MaxHealingAttempts)
        {
            _logger.LogWarning(
                "Maximum healing attempts ({MaxAttempts}) exceeded for step {StepNumber}",
                MaxHealingAttempts,
                failedStep.StepNumber);

            return new HealingResult
            {
                Success = false,
                Explanation = $"Maximum healing attempts ({MaxHealingAttempts}) exceeded for this step"
            };
        }

        // Increment healing attempt counter
        _healingAttempts[failedStep.Id] = attempts + 1;

        try
        {
            // Step 1: Analyze the error
            var errorAnalysis = await AnalyzeErrorAsync(error, context, cancellationToken);

            if (!errorAnalysis.IsHealable)
            {
                _logger.LogWarning(
                    "Error is not healable for step {StepNumber}: {RootCause}",
                    failedStep.StepNumber,
                    errorAnalysis.RootCause);

                return new HealingResult
                {
                    Success = false,
                    Explanation = $"Error is not healable: {errorAnalysis.RootCause}"
                };
            }

            // Step 2: Get page state for context
            PageState? pageState = null;
            try
            {
                pageState = await _browserAgent.GetPageStateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture page state for healing analysis");
            }

            // Step 3: Build diagnostic prompt for LLM
            var diagnosticPrompt = BuildDiagnosticPrompt(
                failedStep,
                error,
                errorAnalysis,
                pageState,
                context);

            _logger.LogDebug(
                "Sending diagnostic request to LLM: {Model}. Prompt length: {PromptLength} characters",
                _llmProvider.GetModelName(),
                diagnosticPrompt.Length);

            // Step 4: Request LLM analysis
            var llmRequest = new LLMRequest
            {
                Model = _llmProvider.GetModelName(),
                Messages = new List<Message>
                {
                    new() { Role = MessageRole.System, Content = BuildSystemPrompt() },
                    new() { Role = MessageRole.User, Content = diagnosticPrompt }
                },
                Temperature = 0.7,
                MaxTokens = 2000,
                ResponseFormat = new ResponseFormat { Type = "json_object" }
            };

            var llmResponse = await _llmProvider.CompleteAsync(llmRequest, cancellationToken);

            if (llmResponse.Choices.Count == 0)
            {
                throw new InvalidOperationException("LLM returned no choices in response");
            }

            var tokenUsage = _llmProvider.GetLastTokenUsage();
            _logger.LogDebug(
                "LLM diagnostic response received. Tokens: {InputTokens} in, {OutputTokens} out, ${Cost:F4} cost",
                tokenUsage.InputTokens,
                tokenUsage.OutputTokens,
                tokenUsage.EstimatedCostUSD);

            // Step 5: Parse LLM response and generate healed step
            var healingStrategy = ParseLLMResponseToStrategy(llmResponse.Content);
            var healedStep = ApplyHealingStrategy(failedStep, healingStrategy, errorAnalysis);

            var healingResult = new HealingResult
            {
                Success = true,
                HealedStep = healedStep,
                Strategy = healingStrategy,
                Explanation = healingStrategy.Description,
                Confidence = healingStrategy.Confidence
            };

            _logger.LogInformation(
                "Healing successful for step {StepNumber}. Strategy: {Strategy}, Confidence: {Confidence:P1}",
                failedStep.StepNumber,
                healingStrategy.Name,
                healingStrategy.Confidence);

            return healingResult;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Healing cancelled for step {StepNumber}", failedStep.StepNumber);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to parse LLM healing response as JSON for step {StepNumber}",
                failedStep.StepNumber);

            return new HealingResult
            {
                Success = false,
                Explanation = "Failed to parse LLM response. The response was not valid JSON."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to heal step {StepNumber}: {ErrorMessage}",
                failedStep.StepNumber,
                ex.Message);

            return new HealingResult
            {
                Success = false,
                Explanation = $"Healing failed: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    public async Task<ErrorAnalysis> AnalyzeErrorAsync(
        Exception error,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("Analyzing error: {ErrorType} - {ErrorMessage}", error.GetType().Name, error.Message);

        var errorType = ClassifyError(error);
        var isHealable = IsErrorHealable(errorType, error);
        var rootCause = DetermineRootCause(error, errorType);
        var severity = DetermineSeverity(errorType);

        var analysis = new ErrorAnalysis
        {
            Type = errorType,
            IsHealable = isHealable,
            RootCause = rootCause,
            Severity = severity,
            SuggestedStrategies = GetSuggestedStrategies(errorType)
        };

        _logger.LogDebug(
            "Error analysis complete: Type={ErrorType}, Healable={IsHealable}, Severity={Severity}",
            errorType,
            isHealable,
            severity);

        return await Task.FromResult(analysis);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HealingStrategy>> SuggestAlternativesAsync(
        IReadOnlyList<AgentStepResult> failedAttempts,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failedAttempts);

        if (failedAttempts.Count == 0)
        {
            return Array.Empty<HealingStrategy>();
        }

        _logger.LogInformation(
            "Suggesting alternatives based on {AttemptCount} failed attempts",
            failedAttempts.Count);

        var strategies = new List<HealingStrategy>();

        // Analyze patterns in failed attempts
        var errorTypes = failedAttempts
            .Where(a => a.Error != null)
            .Select(a => ClassifyError(a.Error!))
            .Distinct()
            .ToList();

        // Generate strategies based on error patterns
        foreach (var errorType in errorTypes)
        {
            strategies.AddRange(GetSuggestedStrategies(errorType));
        }

        // Sort by priority
        var sortedStrategies = strategies
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.Confidence)
            .ToList();

        _logger.LogInformation(
            "Generated {StrategyCount} alternative healing strategies",
            sortedStrategies.Count);

        return await Task.FromResult<IReadOnlyList<HealingStrategy>>(sortedStrategies.AsReadOnly());
    }

    // ============================================================
    // Private Helper Methods
    // ============================================================

    /// <summary>
    /// Builds the system prompt for the healing agent.
    /// </summary>
    private string BuildSystemPrompt()
    {
        return """
            You are an expert browser automation diagnostic agent. Your role is to analyze execution failures
            and suggest alternative approaches to heal broken automation steps.
            
            Key responsibilities:
            - Diagnose why a browser automation step failed
            - Identify the root cause of the error
            - Suggest specific, actionable healing strategies
            - Provide alternative element locators when selectors fail
            - Recommend timing adjustments when elements are not ready
            - Propose step decomposition for complex actions
            
            Available healing strategies:
            - RetryWithDelay: Add a delay and retry the same action
            - AlternativeLocator: Use a different element selector (CSS, XPath, text content, role)
            - ExtendedWait: Wait longer for elements to appear
            - ScrollToElement: Make element visible by scrolling
            - PageRefresh: Reload the page and retry
            - InteractionMethodChange: Try JavaScript click instead of native click
            - PopupHandling: Handle unexpected popups or dialogs
            - SimpleFallback: Use a simpler, more reliable approach
            
            Response format:
            Return a JSON object with:
            - strategy_type: The healing strategy to use (from list above)
            - strategy_name: A descriptive name for this healing approach
            - description: Clear explanation of what changes and why
            - confidence: Confidence level (0.0 to 1.0)
            - priority: Priority level (1-10, higher is better)
            - changes: Specific modifications to apply
            
            Example response:
            {
              "strategy_type": "AlternativeLocator",
              "strategy_name": "Use text content selector",
              "description": "The CSS selector '#login-button' failed. Try finding the button by its text content 'Sign In' instead.",
              "confidence": 0.85,
              "priority": 8,
              "changes": {
                "locator_type": "text",
                "locator_value": "Sign In",
                "reasoning": "Text-based selectors are more resilient to DOM structure changes"
              }
            }
            """;
    }

    /// <summary>
    /// Builds a diagnostic prompt for LLM analysis.
    /// </summary>
    private string BuildDiagnosticPrompt(
        AgentStep failedStep,
        Exception error,
        ErrorAnalysis errorAnalysis,
        PageState? pageState,
        AgentExecutionContext context)
    {
        var promptBuilder = new System.Text.StringBuilder();

        promptBuilder.AppendLine("## Failed Step Analysis");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"**Step Number**: {failedStep.StepNumber}");
        promptBuilder.AppendLine($"**Action**: {failedStep.Action?.Type}");
        promptBuilder.AppendLine($"**Reasoning**: {failedStep.Reasoning}");
        promptBuilder.AppendLine($"**Expected Outcome**: {failedStep.ExpectedOutcome}");
        promptBuilder.AppendLine();

        // Add action details
        if (failedStep.Action != null)
        {
            promptBuilder.AppendLine("**Action Details**:");
            if (failedStep.Action.Target != null)
            {
                promptBuilder.AppendLine($"  - Target: {failedStep.Action.Target.Selector}");
            }
            if (!string.IsNullOrEmpty(failedStep.Action.Value))
            {
                promptBuilder.AppendLine($"  - Value: {failedStep.Action.Value}");
            }
            promptBuilder.AppendLine($"  - Timeout: {failedStep.Action.TimeoutMs}ms");
            promptBuilder.AppendLine();
        }

        // Add error information
        promptBuilder.AppendLine("## Error Information");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"**Error Type**: {error.GetType().Name}");
        promptBuilder.AppendLine($"**Error Message**: {error.Message}");
        promptBuilder.AppendLine($"**Error Classification**: {errorAnalysis.Type}");
        promptBuilder.AppendLine($"**Root Cause**: {errorAnalysis.RootCause}");
        promptBuilder.AppendLine($"**Severity**: {errorAnalysis.Severity}");
        promptBuilder.AppendLine();

        // Add page state if available
        if (pageState != null)
        {
            promptBuilder.AppendLine("## Current Page State");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"**URL**: {pageState.Url}");
            promptBuilder.AppendLine($"**Title**: {pageState.Title}");
            promptBuilder.AppendLine();

            // Add interactive elements context
            if (pageState.InteractiveElements.Count > 0)
            {
                promptBuilder.AppendLine("**Available Interactive Elements** (first 10):");
                foreach (var element in pageState.InteractiveElements.Take(10))
                {
                    var visibility = element.IsVisible ? "visible" : "hidden";
                    var enablement = element.IsInteractable ? "enabled" : "disabled";
                    promptBuilder.AppendLine($"  - {element.TagName}: \"{element.Text}\" ({visibility}, {enablement})");
                    promptBuilder.AppendLine($"    Selector: {element.Selector}");
                }
                promptBuilder.AppendLine();
            }
        }

        // Add previous attempts context
        if (context.PreviousSteps.Count > 0)
        {
            promptBuilder.AppendLine("## Previous Steps");
            promptBuilder.AppendLine();
            foreach (var prevStep in context.PreviousSteps.TakeLast(3))
            {
                var status = prevStep.Success ? "?" : "?";
                promptBuilder.AppendLine($"{status} Step {prevStep.StepId}: {(prevStep.Success ? "Success" : "Failed")}");
            }
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("## Task");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Analyze the failure and suggest a specific healing strategy to fix this step.");
        promptBuilder.AppendLine("Focus on the most likely cause and provide a concrete, actionable solution.");
        promptBuilder.AppendLine("Return your response as a JSON object with the structure described in the system prompt.");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Parses LLM response into a healing strategy.
    /// </summary>
    private HealingStrategy ParseLLMResponseToStrategy(string responseContent)
    {
        try
        {
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            var strategyTypeStr = root.TryGetProperty("strategy_type", out var typeProp)
                ? typeProp.GetString() ?? "SimpleFallback"
                : "SimpleFallback";

            var strategyType = Enum.TryParse<HealingStrategyType>(strategyTypeStr, ignoreCase: true, out var parsed)
                ? parsed
                : HealingStrategyType.SimpleFallback;

            var strategy = new HealingStrategy
            {
                Type = strategyType,
                Name = root.TryGetProperty("strategy_name", out var nameProp)
                    ? nameProp.GetString() ?? "Unknown Strategy"
                    : "Unknown Strategy",
                Description = root.TryGetProperty("description", out var descProp)
                    ? descProp.GetString() ?? ""
                    : "",
                Confidence = root.TryGetProperty("confidence", out var confProp)
                    ? confProp.GetDouble()
                    : 0.5,
                Priority = root.TryGetProperty("priority", out var prioProp)
                    ? prioProp.GetInt32()
                    : 5
            };

            // Parse changes if present
            if (root.TryGetProperty("changes", out var changesProp))
            {
                var changesJson = changesProp.GetRawText();
                var jsonElements = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(changesJson)
                    ?? new Dictionary<string, JsonElement>();
                
                // Convert JsonElements to objects for storage
                strategy.Parameters = new Dictionary<string, object>();
                foreach (var kvp in jsonElements)
                {
                    strategy.Parameters[kvp.Key] = kvp.Value;
                }
            }

            return strategy;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse healing strategy from LLM response");
            
            // Return fallback strategy
            return new HealingStrategy
            {
                Type = HealingStrategyType.SimpleFallback,
                Name = "Fallback Strategy",
                Description = "Failed to parse LLM response, using simple fallback",
                Confidence = 0.3,
                Priority = 1
            };
        }
    }

    /// <summary>
    /// Applies a healing strategy to create a healed step.
    /// </summary>
    private AgentStep ApplyHealingStrategy(
        AgentStep failedStep,
        HealingStrategy strategy,
        ErrorAnalysis errorAnalysis)
    {
        var healedStep = new AgentStep
        {
            Id = Guid.NewGuid().ToString(),
            StepNumber = failedStep.StepNumber,
            Action = failedStep.Action != null ? CopyBrowserAction(failedStep.Action) : null,
            Reasoning = $"{failedStep.Reasoning} [Healed: {strategy.Name}]",
            ExpectedOutcome = failedStep.ExpectedOutcome,
            Dependencies = new List<string>(failedStep.Dependencies),
            TimeoutMs = failedStep.TimeoutMs,
            IsOptional = failedStep.IsOptional,
            RetryConfig = failedStep.RetryConfig,
            ValidationRules = new List<ValidationRule>(failedStep.ValidationRules)
        };

        // Apply strategy-specific modifications
        switch (strategy.Type)
        {
            case HealingStrategyType.AlternativeLocator:
                ApplyAlternativeLocator(healedStep, strategy);
                break;

            case HealingStrategyType.ExtendedWait:
                ApplyExtendedWait(healedStep, strategy);
                break;

            case HealingStrategyType.RetryWithDelay:
                ApplyRetryWithDelay(healedStep, strategy);
                break;

            case HealingStrategyType.ScrollToElement:
                // Keep original step but add scroll action metadata
                if (healedStep.Action != null)
                {
                    healedStep.Action.Options["scroll_before_action"] = true;
                }
                break;

            case HealingStrategyType.InteractionMethodChange:
                // Change to JavaScript click if possible
                if (healedStep.Action?.Type == ActionType.Click)
                {
                    healedStep.Action.Options["use_javascript"] = true;
                }
                break;

            case HealingStrategyType.SimpleFallback:
            default:
                // Use default modifications
                healedStep.TimeoutMs = Math.Min(healedStep.TimeoutMs * 2, 60000);
                break;
        }

        // Mark as healed
        healedStep.Metadata["healing_applied"] = true;
        healedStep.Metadata["healing_strategy"] = strategy.Name;
        healedStep.Metadata["healing_confidence"] = strategy.Confidence;
        healedStep.Metadata["original_step_id"] = failedStep.Id;

        return healedStep;
    }

    /// <summary>
    /// Applies alternative locator healing.
    /// </summary>
    private void ApplyAlternativeLocator(AgentStep healedStep, HealingStrategy strategy)
    {
        if (healedStep.Action == null) return;

        if (strategy.Parameters.TryGetValue("locator_type", out var locatorTypeObj) &&
            strategy.Parameters.TryGetValue("locator_value", out var locatorValueObj))
        {
            var locatorType = locatorTypeObj is JsonElement jeType 
                ? jeType.GetString() ?? "css"
                : locatorTypeObj?.ToString() ?? "css";
            var locatorValue = locatorValueObj is JsonElement jeValue
                ? jeValue.GetString() ?? ""
                : locatorValueObj?.ToString() ?? "";

            healedStep.Action.Target = locatorType.ToLowerInvariant() switch
            {
                "text" => ElementLocator.Text(locatorValue),
                "xpath" => ElementLocator.XPath(locatorValue),
                "role" => ElementLocator.Role(locatorValue),
                "testid" => ElementLocator.TestId(locatorValue),
                _ => ElementLocator.Css(locatorValue)
            };

            _logger.LogDebug(
                "Applied alternative locator: {LocatorType}={LocatorValue}",
                locatorType,
                locatorValue);
        }
    }

    /// <summary>
    /// Applies extended wait healing.
    /// </summary>
    private void ApplyExtendedWait(AgentStep healedStep, HealingStrategy strategy)
    {
        // Increase timeout
        var multiplier = strategy.Parameters.TryGetValue("timeout_multiplier", out var mult)
            ? mult is JsonElement je ? je.GetDouble() : Convert.ToDouble(mult)
            : 2.0;

        healedStep.TimeoutMs = (int)Math.Min(healedStep.TimeoutMs * multiplier, 60000);

        _logger.LogDebug(
            "Applied extended wait: {NewTimeout}ms",
            healedStep.TimeoutMs);
    }

    /// <summary>
    /// Applies retry with delay healing.
    /// </summary>
    private void ApplyRetryWithDelay(AgentStep healedStep, HealingStrategy strategy)
    {
        if (healedStep.RetryConfig == null)
        {
            healedStep.RetryConfig = new RetryConfiguration();
        }

        var delayMs = strategy.Parameters.TryGetValue("retry_delay_ms", out var delay)
            ? delay is JsonElement je ? je.GetInt32() : Convert.ToInt32(delay)
            : 2000;

        healedStep.RetryConfig.RetryDelayMs = delayMs;
        healedStep.RetryConfig.MaxRetries = Math.Max(healedStep.RetryConfig.MaxRetries, 2);

        _logger.LogDebug(
            "Applied retry with delay: {RetryDelay}ms, {MaxRetries} retries",
            delayMs,
            healedStep.RetryConfig.MaxRetries);
    }

    /// <summary>
    /// Creates a copy of a BrowserAction.
    /// </summary>
    private BrowserAction CopyBrowserAction(BrowserAction action)
    {
        return new BrowserAction
        {
            Type = action.Type,
            Target = action.Target,
            Value = action.Value,
            Options = new Dictionary<string, object>(action.Options),
            TimeoutMs = action.TimeoutMs,
            WaitForNavigation = action.WaitForNavigation,
            Description = action.Description
        };
    }

    /// <summary>
    /// Classifies an error into a known error type.
    /// </summary>
    private ErrorType ClassifyError(Exception error)
    {
        var errorMessage = error.Message ?? "";

        if (errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("could not find", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.ElementNotFound;
        }

        if (error is TimeoutException ||
            errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.Timeout;
        }

        if (errorMessage.Contains("not interactable", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("not visible", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("not attached", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.ElementNotInteractable;
        }

        if (errorMessage.Contains("navigation", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("navigate", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.NavigationFailure;
        }

        if (errorMessage.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("script", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.JavaScriptError;
        }

        if (errorMessage.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.NetworkError;
        }

        if (errorMessage.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.AuthenticationRequired;
        }

        if (errorMessage.Contains("changed", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("stale", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorType.PageStructureChanged;
        }

        return ErrorType.Unknown;
    }

    /// <summary>
    /// Determines if an error type is healable.
    /// </summary>
    private bool IsErrorHealable(ErrorType errorType, Exception error)
    {
        return errorType switch
        {
            ErrorType.ElementNotFound => true,
            ErrorType.Timeout => true,
            ErrorType.ElementNotInteractable => true,
            ErrorType.PageStructureChanged => true,
            ErrorType.JavaScriptError => false, // Usually not healable
            ErrorType.NavigationFailure => false, // Usually not healable
            ErrorType.NetworkError => false, // Usually not healable
            ErrorType.AuthenticationRequired => false, // Requires credentials
            ErrorType.Unknown => false,
            _ => false
        };
    }

    /// <summary>
    /// Determines the root cause of an error.
    /// </summary>
    private string DetermineRootCause(Exception error, ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.ElementNotFound => "Element could not be found with the specified selector",
            ErrorType.Timeout => "Operation timed out waiting for element or condition",
            ErrorType.ElementNotInteractable => "Element exists but is not visible or interactable",
            ErrorType.NavigationFailure => "Navigation to URL failed",
            ErrorType.JavaScriptError => "JavaScript execution error",
            ErrorType.NetworkError => "Network connection error",
            ErrorType.AuthenticationRequired => "Authentication or authorization required",
            ErrorType.PageStructureChanged => "Page structure changed, selectors may be stale",
            _ => $"{error.GetType().Name}: {error.Message}"
        };
    }

    /// <summary>
    /// Determines error severity.
    /// </summary>
    private ErrorSeverity DetermineSeverity(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.ElementNotFound => ErrorSeverity.Medium,
            ErrorType.Timeout => ErrorSeverity.Medium,
            ErrorType.ElementNotInteractable => ErrorSeverity.Low,
            ErrorType.NavigationFailure => ErrorSeverity.High,
            ErrorType.JavaScriptError => ErrorSeverity.Medium,
            ErrorType.NetworkError => ErrorSeverity.High,
            ErrorType.AuthenticationRequired => ErrorSeverity.Critical,
            ErrorType.PageStructureChanged => ErrorSeverity.High,
            _ => ErrorSeverity.Medium
        };
    }

    /// <summary>
    /// Gets suggested healing strategies for an error type.
    /// </summary>
    private List<HealingStrategy> GetSuggestedStrategies(ErrorType errorType)
    {
        var strategies = new List<HealingStrategy>();

        switch (errorType)
        {
            case ErrorType.ElementNotFound:
                strategies.Add(new HealingStrategy
                {
                    Type = HealingStrategyType.AlternativeLocator,
                    Name = "Try Alternative Selector",
                    Description = "Use a different selector strategy (XPath, text content, role)",
                    Confidence = 0.8,
                    Priority = 9,
                    ApplicableErrorTypes = new List<string> { ErrorType.ElementNotFound.ToString() },
                    EstimatedTimeMs = 2000
                });
                strategies.Add(new HealingStrategy
                {
                    Type = HealingStrategyType.ExtendedWait,
                    Name = "Wait Longer",
                    Description = "Increase wait time for dynamic content",
                    Confidence = 0.7,
                    Priority = 7,
                    ApplicableErrorTypes = new List<string> { ErrorType.ElementNotFound.ToString() },
                    EstimatedTimeMs = 5000
                });
                break;

            case ErrorType.Timeout:
                strategies.Add(new HealingStrategy
                {
                    Type = HealingStrategyType.ExtendedWait,
                    Name = "Increase Timeout",
                    Description = "Double the timeout duration",
                    Confidence = 0.75,
                    Priority = 8,
                    ApplicableErrorTypes = new List<string> { ErrorType.Timeout.ToString() },
                    EstimatedTimeMs = 0
                });
                break;

            case ErrorType.ElementNotInteractable:
                strategies.Add(new HealingStrategy
                {
                    Type = HealingStrategyType.ScrollToElement,
                    Name = "Scroll to Element",
                    Description = "Scroll element into view before interaction",
                    Confidence = 0.85,
                    Priority = 9,
                    ApplicableErrorTypes = new List<string> { ErrorType.ElementNotInteractable.ToString() },
                    EstimatedTimeMs = 1000
                });
                strategies.Add(new HealingStrategy
                {
                    Type = HealingStrategyType.InteractionMethodChange,
                    Name = "Use JavaScript Click",
                    Description = "Try JavaScript click instead of native click",
                    Confidence = 0.75,
                    Priority = 7,
                    ApplicableErrorTypes = new List<string> { ErrorType.ElementNotInteractable.ToString() },
                    EstimatedTimeMs = 500
                });
                break;

            case ErrorType.PageStructureChanged:
                strategies.Add(new HealingStrategy
                {
                    Type = HealingStrategyType.PageRefresh,
                    Name = "Refresh Page",
                    Description = "Reload the page and retry",
                    Confidence = 0.6,
                    Priority = 5,
                    ApplicableErrorTypes = new List<string> { ErrorType.PageStructureChanged.ToString() },
                    EstimatedTimeMs = 3000
                });
                break;

            default:
                strategies.Add(new HealingStrategy
                {
                    Type = HealingStrategyType.RetryWithDelay,
                    Name = "Retry with Delay",
                    Description = "Wait and retry the same action",
                    Confidence = 0.5,
                    Priority = 4,
                    ApplicableErrorTypes = new List<string>(),
                    EstimatedTimeMs = 2000
                });
                break;
        }

        return strategies;
    }
}
