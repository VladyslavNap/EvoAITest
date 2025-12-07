using EvoAITest.Core.Models;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for comparing screenshots and managing visual regression baselines.
/// </summary>
public interface IVisualComparisonService
{
    /// <summary>
    /// Compares an actual screenshot against a baseline image.
    /// </summary>
    /// <param name="checkpoint">The visual checkpoint configuration.</param>
    /// <param name="actualImage">The actual screenshot captured during execution.</param>
    /// <param name="taskId">The task ID.</param>
    /// <param name="environment">The environment (dev, staging, prod).</param>
    /// <param name="browser">The browser used (chromium, firefox, webkit).</param>
    /// <param name="viewport">The viewport size (e.g., "1920x1080").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The comparison result with difference metrics and paths.</returns>
    Task<VisualComparisonResult> CompareAsync(
        VisualCheckpoint checkpoint,
        byte[] actualImage,
        Guid taskId,
        string environment,
        string browser,
        string viewport,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates or updates a baseline image.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="checkpointName">The checkpoint name.</param>
    /// <param name="image">The baseline image bytes.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="browser">The browser.</param>
    /// <param name="viewport">The viewport size.</param>
    /// <param name="approvedBy">Who approved this baseline.</param>
    /// <param name="updateReason">Reason for baseline update (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated baseline entity.</returns>
    Task<VisualBaseline> CreateBaselineAsync(
        Guid taskId,
        string checkpointName,
        byte[] image,
        string environment,
        string browser,
        string viewport,
        string approvedBy,
        string? updateReason = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current baseline for a checkpoint.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="checkpointName">The checkpoint name.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="browser">The browser.</param>
    /// <param name="viewport">The viewport size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The baseline entity if found; otherwise, null.</returns>
    Task<VisualBaseline?> GetBaselineAsync(
        Guid taskId,
        string checkpointName,
        string environment,
        string browser,
        string viewport,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Approves a new baseline from a failed comparison.
    /// </summary>
    /// <param name="comparisonId">The comparison result ID.</param>
    /// <param name="approvedBy">Who is approving the new baseline.</param>
    /// <param name="reason">Reason for approval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly approved baseline entity.</returns>
    Task<VisualBaseline> ApproveNewBaselineAsync(
        Guid comparisonId,
        string approvedBy,
        string reason,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets comparison history for a checkpoint.
    /// </summary>
    /// <param name="taskId">The task ID.</param>
    /// <param name="checkpointName">The checkpoint name.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of comparison results ordered by date descending.</returns>
    Task<List<VisualComparisonResult>> GetHistoryAsync(
        Guid taskId,
        string checkpointName,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
