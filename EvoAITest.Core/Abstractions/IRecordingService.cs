using EvoAITest.Core.Models.Recording;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for recording user interactions in the browser
/// </summary>
public interface IRecordingService
{
    /// <summary>
    /// Starts a new recording session
    /// </summary>
    /// <param name="name">Name of the recording session</param>
    /// <param name="startUrl">Starting URL for the recording</param>
    /// <param name="configuration">Configuration options for recording</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created recording session</returns>
    Task<RecordingSession> StartRecordingAsync(
        string name,
        string startUrl,
        RecordingConfiguration? configuration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the current recording session
    /// </summary>
    /// <param name="sessionId">ID of the session to stop</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The completed recording session</returns>
    Task<RecordingSession> StopRecordingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pauses the current recording session
    /// </summary>
    /// <param name="sessionId">ID of the session to pause</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PauseRecordingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resumes a paused recording session
    /// </summary>
    /// <param name="sessionId">ID of the session to resume</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResumeRecordingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records a user interaction
    /// </summary>
    /// <param name="sessionId">ID of the recording session</param>
    /// <param name="interaction">The user interaction to record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordInteractionAsync(
        Guid sessionId,
        UserInteraction interaction,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a recording session by ID
    /// </summary>
    /// <param name="sessionId">ID of the session to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The recording session</returns>
    Task<RecordingSession?> GetRecordingSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all recording sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all recording sessions</returns>
    Task<List<RecordingSession>> GetAllRecordingSessionsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a recording session
    /// </summary>
    /// <param name="sessionId">ID of the session to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRecordingSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
