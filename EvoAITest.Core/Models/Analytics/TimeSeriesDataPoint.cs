using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Time series data point for historical trend analysis.
/// Captures aggregated metrics at specific time intervals for charting and analysis.
/// </summary>
[Table("TimeSeriesData")]
[Index(nameof(Timestamp))]
[Index(nameof(Interval))]
[Index(nameof(MetricType))]
public sealed class TimeSeriesDataPoint
{
    /// <summary>
    /// Gets or sets the unique identifier for this data point.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the timestamp for this data point.
    /// </summary>
    [Required]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the time interval this data point represents.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public TimeInterval Interval { get; set; }

    /// <summary>
    /// Gets or sets the type of metric this data point represents.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public MetricType MetricType { get; set; }

    /// <summary>
    /// Gets or sets the numeric value for this metric.
    /// </summary>
    [Required]
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions in this time period.
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets or sets the success rate for this period (0-100).
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average duration in milliseconds for this period.
    /// </summary>
    public long AverageDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the minimum duration in milliseconds.
    /// </summary>
    public long MinDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration in milliseconds.
    /// </summary>
    public long MaxDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the task ID if this data point is task-specific (null for aggregate).
    /// </summary>
    public Guid? TaskId { get; set; }

    /// <summary>
    /// Gets or sets additional aggregated data as JSON.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? AggregatedData { get; set; }

    /// <summary>
    /// Gets or sets when this data point was calculated.
    /// </summary>
    [Required]
    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Time interval for data aggregation.
/// </summary>
public enum TimeInterval
{
    /// <summary>
    /// Per-minute interval.
    /// </summary>
    Minute,

    /// <summary>
    /// Five-minute interval.
    /// </summary>
    FiveMinutes,

    /// <summary>
    /// Fifteen-minute interval.
    /// </summary>
    FifteenMinutes,

    /// <summary>
    /// Per-hour interval.
    /// </summary>
    Hour,

    /// <summary>
    /// Per-day interval.
    /// </summary>
    Day,

    /// <summary>
    /// Per-week interval.
    /// </summary>
    Week,

    /// <summary>
    /// Per-month interval.
    /// </summary>
    Month
}

/// <summary>
/// Type of metric being tracked.
/// </summary>
public enum MetricType
{
    /// <summary>
    /// Total execution count.
    /// </summary>
    ExecutionCount,

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    SuccessRate,

    /// <summary>
    /// Failure rate percentage.
    /// </summary>
    FailureRate,

    /// <summary>
    /// Average execution duration.
    /// </summary>
    AverageDuration,

    /// <summary>
    /// Throughput (executions per time unit).
    /// </summary>
    Throughput,

    /// <summary>
    /// Error rate.
    /// </summary>
    ErrorRate,

    /// <summary>
    /// Active execution count.
    /// </summary>
    ActiveExecutions,

    /// <summary>
    /// Healing success rate.
    /// </summary>
    HealingSuccessRate,

    /// <summary>
    /// Retry count.
    /// </summary>
    RetryCount,

    /// <summary>
    /// Custom metric.
    /// </summary>
    Custom
}

/// <summary>
/// Extension methods for time series data.
/// </summary>
public static class TimeSeriesExtensions
{
    /// <summary>
    /// Gets the duration in TimeSpan for a given interval.
    /// </summary>
    public static TimeSpan GetDuration(this TimeInterval interval)
    {
        return interval switch
        {
            TimeInterval.Minute => TimeSpan.FromMinutes(1),
            TimeInterval.FiveMinutes => TimeSpan.FromMinutes(5),
            TimeInterval.FifteenMinutes => TimeSpan.FromMinutes(15),
            TimeInterval.Hour => TimeSpan.FromHours(1),
            TimeInterval.Day => TimeSpan.FromDays(1),
            TimeInterval.Week => TimeSpan.FromDays(7),
            TimeInterval.Month => TimeSpan.FromDays(30),
            _ => TimeSpan.FromMinutes(1)
        };
    }

    /// <summary>
    /// Rounds a timestamp to the nearest interval boundary.
    /// </summary>
    public static DateTimeOffset RoundToInterval(this DateTimeOffset timestamp, TimeInterval interval)
    {
        return interval switch
        {
            TimeInterval.Minute => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, 
                timestamp.Hour, timestamp.Minute, 0, timestamp.Offset),
            TimeInterval.FiveMinutes => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day,
                timestamp.Hour, (timestamp.Minute / 5) * 5, 0, timestamp.Offset),
            TimeInterval.FifteenMinutes => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day,
                timestamp.Hour, (timestamp.Minute / 15) * 15, 0, timestamp.Offset),
            TimeInterval.Hour => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day,
                timestamp.Hour, 0, 0, timestamp.Offset),
            TimeInterval.Day => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day,
                0, 0, 0, timestamp.Offset),
            TimeInterval.Week => new DateTimeOffset(
                timestamp.AddDays(-(int)timestamp.DayOfWeek).Date,
                timestamp.Offset),
            TimeInterval.Month => new DateTimeOffset(timestamp.Year, timestamp.Month, 1,
                0, 0, 0, timestamp.Offset),
            _ => timestamp
        };
    }
}
