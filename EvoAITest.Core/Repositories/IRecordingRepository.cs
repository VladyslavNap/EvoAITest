using EvoAITest.Core.Models.Recording;

namespace EvoAITest.Core.Repositories;

/// <summary>
/// Repository interface for recording session persistence
/// </summary>
public interface IRecordingRepository
{
    /// <summary>
    /// Creates a new recording session
    /// </summary>
    Task<RecordingSession> CreateSessionAsync(
        RecordingSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing recording session
    /// </summary>
    Task<RecordingSession> UpdateSessionAsync(
        RecordingSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a recording session by ID
    /// </summary>
    Task<RecordingSession?> GetSessionByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recording sessions
    /// </summary>
    Task<List<RecordingSession>> GetAllSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recording sessions by status
    /// </summary>
    Task<List<RecordingSession>> GetSessionsByStatusAsync(
        RecordingStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recording sessions by user
    /// </summary>
    Task<List<RecordingSession>> GetSessionsByUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent recording sessions
    /// </summary>
    Task<List<RecordingSession>> GetRecentSessionsAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a recording session
    /// </summary>
    Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an interaction to a session
    /// </summary>
    Task AddInteractionAsync(
        Guid sessionId,
        UserInteraction interaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an interaction
    /// </summary>
    Task UpdateInteractionAsync(
        UserInteraction interaction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets interactions for a session
    /// </summary>
    Task<List<UserInteraction>> GetSessionInteractionsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old recordings based on retention policy
    /// </summary>
    Task<int> DeleteOldRecordingsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);
}
