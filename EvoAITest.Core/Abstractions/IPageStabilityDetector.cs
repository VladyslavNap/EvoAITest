using EvoAITest.Core.Models.SmartWaiting;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for detecting page stability through various monitoring strategies.
/// </summary>
public interface IPageStabilityDetector
{
    /// <summary>
    /// Checks if the DOM is currently stable (no mutations for a period).
    /// </summary>
    /// <param name="stabilityPeriodMs">Period to check for stability in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if DOM is stable, false otherwise.</returns>
    Task<bool> IsDomStableAsync(
        int stabilityPeriodMs = 500,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors DOM mutations for a specified period.
    /// </summary>
    /// <param name="durationMs">Duration to monitor in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of mutations detected.</returns>
    Task<int> MonitorDomMutationsAsync(
        int durationMs = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all CSS animations are complete.
    /// </summary>
    /// <param name="selector">Optional selector to check specific element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if no active animations, false otherwise.</returns>
    Task<bool> AreAnimationsCompleteAsync(
        string? selector = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active CSS animations.
    /// </summary>
    /// <param name="selector">Optional selector to check specific element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of active animations.</returns>
    Task<int> GetActiveAnimationCountAsync(
        string? selector = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the network is idle (no active requests).
    /// </summary>
    /// <param name="maxActiveRequests">Maximum active requests to consider idle.</param>
    /// <param name="idleDurationMs">Duration to check for idle state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if network is idle, false otherwise.</returns>
    Task<bool> IsNetworkIdleAsync(
        int maxActiveRequests = 0,
        int idleDurationMs = 500,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active network requests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of active requests.</returns>
    Task<int> GetActiveRequestCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if loading indicators/spinners are hidden.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all loaders are hidden, false otherwise.</returns>
    Task<bool> AreLoadersHiddenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects visible loading indicators on the page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of selectors for visible loaders.</returns>
    Task<List<string>> DetectLoadersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if JavaScript execution is idle (no pending microtasks).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if JavaScript is idle, false otherwise.</returns>
    Task<bool> IsJavaScriptIdleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all images are loaded.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all images are loaded, false otherwise.</returns>
    Task<bool> AreImagesLoadedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all web fonts are loaded.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all fonts are loaded, false otherwise.</returns>
    Task<bool> AreFontsLoadedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive stability metrics for the page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete stability metrics.</returns>
    Task<StabilityMetrics> GetStabilityMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until the page reaches a stable state.
    /// </summary>
    /// <param name="maxWaitMs">Maximum time to wait in milliseconds.</param>
    /// <param name="checkIntervalMs">Interval to check stability in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if page became stable, false if timeout occurred.</returns>
    Task<bool> WaitForStabilityAsync(
        int maxWaitMs = 10000,
        int checkIntervalMs = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts monitoring page stability in the background.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops background monitoring of page stability.
    /// </summary>
    Task StopMonitoringAsync();

    /// <summary>
    /// Gets whether monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Gets the most recent stability metrics from monitoring.
    /// </summary>
    StabilityMetrics? CurrentMetrics { get; }
}
