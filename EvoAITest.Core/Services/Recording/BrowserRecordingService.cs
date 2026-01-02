using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Recording;
using EvoAITest.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace EvoAITest.Core.Services.Recording;

/// <summary>
/// Service for recording user interactions in the browser
/// </summary>
public sealed class BrowserRecordingService : IRecordingService
{
    private readonly IBrowserAgent _browserAgent;
    private readonly ILogger<BrowserRecordingService> _logger;
    private readonly RecordingOptions _options;
    private readonly ConcurrentDictionary<Guid, RecordingSession> _activeSessions = new();
    private readonly ConcurrentDictionary<Guid, RecordingEventCapture> _eventCaptures = new();

    public BrowserRecordingService(
        IBrowserAgent browserAgent,
        ILogger<BrowserRecordingService> logger,
        IOptions<RecordingOptions> options)
    {
        _browserAgent = browserAgent;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<RecordingSession> StartRecordingAsync(
        string name,
        string startUrl,
        RecordingConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting recording session: {Name} at {Url}", name, startUrl);

        var config = configuration ?? new RecordingConfiguration
        {
            CaptureScreenshots = _options.CaptureScreenshots,
            RecordNetwork = _options.RecordNetworkTraffic,
            RecordConsoleLogs = _options.RecordConsoleLogs,
            ActionDebounceMs = _options.ActionDebounceMs,
            AutoGenerateAssertions = _options.AutoGenerateAssertions,
            UseAiIntentDetection = _options.UseAiAnalysis
        };

        var session = new RecordingSession
        {
            Name = name,
            StartUrl = startUrl,
            Configuration = config,
            Status = RecordingStatus.Recording
        };

        _activeSessions[session.Id] = session;

        // Initialize browser agent if needed
        await _browserAgent.InitializeAsync(cancellationToken);

        // Inject recording scripts
        var eventCapture = new RecordingEventCapture(session.Id, config);
        _eventCaptures[session.Id] = eventCapture;

        await InjectRecordingScriptsAsync(session.Id, cancellationToken);

        _logger.LogInformation("Recording session started: {SessionId}", session.Id);
        return session;
    }

    public async Task<RecordingSession> StopRecordingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping recording session: {SessionId}", sessionId);

        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Recording session {sessionId} not found");
        }

        session.Status = RecordingStatus.Stopped;
        session.EndedAt = DateTimeOffset.UtcNow;

        // Update metrics
        session.Metrics.TotalActions = session.Interactions.Count;
        session.Metrics.PagesVisited = session.Interactions
            .Select(i => i.Context.Url)
            .Distinct()
            .Count();
        session.Metrics.FormSubmissions = session.Interactions
            .Count(i => i.ActionType == ActionType.Submit);
        session.Metrics.AssertionsGenerated = session.Interactions
            .SelectMany(i => i.Assertions)
            .Count();

        if (session.Interactions.Any())
        {
            session.Metrics.AverageIntentConfidence = session.Interactions
                .Average(i => i.IntentConfidence);
        }

        // Cleanup event capture
        if (_eventCaptures.TryRemove(sessionId, out var eventCapture))
        {
            await eventCapture.DisposeAsync();
        }

        _logger.LogInformation(
            "Recording session stopped: {SessionId}, Actions: {ActionCount}, Duration: {Duration}ms",
            sessionId,
            session.Metrics.TotalActions,
            session.DurationMs);

        return session;
    }

    public async Task PauseRecordingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pausing recording session: {SessionId}", sessionId);

        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Recording session {sessionId} not found");
        }

        session.Status = RecordingStatus.Paused;

        if (_eventCaptures.TryGetValue(sessionId, out var eventCapture))
        {
            eventCapture.IsPaused = true;
        }

        await Task.CompletedTask;
    }

    public async Task ResumeRecordingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming recording session: {SessionId}", sessionId);

        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Recording session {sessionId} not found");
        }

        session.Status = RecordingStatus.Recording;

        if (_eventCaptures.TryGetValue(sessionId, out var eventCapture))
        {
            eventCapture.IsPaused = false;
        }

        await Task.CompletedTask;
    }

    public async Task RecordInteractionAsync(
        Guid sessionId,
        UserInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Recording session {sessionId} not found");
        }

        if (session.Status != RecordingStatus.Recording)
        {
            _logger.LogDebug("Ignoring interaction for non-recording session: {SessionId}", sessionId);
            return;
        }

        // Check if action should be ignored
        if (_options.IgnoredActions.Contains(interaction.ActionType.ToString()))
        {
            return;
        }

        // Check max actions limit
        if (_options.MaxActionsPerSession > 0 &&
            session.Interactions.Count >= _options.MaxActionsPerSession)
        {
            _logger.LogWarning(
                "Maximum actions limit reached for session: {SessionId}",
                sessionId);
            await StopRecordingAsync(sessionId, cancellationToken);
            return;
        }

        session.Interactions.Add(interaction);

        _logger.LogDebug(
            "Recorded interaction: {ActionType} at {Url}",
            interaction.ActionType,
            interaction.Context.Url);

        await Task.CompletedTask;
    }

    public Task<RecordingSession?> GetRecordingSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _activeSessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<List<RecordingSession>> GetAllRecordingSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var sessions = _activeSessions.Values.ToList();
        return Task.FromResult(sessions);
    }

    public async Task DeleteRecordingSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (_activeSessions.TryRemove(sessionId, out var session))
        {
            if (session.Status == RecordingStatus.Recording)
            {
                await StopRecordingAsync(sessionId, cancellationToken);
            }

            _logger.LogInformation("Deleted recording session: {SessionId}", sessionId);
        }
    }

    private async Task InjectRecordingScriptsAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        // Note: In a real implementation, you would inject recording scripts into the browser page
        // using the browser agent's JavaScript evaluation capabilities
        await Task.CompletedTask;

        _logger.LogDebug("Recording scripts injected for session: {SessionId}", sessionId);
    }
}

/// <summary>
/// Internal class for managing event capture for a recording session
/// </summary>
internal sealed class RecordingEventCapture : IAsyncDisposable
{
    public Guid SessionId { get; }
    public RecordingConfiguration Configuration { get; }
    public bool IsPaused { get; set; }

    private readonly List<UserInteraction> _bufferedInteractions = new();
    private readonly DateTime _lastActionTime = DateTime.UtcNow;

    public RecordingEventCapture(Guid sessionId, RecordingConfiguration configuration)
    {
        SessionId = sessionId;
        Configuration = configuration;
    }

    public async ValueTask DisposeAsync()
    {
        _bufferedInteractions.Clear();
        await Task.CompletedTask;
    }
}
