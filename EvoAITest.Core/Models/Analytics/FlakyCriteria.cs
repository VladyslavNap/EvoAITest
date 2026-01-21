namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Configuration for flaky test detection criteria
/// </summary>
public sealed class FlakyCriteria
{
    /// <summary>
    /// Minimum number of executions required to assess flakiness
    /// </summary>
    public int MinimumExecutions { get; set; } = 10;

    /// <summary>
    /// Minimum pass rate below which test is considered flaky (0-100)
    /// </summary>
    public double MinimumPassRate { get; set; } = 85.0;

    /// <summary>
    /// Maximum acceptable coefficient of variation for execution duration
    /// </summary>
    public double MaxDurationVariability { get; set; } = 0.5;

    /// <summary>
    /// Number of retry passes that indicate flakiness
    /// </summary>
    public int FlakyRetryThreshold { get; set; } = 2;

    /// <summary>
    /// Time window in days to consider for recent behavior analysis
    /// </summary>
    public int RecentWindowDays { get; set; } = 30;

    /// <summary>
    /// Minimum flakiness score to classify as flaky (0-100)
    /// </summary>
    public double FlakyScoreThreshold { get; set; } = 20.0;

    /// <summary>
    /// Minimum confidence level required for pattern detection (0-100)
    /// </summary>
    public double MinimumPatternConfidence { get; set; } = 70.0;

    /// <summary>
    /// Number of consecutive failures before marking as consistently failing
    /// </summary>
    public int ConsistentFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Maximum acceptable standard deviation for execution duration (ms)
    /// </summary>
    public long MaxDurationStandardDeviation { get; set; } = 5000;

    /// <summary>
    /// Whether to consider environmental factors in flakiness detection
    /// </summary>
    public bool ConsiderEnvironmentalFactors { get; set; } = true;

    /// <summary>
    /// Whether to analyze temporal patterns (time-based failures)
    /// </summary>
    public bool AnalyzeTemporalPatterns { get; set; } = true;

    /// <summary>
    /// Whether to detect sequential/order-dependent failures
    /// </summary>
    public bool DetectSequentialPatterns { get; set; } = true;

    /// <summary>
    /// Minimum number of occurrences to establish a pattern
    /// </summary>
    public int MinimumPatternOccurrences { get; set; } = 3;

    /// <summary>
    /// Weight for recent failures vs historical failures (0-1)
    /// </summary>
    public double RecencyWeight { get; set; } = 0.7;

    /// <summary>
    /// Creates default criteria configuration
    /// </summary>
    public static FlakyCriteria Default => new();

    /// <summary>
    /// Creates strict criteria (more sensitive to flakiness)
    /// </summary>
    public static FlakyCriteria Strict => new()
    {
        MinimumPassRate = 95.0,
        FlakyScoreThreshold = 10.0,
        FlakyRetryThreshold = 1,
        MaxDurationVariability = 0.3,
        ConsistentFailureThreshold = 3
    };

    /// <summary>
    /// Creates lenient criteria (less sensitive to flakiness)
    /// </summary>
    public static FlakyCriteria Lenient => new()
    {
        MinimumPassRate = 75.0,
        FlakyScoreThreshold = 35.0,
        FlakyRetryThreshold = 4,
        MaxDurationVariability = 0.8,
        ConsistentFailureThreshold = 8
    };

    /// <summary>
    /// Validates that the criteria configuration is sensible
    /// </summary>
    public bool Validate()
    {
        return MinimumExecutions > 0
            && MinimumPassRate >= 0 && MinimumPassRate <= 100
            && FlakyScoreThreshold >= 0 && FlakyScoreThreshold <= 100
            && RecentWindowDays > 0
            && RecencyWeight >= 0 && RecencyWeight <= 1
            && MinimumPatternConfidence >= 0 && MinimumPatternConfidence <= 100;
    }
}
