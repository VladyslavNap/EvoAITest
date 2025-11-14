using EvoAITest.Core.Models;

namespace EvoAITest.Agents.Models;

/// <summary>
/// Represents a single step in an agent's execution plan.
/// </summary>
public sealed class AgentStep
{
    /// <summary>
    /// Gets or sets the unique step identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the step number in the sequence.
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Gets or sets the browser action to perform.
    /// </summary>
    public BrowserAction? Action { get; set; }

    /// <summary>
    /// Gets or sets the reasoning behind this step.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Gets or sets the expected outcome of this step.
    /// </summary>
    public string? ExpectedOutcome { get; set; }

    /// <summary>
    /// Gets or sets dependencies on other steps.
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum execution time for this step in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets whether this step is optional.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Gets or sets retry configuration for this step.
    /// </summary>
    public RetryConfiguration? RetryConfig { get; set; }

    /// <summary>
    /// Gets or sets validation rules to check after execution.
    /// </summary>
    public List<ValidationRule> ValidationRules { get; set; } = new();

    /// <summary>
    /// Gets or sets metadata about the step.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the result of executing an agent step.
/// </summary>
public sealed class AgentStepResult
{
    /// <summary>
    /// Gets or sets the step ID.
    /// </summary>
    public string StepId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution result.
    /// </summary>
    public ExecutionResult? ExecutionResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the step succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error if the step failed.
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    /// Gets or sets data extracted by this step.
    /// </summary>
    public Dictionary<string, object> ExtractedData { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of retry attempts made.
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets whether healing was applied.
    /// </summary>
    public bool HealingApplied { get; set; }

    /// <summary>
    /// Gets or sets the healing strategy used (if any).
    /// </summary>
    public HealingStrategy? HealingStrategy { get; set; }

    /// <summary>
    /// Gets or sets validation results.
    /// </summary>
    public List<ValidationResult> ValidationResults { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when execution completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }
}

/// <summary>
/// Represents retry configuration for a step.
/// </summary>
public sealed class RetryConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retries in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets error types that should trigger a retry.
    /// </summary>
    public List<string> RetryableErrors { get; set; } = new();
}

/// <summary>
/// Represents a validation rule to check after step execution.
/// </summary>
public sealed class ValidationRule
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation type.
    /// </summary>
    public ValidationType Type { get; set; }

    /// <summary>
    /// Gets or sets the expected value or pattern.
    /// </summary>
    public object? ExpectedValue { get; set; }

    /// <summary>
    /// Gets or sets whether this validation is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Defines validation types.
/// </summary>
public enum ValidationType
{
    /// <summary>Validate URL matches pattern.</summary>
    UrlPattern,
    
    /// <summary>Validate element exists.</summary>
    ElementExists,
    
    /// <summary>Validate element contains text.</summary>
    ElementText,
    
    /// <summary>Validate page title.</summary>
    PageTitle,
    
    /// <summary>Validate data was extracted.</summary>
    DataExtracted,
    
    /// <summary>Custom validation logic.</summary>
    Custom
}

/// <summary>
/// Represents the result of a validation rule check.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether validation passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets or sets the actual value found.
    /// </summary>
    public object? ActualValue { get; set; }

    /// <summary>
    /// Gets or sets the expected value.
    /// </summary>
    public object? ExpectedValue { get; set; }

    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
