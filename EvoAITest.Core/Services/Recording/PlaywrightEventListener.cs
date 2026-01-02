using EvoAITest.Core.Models.Recording;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services.Recording;

/// <summary>
/// Manages event listeners for browser interaction recording using Playwright
/// </summary>
public sealed class PlaywrightEventListener : IAsyncDisposable
{
    private readonly ILogger<PlaywrightEventListener> _logger;
    private readonly Guid _sessionId;
    private readonly RecordingConfiguration _configuration;
    private readonly Func<UserInteraction, Task> _onInteractionRecorded;
    
    private int _sequenceNumber;
    private DateTimeOffset _lastActionTime = DateTimeOffset.UtcNow;
    private readonly Dictionary<string, int> _actionCountByType = new();

    public PlaywrightEventListener(
        Guid sessionId,
        RecordingConfiguration configuration,
        Func<UserInteraction, Task> onInteractionRecorded,
        ILogger<PlaywrightEventListener> logger)
    {
        _sessionId = sessionId;
        _configuration = configuration;
        _onInteractionRecorded = onInteractionRecorded;
        _logger = logger;
    }

    /// <summary>
    /// Attaches event listeners to a Playwright page
    /// </summary>
    public async Task AttachToPageAsync(object page)
    {
        // Note: This would integrate with the actual Playwright page object
        // For now, this demonstrates the structure
        
        _logger.LogInformation("Attaching event listeners for session: {SessionId}", _sessionId);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates an interaction from captured event data
    /// </summary>
    public async Task<UserInteraction> CreateInteractionAsync(
        ActionType actionType,
        ActionContext context,
        string? inputValue = null,
        string? key = null,
        (int X, int Y)? coordinates = null)
    {
        // Apply debounce logic
        var now = DateTimeOffset.UtcNow;
        var timeSinceLastAction = (now - _lastActionTime).TotalMilliseconds;
        
        if (timeSinceLastAction < _configuration.ActionDebounceMs)
        {
            _logger.LogTrace("Action debounced: {ActionType}", actionType);
            // Return a placeholder that won't be recorded
            return null!;
        }

        _lastActionTime = now;
        _sequenceNumber++;

        // Track action counts
        var actionTypeKey = actionType.ToString();
        _actionCountByType.TryGetValue(actionTypeKey, out var count);
        _actionCountByType[actionTypeKey] = count + 1;

        var interaction = new UserInteraction
        {
            SessionId = _sessionId,
            SequenceNumber = _sequenceNumber,
            ActionType = actionType,
            Context = context,
            InputValue = inputValue,
            Key = key,
            Coordinates = coordinates,
            Timestamp = now
        };

        await _onInteractionRecorded(interaction);

        return interaction;
    }

    /// <summary>
    /// Handles click events
    /// </summary>
    public async Task HandleClickAsync(
        string selector,
        string url,
        string? elementText,
        Dictionary<string, string> attributes,
        int clientX,
        int clientY)
    {
        var context = new ActionContext
        {
            Url = url,
            PageTitle = string.Empty, // Would be captured from page
            ViewportWidth = 1920, // Would be captured from page
            ViewportHeight = 1080,
            TargetSelector = selector,
            ElementText = elementText,
            ElementTag = attributes.GetValueOrDefault("tagName", "unknown"),
            ElementAttributes = attributes,
            ScrollX = 0, // Would be captured from page
            ScrollY = 0
        };

        await CreateInteractionAsync(
            ActionType.Click,
            context,
            coordinates: (clientX, clientY));
    }

    /// <summary>
    /// Handles input events
    /// </summary>
    public async Task HandleInputAsync(
        string selector,
        string url,
        string value,
        Dictionary<string, string> attributes)
    {
        var context = new ActionContext
        {
            Url = url,
            TargetSelector = selector,
            ElementTag = attributes.GetValueOrDefault("tagName", "input"),
            ElementAttributes = attributes
        };

        await CreateInteractionAsync(
            ActionType.Input,
            context,
            inputValue: value);
    }

    /// <summary>
    /// Handles navigation events
    /// </summary>
    public async Task HandleNavigationAsync(
        string fromUrl,
        string toUrl)
    {
        var context = new ActionContext
        {
            Url = toUrl,
            Metadata = new Dictionary<string, object>
            {
                ["previousUrl"] = fromUrl
            }
        };

        await CreateInteractionAsync(
            ActionType.Navigation,
            context);
    }

    /// <summary>
    /// Handles form submission events
    /// </summary>
    public async Task HandleSubmitAsync(
        string selector,
        string url,
        Dictionary<string, string> formData)
    {
        var context = new ActionContext
        {
            Url = url,
            TargetSelector = selector,
            ElementTag = "form",
            Metadata = new Dictionary<string, object>
            {
                ["formData"] = formData
            }
        };

        await CreateInteractionAsync(
            ActionType.Submit,
            context);
    }

    /// <summary>
    /// Handles keyboard events
    /// </summary>
    public async Task HandleKeyPressAsync(
        string selector,
        string url,
        string key,
        Dictionary<string, string> attributes)
    {
        var context = new ActionContext
        {
            Url = url,
            TargetSelector = selector,
            ElementTag = attributes.GetValueOrDefault("tagName", "unknown"),
            ElementAttributes = attributes
        };

        await CreateInteractionAsync(
            ActionType.KeyPress,
            context,
            key: key);
    }

    /// <summary>
    /// Handles select/dropdown events
    /// </summary>
    public async Task HandleSelectAsync(
        string selector,
        string url,
        string selectedValue,
        Dictionary<string, string> attributes)
    {
        var context = new ActionContext
        {
            Url = url,
            TargetSelector = selector,
            ElementTag = "select",
            ElementAttributes = attributes
        };

        await CreateInteractionAsync(
            ActionType.Select,
            context,
            inputValue: selectedValue);
    }

    /// <summary>
    /// Gets recording statistics
    /// </summary>
    public RecordingStatistics GetStatistics()
    {
        return new RecordingStatistics
        {
            TotalInteractions = _sequenceNumber,
            ActionCounts = new Dictionary<string, int>(_actionCountByType),
            RecordingDuration = DateTimeOffset.UtcNow - (_lastActionTime - TimeSpan.FromMilliseconds(_configuration.ActionDebounceMs))
        };
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation(
            "Disposing event listener for session: {SessionId}, Total interactions: {Count}",
            _sessionId,
            _sequenceNumber);

        _actionCountByType.Clear();
        await Task.CompletedTask;
    }
}

/// <summary>
/// Statistics about a recording session
/// </summary>
public sealed class RecordingStatistics
{
    public required int TotalInteractions { get; init; }
    public required Dictionary<string, int> ActionCounts { get; init; }
    public required TimeSpan RecordingDuration { get; init; }
}
