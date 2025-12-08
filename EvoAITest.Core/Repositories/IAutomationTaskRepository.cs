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

    // ========== Visual Regression Methods ==========

    /// <summary>
    /// Retrieves a visual baseline for a specific checkpoint configuration.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="checkpointName">The checkpoint name.</param>
    /// <param name="environment">The environment (dev, staging, prod).</param>
    /// <param name="browser">The browser (chromium, firefox, webkit).</param>
    /// <param name="viewport">The viewport size (e.g., "1920x1080").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The most recent baseline for the specified configuration, or null if not found.</returns>
    Task<VisualBaseline?> GetBaselineAsync(
        Guid taskId,
        string checkpointName,
        string environment,
        string browser,
        string viewport,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a visual baseline to the database.
    /// </summary>
    /// <param name="baseline">The baseline to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved baseline with database-generated values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when baseline is null.</exception>
    Task<VisualBaseline> SaveBaselineAsync(VisualBaseline baseline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves comparison history for a specific checkpoint.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="checkpointName">The checkpoint name.</param>
    /// <param name="limit">Maximum number of results to return (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of comparison results ordered by date descending.</returns>
    Task<List<VisualComparisonResult>> GetComparisonHistoryAsync(
        Guid taskId,
        string checkpointName,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a visual comparison result to the database.
    /// </summary>
    /// <param name="result">The comparison result to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved comparison result with database-generated values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when result is null.</exception>
    Task<VisualComparisonResult> SaveComparisonResultAsync(
        VisualComparisonResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all baselines for a specific task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all baselines for the task.</returns>
    Task<List<VisualBaseline>> GetBaselinesByTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all baselines for a specific Git branch.
    /// </summary>
    /// <param name="gitBranch">The Git branch name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of baselines for the specified branch.</returns>
    Task<List<VisualBaseline>> GetBaselinesByBranchAsync(string gitBranch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed visual comparison results for a task.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="limit">Maximum number of results to return (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of failed comparison results.</returns>
    Task<List<VisualComparisonResult>> GetFailedComparisonsAsync(
        Guid taskId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old baselines older than the specified date.
    /// </summary>
    /// <param name="olderThan">Delete baselines created before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of baselines deleted.</returns>
    Task<int> DeleteOldBaselinesAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old baselines based on retention policy.
    /// </summary>
    Task<int> DeleteOldBaselinesAsync(
        Guid taskId,
        int retentionDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific comparison result by ID.
    /// </summary>
    Task<VisualComparisonResult?> GetComparisonResultAsync(
        Guid comparisonId,
        CancellationToken cancellationToken = default);
}
