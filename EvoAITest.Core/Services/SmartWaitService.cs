using System.Collections.Concurrent;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.SmartWaiting;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services;

/// <summary>
/// Implementation of intelligent, adaptive waiting strategies.
/// </summary>
public sealed class SmartWaitService : ISmartWaitService
{
    /// <summary>
    /// Default stability period in milliseconds for checking DOM stability.
    /// </summary>
    private const int DefaultStabilityPeriodMs = 500;

    private readonly IPageStabilityDetector _stabilityDetector;
    private readonly ILogger<SmartWaitService> _logger;
    private readonly ConcurrentDictionary<string, HistoricalData> _historicalData = new();

    public WaitStrategy DefaultStrategy { get; }
    public int DefaultMaxWaitMs { get; }
    public int DefaultPollingIntervalMs { get; }

    public SmartWaitService(
        IPageStabilityDetector stabilityDetector,
        ILogger<SmartWaitService> logger,
        WaitStrategy defaultStrategy = WaitStrategy.Adaptive,
        int defaultMaxWaitMs = 10000,
        int defaultPollingIntervalMs = 100)
    {
        _stabilityDetector = stabilityDetector ?? throw new ArgumentNullException(nameof(stabilityDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DefaultStrategy = defaultStrategy;
        DefaultMaxWaitMs = defaultMaxWaitMs;
        DefaultPollingIntervalMs = defaultPollingIntervalMs;
    }

    public async Task<bool> WaitForStableStateAsync(
        WaitConditions conditions,
        int? maxWaitMs = null,
        CancellationToken cancellationToken = default)
    {
        var timeout = maxWaitMs ?? conditions.MaxWaitMs;
        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime.AddMilliseconds(timeout);

        _logger.LogInformation(
            "Waiting for stable state with {ConditionCount} conditions (RequireAll: {RequireAll}), timeout: {Timeout}ms",
            conditions.Conditions.Count,
            conditions.RequireAll,
            timeout);

        try
        {
            while (DateTimeOffset.UtcNow < endTime)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var results = await CheckConditionsAsync(conditions, cancellationToken);

                if (conditions.RequireAll && results.All(r => r))
                {
                    var elapsed = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation("All conditions met after {Elapsed}ms", elapsed);
                    return true;
                }

                if (!conditions.RequireAll && results.Any(r => r))
                {
                    var elapsed = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation("At least one condition met after {Elapsed}ms", elapsed);
                    return true;
                }

                await Task.Delay(conditions.PollingIntervalMs, cancellationToken);
            }

            _logger.LogWarning("Timeout waiting for stable state after {Timeout}ms", timeout);

            if (conditions.ThrowOnTimeout)
            {
                throw new TimeoutException($"Timeout waiting for stable state after {timeout}ms");
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Wait for stable state was cancelled");
            throw;
        }
    }

    public async Task<bool> WaitForConditionAsync(
        Func<Task<bool>> predicate,
        TimeSpan? timeout = null,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default)
    {
        var timeoutMs = timeout?.TotalMilliseconds ?? DefaultMaxWaitMs;
        var pollingMs = pollingInterval?.TotalMilliseconds ?? DefaultPollingIntervalMs;
        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime.AddMilliseconds(timeoutMs);

        _logger.LogDebug("Waiting for custom condition, timeout: {Timeout}ms", timeoutMs);

        try
        {
            while (DateTimeOffset.UtcNow < endTime)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await predicate())
                {
                    var elapsed = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogDebug("Condition met after {Elapsed}ms", elapsed);
                    return true;
                }

                await Task.Delay((int)pollingMs, cancellationToken);
            }

            _logger.LogWarning("Timeout waiting for condition after {Timeout}ms", timeoutMs);
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Wait for condition was cancelled");
            throw;
        }
    }

    public async Task WaitForNetworkIdleAsync(
        int maxActiveRequests = 0,
        int idleDurationMs = 500,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Waiting for network idle (max {MaxRequests} requests, {Duration}ms duration)",
            maxActiveRequests,
            idleDurationMs);

