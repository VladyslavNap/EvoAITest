using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskStatus = EvoAITest.Core.Models.TaskStatus;

namespace EvoAITest.Core.Repositories;

/// <summary>
/// Repository implementation for managing AutomationTask entities using Entity Framework Core.
/// Provides efficient data access with logging, error handling, and concurrency management.
/// </summary>
public sealed class AutomationTaskRepository : IAutomationTaskRepository
{
    private readonly EvoAIDbContext _context;
    private readonly ILogger<AutomationTaskRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutomationTaskRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when context or logger is null.</exception>
    public AutomationTaskRepository(
        EvoAIDbContext context,
        ILogger<AutomationTaskRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AutomationTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving task {TaskId}", id);

        try
        {
            var task = await _context.AutomationTasks
                .Include(t => t.Executions)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found", id);
            }
            else
            {
                _logger.LogDebug("Task {TaskId} retrieved successfully with {ExecutionCount} execution records",
                    id, task.Executions.Count);
            }

            return task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task {TaskId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<AutomationTask>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        _logger.LogInformation("Retrieving tasks for user {UserId}", userId);

        try
        {
            var tasks = await _context.AutomationTasks
                .Where(t => t.UserId == userId)
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {TaskCount} tasks for user {UserId}", tasks.Count, userId);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<AutomationTask>> GetByStatusAsync(TaskStatus status, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving tasks with status {Status}", status);

        try
        {
            var tasks = await _context.AutomationTasks
                .Where(t => t.Status == status)
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {TaskCount} tasks with status {Status}", tasks.Count, status);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks with status {Status}", status);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<AutomationTask>> GetByUserIdAndStatusAsync(
        string userId,
        TaskStatus status,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        _logger.LogInformation("Retrieving tasks for user {UserId} with status {Status}", userId, status);

        try
        {
            // Uses composite index (UserId, Status) for efficient querying
            var tasks = await _context.AutomationTasks
                .Where(t => t.UserId == userId && t.Status == status)
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {TaskCount} tasks for user {UserId} with status {Status}",
                tasks.Count, userId, status);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks for user {UserId} with status {Status}", userId, status);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AutomationTask> CreateAsync(AutomationTask task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task, nameof(task));

        _logger.LogInformation("Creating task {TaskName} for user {UserId}", task.Name, task.UserId);

        try
        {
            // Ensure timestamps are set
            if (task.CreatedAt == default)
            {
                task.CreatedAt = DateTimeOffset.UtcNow;
            }
            if (task.UpdatedAt == default)
            {
                task.UpdatedAt = DateTimeOffset.UtcNow;
            }

            _context.AutomationTasks.Add(task);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} created successfully", task.Id);

            return task;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating task {TaskName}", task.Name);
            throw new InvalidOperationException("Failed to create task. See inner exception for details.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task {TaskName}", task.Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(AutomationTask task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task, nameof(task));

        _logger.LogInformation("Updating task {TaskId}", task.Id);

        try
        {
            // Update timestamp
            task.UpdatedAt = DateTimeOffset.UtcNow;

            _context.AutomationTasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} updated successfully", task.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating task {TaskId}", task.Id);
            throw new InvalidOperationException(
                $"Concurrency conflict: Task {task.Id} was modified by another process. Please refresh and try again.",
                ex);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating task {TaskId}", task.Id);
            throw new InvalidOperationException("Failed to update task. See inner exception for details.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", task.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting task {TaskId}", id);

        try
        {
            var task = await _context.AutomationTasks.FindAsync(new object[] { id }, cancellationToken);

            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for deletion", id);
                throw new InvalidOperationException($"Task {id} not found.");
            }

            _context.AutomationTasks.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} and related execution history deleted successfully (cascade)", id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting task {TaskId}", id);
            throw new InvalidOperationException("Failed to delete task. See inner exception for details.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ExecutionHistory>> GetExecutionHistoryAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving execution history for task {TaskId}", taskId);

        try
        {
            var history = await _context.ExecutionHistory
                .Where(h => h.TaskId == taskId)
                .AsNoTracking()
                .OrderByDescending(h => h.StartedAt)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {HistoryCount} execution records for task {TaskId}",
                history.Count, taskId);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving execution history for task {TaskId}", taskId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ExecutionHistory> AddExecutionHistoryAsync(
        ExecutionHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history, nameof(history));

        _logger.LogInformation("Adding execution history for task {TaskId}", history.TaskId);

        try
        {
            // Verify the task exists
            var taskExists = await _context.AutomationTasks.AnyAsync(
                t => t.Id == history.TaskId,
                cancellationToken);

            if (!taskExists)
            {
                _logger.LogWarning("Task {TaskId} not found for execution history", history.TaskId);
                throw new InvalidOperationException($"Task {history.TaskId} not found.");
            }

            // Ensure timestamps are set
            if (history.StartedAt == default)
            {
                history.StartedAt = DateTimeOffset.UtcNow;
            }
            if (history.CreatedAt == default)
            {
                history.CreatedAt = DateTimeOffset.UtcNow;
            }

            _context.ExecutionHistory.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Execution history {HistoryId} added for task {TaskId}",
                history.Id, history.TaskId);

            return history;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error adding execution history for task {TaskId}", history.TaskId);
            throw new InvalidOperationException("Failed to add execution history. See inner exception for details.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error adding execution history for task {TaskId}", history.TaskId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if task {TaskId} exists", id);

        try
        {
            var exists = await _context.AutomationTasks
                .AsNoTracking()
                .AnyAsync(t => t.Id == id, cancellationToken);

            _logger.LogDebug("Task {TaskId} exists: {Exists}", id, exists);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if task {TaskId} exists", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetTaskCountByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        _logger.LogDebug("Counting tasks for user {UserId}", userId);

        try
        {
            var count = await _context.AutomationTasks
                .AsNoTracking()
                .CountAsync(t => t.UserId == userId, cancellationToken);

            _logger.LogDebug("User {UserId} has {TaskCount} tasks", userId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting tasks for user {UserId}", userId);
            throw;
        }
    }

    // ========== Visual Regression Methods ==========

    /// <inheritdoc/>
    public async Task<VisualBaseline?> GetBaselineAsync(
        Guid taskId,
        string checkpointName,
        string environment,
        string browser,
        string viewport,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointName);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(browser);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewport);

        _logger.LogInformation(
            "Retrieving baseline for task {TaskId}, checkpoint '{CheckpointName}', {Environment}/{Browser}/{Viewport}",
            taskId, checkpointName, environment, browser, viewport);

        try
        {
            var baseline = await _context.VisualBaselines
                .Where(b => b.TaskId == taskId
                         && b.CheckpointName == checkpointName
                         && b.Environment == environment
                         && b.Browser == browser
                         && b.Viewport == viewport)
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (baseline == null)
            {
                _logger.LogDebug("No baseline found for checkpoint '{CheckpointName}'", checkpointName);
            }
            else
            {
                _logger.LogDebug("Baseline {BaselineId} retrieved for checkpoint '{CheckpointName}'",
                    baseline.Id, checkpointName);
            }

            return baseline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving baseline for checkpoint '{CheckpointName}'", checkpointName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VisualBaseline> SaveBaselineAsync(
        VisualBaseline baseline,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        _logger.LogInformation("Saving baseline for checkpoint '{CheckpointName}'", baseline.CheckpointName);

        try
        {
            // Check if task exists
            var taskExists = await _context.AutomationTasks.AnyAsync(
                t => t.Id == baseline.TaskId,
                cancellationToken);

            if (!taskExists)
            {
                _logger.LogWarning("Task {TaskId} not found for baseline", baseline.TaskId);
                throw new InvalidOperationException($"Task {baseline.TaskId} not found.");
            }

            _context.VisualBaselines.Add(baseline);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Baseline {BaselineId} saved successfully", baseline.Id);

            return baseline;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error saving baseline for checkpoint '{CheckpointName}'",
                baseline.CheckpointName);
            throw new InvalidOperationException("Failed to save baseline. See inner exception for details.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error saving baseline for checkpoint '{CheckpointName}'",
                baseline.CheckpointName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<VisualComparisonResult>> GetComparisonHistoryAsync(
        Guid taskId,
        string checkpointName,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointName);

        _logger.LogInformation(
            "Retrieving comparison history for task {TaskId}, checkpoint '{CheckpointName}', limit {Limit}",
            taskId, checkpointName, limit);

        try
        {
            var history = await _context.VisualComparisonResults
                .Where(c => c.TaskId == taskId && c.CheckpointName == checkpointName)
                .AsNoTracking()
                .OrderByDescending(c => c.ComparedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} comparison results for checkpoint '{CheckpointName}'",
                history.Count, checkpointName);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comparison history for checkpoint '{CheckpointName}'",
                checkpointName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VisualComparisonResult> SaveComparisonResultAsync(
        VisualComparisonResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        _logger.LogInformation("Saving comparison result for checkpoint '{CheckpointName}'", result.CheckpointName);

        try
        {
            // Check if task exists
            var taskExists = await _context.AutomationTasks.AnyAsync(
                t => t.Id == result.TaskId,
                cancellationToken);

            if (!taskExists)
            {
                _logger.LogWarning("Task {TaskId} not found for comparison result", result.TaskId);
                throw new InvalidOperationException($"Task {result.TaskId} not found.");
            }

            _context.VisualComparisonResults.Add(result);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Comparison result {ResultId} saved (Passed: {Passed}, Diff: {Difference:P2})",
                result.Id, result.Passed, result.DifferencePercentage);

            return result;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error saving comparison result for checkpoint '{CheckpointName}'",
                result.CheckpointName);
            throw new InvalidOperationException("Failed to save comparison result. See inner exception for details.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error saving comparison result for checkpoint '{CheckpointName}'",
                result.CheckpointName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<VisualBaseline>> GetBaselinesByTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all baselines for task {TaskId}", taskId);

        try
        {
            var baselines = await _context.VisualBaselines
                .Where(b => b.TaskId == taskId)
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} baselines for task {TaskId}", baselines.Count, taskId);

            return baselines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving baselines for task {TaskId}", taskId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<VisualBaseline>> GetBaselinesByBranchAsync(
        string gitBranch,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gitBranch);

        _logger.LogInformation("Retrieving baselines for Git branch '{GitBranch}'", gitBranch);

        try
        {
            var baselines = await _context.VisualBaselines
                .Where(b => b.GitBranch == gitBranch)
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} baselines for branch '{GitBranch}'", baselines.Count, gitBranch);

            return baselines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving baselines for branch '{GitBranch}'", gitBranch);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<VisualComparisonResult>> GetFailedComparisonsAsync(
        Guid taskId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving failed comparisons for task {TaskId}, limit {Limit}", taskId, limit);

        try
        {
            var failed = await _context.VisualComparisonResults
                .Where(c => c.TaskId == taskId && !c.Passed)
                .AsNoTracking()
                .OrderByDescending(c => c.ComparedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} failed comparisons for task {TaskId}", failed.Count, taskId);

            return failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed comparisons for task {TaskId}", taskId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOldBaselinesAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting baselines older than {OlderThan}", olderThan);

        try
        {
            var oldBaselines = await _context.VisualBaselines
                .Where(b => b.CreatedAt < olderThan)
                .ToListAsync(cancellationToken);

            if (oldBaselines.Count == 0)
            {
                _logger.LogInformation("No baselines found older than {OlderThan}", olderThan);
                return 0;
            }

            _context.VisualBaselines.RemoveRange(oldBaselines);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} baselines older than {OlderThan}",
                oldBaselines.Count, olderThan);

            return oldBaselines.Count;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting old baselines");
            throw new InvalidOperationException("Failed to delete old baselines. See inner exception for details.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error deleting old baselines");
            throw;
        }
    }
}
