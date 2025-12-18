using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.SmartWaiting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using PlaywrightPage = Microsoft.Playwright.IPage;

namespace EvoAITest.Core.Services;

/// <summary>
/// Implementation of page stability detection using Playwright.
/// </summary>
public sealed class PageStabilityDetector : IPageStabilityDetector, IDisposable
{
    /// <summary>
    /// Default interval in milliseconds for background stability monitoring checks.
    /// </summary>
    private const int MonitoringIntervalMs = 1000;

    /// <summary>
    /// Default stability period in milliseconds for checking page stability.
    /// </summary>
    private const int DefaultStabilityPeriodMs = 500;

    private readonly ILogger<PageStabilityDetector> _logger;
    private readonly SemaphoreSlim _monitoringLock = new(1, 1);
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;
    private StabilityMetrics? _currentMetrics;
    private volatile PlaywrightPage? _page;

    public bool IsMonitoring { get; private set; }
    public StabilityMetrics? CurrentMetrics => _currentMetrics;

    // Common loader selectors to check
    private static readonly string[] LoaderSelectors = new[]
    {
        ".loading", ".spinner", ".loader", "[role='progressbar']",
        ".loading-overlay", ".loading-spinner", ".sk-circle",
        ".fa-spinner", ".icon-spinner", "[data-loading='true']"
    };

    public PageStabilityDetector(ILogger<PageStabilityDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sets the current page for stability detection.
    /// </summary>
    public void SetPage(PlaywrightPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
    }

    public async Task<bool> IsDomStableAsync(
        int stabilityPeriodMs = 500,
        CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            var mutationCount = await MonitorDomMutationsAsync(stabilityPeriodMs, cancellationToken);
            return mutationCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking DOM stability");
            return false;
        }
    }

