using EvoAITest.Core.Models.SmartWaiting;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for intelligent, adaptive waiting strategies that reduce test flakiness.
/// </summary>
public interface ISmartWaitService
{
    /// <summary>
    /// Waits for the page to reach a stable state based on multiple conditions.
    /// </summary>
    /// <param name="conditions">The conditions to wait for.</param>
    /// <param name="maxWaitMs">Maximum time to wait in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if conditions were met, false if timeout occurred.</returns>
    /// <example>
    /// <code>
    /// var conditions = WaitConditions.ForStability();
    /// var success = await smartWait.WaitForStableStateAsync(conditions);
    /// if (success)
    /// {
    ///     // Page is stable, proceed with actions
    /// }
    /// </code>
    /// </example>
    Task<bool> WaitForStableStateAsync(
        WaitConditions conditions,
        int? maxWaitMs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for a custom condition to become true.
    /// </summary>
    /// <param name="predicate">The condition to check.</param>
    /// <param name="timeout">Optional timeout (uses default if null).</param>
    /// <param name="pollingInterval">Optional polling interval (uses default if null).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if condition was met, false if timeout occurred.</returns>
    /// <example>
    /// <code>
    /// var success = await smartWait.WaitForConditionAsync(
    ///     async () => await page.IsVisibleAsync("#result"),
    ///     timeout: TimeSpan.FromSeconds(5));
    /// </code>
    /// </example>
    Task<bool> WaitForConditionAsync(
        Func<Task<bool>> predicate,
        TimeSpan? timeout = null,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for network activity to become idle.
    /// </summary>
    /// <param name="maxActiveRequests">Maximum number of active requests to consider as idle.</param>
    /// <param name="idleDurationMs">Duration network must be idle in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <example>
    /// <code>
    /// // Wait for no active requests for 500ms
    /// await smartWait.WaitForNetworkIdleAsync(maxActiveRequests: 0, idleDurationMs: 500);
    /// </code>
    /// </example>
    Task WaitForNetworkIdleAsync(
        int maxActiveRequests = 0,
        int idleDurationMs = 500,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for CSS animations to complete.
    /// </summary>
    /// <param name="selector">Optional selector to check animations for specific element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <example>
    /// <code>
    /// // Wait for all animations
    /// await smartWait.WaitForAnimationsAsync();
    /// 
    /// // Wait for animations on specific element
    /// await smartWait.WaitForAnimationsAsync("#modal");
    /// </code>
    /// </example>
    Task WaitForAnimationsAsync(
        string? selector = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates an optimal timeout based on historical data.
    /// </summary>
    /// <param name="action">The action being performed.</param>
    /// <param name="history">Historical wait time data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recommended timeout duration.</returns>
    /// <example>
    /// <code>
    /// var history = await GetHistoricalData("login");
    /// var timeout = await smartWait.CalculateOptimalTimeoutAsync("login", history);
    /// </code>
    /// </example>
    Task<TimeSpan> CalculateOptimalTimeoutAsync(
        string action,
        HistoricalData history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for page load completion (load event + DOM content loaded).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WaitForPageLoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for loading spinners/indicators to disappear.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <example>
    /// <code>
    /// await smartWait.WaitForLoadersHiddenAsync();
    /// </code>
    /// </example>
    Task WaitForLoadersHiddenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current stability metrics for the page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current stability metrics.</returns>
    Task<StabilityMetrics> GetStabilityMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records wait time for a specific action to improve future adaptive timeouts.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="actualWaitMs">Actual time waited in milliseconds.</param>
    /// <param name="success">Whether the wait was successful.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordWaitTimeAsync(
        string action,
        int actualWaitMs,
        bool success,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical wait time data for a specific action.
    /// </summary>
    /// <param name="action">The action to get history for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Historical data or null if no history exists.</returns>
    Task<HistoricalData?> GetHistoricalDataAsync(
        string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default wait strategy configured for this service.
    /// </summary>
    WaitStrategy DefaultStrategy { get; }

    /// <summary>
    /// Gets the default maximum wait time in milliseconds.
    /// </summary>
    int DefaultMaxWaitMs { get; }

    /// <summary>
    /// Gets the default polling interval in milliseconds.
    /// </summary>
    int DefaultPollingIntervalMs { get; }
}
