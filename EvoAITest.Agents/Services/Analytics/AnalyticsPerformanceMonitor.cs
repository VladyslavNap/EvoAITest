using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Agents.Services.Analytics;

/// <summary>
/// Service for monitoring analytics performance metrics
/// </summary>
public sealed class AnalyticsPerformanceMonitor
{
    private readonly ILogger<AnalyticsPerformanceMonitor> _logger;
    private readonly Dictionary<string, PerformanceMetric> _metrics = new();
    private readonly object _lock = new();

    public AnalyticsPerformanceMonitor(ILogger<AnalyticsPerformanceMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Measures execution time of an operation
    /// </summary>
    public async Task<T> MeasureAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        bool logWarningIfSlow = true,
        int warningThresholdMs = 1000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            stopwatch.Stop();
            
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, true);
            
            if (logWarningIfSlow && stopwatch.ElapsedMilliseconds > warningThresholdMs)
            {
                _logger.LogWarning(
                    "Slow operation detected: {Operation} took {Duration}ms (threshold: {Threshold}ms)",
                    operationName,
                    stopwatch.ElapsedMilliseconds,
                    warningThresholdMs);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordMetric(operationName, stopwatch.ElapsedMilliseconds, false);
            _logger.LogError(ex, "Operation failed: {Operation} after {Duration}ms", operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Records a performance metric
    /// </summary>
    private void RecordMetric(string operationName, long durationMs, bool success)
    {
        lock (_lock)
        {
            if (!_metrics.ContainsKey(operationName))
            {
                _metrics[operationName] = new PerformanceMetric { OperationName = operationName };
            }

            var metric = _metrics[operationName];
            metric.ExecutionCount++;
            metric.TotalDurationMs += durationMs;
            metric.AverageDurationMs = metric.TotalDurationMs / metric.ExecutionCount;
            
            if (durationMs < metric.MinDurationMs || metric.MinDurationMs == 0)
            {
                metric.MinDurationMs = durationMs;
            }
            
            if (durationMs > metric.MaxDurationMs)
            {
                metric.MaxDurationMs = durationMs;
            }

            if (success)
            {
                metric.SuccessCount++;
            }
            else
            {
                metric.FailureCount++;
            }

            metric.LastExecutionTime = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Gets performance metrics for an operation
    /// </summary>
    public PerformanceMetric? GetMetric(string operationName)
    {
        lock (_lock)
        {
            return _metrics.TryGetValue(operationName, out var metric) ? metric : null;
        }
    }

    /// <summary>
    /// Gets all performance metrics
    /// </summary>
    public List<PerformanceMetric> GetAllMetrics()
    {
        lock (_lock)
        {
            return _metrics.Values.ToList();
        }
    }

    /// <summary>
    /// Clears all metrics
    /// </summary>
    public void ClearMetrics()
    {
        lock (_lock)
        {
            _metrics.Clear();
            _logger.LogInformation("All performance metrics cleared");
        }
    }

    /// <summary>
    /// Gets slowest operations
    /// </summary>
    public List<PerformanceMetric> GetSlowestOperations(int count = 10)
    {
        lock (_lock)
        {
            return _metrics.Values
                .OrderByDescending(m => m.AverageDurationMs)
                .Take(count)
                .ToList();
        }
    }

    /// <summary>
    /// Gets most frequently executed operations
    /// </summary>
    public List<PerformanceMetric> GetMostFrequentOperations(int count = 10)
    {
        lock (_lock)
        {
            return _metrics.Values
                .OrderByDescending(m => m.ExecutionCount)
                .Take(count)
                .ToList();
        }
    }
}

/// <summary>
/// Performance metric for an operation
/// </summary>
public sealed class PerformanceMetric
{
    public string OperationName { get; set; } = "";
    public long ExecutionCount { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public long TotalDurationMs { get; set; }
    public long AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public DateTimeOffset LastExecutionTime { get; set; }
    public double SuccessRate => ExecutionCount > 0 ? (SuccessCount / (double)ExecutionCount) * 100 : 0;
}
