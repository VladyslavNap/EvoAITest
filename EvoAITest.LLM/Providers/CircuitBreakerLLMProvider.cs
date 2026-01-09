using EvoAITest.Core.Models;
using EvoAITest.Core.Options;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvoAITest.LLM.Providers;

/// <summary>
/// Wraps an LLM provider with circuit breaker pattern for automatic failover.
/// Monitors provider health and routes to fallback when primary provider fails.
/// </summary>
/// <remarks>
/// The circuit breaker prevents cascading failures by:
/// 1. Tracking consecutive failures
/// 2. Opening the circuit when threshold is exceeded
/// 3. Routing all requests to fallback while open
/// 4. Periodically testing primary provider recovery
/// 5. Closing circuit when provider recovers
/// 
/// Thread-safe implementation using lock-free atomic operations where possible.
/// </remarks>
public sealed class CircuitBreakerLLMProvider : ILLMProvider
{
    private readonly ILLMProvider _primaryProvider;
    private readonly ILLMProvider _fallbackProvider;
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreakerLLMProvider> _logger;

    private readonly object _stateLock = new();
    private CircuitBreaker.CircuitBreakerStatus _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerLLMProvider"/> class.
    /// </summary>
    /// <param name="primaryProvider">The primary LLM provider to call.</param>
    /// <param name="fallbackProvider">The fallback LLM provider to use when primary fails.</param>
    /// <param name="options">Circuit breaker configuration options.</param>
    /// <param name="logger">Logger for telemetry and diagnostics.</param>
    public CircuitBreakerLLMProvider(
        ILLMProvider primaryProvider,
        ILLMProvider fallbackProvider,
        IOptions<CircuitBreakerOptions> options,
        ILogger<CircuitBreakerLLMProvider> logger)
    {
        _primaryProvider = primaryProvider ?? throw new ArgumentNullException(nameof(primaryProvider));
        _fallbackProvider = fallbackProvider ?? throw new ArgumentNullException(nameof(fallbackProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _status = CircuitBreaker.CircuitBreakerStatus.CreateDefault();

        ValidateConfiguration();
    }

    /// <inheritdoc/>
    public string Name => $"CircuitBreaker({_primaryProvider.Name} ? {_fallbackProvider.Name})";

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedModels => _primaryProvider.SupportedModels;

    /// <summary>
    /// Gets the current circuit breaker status.
    /// </summary>
    /// <remarks>
    /// Returns an immutable snapshot of the current state.
    /// Safe to call from any thread.
    /// </remarks>
    public CircuitBreaker.CircuitBreakerStatus GetStatus()
    {
        lock (_stateLock)
        {
            return _status;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(
        string prompt,
        Dictionary<string, string>? variables = null,
        List<BrowserTool>? tools = null,
        int maxTokens = 2000,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProviderForRequest();
        var isUsingFallback = provider == _fallbackProvider;

        if (isUsingFallback)
        {
            RecordFallbackUsage();
            _logger.LogWarning(
                "Circuit breaker open, using fallback provider {Provider}",
                _fallbackProvider.Name);
        }

        try
        {
            var response = await provider.GenerateAsync(
                prompt,
                variables,
                tools,
                maxTokens,
                cancellationToken);

            if (!isUsingFallback)
            {
                OnSuccess();
            }

            return response;
        }
        catch (Exception ex)
        {
            if (!isUsingFallback)
            {
                OnFailure(ex);

                // Check if circuit opened due to this failure
                var currentStatus = GetStatus();
                if (currentStatus.State == CircuitBreaker.CircuitBreakerState.Open)
                {
                    _logger.LogWarning(
                        "Circuit breaker opened, retrying with fallback provider {Provider}",
                        _fallbackProvider.Name);

                    RecordFallbackUsage();
                    return await _fallbackProvider.GenerateAsync(
                        prompt,
                        variables,
                        tools,
                        maxTokens,
                        cancellationToken);
                }
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ToolCall>> ParseToolCallsAsync(
        string response,
        CancellationToken cancellationToken = default)
    {
        // Delegate to primary provider for parsing
        // Tool call parsing doesn't affect circuit breaker state
        try
        {
            return await _primaryProvider.ParseToolCallsAsync(response, cancellationToken);
        }
        catch
        {
            // Try fallback if primary fails
            return await _fallbackProvider.ParseToolCallsAsync(response, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public string GetModelName() => _primaryProvider.GetModelName();

    /// <inheritdoc/>
    public TokenUsage GetLastTokenUsage()
    {
        var status = GetStatus();
        return status.IsUsingFallback
            ? _fallbackProvider.GetLastTokenUsage()
            : _primaryProvider.GetLastTokenUsage();
    }

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var status = GetStatus();

        // If circuit is open, only check fallback
        if (status.State == CircuitBreaker.CircuitBreakerState.Open)
        {
            return await _fallbackProvider.IsAvailableAsync(cancellationToken);
        }

        // Otherwise check primary
        try
        {
            var isAvailable = await _primaryProvider.IsAvailableAsync(cancellationToken);
            if (isAvailable)
            {
                OnSuccess();
            }
            return isAvailable;
        }
        catch (Exception ex)
        {
            OnFailure(ex);
            // If primary fails, check if fallback is available
            return await _fallbackProvider.IsAvailableAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<LLMResponse> CompleteAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProviderForRequest();
        var isUsingFallback = provider == _fallbackProvider;

        if (isUsingFallback)
        {
            RecordFallbackUsage();
            _logger.LogWarning(
                "Circuit breaker open, using fallback provider {Provider}",
                _fallbackProvider.Name);
        }

        try
        {
            var response = await provider.CompleteAsync(request, cancellationToken);

            if (!isUsingFallback)
            {
                OnSuccess();
            }

            return response;
        }
        catch (Exception ex)
        {
            if (!isUsingFallback)
            {
                OnFailure(ex);

                var currentStatus = GetStatus();
                if (currentStatus.State == CircuitBreaker.CircuitBreakerState.Open)
                {
                    _logger.LogWarning("Circuit opened, using fallback provider");
                    RecordFallbackUsage();
                    return await _fallbackProvider.CompleteAsync(request, cancellationToken);
                }
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(
        LLMRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var provider = GetProviderForRequest();
        var isUsingFallback = provider == _fallbackProvider;

        if (isUsingFallback)
        {
            RecordFallbackUsage();
            _logger.LogWarning("Circuit breaker open, using fallback for streaming");
        }

        var hasError = false;
        await foreach (var chunk in provider.StreamCompleteAsync(request, cancellationToken))
        {
            yield return chunk;
        }

        if (!hasError && !isUsingFallback)
        {
            OnSuccess();
        }
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProviderForRequest();
        var isUsingFallback = provider == _fallbackProvider;

        try
        {
            var embedding = await provider.GenerateEmbeddingAsync(text, model, cancellationToken);

            if (!isUsingFallback)
            {
                OnSuccess();
            }

            return embedding;
        }
        catch (Exception ex)
        {
            if (!isUsingFallback)
            {
                OnFailure(ex);

                var currentStatus = GetStatus();
                if (currentStatus.State == CircuitBreaker.CircuitBreakerState.Open)
                {
                    RecordFallbackUsage();
                    return await _fallbackProvider.GenerateEmbeddingAsync(text, model, cancellationToken);
                }
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public ProviderCapabilities GetCapabilities()
    {
        // Return union of both providers' capabilities
        var primaryCaps = _primaryProvider.GetCapabilities();
        var fallbackCaps = _fallbackProvider.GetCapabilities();

        return new ProviderCapabilities
        {
            SupportsStreaming = primaryCaps.SupportsStreaming || fallbackCaps.SupportsStreaming,
            SupportsFunctionCalling = primaryCaps.SupportsFunctionCalling || fallbackCaps.SupportsFunctionCalling,
            SupportsVision = primaryCaps.SupportsVision || fallbackCaps.SupportsVision,
            SupportsEmbeddings = primaryCaps.SupportsEmbeddings || fallbackCaps.SupportsEmbeddings,
            MaxContextTokens = Math.Max(primaryCaps.MaxContextTokens, fallbackCaps.MaxContextTokens),
            MaxOutputTokens = Math.Max(primaryCaps.MaxOutputTokens, fallbackCaps.MaxOutputTokens)
        };
    }

    /// <summary>
    /// Gets the appropriate provider for the next request based on circuit breaker state.
    /// </summary>
    private ILLMProvider GetProviderForRequest()
    {
        lock (_stateLock)
        {
            // Check if circuit should transition from Open to Half-Open
            if (_status.State == CircuitBreaker.CircuitBreakerState.Open)
            {
                if (ShouldAttemptReset())
                {
                    TransitionToHalfOpen();
                    return _primaryProvider; // Test primary in half-open state
                }

                return _fallbackProvider; // Circuit still open, use fallback
            }

            // Closed or Half-Open states use primary provider
            return _primaryProvider;
        }
    }

    /// <summary>
    /// Records a successful request.
    /// </summary>
    private void OnSuccess()
    {
        lock (_stateLock)
        {
            var oldState = _status.State;
            _status = _status.WithSuccess();

            // Handle state transitions
            if (oldState == CircuitBreaker.CircuitBreakerState.HalfOpen &&
                _status.SuccessCount >= _options.SuccessThresholdInHalfOpen)
            {
                // Successful request in Half-Open state
                TransitionToClosed();
            }
        }
    }

    /// <summary>
    /// Records a failed request.
    /// </summary>
    private void OnFailure(Exception ex)
    {
        lock (_stateLock)
        {
            var oldState = _status.State;
            _status = _status.WithFailure();

            _logger.LogWarning(
                ex,
                "Request failed on {Provider}, failure count: {Count}/{Threshold}",
                _primaryProvider.Name,
                _status.FailureCount,
                _options.FailureThreshold);

            // Check if should open circuit
            if (oldState == CircuitBreaker.CircuitBreakerState.Closed &&
                _status.FailureCount >= _options.FailureThreshold)
            {
                TransitionToOpen();
            }
            else if (oldState == CircuitBreaker.CircuitBreakerState.HalfOpen)
            {
                // Failure in Half-Open means provider still unhealthy
                TransitionToOpen();
            }
        }
    }

    /// <summary>
    /// Records that a fallback provider was used.
    /// </summary>
    private void RecordFallbackUsage()
    {
        lock (_stateLock)
        {
            _status = _status.WithFallback();
        }
    }

    /// <summary>
    /// Transitions circuit breaker to Closed state.
    /// </summary>
    private void TransitionToClosed()
    {
        _status = _status.WithState(CircuitBreaker.CircuitBreakerState.Closed);

        if (_options.EmitTelemetryEvents)
        {
            _logger.LogInformation(
                "Circuit breaker closed for {Provider} after successful recovery",
                _primaryProvider.Name);
        }
    }

    /// <summary>
    /// Transitions circuit breaker to Open state.
    /// </summary>
    private void TransitionToOpen()
    {
        _status = _status.WithState(CircuitBreaker.CircuitBreakerState.Open);

        if (_options.EmitTelemetryEvents)
        {
            _logger.LogError(
                "Circuit breaker opened for {Provider} after {Count} failures. " +
                "Routing to fallback {Fallback} for {Duration}s",
                _primaryProvider.Name,
                _status.FailureCount,
                _fallbackProvider.Name,
                _options.OpenDuration.TotalSeconds);
        }
    }

    /// <summary>
    /// Transitions circuit breaker to Half-Open state.
    /// </summary>
    private void TransitionToHalfOpen()
    {
        _status = _status.WithState(CircuitBreaker.CircuitBreakerState.HalfOpen);

        if (_options.EmitTelemetryEvents)
        {
            _logger.LogInformation(
                "Circuit breaker entering half-open state for {Provider}, testing recovery",
                _primaryProvider.Name);
        }
    }

    /// <summary>
    /// Determines if circuit breaker should attempt to reset from Open to Half-Open.
    /// </summary>
    private bool ShouldAttemptReset()
    {
        if (_status.LastFailureTime == null)
        {
            return false;
        }

        var timeSinceFailure = DateTimeOffset.UtcNow - _status.LastFailureTime.Value;
        return timeSinceFailure >= _options.OpenDuration;
    }

    /// <summary>
    /// Validates the circuit breaker configuration.
    /// </summary>
    private void ValidateConfiguration()
    {
        var (isValid, errors) = _options.Validate();
        if (!isValid)
        {
            var errorMessage = $"Invalid circuit breaker configuration: {string.Join("; ", errors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation(
            "Circuit breaker initialized for {Primary} ? {Fallback} " +
            "(threshold: {Threshold}, open duration: {Duration}s)",
            _primaryProvider.Name,
            _fallbackProvider.Name,
            _options.FailureThreshold,
            _options.OpenDuration.TotalSeconds);
    }
}
