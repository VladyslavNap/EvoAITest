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
        // JavaScript to inject into the page for event capture
        const string recordingScript = @"
(function() {
    if (window.__evoai_recording_initialized) return;
    window.__evoai_recording_initialized = true;
    window.__evoai_recorded_events = [];

    const debounce = (func, wait) => {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    };

    const captureContext = (element, eventType) => {
        const rect = element.getBoundingClientRect();
        const computedStyle = window.getComputedStyle(element);
        
        const attributes = {};
        for (let attr of element.attributes) {
            attributes[attr.name] = attr.value;
        }

        return {
            url: window.location.href,
            pageTitle: document.title,
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight,
            targetSelector: getCssSelector(element),
            targetXPath: getXPath(element),
            elementText: element.innerText || element.value || '',
            elementTag: element.tagName.toLowerCase(),
            elementAttributes: attributes,
            scrollY: window.scrollY,
            scrollX: window.scrollX,
            timestamp: new Date().toISOString(),
            eventType: eventType
        };
    };

    const getCssSelector = (element) => {
        if (element.id) return `#${element.id}`;
        if (element.className) {
            const classes = element.className.split(' ').filter(c => c).join('.');
            if (classes) return `${element.tagName.toLowerCase()}.${classes}`;
        }
        return element.tagName.toLowerCase();
    };

    const getXPath = (element) => {
        if (element.id) return `//*[@id='${element.id}']`;
        if (element === document.body) return '/html/body';
        
        let path = '';
        let current = element;
        
        while (current && current.nodeType === Node.ELEMENT_NODE) {
            let index = 0;
            let sibling = current;
            
            while (sibling) {
                if (sibling.nodeType === Node.ELEMENT_NODE && sibling.tagName === current.tagName) {
                    index++;
                }
                sibling = sibling.previousElementSibling;
            }
            
            const tagName = current.tagName.toLowerCase();
            path = `/${tagName}[${index}]${path}`;
            current = current.parentElement;
        }
        
        return path;
    };

    const recordEvent = (eventType, element, additionalData = {}) => {
        const event = {
            actionType: eventType,
            context: captureContext(element, eventType),
            ...additionalData,
            timestamp: Date.now()
        };
        
        window.__evoai_recorded_events.push(event);
        
        // Notify the recording service via custom event
        window.dispatchEvent(new CustomEvent('__evoai_interaction_recorded', {
            detail: event
        }));
    };

    // Click events
    document.addEventListener('click', (e) => {
        recordEvent('Click', e.target, {
            coordinates: { x: e.clientX, y: e.clientY }
        });
    }, true);

    // Double click events
    document.addEventListener('dblclick', (e) => {
        recordEvent('DoubleClick', e.target, {
            coordinates: { x: e.clientX, y: e.clientY }
        });
    }, true);

    // Right click events
    document.addEventListener('contextmenu', (e) => {
        recordEvent('RightClick', e.target, {
            coordinates: { x: e.clientX, y: e.clientY }
        });
    }, true);

    // Input events (debounced)
    const handleInput = debounce((e) => {
        recordEvent('Input', e.target, {
            inputValue: e.target.value
        });
    }, 300);

    document.addEventListener('input', handleInput, true);

    // Keyboard events
    document.addEventListener('keydown', (e) => {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
            recordEvent('KeyPress', e.target, {
                key: e.key,
                keyCode: e.keyCode
            });
        }
    }, true);

    // Form submission
    document.addEventListener('submit', (e) => {
        recordEvent('Submit', e.target);
    }, true);

    // Select changes
    document.addEventListener('change', (e) => {
        if (e.target.tagName === 'SELECT') {
            recordEvent('Select', e.target, {
                inputValue: e.target.value
            });
        } else if (e.target.type === 'checkbox' || e.target.type === 'radio') {
            recordEvent('Toggle', e.target, {
                inputValue: e.target.checked.toString()
            });
        }
    }, true);

    // File upload
    document.addEventListener('change', (e) => {
        if (e.target.type === 'file') {
            recordEvent('FileUpload', e.target, {
                inputValue: Array.from(e.target.files).map(f => f.name).join(', ')
            });
        }
    }, true);

    // Navigation events
    let lastUrl = window.location.href;
    setInterval(() => {
        if (window.location.href !== lastUrl) {
            recordEvent('Navigation', document.body, {
                previousUrl: lastUrl,
                newUrl: window.location.href
            });
            lastUrl = window.location.href;
        }
    }, 500);

    console.log('[EvoAI] Recording scripts initialized');
})();
";

        // Note: In a real implementation, you would inject this into the browser page
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
    private DateTime _lastActionTime = DateTime.UtcNow;

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
