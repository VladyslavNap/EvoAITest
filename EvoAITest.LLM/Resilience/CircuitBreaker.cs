using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EvoAITest.LLM.Resilience;

/// <summary>
/// Implements the circuit breaker pattern for LLM provider reliability.
/// </summary>
/// <remarks>
/// <para>
/// The circuit breaker prevents cascading failures by temporarily disabling
/// providers that are experiencing errors. It has three states:
/// </para>
/// <list type="bullet">
/// <item><description><b>Closed</b>: Normal operation, requests pass through</description></item>
/// <item><description><b>Open</b>: Provider is failing, requests are blocked</description></item>
/// <item><description><b>Half-Open</b>: Testing if provider has recovered</description></item>
/// </list>
/// <para>
/// When a provider fails repeatedly, the circuit opens and requests are automatically
/// routed to fallback providers. After a timeout, the circuit enters half-open state
/// to test if the provider has recovered.
/// </para>
/// </remarks>
public sealed class CircuitBreaker
{
    private readonly string _providerName;
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker> _logger;

    private CircuitState _state = CircuitState.Closed;
    private int _consecutiveFailures;
    private DateTimeOffset _lastFailureTime;
    private DateTimeOffset _openedAt;
    private readonly object _stateLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="providerName">The name of the provider this circuit breaker protects.</param>
    /// <param name="options">Circuit breaker configuration options.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public CircuitBreaker(
        string providerName,
        CircuitBreakerOptions options,
        ILogger<CircuitBreaker> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _providerName = providerName;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the circuit breaker allows requests.
    /// </summary>
    /// <returns>
    /// True if the circuit is closed or half-open and can accept requests;
    /// false if the circuit is open and should reject requests.
    /// </returns>
    public bool IsRequestAllowed()
    {
        lock (_stateLock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    // Check if we should transition to half-open
                    if (DateTimeOffset.UtcNow - _openedAt >= _options.OpenDuration)
                    {
                        TransitionToHalfOpen();
                        return true;
                    }
                    return false;

                case CircuitState.HalfOpen:
                    return true;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Records a successful request, potentially closing the circuit.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_stateLock)
        {
            switch (_state)
            {
                case CircuitState.HalfOpen:
                    _logger.LogInformation(
                        "Circuit breaker for {Provider} transitioning to Closed after successful half-open test",
                        _providerName);
                    TransitionToClosed();
                    break;

                case CircuitState.Closed:
                    // Reset failure counter on success
                    if (_consecutiveFailures > 0)
                    {
                        _consecutiveFailures = 0;
                        _logger.LogDebug(
                            "Circuit breaker for {Provider}: Reset failure counter after success",
                            _providerName);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Records a failed request, potentially opening the circuit.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    public void RecordFailure(Exception exception)
    {
        lock (_stateLock)
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTimeOffset.UtcNow;

            _logger.LogWarning(exception,
                "Circuit breaker for {Provider}: Recorded failure #{Count}. State: {State}",
                _providerName, _consecutiveFailures, _state);

            switch (_state)
            {
                case CircuitState.Closed:
                    if (_consecutiveFailures >= _options.FailureThreshold)
                    {
                        TransitionToOpen();
                    }
                    break;

                case CircuitState.HalfOpen:
                    _logger.LogWarning(
                        "Circuit breaker for {Provider}: Half-open test failed, returning to Open",
                        _providerName);
                    TransitionToOpen();
                    break;
            }
        }
    }

    /// <summary>
    /// Manually resets the circuit breaker to closed state.
    /// </summary>
    public void Reset()
    {
        lock (_stateLock)
        {
            _logger.LogInformation(
                "Circuit breaker for {Provider}: Manual reset to Closed",
                _providerName);
            TransitionToClosed();
        }
    }

    /// <summary>
    /// Gets statistics about the circuit breaker's operation.
    /// </summary>
    /// <returns>Circuit breaker statistics.</returns>
    public CircuitBreakerStats GetStats()
    {
        lock (_stateLock)
        {
            return new CircuitBreakerStats
            {
                ProviderName = _providerName,
                State = _state,
                ConsecutiveFailures = _consecutiveFailures,
                LastFailureTime = _lastFailureTime,
                OpenedAt = _state == CircuitState.Open ? _openedAt : null
            };
        }
    }

    private void TransitionToClosed()
    {
        _state = CircuitState.Closed;
        _consecutiveFailures = 0;
    }

    private void TransitionToOpen()
    {
        _state = CircuitState.Open;
        _openedAt = DateTimeOffset.UtcNow;
        
        _logger.LogError(
            "Circuit breaker for {Provider} is now OPEN after {Count} consecutive failures. " +
            "Will attempt recovery in {Duration}s",
            _providerName, _consecutiveFailures, _options.OpenDuration.TotalSeconds);
    }

    private void TransitionToHalfOpen()
    {
        _state = CircuitState.HalfOpen;
        
        _logger.LogInformation(
            "Circuit breaker for {Provider} is now HALF-OPEN, testing recovery",
            _providerName);
    }
}

/// <summary>
/// Defines the states of a circuit breaker.
/// </summary>
public enum CircuitState
{
    /// <summary>Normal operation, requests pass through.</summary>
    Closed,
    
    /// <summary>Provider is failing, requests are blocked.</summary>
    Open,
    
    /// <summary>Testing if provider has recovered.</summary>
    HalfOpen
}

/// <summary>
/// Configuration options for circuit breaker behavior.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the number of consecutive failures before opening the circuit.
    /// </summary>
    /// <value>
    /// Default is 5 failures. Higher values make the circuit breaker less sensitive
    /// to transient errors.
    /// </value>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets how long the circuit stays open before attempting recovery.
    /// </summary>
    /// <value>
    /// Default is 30 seconds. After this duration, the circuit transitions to
    /// half-open and allows a test request through.
    /// </value>
    public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the timeout for individual requests.
    /// </summary>
    /// <value>
    /// Default is 60 seconds. Requests exceeding this timeout are treated as failures.
    /// </value>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);
}

/// <summary>
/// Statistics about a circuit breaker's operation.
/// </summary>
public sealed class CircuitBreakerStats
{
    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Gets or sets the current circuit state.
    /// </summary>
    public required CircuitState State { get; init; }

    /// <summary>
    /// Gets or sets the number of consecutive failures.
    /// </summary>
    public required int ConsecutiveFailures { get; init; }

    /// <summary>
    /// Gets or sets the time of the last failure.
    /// </summary>
    public required DateTimeOffset LastFailureTime { get; init; }

    /// <summary>
    /// Gets or sets the time when the circuit was opened (null if not open).
    /// </summary>
    public DateTimeOffset? OpenedAt { get; init; }
}

/// <summary>
/// Manages circuit breakers for multiple providers.
/// </summary>
public sealed class CircuitBreakerRegistry
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _breakers = new();
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerRegistry"/> class.
    /// </summary>
    /// <param name="options">Circuit breaker configuration options.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public CircuitBreakerRegistry(
        CircuitBreakerOptions options,
        ILogger<CircuitBreaker> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a circuit breaker for the specified provider.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The circuit breaker for the provider.</returns>
    public CircuitBreaker GetOrCreateBreaker(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        return _breakers.GetOrAdd(
            providerName,
            name => new CircuitBreaker(name, _options, _logger));
    }

    /// <summary>
    /// Gets statistics for all circuit breakers.
    /// </summary>
    /// <returns>List of circuit breaker statistics.</returns>
    public List<CircuitBreakerStats> GetAllStats()
    {
        return _breakers.Values
            .Select(b => b.GetStats())
            .ToList();
    }

    /// <summary>
    /// Resets all circuit breakers to closed state.
    /// </summary>
    public void ResetAll()
    {
        foreach (var breaker in _breakers.Values)
        {
            breaker.Reset();
        }
    }
}
