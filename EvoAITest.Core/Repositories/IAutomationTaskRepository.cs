using EvoAITest.Core.Models;
using TaskStatus = EvoAITest.Core.Models.TaskStatus;

namespace EvoAITest.Core.Repositories;

/// <summary>
/// Repository interface for managing AutomationTask entities and their execution history.
/// Provides CRUD operations and specialized queries for task management.
/// </summary>
public interface IAutomationTaskRepository
{
    /// <summary>
    /// Retrieves an automation task by its unique identifier, including related execution history.
    /// </summary>
    /// <param name="id">The unique identifier of the task.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The automation task if found; otherwise, null.</returns>
    Task<AutomationTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all automation tasks for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of automation tasks for the specified user.</returns>
    Task<List<AutomationTask>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all automation tasks with a specific status.
    /// </summary>
    /// <param name="status">The task status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of automation tasks with the specified status.</returns>
    Task<List<AutomationTask>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all automation tasks for a specific user with a specific status.
    /// Uses the composite index (UserId, Status) for efficient querying.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="status">The task status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of automation tasks matching the criteria.</returns>
    Task<List<AutomationTask>> GetByUserIdAndStatusAsync(
        string userId, 
        TaskStatus status, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new automation task in the database.
    /// </summary>
    /// <param name="task">The automation task to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created automation task with database-generated values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when task is null.</exception>
    Task<AutomationTask> CreateAsync(AutomationTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing automation task in the database.
    /// Handles concurrency conflicts automatically.
    /// </summary>
    /// <param name="task">The automation task to update.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when task is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when task is not found or concurrency conflict occurs.</exception>
    Task UpdateAsync(AutomationTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an automation task and all related execution history (cascade delete).
    /// </summary>
    /// <param name="id">The unique identifier of the task to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when task is not found.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all execution history records for a specific task.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of execution history records ordered by StartedAt descending (most recent first).</returns>
    Task<List<ExecutionHistory>> GetExecutionHistoryAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new execution history record for a task.
    /// </summary>
    /// <param name="history">The execution history record to add.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created execution history record with database-generated values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when history is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the referenced task does not exist.</exception>
    Task<ExecutionHistory> AddExecutionHistoryAsync(ExecutionHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an automation task exists.
    /// </summary>
    /// <param name="id">The unique identifier of the task.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the task exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of tasks for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The total number of tasks for the user.</returns>
    Task<int> GetTaskCountByUserAsync(string userId, CancellationToken cancellationToken = default);
}
