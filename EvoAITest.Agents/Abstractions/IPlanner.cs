using EvoAITest.Agents.Models;

namespace EvoAITest.Agents.Abstractions;

/// <summary>
/// Defines a contract for planning agent tasks by breaking them into executable steps.
/// </summary>
public interface IPlanner
{
    /// <summary>
    /// Creates an execution plan for a task.
    /// </summary>
    /// <param name="task">The task to plan.</param>
    /// <param name="context">Current execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An execution plan.</returns>
    Task<ExecutionPlan> CreatePlanAsync(AgentTask task, ExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines an existing plan based on execution results.
    /// </summary>
    /// <param name="plan">The plan to refine.</param>
    /// <param name="executionResults">Results from previous steps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A refined execution plan.</returns>
    Task<ExecutionPlan> RefinePlanAsync(ExecutionPlan plan, IReadOnlyList<AgentStepResult> executionResults, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a plan is feasible.
    /// </summary>
    /// <param name="plan">The plan to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<PlanValidation> ValidatePlanAsync(ExecutionPlan plan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an execution plan for a task.
/// </summary>
public sealed class ExecutionPlan
{
    /// <summary>
    /// Gets or sets the plan ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the task ID this plan is for.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the planned steps.
    /// </summary>
    public List<AgentStep> Steps { get; set; } = new();

    /// <summary>
    /// Gets or sets the estimated duration in milliseconds.
    /// </summary>
    public long EstimatedDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the confidence level (0-1) in this plan.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets alternative plans to try if this one fails.
    /// </summary>
    public List<ExecutionPlan> Alternatives { get; set; } = new();

    /// <summary>
    /// Gets or sets metadata about the plan.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the plan was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents validation result for an execution plan.
/// </summary>
public sealed class PlanValidation
{
    /// <summary>
    /// Gets or sets a value indicating whether the plan is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation errors found.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets suggestions for improving the plan.
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// Represents the context in which a task is being executed.
/// </summary>
public sealed class ExecutionContext
{
    /// <summary>
    /// Gets or sets the current browser state.
    /// </summary>
    public object? BrowserState { get; set; }

    /// <summary>
    /// Gets or sets previously executed steps.
    /// </summary>
    public List<AgentStepResult> PreviousSteps { get; set; } = new();

    /// <summary>
    /// Gets or sets available tools and their capabilities.
    /// </summary>
    public Dictionary<string, object> AvailableTools { get; set; } = new();

    /// <summary>
    /// Gets or sets environment variables and configuration.
    /// </summary>
    public Dictionary<string, object> Environment { get; set; } = new();

    /// <summary>
    /// Gets or sets the session ID for this execution.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
}