        var success = await _stabilityDetector.IsNetworkIdleAsync(
            maxActiveRequests,
            idleDurationMs,
            cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Network did not become idle within expected time");
        }
    }

    public async Task WaitForAnimationsAsync(
        string? selector = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Waiting for animations to complete{Selector}",
            selector != null ? $" on {selector}" : "");

        var conditions = WaitConditions.ForAnimations(selector);
        await WaitForStableStateAsync(conditions, cancellationToken: cancellationToken);
    }

    public Task<TimeSpan> CalculateOptimalTimeoutAsync(
        string action,
        HistoricalData history,
        CancellationToken cancellationToken = default)
    {
        if (!history.HasSufficientData())
        {
            _logger.LogWarning(
                "Insufficient historical data for {Action} ({SampleCount} samples), using default timeout",
                action,
                history.SampleCount);
            return Task.FromResult(TimeSpan.FromMilliseconds(DefaultMaxWaitMs));
        }

        var timeoutMs = history.CalculateAdaptiveTimeout(DefaultStrategy);

        _logger.LogInformation(
            "Calculated optimal timeout for {Action}: {Timeout}ms (based on {Samples} samples, {Strategy} strategy)",
            action,
            timeoutMs,
            history.SampleCount,
            DefaultStrategy);

        return Task.FromResult(TimeSpan.FromMilliseconds(timeoutMs));
    }

    public async Task WaitForPageLoadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Waiting for page load completion");

        var conditions = WaitConditions.ForPageLoad();
        await WaitForStableStateAsync(conditions, cancellationToken: cancellationToken);
    }

    public async Task WaitForLoadersHiddenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Waiting for loading indicators to be hidden");

        var success = await WaitForConditionAsync(
            async () => await _stabilityDetector.AreLoadersHiddenAsync(cancellationToken),
            TimeSpan.FromMilliseconds(DefaultMaxWaitMs),
            TimeSpan.FromMilliseconds(DefaultPollingIntervalMs),
            cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Loading indicators did not hide within timeout");
        }
    }

    public Task<StabilityMetrics> GetStabilityMetricsAsync(CancellationToken cancellationToken = default)
    {
        return _stabilityDetector.GetStabilityMetricsAsync(cancellationToken);
    }

    public Task RecordWaitTimeAsync(
        string action,
        int actualWaitMs,
        bool success,
        CancellationToken cancellationToken = default)
    {
        if (!_historicalData.TryGetValue(action, out var history))
        {
            history = HistoricalData.CreateEmpty(action);
        }

        var updatedHistory = history.WithNewWaitTime(actualWaitMs, success);
        _historicalData[action] = updatedHistory;

        _logger.LogDebug(
            "Recorded wait time for {Action}: {WaitTime}ms (success: {Success}), total samples: {Samples}",
            action,
            actualWaitMs,
            success,
            updatedHistory.SampleCount);

        return Task.CompletedTask;
    }

    public Task<HistoricalData?> GetHistoricalDataAsync(
        string action,
        CancellationToken cancellationToken = default)
    {
        _historicalData.TryGetValue(action, out var history);
        return Task.FromResult(history);
    }

    /// <summary>
    /// Checks all conditions and returns their results.
    /// </summary>
    private async Task<List<bool>> CheckConditionsAsync(
        WaitConditions conditions,
        CancellationToken cancellationToken)
    {
        var checkTasks = conditions.Conditions.Select(async condition =>
        {
            return condition switch
            {
                WaitConditionType.NetworkIdle => await CheckNetworkIdleAsync(conditions, cancellationToken),
                WaitConditionType.DomStable => await _stabilityDetector.IsDomStableAsync(DefaultStabilityPeriodMs, cancellationToken),
                WaitConditionType.AnimationsComplete => await _stabilityDetector.AreAnimationsCompleteAsync(
                    conditions.Selector, cancellationToken),
                WaitConditionType.LoadersHidden => await _stabilityDetector.AreLoadersHiddenAsync(cancellationToken),
                WaitConditionType.JavaScriptIdle => await _stabilityDetector.IsJavaScriptIdleAsync(cancellationToken),
                WaitConditionType.ImagesLoaded => await _stabilityDetector.AreImagesLoadedAsync(cancellationToken),
                WaitConditionType.FontsLoaded => await _stabilityDetector.AreFontsLoadedAsync(cancellationToken),
                WaitConditionType.CustomPredicate => conditions.CustomPredicate != null &&
                                                     await conditions.CustomPredicate(),
                WaitConditionType.PageLoad => true, // Handled separately
                WaitConditionType.DomContentLoaded => true, // Handled separately
                _ => false
            };
        });

        var results = await Task.WhenAll(checkTasks);
        return results.ToList();
    }

    /// <summary>
    /// Checks network idle condition with configured settings.
    /// </summary>
    private async Task<bool> CheckNetworkIdleAsync(
        WaitConditions conditions,
        CancellationToken cancellationToken)
    {
        var settings = conditions.NetworkIdleSettings ?? new NetworkIdleSettings();

        return await _stabilityDetector.IsNetworkIdleAsync(
            settings.MaxActiveRequests,
            settings.IdleDurationMs,
            cancellationToken);
    }
}
