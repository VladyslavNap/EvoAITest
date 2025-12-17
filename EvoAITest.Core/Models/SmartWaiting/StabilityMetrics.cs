namespace EvoAITest.Core.Models.SmartWaiting;

/// <summary>
/// Metrics indicating the stability of a page.
/// </summary>
public sealed record StabilityMetrics
{
    /// <summary>
    /// Gets whether the DOM is stable (no mutations for a period).
    /// </summary>
    public required bool IsDomStable { get; init; }

    /// <summary>
    /// Gets the number of DOM mutations in the last check period.
    /// </summary>
    public int DomMutationCount { get; init; }

    /// <summary>
    /// Gets whether animations are complete.
    /// </summary>
    public required bool AreAnimationsComplete { get; init; }

    /// <summary>
    /// Gets the number of active animations.
    /// </summary>
    public int ActiveAnimationCount { get; init; }

    /// <summary>
    /// Gets whether the network is idle.
    /// </summary>
    public required bool IsNetworkIdle { get; init; }

    /// <summary>
    /// Gets the number of active network requests.
    /// </summary>
    public int ActiveRequestCount { get; init; }

    /// <summary>
    /// Gets whether loading indicators are hidden.
    /// </summary>
    public required bool AreLoadersHidden { get; init; }

    /// <summary>
    /// Gets the number of visible loading indicators.
    /// </summary>
    public int VisibleLoaderCount { get; init; }

    /// <summary>
    /// Gets whether JavaScript is idle (no pending tasks).
    /// </summary>
    public bool IsJavaScriptIdle { get; init; } = true;

    /// <summary>
    /// Gets whether all images are loaded.
    /// </summary>
    public bool AreImagesLoaded { get; init; } = true;

    /// <summary>
    /// Gets whether all fonts are loaded.
    /// </summary>
    public bool AreFontsLoaded { get; init; } = true;

    /// <summary>
    /// Gets the overall stability score (0.0 to 1.0).
    /// </summary>
    public double StabilityScore { get; init; }

    /// <summary>
    /// Gets when these metrics were captured.
    /// </summary>
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets additional diagnostic information.
    /// </summary>
    public Dictionary<string, object>? Diagnostics { get; init; }

    /// <summary>
    /// Determines if the page is considered stable based on all metrics.
    /// </summary>
    public bool IsStable()
    {
        return IsDomStable &&
               AreAnimationsComplete &&
               IsNetworkIdle &&
               AreLoadersHidden &&
               IsJavaScriptIdle;
    }

    /// <summary>
    /// Calculates the overall stability score based on individual metrics.
    /// </summary>
    public static double CalculateStabilityScore(StabilityMetrics metrics)
    {
        var scores = new List<double>();

        if (metrics.IsDomStable) scores.Add(1.0);
        else scores.Add(Math.Max(0, 1.0 - (metrics.DomMutationCount / 100.0)));

        if (metrics.AreAnimationsComplete) scores.Add(1.0);
        else scores.Add(Math.Max(0, 1.0 - (metrics.ActiveAnimationCount / 10.0)));

        if (metrics.IsNetworkIdle) scores.Add(1.0);
        else scores.Add(Math.Max(0, 1.0 - (metrics.ActiveRequestCount / 5.0)));

        if (metrics.AreLoadersHidden) scores.Add(1.0);
        else scores.Add(Math.Max(0, 1.0 - (metrics.VisibleLoaderCount / 3.0)));

        if (metrics.IsJavaScriptIdle) scores.Add(1.0);
        if (metrics.AreImagesLoaded) scores.Add(1.0);
        if (metrics.AreFontsLoaded) scores.Add(1.0);

        return scores.Average();
    }

    /// <summary>
    /// Creates stability metrics indicating a stable page.
    /// </summary>
    public static StabilityMetrics CreateStable()
    {
        return new StabilityMetrics
        {
            IsDomStable = true,
            AreAnimationsComplete = true,
            IsNetworkIdle = true,
            AreLoadersHidden = true,
            IsJavaScriptIdle = true,
            AreImagesLoaded = true,
            AreFontsLoaded = true,
            StabilityScore = 1.0
        };
    }

    /// <summary>
    /// Creates stability metrics indicating an unstable page.
    /// </summary>
    public static StabilityMetrics CreateUnstable(
        int domMutations = 0,
        int activeAnimations = 0,
        int activeRequests = 0,
        int visibleLoaders = 0)
    {
        var metrics = new StabilityMetrics
        {
            IsDomStable = domMutations == 0,
            DomMutationCount = domMutations,
            AreAnimationsComplete = activeAnimations == 0,
            ActiveAnimationCount = activeAnimations,
            IsNetworkIdle = activeRequests == 0,
            ActiveRequestCount = activeRequests,
            AreLoadersHidden = visibleLoaders == 0,
            VisibleLoaderCount = visibleLoaders
        };

        return metrics with { StabilityScore = CalculateStabilityScore(metrics) };
    }
}
