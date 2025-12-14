namespace EvoAITest.Core.Browser;

using System.Collections.Concurrent;
using System.Diagnostics;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Playwright-based implementation of network interception and mocking.
/// </summary>
public sealed class PlaywrightNetworkInterceptor : INetworkInterceptor
{
    private readonly Microsoft.Playwright.IPage _page;
    private readonly ILogger<PlaywrightNetworkInterceptor> _logger;
    private readonly ConcurrentDictionary<Guid, NetworkLog> _networkLogs = new();
    private readonly ConcurrentBag<string> _activeRoutes = new();
    private bool _networkLoggingEnabled;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaywrightNetworkInterceptor"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <param name="logger">Logger instance.</param>
    public PlaywrightNetworkInterceptor(Microsoft.Playwright.IPage page, ILogger<PlaywrightNetworkInterceptor> logger)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsNetworkLoggingEnabled => _networkLoggingEnabled;

    /// <inheritdoc />
    public async Task InterceptRequestAsync(
        string pattern,
        Func<InterceptedRequest, Task<InterceptedResponse?>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation("Setting up request interception for pattern: {Pattern}", pattern);

        await _page.RouteAsync(pattern, async route =>
        {
            var request = route.Request;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var interceptedRequest = new InterceptedRequest
                {
                    Url = request.Url,
                    Method = request.Method,
                    Headers = request.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    PostData = request.PostData,
                    ResourceType = request.ResourceType,
                    IsNavigationRequest = request.IsNavigationRequest
                };

                // Log the request
                Guid? logId = null;
                if (_networkLoggingEnabled)
                {
                    logId = LogRequest(interceptedRequest, stopwatch);
                }

                // Call handler
                var response = await handler(interceptedRequest).ConfigureAwait(false);

                if (response != null)
                {
                    // Handler provided a response - fulfill it
                    var fulfillOptions = new Microsoft.Playwright.RouteFulfillOptions
                    {
                        Status = response.Status,
                        Headers = response.Headers.Count > 0 ? response.Headers : null,
                        Body = response.Body,
                        BodyBytes = response.BodyBytes,
                        ContentType = response.ContentType
                    };

                    await route.FulfillAsync(fulfillOptions).ConfigureAwait(false);

                    // Log mocked response
                    if (_networkLoggingEnabled && logId.HasValue)
                    {
                        LogResponse(logId.Value, response.Status, stopwatch, wasMocked: true);
                    }

                    _logger.LogDebug("Request intercepted and fulfilled: {Method} {Url}", request.Method, request.Url);
                }
                else
                {
                    // Handler returned null - continue with original request
                    await route.ContinueAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request handler for pattern {Pattern}", pattern);
                await route.ContinueAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        _activeRoutes.Add(pattern);
    }

    /// <inheritdoc />
    public async Task BlockRequestAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        _logger.LogInformation("Blocking requests matching pattern: {Pattern}", pattern);

        await _page.RouteAsync(pattern, async route =>
        {
            var request = route.Request;

            if (_networkLoggingEnabled)
            {
                var log = new NetworkLog
                {
                    Url = request.Url,
                    Method = request.Method,
                    ResourceType = request.ResourceType,
                    Timestamp = DateTimeOffset.UtcNow,
                    WasBlocked = true,
                    StatusCode = 0
                };
                _networkLogs.TryAdd(log.Id, log);
            }

            _logger.LogDebug("Blocking request: {Method} {Url}", request.Method, request.Url);
            await route.AbortAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        _activeRoutes.Add(pattern);
    }

    /// <inheritdoc />
    public async Task MockResponseAsync(
        string pattern,
        MockResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(response);

        _logger.LogInformation("Mocking responses for pattern: {Pattern} (Status: {Status})", pattern, response.Status);

        await _page.RouteAsync(pattern, async route =>
        {
            var request = route.Request;
            var stopwatch = Stopwatch.StartNew();

            // Apply delay if specified
            if (response.DelayMs.HasValue && response.DelayMs.Value > 0)
            {
                await Task.Delay(response.DelayMs.Value).ConfigureAwait(false);
            }

            var headers = response.Headers ?? new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(response.ContentType) && !headers.ContainsKey("Content-Type"))
            {
                headers["Content-Type"] = response.ContentType;
            }

            var fulfillOptions = new Microsoft.Playwright.RouteFulfillOptions
            {
                Status = response.Status,
                Headers = headers.Count > 0 ? headers : null,
                Body = response.Body,
                ContentType = response.ContentType
            };

            await route.FulfillAsync(fulfillOptions).ConfigureAwait(false);

            stopwatch.Stop();

            if (_networkLoggingEnabled)
            {
                var log = new NetworkLog
                {
                    Url = request.Url,
                    Method = request.Method,
                    ResourceType = request.ResourceType,
                    StatusCode = response.Status,
                    Timestamp = DateTimeOffset.UtcNow,
                    DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                    WasMocked = true,
                    RequestHeaders = request.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    ResponseHeaders = headers
                };
                _networkLogs.TryAdd(log.Id, log);
            }

            _logger.LogDebug("Mocked response: {Method} {Url} -> {Status}", request.Method, request.Url, response.Status);
        }).ConfigureAwait(false);

        _activeRoutes.Add(pattern);
    }

    /// <inheritdoc />
    public Task<List<NetworkLog>> GetNetworkLogsAsync(CancellationToken cancellationToken = default)
    {
        var logs = _networkLogs.Values.OrderBy(l => l.Timestamp).ToList();
        _logger.LogDebug("Retrieved {Count} network logs", logs.Count);
        return Task.FromResult(logs);
    }

    /// <inheritdoc />
    public Task ClearNetworkLogsAsync(CancellationToken cancellationToken = default)
    {
        _networkLogs.Clear();
        _logger.LogInformation("Network logs cleared");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ClearInterceptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing all route interceptions ({Count} routes)", _activeRoutes.Count);

        // Unroute all patterns
        foreach (var pattern in _activeRoutes)
        {
            try
            {
                await _page.UnrouteAsync(pattern).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to unroute pattern: {Pattern}", pattern);
            }
        }

        _activeRoutes.Clear();
    }

    /// <inheritdoc />
    public Task SetNetworkLoggingAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        _networkLoggingEnabled = enabled;
        _logger.LogInformation("Network logging {Status}", enabled ? "enabled" : "disabled");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await ClearInterceptionsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing interceptions during dispose");
        }

        _networkLogs.Clear();
    }

    private Guid LogRequest(InterceptedRequest request, Stopwatch stopwatch)
    {
        var log = new NetworkLog
        {
            Url = request.Url,
            Method = request.Method,
            ResourceType = request.ResourceType,
            Timestamp = DateTimeOffset.UtcNow,
            RequestHeaders = request.Headers
        };
        _networkLogs.TryAdd(log.Id, log);
        return log.Id;
    }

    private void LogResponse(Guid logId, int statusCode, Stopwatch stopwatch, bool wasMocked = false)
    {
        // Update the existing log entry in-place
        if (_networkLogs.TryGetValue(logId, out var existingLog))
        {
            var updatedLog = existingLog with
            {
                StatusCode = statusCode,
                DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                WasMocked = wasMocked
            };
            _networkLogs.TryUpdate(logId, updatedLog, existingLog);
        }
    }
}
