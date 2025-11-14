using EvoAITest.Agents.Models;

namespace EvoAITest.Agents.Abstractions;

/// <summary>
/// Defines the core contract for AI agents that execute browser automation tasks.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Gets the unique identifier of this agent.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of this agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the agent's capabilities and specializations.
    /// </summary>
    AgentCapabilities Capabilities { get; }

    /// <summary>
    /// Executes an automation task.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task execution result.</returns>
    Task<AgentTaskResult> ExecuteTaskAsync(AgentTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plans the steps required to complete a task.
    /// </summary>
    /// <param name="task">The task to plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of planned steps.</returns>
    Task<IReadOnlyList<AgentStep>> PlanTaskAsync(AgentTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adapts the agent's behavior based on feedback or errors.
    /// </summary>
    /// <param name="feedback">Feedback about previous execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the learning operation.</returns>
    Task LearnFromFeedbackAsync(AgentFeedback feedback, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines capabilities of an agent.
/// </summary>
public sealed class AgentCapabilities
{
    /// <summary>
    /// Gets or sets a value indicating whether the agent can handle form filling.
    /// </summary>
    public bool CanFillForms { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the agent can handle navigation.
    /// </summary>
    public bool CanNavigate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the agent can extract data.
    /// </summary>
    public bool CanExtractData { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the agent can handle authentication.
    /// </summary>
    public bool CanAuthenticate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the agent can self-heal from errors.
    /// </summary>
    public bool CanSelfHeal { get; set; }

    /// <summary>
    /// Gets or sets the supported browsers.
    /// </summary>
    public List<string> SupportedBrowsers { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum complexity of tasks this agent can handle.
    /// </summary>
    public TaskComplexity MaxComplexity { get; set; } = TaskComplexity.Medium;
}

/// <summary>
/// Defines task complexity levels.
/// </summary>
public enum TaskComplexity
{
    /// <summary>Simple single-step tasks.</summary>
    Simple,
    
    /// <summary>Multi-step tasks with basic logic.</summary>
    Medium,
    
    /// <summary>Complex tasks with branching and conditions.</summary>
    Complex,
    
    /// <summary>Advanced tasks requiring learning and adaptation.</summary>
    Advanced
}

/// <summary>
/// Represents feedback about an agent's execution.
/// </summary>
public sealed class AgentFeedback
{
    /// <summary>
    /// Gets or sets the task ID this feedback relates to.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error encountered (if any).
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets suggestions for improvement.
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Gets or sets metadata about the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when feedback was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
