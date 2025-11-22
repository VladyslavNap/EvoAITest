# HealerAgent Implementation - Day 11 Complete

## Overview
`HealerAgent` is the default `IHealer` implementation that receives failed `AgentStepResult` objects from the executor stage, diagnoses the root cause with the configured LLM, inspects live browser state, and applies adaptive strategies (alternative locators, extended waits, retries, replanning suggestions) to keep automation runs progressing without manual intervention.

## Responsibilities
- Classify executor failures (element not found, timeouts, navigation/auth errors, page structure changes, etc.) and assign severity.
- Build rich LLM prompts that include step metadata, recent execution history, exception details, and optional page state snapshots.
- Apply one or more healing strategies, mutate the failed `AgentStep`, and return a `HealingResult` with explanations and confidence scores.
- Expose `SuggestAlternativesAsync` to surface additional strategies whenever repeated attempts fail.
- Track per-step healing attempts (default max: 3) to avoid infinite loops and provide operators with actionable telemetry.

## Architecture
### Dependencies
```
HealerAgent
  ├─ ILLMProvider (Azure OpenAI GPT-5 / Ollama)  // Diagnostic reasoning + strategy planning
  ├─ IBrowserAgent                               // Page state, DOM snapshots, screenshots
  └─ ILogger<HealerAgent>                        // Structured healing telemetry
```

### Healing Flow
1. **Failure Intake** – `HealStepAsync` receives the failed `AgentStep`, exception, execution context, and correlation id.
2. **Error Analysis** – `AnalyzeErrorAsync` (LLM + heuristics) classifies the error, sets severity, and determines if healing is possible.
3. **Context Gathering** – Attempts to capture page state via `IBrowserAgent` (title, URL, DOM summary, screenshot hints).
4. **Prompt Construction** – Builds a compact JSON instruction set that includes failure metadata, last known selectors, and validation results.
5. **LLM Response Parsing** – Expects structured JSON describing root cause, recommended strategy, selector adjustments, wait hints, and optional replanning instructions.
6. **Strategy Application** – Converts the LLM recommendation into a `HealingStrategy`, clones the original `AgentStep`, applies the change, and returns a `HealingResult`.
7. **Attempt Tracking** – `_healingAttempts` dictionary prevents more than three consecutive attempts per step and surfaces warnings when the cap is reached.

## Key Features
- **LLM Diagnostics** – Uses GPT-5 (or configured Ollama model) to reason about DOM changes, race conditions, and flaky selectors.
- **Context-Aware Healing** – Enriches prompts with current page state, previous retries, and validation details for higher-quality suggestions.
- **Strategy Catalog** – Supports alternative locators, extended waits, scroll + retry, refresh + retry, manual inspection hints, and replanning guidance.
- **Safety Guardrails** – Attempt limits, JSON validation, cancellation-token propagation, and structured logging keep self-healing deterministic.
- **Alternatives API** – `SuggestAlternativesAsync` produces additional strategies that operators or higher-level agents can review.

## Usage
### Registration
`HealerAgent` is wired into DI via `builder.Services.AddAgentServices();`, alongside PlannerAgent and ExecutorAgent. You can override it with `builder.Services.AddHealer<MyCustomHealer>();` if needed.

### Basic Healing Loop
```csharp
var healer = serviceProvider.GetRequiredService<IHealer>();

if (!stepResult.Success && stepResult.Error is not null)
{
    var analysis = await healer.AnalyzeErrorAsync(stepResult.Error, context, cancellationToken);

    if (analysis.IsHealable)
    {
        var healingResult = await healer.HealStepAsync(step, stepResult.Error, context, cancellationToken);

        if (healingResult.Success && healingResult.HealedStep is not null)
        {
            await executor.ExecuteStepAsync(healingResult.HealedStep, context, cancellationToken);
        }
        else
        {
            logger.LogWarning("Healing failed: {Explanation}", healingResult.Explanation);
        }
    }
    else
    {
        logger.LogWarning("Error classified as non-healable: {RootCause}", analysis.RootCause);
    }
}
```

### Request Additional Alternatives
```csharp
var alternatives = await healer.SuggestAlternativesAsync(
    failedAttempts: context.PreviousSteps.Where(sr => !sr.Success).ToList(),
    cancellationToken);

foreach (var alt in alternatives)
{
    logger.LogInformation("Strategy {Name} ({Confidence:P0}): {Description}",
        alt.Name,
        alt.Confidence,
        alt.Description);
}
```

## Telemetry & Logging
- `LogInformation` entries capture each healing attempt with step number, action type, correlation id, and applied strategy.
- Warnings surface non-healable errors, maximum-attempt breaches, or issues capturing page state.
- All LLM responses are validated; malformed JSON results in structured warnings plus fallback error messages.

## Testing
- File: `EvoAITest.Tests/Agents/HealerAgentTests.cs` (25 unit tests).
- Coverage includes: happy-path healing, non-healable error handling, malformed LLM responses, cancellation propagation, attempt limit enforcement, alternative strategy suggestions, and exception-to-strategy mapping.

## Troubleshooting
| Issue | Symptoms | Fix |
|-------|----------|-----|
| `Maximum healing attempts exceeded` | Same step keeps failing even after healing | Inspect logs for each attempt; consider replanning or operator intervention. |
| Malformed LLM response | JSON parse errors logged; healing aborted | Review prompt/response, lower temperature, ensure `response_format=json_object` is configured. |
| Page state capture failures | Warning about `GetPageStateAsync` | Ensure browser session is still valid; fall back to manual screenshots if needed. |
| Non-healable classification | `Error is not healable` warning | Use `SuggestAlternativesAsync` for manual review or trigger a PlannerAgent replan. |

## Related Docs
- [PlannerAgent_README](PlannerAgent_README.md)
- [ExecutorAgent_README](ExecutorAgent_README.md)
- [Agent Implementation Summary](../IMPLEMENTATION_SUMMARY.md)
