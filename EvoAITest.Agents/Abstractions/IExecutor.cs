using EvoAITest.Agents.Models;

namespace EvoAITest.Agents.Abstractions;

/// <summary>
/// Defines a contract for executing agent steps and managing execution flow.
/// </summary>
public interface IExecutor
{
    /// <summary>
    /// Executes a single agent step.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <param name="context">Execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The step execution result.</returns>
    Task<AgentStepResult> ExecuteStepAsync(AgentStep step, ExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an entire execution plan.
    /// </summary>
    /// <param name="plan">The plan to execute.</param>
    /// <param name="context">Execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The task execution result.</returns>
    Task<AgentTaskResult> ExecutePlanAsync(ExecutionPlan plan, ExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses execution of the current task.
    /// </summary>
    /// <param name="taskId">The task ID to pause.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the pause operation.</returns>
    Task PauseExecutionAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes execution of a paused task.
    /// </summary>
    /// <param name="taskId">The task ID to resume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the resume operation.</returns>
    Task ResumeExecutionAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels execution of a task.
    /// </summary>
    /// <param name="taskId">The task ID to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the cancel operation.</returns>
    Task CancelExecutionAsync(string taskId, CancellationToken cancellationToken = default);
}
