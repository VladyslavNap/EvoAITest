namespace EvoAITest.Core.Models.SmartWaiting;

/// <summary>
/// Historical wait time data used for adaptive timeout calculation.
/// </summary>
public sealed class HistoricalData
{
    /// <summary>
    /// Default timeout in milliseconds when no historical data is available.
    /// </summary>
    public const int DefaultTimeoutMs = 10000;

    /// <summary>
    /// Minimum allowed timeout in milliseconds.
    /// </summary>
    public const int MinimumTimeoutMs = 1000;

    /// <summary>
    /// Maximum allowed timeout in milliseconds.
    /// </summary>
    public const int MaximumTimeoutMs = 60000;

    /// <summary>
    /// Maximum number of samples to keep in history to prevent memory issues.
    /// </summary>
    public const int MaxSampleCount = 100;

    /// <summary>
    /// Gets the action or operation being waited for.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the list of historical wait times in milliseconds.
    /// </summary>
    public required List<int> WaitTimesMs { get; init; }

    /// <summary>
    /// Gets the success rate of this action (0.0 to 1.0).
    /// </summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// Gets the average wait time in milliseconds.
    /// </summary>
    public double AverageWaitMs => WaitTimesMs.Count > 0 ? WaitTimesMs.Average() : 0;

    /// <summary>
    /// Gets the median wait time in milliseconds.
    /// </summary>
    public double MedianWaitMs
    {
        get
        {
            if (WaitTimesMs.Count == 0) return 0;

            var sorted = WaitTimesMs.OrderBy(x => x).ToList();
            var mid = sorted.Count / 2;

            return sorted.Count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];
        }
    }

    /// <summary>
    /// Gets the 95th percentile wait time in milliseconds.
    /// </summary>
    public double Percentile95Ms => CalculatePercentile(0.95);

    /// <summary>
    /// Gets the 99th percentile wait time in milliseconds.
    /// </summary>
    public double Percentile99Ms => CalculatePercentile(0.99);

    /// <summary>
    /// Gets the minimum wait time in milliseconds.
    /// </summary>
    public int MinWaitMs => WaitTimesMs.Count > 0 ? WaitTimesMs.Min() : 0;

    /// <summary>
    /// Gets the maximum wait time in milliseconds.
    /// </summary>
    public int MaxWaitMs => WaitTimesMs.Count > 0 ? WaitTimesMs.Max() : 0;

    /// <summary>
    /// Gets the standard deviation of wait times.
    /// </summary>
    public double StandardDeviation
    {
        get
        {
            if (WaitTimesMs.Count == 0) return 0;

            var avg = AverageWaitMs;
            var sumOfSquares = WaitTimesMs.Sum(x => Math.Pow(x - avg, 2));
            return Math.Sqrt(sumOfSquares / WaitTimesMs.Count);
        }
    }

    /// <summary>
    /// Gets the number of data points.
    /// </summary>
    public int SampleCount => WaitTimesMs.Count;

    /// <summary>
    /// Gets when this data was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets additional metadata about the historical data.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Calculates the percentile value.
    /// </summary>
    private double CalculatePercentile(double percentile)
    {
        if (WaitTimesMs.Count == 0) return 0;

        var sorted = WaitTimesMs.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Count - 1));

        return sorted[index];
    }

    /// <summary>
    /// Calculates an adaptive timeout based on historical data.
    /// </summary>
    /// <param name="strategy">The strategy to use for calculation.</param>
    /// <param name="safetyMultiplier">Multiplier for safety margin (e.g., 1.5 = 50% buffer).</param>
    /// <returns>Recommended timeout in milliseconds.</returns>
    public int CalculateAdaptiveTimeout(WaitStrategy strategy = WaitStrategy.Percentile, double safetyMultiplier = 1.5)
    {
        if (WaitTimesMs.Count == 0)
            return DefaultTimeoutMs;

        var baseTimeout = strategy switch
        {
            WaitStrategy.Fixed => MaxWaitMs,
            WaitStrategy.Adaptive => (int)(AverageWaitMs + StandardDeviation),
            WaitStrategy.Percentile => (int)Percentile95Ms,
            _ => (int)AverageWaitMs
        };

        var timeout = (int)(baseTimeout * safetyMultiplier);

        // Ensure reasonable bounds
        return Math.Max(MinimumTimeoutMs, Math.Min(timeout, MaximumTimeoutMs));
    }

    /// <summary>
    /// Default minimum number of samples required to consider data sufficient.
    /// </summary>
    public const int MinimumSamplesForSufficientData = 10;

    /// <summary>
    /// Determines if there is enough data for reliable adaptive timeouts.
    /// </summary>
    public bool HasSufficientData(int minSamples = MinimumSamplesForSufficientData)
    {
        return SampleCount >= minSamples;
    }

    /// <summary>
    /// Creates empty historical data.
    /// </summary>
    public static HistoricalData CreateEmpty(string action)
    {
        return new HistoricalData
        {
            Action = action,
            WaitTimesMs = new List<int>(),
            SuccessRate = 1.0
        };
    }

    /// <summary>
    /// Creates historical data from a list of wait times.
    /// </summary>
    public static HistoricalData FromWaitTimes(string action, List<int> waitTimes, double successRate = 1.0)
    {
        return new HistoricalData
        {
            Action = action,
            WaitTimesMs = new List<int>(waitTimes),
            SuccessRate = successRate
        };
    }

    /// <summary>
    /// Adds a new wait time to the historical data.
    /// </summary>
    public HistoricalData WithNewWaitTime(int waitTimeMs, bool success = true)
    {
        var newWaitTimes = new List<int>(WaitTimesMs) { waitTimeMs };
        
        // Keep only last MaxSampleCount samples to prevent memory issues
        if (newWaitTimes.Count > MaxSampleCount)
        {
            newWaitTimes = newWaitTimes.Skip(newWaitTimes.Count - MaxSampleCount).ToList();
        }

        var totalAttempts = SampleCount + 1;
        var successfulAttempts = (int)(SuccessRate * SampleCount) + (success ? 1 : 0);
        var newSuccessRate = (double)successfulAttempts / totalAttempts;

        return new HistoricalData
        {
            Action = Action,
            WaitTimesMs = newWaitTimes,
            SuccessRate = newSuccessRate,
            LastUpdated = DateTimeOffset.UtcNow,
            Metadata = Metadata
        };
    }
}
