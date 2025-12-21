namespace EvoAITest.Core.Models.SmartWaiting;

/// <summary>
/// Defines one or more conditions to wait for before proceeding.
/// </summary>
public sealed class WaitConditions
{
    /// <summary>
    /// Gets the list of condition types to wait for.
    /// </summary>
    public required List<WaitConditionType> Conditions { get; init; }

    /// <summary>
    /// Gets whether all conditions must be met (AND) or any condition (OR).
    /// Default is true (all conditions must be met).
    /// </summary>
    public bool RequireAll { get; init; } = true;

    /// <summary>
    /// Gets the maximum time to wait in milliseconds.
    /// </summary>
    public int MaxWaitMs { get; init; } = 10000;

    /// <summary>
    /// Gets the polling interval in milliseconds for checking conditions.
    /// </summary>
    public int PollingIntervalMs { get; init; } = 100;

    /// <summary>
    /// Gets the wait strategy to use.
    /// </summary>
    public WaitStrategy Strategy { get; init; } = WaitStrategy.Fixed;

    /// <summary>
    /// Gets custom predicate function for CustomPredicate condition type.
    /// </summary>
    public Func<Task<bool>>? CustomPredicate { get; init; }

    /// <summary>
    /// Gets the selector to wait for (for element-specific conditions).
    /// </summary>
    public string? Selector { get; init; }

    /// <summary>
    /// Gets network idle settings (max active requests and idle duration).
    /// </summary>
    public NetworkIdleSettings? NetworkIdleSettings { get; init; }

    /// <summary>
    /// Gets whether to throw on timeout or return false.
    /// </summary>
    public bool ThrowOnTimeout { get; init; } = true;

    /// <summary>
    /// Gets additional metadata for the wait operation.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates wait conditions for network idle.
    /// </summary>
    public static WaitConditions ForNetworkIdle(int maxActiveRequests = 0, int idleDurationMs = 500)
    {
        return new WaitConditions
        {
            Conditions = new List<WaitConditionType> { WaitConditionType.NetworkIdle },
            NetworkIdleSettings = new NetworkIdleSettings
            {
                MaxActiveRequests = maxActiveRequests,
                IdleDurationMs = idleDurationMs
            }
        };
    }

    /// <summary>
    /// Creates wait conditions for page stability (DOM + animations).
    /// </summary>
    public static WaitConditions ForStability()
    {
        return new WaitConditions
        {
            Conditions = new List<WaitConditionType>
            {
                WaitConditionType.DomStable,
                WaitConditionType.AnimationsComplete,
                WaitConditionType.LoadersHidden
            },
            RequireAll = true
        };
    }

    /// <summary>
    /// Creates wait conditions for animations only.
    /// </summary>
    public static WaitConditions ForAnimations(string? selector = null)
    {
        return new WaitConditions
        {
            Conditions = new List<WaitConditionType> { WaitConditionType.AnimationsComplete },
            Selector = selector
        };
    }

    /// <summary>
    /// Creates wait conditions for page load.
    /// </summary>
    public static WaitConditions ForPageLoad()
    {
        return new WaitConditions
        {
            Conditions = new List<WaitConditionType>
            {
                WaitConditionType.PageLoad,
                WaitConditionType.DomContentLoaded
            },
            RequireAll = true
        };
    }

    /// <summary>
    /// Creates wait conditions with custom predicate.
    /// </summary>
    public static WaitConditions ForCustom(Func<Task<bool>> predicate, int maxWaitMs = 10000)
    {
        return new WaitConditions
        {
            Conditions = new List<WaitConditionType> { WaitConditionType.CustomPredicate },
            CustomPredicate = predicate,
            MaxWaitMs = maxWaitMs
        };
    }
}

/// <summary>
/// Settings for network idle detection.
/// </summary>
public sealed class NetworkIdleSettings
{
    /// <summary>
    /// Gets the maximum number of active network requests to consider as idle.
    /// 0 means no active requests.
    /// </summary>
    public int MaxActiveRequests { get; init; } = 0;

    /// <summary>
    /// Gets the duration in milliseconds that network must be idle.
    /// </summary>
    public int IdleDurationMs { get; init; } = 500;

    /// <summary>
    /// Gets whether to ignore certain URL patterns (e.g., analytics).
    /// </summary>
    public List<string>? IgnoreUrlPatterns { get; init; }
}