    public async Task<int> MonitorDomMutationsAsync(
        int durationMs = 1000,
        CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            var script = @"
                () => {
                    return new Promise((resolve) => {
                        let mutationCount = 0;
                        const observer = new MutationObserver((mutations) => {
                            mutationCount += mutations.length;
                        });

                        observer.observe(document.body, {
                            childList: true,
                            subtree: true,
                            attributes: true,
                            characterData: true
                        });

                        setTimeout(() => {
                            observer.disconnect();
                            resolve(mutationCount);
                        }, " + durationMs + @");
                    });
                }";

            var result = await _page!.EvaluateAsync<int>(script);
            _logger.LogDebug("Detected {Count} DOM mutations in {Duration}ms", result, durationMs);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error monitoring DOM mutations");
            return -1;
        }
    }

    public async Task<bool> AreAnimationsCompleteAsync(
        string? selector = null,
        CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            var count = await GetActiveAnimationCountAsync(selector, cancellationToken);
            return count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking animations");
            return true; // Assume complete on error
        }
    }

    public async Task<int> GetActiveAnimationCountAsync(
        string? selector = null,
        CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            int result;
            if (selector != null)
            {
                var script = @"
                    (selector) => {
                        const element = document.querySelector(selector);
                        if (!element) return 0;
                        const animations = element.getAnimations();
                        return animations.filter(a => a.playState === 'running').length;
                    }";
                result = await _page!.EvaluateAsync<int>(script, selector);
            }
            else
            {
                var script = @"
                    () => {
                        const animations = document.getAnimations();
                        return animations.filter(a => a.playState === 'running').length;
                    }";
                result = await _page!.EvaluateAsync<int>(script);
            }

            _logger.LogDebug("Active animations: {Count}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting animation count");
            return 0;
        }
    }

    public async Task<bool> IsNetworkIdleAsync(
        int maxActiveRequests = 0,
        int idleDurationMs = 500,
        CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            // Wait for network idle using Playwright's built-in functionality
            await _page!.WaitForLoadStateAsync(
                LoadState.NetworkIdle,
                new PageWaitForLoadStateOptions { Timeout = idleDurationMs });

            return true;
        }
        catch (TimeoutException)
        {
            _logger.LogDebug("Network not idle after {Duration}ms", idleDurationMs);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking network idle");
            return false;
        }
    }

    public async Task<int> GetActiveRequestCountAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            // Note: Playwright doesn't directly expose pending request count
            // This is an approximation based on network idle state
            var isIdle = await IsNetworkIdleAsync(0, 100, cancellationToken);
            return isIdle ? 0 : 1; // Return 1 if not idle, 0 if idle
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting active request count");
            return 0;
        }
    }

    public async Task<bool> AreLoadersHiddenAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            var loaders = await DetectLoadersAsync(cancellationToken);
            return loaders.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking loaders");
            return true; // Assume hidden on error
        }
    }

    public async Task<List<string>> DetectLoadersAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            var checkTasks = LoaderSelectors.Select(async selector =>
            {
                var elements = await _page!.QuerySelectorAllAsync(selector);
                var visibilityTasks = elements.Select(element => element.IsVisibleAsync());
                var visibilityResults = await Task.WhenAll(visibilityTasks);
                
                return visibilityResults.Any(isVisible => isVisible) ? selector : null;
            });

            var results = await Task.WhenAll(checkTasks);
            var visibleLoaders = results.Where(selector => selector != null).Select(s => s!).ToList();

            _logger.LogDebug("Detected {Count} visible loaders", visibleLoaders.Count);
            return visibleLoaders;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting loaders");
            return new List<string>();
        }
    }

    public async Task<bool> IsJavaScriptIdleAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            var script = @"
                () => {
                    return new Promise((resolve) => {
                        // Check if there are pending microtasks
                        setTimeout(() => {
                            resolve(true);
                        }, 0);
                    });
                }";

            await _page!.EvaluateAsync<bool>(script);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking JavaScript idle");
            return true;
        }
    }

    public async Task<bool> AreImagesLoadedAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            var script = @"
                () => {
                    const images = Array.from(document.images);
                    return images.every(img => img.complete && img.naturalWidth > 0);
                }";

            return await _page!.EvaluateAsync<bool>(script);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking images loaded");
            return true;
        }
    }

    public async Task<bool> AreFontsLoadedAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            var script = @"
                () => {
                    return document.fonts ? document.fonts.status === 'loaded' : true;
                }";

            return await _page!.EvaluateAsync<bool>(script);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking fonts loaded");
            return true;
        }
    }

    public async Task<StabilityMetrics> GetStabilityMetricsAsync(CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        try
        {
            // Start independent checks concurrently
            var domStableTask = IsDomStableAsync(DefaultStabilityPeriodMs, cancellationToken);
            var animationsCompleteTask = AreAnimationsCompleteAsync(null, cancellationToken);
            var networkIdleTask = IsNetworkIdleAsync(0, DefaultStabilityPeriodMs, cancellationToken);
            var activeRequestsTask = GetActiveRequestCountAsync(cancellationToken);
            var loadersTask = DetectLoadersAsync(cancellationToken);
            var jsIdleTask = IsJavaScriptIdleAsync(cancellationToken);
            var imagesLoadedTask = AreImagesLoadedAsync(cancellationToken);
            var fontsLoadedTask = AreFontsLoadedAsync(cancellationToken);

            // Await all independent checks together
            await Task.WhenAll(
                domStableTask,
                animationsCompleteTask,
                networkIdleTask,
                activeRequestsTask,
                loadersTask,
                jsIdleTask,
                imagesLoadedTask,
                fontsLoadedTask);

            var isDomStable = await domStableTask;
            var areAnimationsComplete = await animationsCompleteTask;
            var isNetworkIdle = await networkIdleTask;
            var activeRequests = await activeRequestsTask;
            var loaders = await loadersTask;
            var isJsIdle = await jsIdleTask;
            var areImagesLoaded = await imagesLoadedTask;
            var areFontsLoaded = await fontsLoadedTask;

            // Dependent checks: only when needed, and run them in parallel if both are required
            var domMutations = 0;
            var activeAnimations = 0;

            if (!isDomStable && !areAnimationsComplete)
            {
                var domMutationsTask = MonitorDomMutationsAsync(DefaultStabilityPeriodMs, cancellationToken);
                var activeAnimationsTask = GetActiveAnimationCountAsync(null, cancellationToken);

                await Task.WhenAll(domMutationsTask, activeAnimationsTask);

                domMutations = await domMutationsTask;
                activeAnimations = await activeAnimationsTask;
            }
            else if (!isDomStable)
            {
                domMutations = await MonitorDomMutationsAsync(DefaultStabilityPeriodMs, cancellationToken);
            }
            else if (!areAnimationsComplete)
            {
                activeAnimations = await GetActiveAnimationCountAsync(null, cancellationToken);
            }

            var metrics = new StabilityMetrics
            {
                IsDomStable = isDomStable,
                DomMutationCount = domMutations,
                AreAnimationsComplete = areAnimationsComplete,
                ActiveAnimationCount = activeAnimations,
                IsNetworkIdle = isNetworkIdle,
                ActiveRequestCount = activeRequests,
                AreLoadersHidden = loaders.Count == 0,
                VisibleLoaderCount = loaders.Count,
                IsJavaScriptIdle = isJsIdle,
                AreImagesLoaded = areImagesLoaded,
                AreFontsLoaded = areFontsLoaded
            };

            var score = StabilityMetrics.CalculateStabilityScore(metrics);
            return metrics with { StabilityScore = score };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stability metrics");
            return StabilityMetrics.CreateUnstable();
        }
    }

    public async Task<bool> WaitForStabilityAsync(
        int maxWaitMs = 10000,
        int checkIntervalMs = 100,
        CancellationToken cancellationToken = default)
    {
        EnsurePageSet();

        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime.AddMilliseconds(maxWaitMs);

        _logger.LogInformation("Waiting for page stability (max {MaxWait}ms)", maxWaitMs);

        while (DateTimeOffset.UtcNow < endTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metrics = await GetStabilityMetricsAsync(cancellationToken);
            
            if (metrics.IsStable())
            {
                var elapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("Page became stable after {Elapsed}ms", elapsed);
                return true;
            }

            await Task.Delay(checkIntervalMs, cancellationToken);
        }

        _logger.LogWarning("Page did not become stable within {MaxWait}ms", maxWaitMs);
        return false;
    }

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        await _monitoringLock.WaitAsync(cancellationToken);
        try
        {
            if (IsMonitoring)
            {
                _logger.LogWarning("Monitoring is already active");
                return;
            }

            EnsurePageSet();

            _monitoringCts = new CancellationTokenSource();
            IsMonitoring = true;

            _monitoringTask = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Started background stability monitoring");

                    while (!_monitoringCts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            _currentMetrics = await GetStabilityMetricsAsync(_monitoringCts.Token);
                            await Task.Delay(MonitoringIntervalMs, _monitoringCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error during stability monitoring");
                        }
                    }

                    _logger.LogInformation("Stopped background stability monitoring");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in background stability monitoring");
                }
            }, _monitoringCts.Token);
        }
        finally
        {
            _monitoringLock.Release();
        }
    }

    public async Task StopMonitoringAsync()
    {
        await _monitoringLock.WaitAsync();
        try
        {
            if (!IsMonitoring)
            {
                return;
            }

            _monitoringCts?.Cancel();
            
            if (_monitoringTask != null)
            {
                try
                {
                    await _monitoringTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            _monitoringCts?.Dispose();
            _monitoringCts = null;
            _monitoringTask = null;
            IsMonitoring = false;
        }
        finally
        {
            _monitoringLock.Release();
        }
    }

    private void EnsurePageSet()
    {
        if (_page is null)
        {
            throw new InvalidOperationException(
                "Page not set. Call SetPage() before using PageStabilityDetector.");
        }
    }

    public void Dispose()
    {
        // Best-effort synchronous cleanup without blocking on async operations
        try
        {
            if (IsMonitoring)
            {
                _monitoringCts?.Cancel();
            }

            _monitoringCts?.Dispose();
            _monitoringCts = null;
            _monitoringTask = null;
            IsMonitoring = false;
        }
        finally
        {
            _monitoringLock.Dispose();
        }
    }
}
