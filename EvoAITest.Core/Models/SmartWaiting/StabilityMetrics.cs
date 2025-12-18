namespace EvoAITest.Core.Models.SmartWaiting;

/// <summary>
/// Metrics indicating the stability of a page.
/// </summary>
public sealed record StabilityMetrics
{
    /// <summary>
    /// Threshold for DOM mutations in stability score calculation.
    /// A page with more mutations relative to this threshold receives a lower stability score.
    /// </summary>
    private const double DomMutationThreshold = 100.0;

    /// <summary>
    /// Threshold for active animations in stability score calculation.
    /// A page with more active animations relative to this threshold receives a lower stability score.
    /// </summary>
    private const double AnimationCountThreshold = 10.0;

    /// <summary>
    /// Threshold for active requests in stability score calculation.
    /// A page with more active requests relative to this threshold receives a lower stability score.
    /// </summary>
    private const double ActiveRequestThreshold = 5.0;

    /// <summary>
    /// Threshold for visible loaders in stability score calculation.
    /// A page with more visible loaders relative to this threshold receives a lower stability score.
    /// </summary>
    private const double VisibleLoaderThreshold = 3.0;

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
    /// The score is computed as the average of seven individual metric scores, each ranging from 0.0 to 1.0:
    /// 1. DOM Stability: 1.0 if stable, otherwise scaled by mutation count relative to threshold (100 mutations)
    /// 2. Animation Completion: 1.0 if complete, otherwise scaled by active animations relative to threshold (10 animations)
    /// 3. Network Idle: 1.0 if idle, otherwise scaled by active requests relative to threshold (5 requests)
    /// 4. Loader Visibility: 1.0 if hidden, otherwise scaled by visible loaders relative to threshold (3 loaders)
    /// 5. JavaScript Idle: 1.0 if idle, 0.0 otherwise
    /// 6. Images Loaded: 1.0 if loaded, 0.0 otherwise
    /// 7. Fonts Loaded: 1.0 if loaded, 0.0 otherwise
    /// </summary>
    /// <param name="metrics">The stability metrics to calculate the score from.</param>
    /// <returns>A score from 0.0 (completely unstable) to 1.0 (perfectly stable).</returns>
    public static double CalculateStabilityScore(StabilityMetrics metrics)
    {
        var scores = new List<double>();

        if (metrics.IsDomStable) scores.Add(1.0);
        else scores.Add(Math.Max(0, 1.0 - (metrics.DomMutationCount / DomMutationThreshold)));

        if (metrics.AreAnimationsComplete) scores.Add(1.0);
        else scores.Add(Math.Max(0, 1.0 - (metrics.ActiveAnimationCount / AnimationCountThreshold)));

        if (metrics.IsNetworkIdle) scores.Add(1.0);
        else scores.Add(Math.Max(0, 1.0 - (metrics.ActiveRequestCount / ActiveRequestThreshold)));

        if (metrics.AreLoadersHidden) scores.Add(1.0);
        else scores.Add(Math.Max(0, 1.0 - (metrics.VisibleLoaderCount / VisibleLoaderThreshold)));

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
