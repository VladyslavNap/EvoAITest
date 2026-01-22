using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models.Analytics;
using EvoAITest.Core.Models.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Agents.Services.Analytics;

/// <summary>
/// Service for analyzing test execution data and generating analytics
/// </summary>
public sealed class TestAnalyticsService : ITestAnalyticsService
{
    private readonly ILogger<TestAnalyticsService> _logger;
    private readonly EvoAIDbContext _dbContext;
    private readonly IFlakyTestDetector _flakyDetector;

    public TestAnalyticsService(
        ILogger<TestAnalyticsService> logger,
        EvoAIDbContext dbContext,
        IFlakyTestDetector flakyDetector)
    {
        _logger = logger;
        _dbContext = dbContext;
        _flakyDetector = flakyDetector;
    }

    public async Task<DashboardStatistics> GetDashboardStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating dashboard statistics");

        // Get all execution results
        var allResults = await _dbContext.TestExecutionResults
            .OrderByDescending(r => r.StartedAt)
            .Take(10000) // Limit to recent results for performance
            .ToListAsync(cancellationToken);

        // Calculate time-based filters
        var now = DateTimeOffset.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        // Total statistics
        var totalExecutions = allResults.Count;
        var totalTests = allResults.Select(r => r.TestName).Distinct().Count();
        var totalRecordings = allResults.Select(r => r.RecordingSessionId).Distinct().Count();

        // Pass rate calculations
        var passedExecutions = allResults.Count(r => r.Status == TestExecutionStatus.Passed);
        var overallPassRate = totalExecutions > 0 ? (double)passedExecutions / totalExecutions * 100 : 0;

        // Time-based metrics
        var results24h = allResults.Where(r => r.StartedAt >= last24Hours).ToList();
        var results7d = allResults.Where(r => r.StartedAt >= last7Days).ToList();
        var results30d = allResults.Where(r => r.StartedAt >= last30Days).ToList();

        var passRate24h = results24h.Any()
            ? (double)results24h.Count(r => r.Status == TestExecutionStatus.Passed) / results24h.Count * 100
            : 0;

        var passRate7d = results7d.Any()
            ? (double)results7d.Count(r => r.Status == TestExecutionStatus.Passed) / results7d.Count * 100
            : 0;

        var passRate30d = results30d.Any()
            ? (double)results30d.Count(r => r.Status == TestExecutionStatus.Passed) / results30d.Count * 100
            : 0;

        // Flaky test count
        var flakyTests = await _flakyDetector.GetAllFlakyTestsAsync(cancellationToken: cancellationToken);
        var flakyTestCount = flakyTests.Count;
        var stableTestCount = totalTests - flakyTestCount;

        // Duration metrics
        var avgDuration = allResults.Any() ? (long)allResults.Average(r => r.DurationMs) : 0;
        var totalTimeHours = allResults.Sum(r => r.DurationMs) / 1000.0 / 3600.0;

        // Top lists
        var topExecuted = await GetMostExecutedTestsAsync(10, 30, cancellationToken);
        var topFailing = await GetTopFailingTestsAsync(10, 30, cancellationToken);
        var slowest = await GetSlowestTestsAsync(10, 30, cancellationToken);

        // Recent trends
        var recentTrends = await CalculateTrendsAsync(
            TrendInterval.Daily,
            last30Days,
            now,
            null,
            cancellationToken);

        var statistics = new DashboardStatistics
        {
            TotalExecutions = totalExecutions,
            TotalTests = totalTests,
            TotalRecordings = totalRecordings,
            OverallPassRate = overallPassRate,
            FlakyTestCount = flakyTestCount,
            StableTestCount = stableTestCount,
            AverageExecutionDurationMs = avgDuration,
            TotalExecutionTimeHours = totalTimeHours,
            ExecutionsLast24Hours = results24h.Count,
            ExecutionsLast7Days = results7d.Count,
            ExecutionsLast30Days = results30d.Count,
            PassRateLast24Hours = passRate24h,
            PassRateLast7Days = passRate7d,
            PassRateLast30Days = passRate30d,
            TopExecutedTests = topExecuted,
            TopFailingTests = topFailing,
            SlowestTests = slowest,
            RecentTrends = recentTrends
        };

        statistics.Health = DetermineHealth(statistics);

        _logger.LogInformation(
            "Dashboard statistics generated: {Tests} tests, {PassRate:F1}% pass rate, Health={Health}",
            totalTests,
            overallPassRate,
            statistics.Health);

        return statistics;
    }

    public async Task<List<TestTrend>> CalculateTrendsAsync(
        TrendInterval interval,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Guid? recordingSessionId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating {Interval} trends from {Start} to {End}",
            interval,
            startDate,
            endDate);

        // Get execution results in the time window
        var query = _dbContext.TestExecutionResults
            .Where(r => r.StartedAt >= startDate && r.StartedAt <= endDate);

        if (recordingSessionId.HasValue)
        {
            query = query.Where(r => r.RecordingSessionId == recordingSessionId.Value);
        }

        var results = await query.ToListAsync(cancellationToken);

        if (!results.Any())
        {
            return [];
        }

        // Group results by time interval
        var groupedResults = GroupByInterval(results, interval);

        var trends = new List<TestTrend>();

        foreach (var group in groupedResults)
        {
            var groupResults = group.Value;

            var totalExecutions = groupResults.Count;
            var passedExecutions = groupResults.Count(r => r.Status == TestExecutionStatus.Passed);
            var failedExecutions = groupResults.Count(r => r.Status == TestExecutionStatus.Failed);
            var skippedExecutions = groupResults.Count(r => r.Status == TestExecutionStatus.Skipped);

            var passRate = totalExecutions > 0
                ? (double)passedExecutions / totalExecutions * 100
                : 0;

            var durations = groupResults.Select(r => r.DurationMs).ToList();
            var avgDuration = durations.Any() ? (long)durations.Average() : 0;
            var minDuration = durations.Any() ? durations.Min() : 0;
            var maxDuration = durations.Any() ? durations.Max() : 0;
            var durationStdDev = CalculateStandardDeviation(durations.Select(d => (double)d));

            var uniqueTests = groupResults.Select(r => r.TestName).Distinct().Count();
            var compilationErrors = groupResults.Count(r => r.Status == TestExecutionStatus.CompilationError);

            var trend = new TestTrend
            {
                RecordingSessionId = recordingSessionId,
                Timestamp = group.Key,
                Interval = interval,
                TotalExecutions = totalExecutions,
                PassedExecutions = passedExecutions,
                FailedExecutions = failedExecutions,
                SkippedExecutions = skippedExecutions,
                PassRate = passRate,
                AverageDurationMs = avgDuration,
                MinDurationMs = minDuration,
                MaxDurationMs = maxDuration,
                DurationStdDev = (long)durationStdDev,
                FlakyTestCount = 0, // Calculated separately
                UniqueTestCount = uniqueTests,
                CompilationErrors = compilationErrors,
                RetriedTests = 0, // TODO: Track retries
                AverageStepsPerTest = groupResults.Average(r => r.TotalSteps)
            };

            trends.Add(trend);
        }

        _logger.LogInformation("Calculated {Count} trend data points", trends.Count);

        return trends;
    }

    public async Task<List<TestTrend>> GetRecordingTrendsAsync(
        Guid recordingSessionId,
        TrendInterval interval,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddDays(-days);

        return await CalculateTrendsAsync(
            interval,
            startDate,
            endDate,
            recordingSessionId,
            cancellationToken);
    }

    public async Task<RecordingInsights> GetRecordingInsightsAsync(
        Guid recordingSessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating insights for recording {RecordingId}", recordingSessionId);

        // Get execution results
        var results = await _dbContext.TestExecutionResults
            .Where(r => r.RecordingSessionId == recordingSessionId)
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(cancellationToken);

        if (!results.Any())
        {
            throw new InvalidOperationException($"No execution results found for recording {recordingSessionId}");
        }

        var testName = results.First().TestName;

        // Calculate pass rate
        var passedCount = results.Count(r => r.Status == TestExecutionStatus.Passed);
        var passRate = (double)passedCount / results.Count * 100;

        // Get flaky analysis
        var flakyAnalysis = await _flakyDetector.AnalyzeRecordingAsync(
            recordingSessionId,
            cancellationToken: cancellationToken);

        // Get stability metrics
        var stabilityMetrics = await _flakyDetector.CalculateStabilityMetricsAsync(
            recordingSessionId,
            30,
            cancellationToken);

        // Get recent trends
        var recentTrends = await GetRecordingTrendsAsync(
            recordingSessionId,
            TrendInterval.Daily,
            30,
            cancellationToken);

        // Calculate performance baseline
        var baselineDuration = (long)results.Average(r => r.DurationMs);

        // Check for performance degradation
        var recentResults = results.Take(results.Count / 4).ToList();
        var oldResults = results.Skip(results.Count * 3 / 4).ToList();

        var recentAvgDuration = recentResults.Any() ? recentResults.Average(r => r.DurationMs) : 0;
        var oldAvgDuration = oldResults.Any() ? oldResults.Average(r => r.DurationMs) : 0;

        var performanceDegrading = recentAvgDuration > oldAvgDuration * 1.2; // 20% slower

        // Generate recommendations
        var recommendations = new List<string>();
        var issues = new List<string>();

        if (flakyAnalysis.IsFlaky)
        {
            issues.Add($"Test is flaky with score {flakyAnalysis.FlakinessScore:F1}");
            recommendations.AddRange(flakyAnalysis.Recommendations);
        }

        if (performanceDegrading)
        {
            issues.Add($"Performance degrading: {oldAvgDuration:F0}ms → {recentAvgDuration:F0}ms");
            recommendations.Add("Investigate recent changes causing performance degradation");
        }

        if (passRate < 90)
        {
            issues.Add($"Low pass rate: {passRate:F1}%");
            recommendations.Add("Review test reliability and assertions");
        }

        if (stabilityMetrics.ConsecutiveFailureStreak >= 3)
        {
            issues.Add($"Test failing consistently ({stabilityMetrics.ConsecutiveFailureStreak} consecutive failures)");
            recommendations.Add("Investigate root cause of consistent failures");
        }

        // Calculate time-based statistics
        var now = DateTimeOffset.UtcNow;
        var last7Days = results.Where(r => r.StartedAt >= now.AddDays(-7)).ToList();
        var last30Days = results.Where(r => r.StartedAt >= now.AddDays(-30)).ToList();

        var insights = new RecordingInsights
        {
            RecordingSessionId = recordingSessionId,
            TestName = testName,
            PassRate = passRate,
            FlakyAnalysis = flakyAnalysis.IsFlaky ? flakyAnalysis : null,
            StabilityMetrics = stabilityMetrics,
            RecentTrends = recentTrends,
            BaselineDurationMs = baselineDuration,
            PerformanceDegrading = performanceDegrading,
            Recommendations = recommendations,
            Issues = issues,
            Statistics = new ExecutionStatisticsSummary
            {
                TotalExecutions = results.Count,
                Last7DaysExecutions = last7Days.Count,
                Last30DaysExecutions = last30Days.Count,
                AverageDurationMs = baselineDuration,
                FastestDurationMs = results.Min(r => r.DurationMs),
                SlowestDurationMs = results.Max(r => r.DurationMs)
            }
        };

        _logger.LogInformation(
            "Insights generated: {IssueCount} issues, {RecCount} recommendations",
            issues.Count,
            recommendations.Count);

        return insights;
    }

    public async Task<List<TestExecutionSummary>> GetTopFailingTestsAsync(
        int count = 10,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days);

        var summaries = await _dbContext.TestExecutionResults
            .Where(r => r.StartedAt >= cutoffDate)
            .GroupBy(r => new { r.RecordingSessionId, r.TestName })
            .Select(g => new
            {
                g.Key.RecordingSessionId,
                g.Key.TestName,
                TotalExecutions = g.Count(),
                FailedExecutions = g.Count(r => r.Status == TestExecutionStatus.Failed),
                PassedExecutions = g.Count(r => r.Status == TestExecutionStatus.Passed),
                AverageDuration = (long)g.Average(r => r.DurationMs),
                LastExecution = g.Max(r => r.StartedAt)
            })
            .Where(x => x.FailedExecutions > 0)
            .OrderByDescending(x => x.FailedExecutions)
            .ThenBy(x => (double)x.PassedExecutions / x.TotalExecutions)
            .Take(count)
            .ToListAsync(cancellationToken);

        return summaries.Select(s => new TestExecutionSummary
        {
            RecordingSessionId = s.RecordingSessionId,
            TestName = s.TestName,
            ExecutionCount = s.TotalExecutions,
            PassedCount = s.PassedExecutions,
            FailedCount = s.FailedExecutions,
            PassRate = s.TotalExecutions > 0 ? (double)s.PassedExecutions / s.TotalExecutions * 100 : 0,
            AverageDurationMs = s.AverageDuration,
            LastExecutedAt = s.LastExecution,
            FlakinessScore = 0, // Calculated separately if needed
            IsFlaky = false
        }).ToList();
    }

    public async Task<List<TestExecutionSummary>> GetSlowestTestsAsync(
        int count = 10,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days);

        var summaries = await _dbContext.TestExecutionResults
            .Where(r => r.StartedAt >= cutoffDate)
            .GroupBy(r => new { r.RecordingSessionId, r.TestName })
            .Select(g => new
            {
                g.Key.RecordingSessionId,
                g.Key.TestName,
                TotalExecutions = g.Count(),
                FailedExecutions = g.Count(r => r.Status == TestExecutionStatus.Failed),
                PassedExecutions = g.Count(r => r.Status == TestExecutionStatus.Passed),
                AverageDuration = (long)g.Average(r => r.DurationMs),
                LastExecution = g.Max(r => r.StartedAt)
            })
            .OrderByDescending(x => x.AverageDuration)
            .Take(count)
            .ToListAsync(cancellationToken);

        return summaries.Select(s => new TestExecutionSummary
        {
            RecordingSessionId = s.RecordingSessionId,
            TestName = s.TestName,
            ExecutionCount = s.TotalExecutions,
            PassedCount = s.PassedExecutions,
            FailedCount = s.FailedExecutions,
            PassRate = s.TotalExecutions > 0 ? (double)s.PassedExecutions / s.TotalExecutions * 100 : 0,
            AverageDurationMs = s.AverageDuration,
            LastExecutedAt = s.LastExecution,
            FlakinessScore = 0,
            IsFlaky = false
        }).ToList();
    }

    public async Task<List<TestExecutionSummary>> GetMostExecutedTestsAsync(
        int count = 10,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days);

        var summaries = await _dbContext.TestExecutionResults
            .Where(r => r.StartedAt >= cutoffDate)
            .GroupBy(r => new { r.RecordingSessionId, r.TestName })
            .Select(g => new
            {
                g.Key.RecordingSessionId,
                g.Key.TestName,
                TotalExecutions = g.Count(),
                FailedExecutions = g.Count(r => r.Status == TestExecutionStatus.Failed),
                PassedExecutions = g.Count(r => r.Status == TestExecutionStatus.Passed),
                AverageDuration = (long)g.Average(r => r.DurationMs),
                LastExecution = g.Max(r => r.StartedAt)
            })
            .OrderByDescending(x => x.TotalExecutions)
            .Take(count)
            .ToListAsync(cancellationToken);

        return summaries.Select(s => new TestExecutionSummary
        {
            RecordingSessionId = s.RecordingSessionId,
            TestName = s.TestName,
            ExecutionCount = s.TotalExecutions,
            PassedCount = s.PassedExecutions,
            FailedCount = s.FailedExecutions,
            PassRate = s.TotalExecutions > 0 ? (double)s.PassedExecutions / s.TotalExecutions * 100 : 0,
            AverageDurationMs = s.AverageDuration,
            LastExecutedAt = s.LastExecution,
            FlakinessScore = 0,
            IsFlaky = false
        }).ToList();
    }

    public async Task SaveTrendsAsync(
        IEnumerable<TestTrend> trends,
        CancellationToken cancellationToken = default)
    {
        var trendsList = trends.ToList();

        if (!trendsList.Any())
        {
            _logger.LogDebug("No trends to save");
            return;
        }

        _logger.LogInformation("Saving {Count} trends to database", trendsList.Count);

        try
        {
            // Check for existing trends to avoid duplicates
            var timestamps = trendsList.Select(t => t.Timestamp).Distinct().ToList();
            var intervals = trendsList.Select(t => t.Interval).Distinct().ToList();
            var recordingIds = trendsList.Select(t => t.RecordingSessionId).Distinct().ToList();

            var existingTrends = await _dbContext.TestTrends
                .Where(t => timestamps.Contains(t.Timestamp) &&
                           intervals.Contains(t.Interval))
                .ToListAsync(cancellationToken);

            // Filter out duplicates
            var newTrends = trendsList.Where(trend =>
                !existingTrends.Any(existing =>
                    existing.Timestamp == trend.Timestamp &&
                    existing.Interval == trend.Interval &&
                    existing.RecordingSessionId == trend.RecordingSessionId &&
                    existing.TestName == trend.TestName))
                .ToList();

            if (!newTrends.Any())
            {
                _logger.LogInformation("All trends already exist in database, skipping insert");
                return;
            }

            // Batch insert for performance
            await _dbContext.TestTrends.AddRangeAsync(newTrends, cancellationToken);
            var savedCount = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saved {SavedCount} new trends (skipped {SkippedCount} duplicates)",
                savedCount,
                trendsList.Count - newTrends.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save trends to database");
            throw;
        }
    }

    public async Task SaveFlakyTestAnalysisAsync(
        FlakyTestAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Saving flaky test analysis for {RecordingId}/{TestName}, Score={Score}, Severity={Severity}",
            analysis.RecordingSessionId,
            analysis.TestName,
            analysis.FlakinessScore,
            analysis.Severity);

        try
        {
            // Check for existing analysis for this recording/test
            var existingAnalysis = await _dbContext.FlakyTestAnalyses
                .Where(a => a.RecordingSessionId == analysis.RecordingSessionId &&
                           a.TestName == analysis.TestName)
                .OrderByDescending(a => a.AnalyzedAt)
                .FirstOrDefaultAsync(cancellationToken);

            // If analysis already exists with same or better score, skip
            if (existingAnalysis != null)
            {
                var scoreDelta = Math.Abs(analysis.FlakinessScore - existingAnalysis.FlakinessScore);

                if (scoreDelta < 5.0) // Less than 5% change
                {
                    _logger.LogInformation(
                        "Skipping flaky test analysis - score delta {Delta:F2} is below threshold",
                        scoreDelta);
                    return;
                }

                _logger.LogInformation(
                    "Superseding previous analysis (Score: {OldScore:F2} → {NewScore:F2})",
                    existingAnalysis.FlakinessScore,
                    analysis.FlakinessScore);
            }

            await _dbContext.FlakyTestAnalyses.AddAsync(analysis, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Flaky test analysis saved: Id={Id}, IsFlaky={IsFlaky}",
                analysis.Id,
                analysis.IsFlaky);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save flaky test analysis for {RecordingId}/{TestName}",
                analysis.RecordingSessionId,
                analysis.TestName);
            throw;
        }
    }

    public async Task<List<TestTrend>> GetHistoricalTrendsAsync(
        Guid? recordingSessionId,
        TrendInterval interval,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving historical trends from database");

        var query = _dbContext.TestTrends
            .Where(t => t.Interval == interval &&
                        t.Timestamp >= startDate &&
                        t.Timestamp <= endDate);

        if (recordingSessionId.HasValue)
        {
            query = query.Where(t => t.RecordingSessionId == recordingSessionId.Value);
        }

        var trends = await query
            .OrderBy(t => t.Timestamp)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} historical trends", trends.Count);

        return trends;
    }

    public TestSuiteHealth DetermineHealth(DashboardStatistics statistics)
    {
        if (statistics.TotalExecutions == 0 || statistics.TotalTests == 0)
        {
            return TestSuiteHealth.Unknown;
        }

        var passRate = statistics.OverallPassRate;
        var flakyPercentage = statistics.TotalTests > 0
            ? (double)statistics.FlakyTestCount / statistics.TotalTests * 100
            : 0;

        // Excellent: >95% pass rate, <5% flaky
        if (passRate > 95 && flakyPercentage < 5)
        {
            return TestSuiteHealth.Excellent;
        }

        // Good: 85-95% pass rate, <10% flaky
        if (passRate >= 85 && passRate <= 95 && flakyPercentage < 10)
        {
            return TestSuiteHealth.Good;
        }

        // Fair: 70-85% pass rate, <20% flaky
        if (passRate >= 70 && passRate < 85 && flakyPercentage < 20)
        {
            return TestSuiteHealth.Fair;
        }

        // Poor: <70% pass rate or >20% flaky
        return TestSuiteHealth.Poor;
    }

    // Private helper methods

    private Dictionary<DateTimeOffset, List<TestExecutionResult>> GroupByInterval(
        List<TestExecutionResult> results,
        TrendInterval interval)
    {
        return interval switch
        {
            TrendInterval.Hourly => results.GroupBy(r => new DateTimeOffset(
                    r.StartedAt.Year,
                    r.StartedAt.Month,
                    r.StartedAt.Day,
                    r.StartedAt.Hour,
                    0, 0,
                    r.StartedAt.Offset))
                .ToDictionary(g => g.Key, g => g.ToList()),

            TrendInterval.Daily => results.GroupBy(r => new DateTimeOffset(
                    r.StartedAt.Year,
                    r.StartedAt.Month,
                    r.StartedAt.Day,
                    0, 0, 0,
                    r.StartedAt.Offset))
                .ToDictionary(g => g.Key, g => g.ToList()),

            TrendInterval.Weekly => results.GroupBy(r => GetStartOfWeek(r.StartedAt))
                .ToDictionary(g => g.Key, g => g.ToList()),

            TrendInterval.Monthly => results.GroupBy(r => new DateTimeOffset(
                    r.StartedAt.Year,
                    r.StartedAt.Month,
                    1, 0, 0, 0,
                    r.StartedAt.Offset))
                .ToDictionary(g => g.Key, g => g.ToList()),

            _ => throw new ArgumentException($"Unsupported interval: {interval}")
        };
    }

    private DateTimeOffset GetStartOfWeek(DateTimeOffset date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Monday as first day

        return new DateTimeOffset(
            date.Year,
            date.Month,
            date.Day,
            0, 0, 0,
            date.Offset).AddDays(-daysToSubtract);
    }

    private double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valuesList = values.ToList();

        if (valuesList.Count < 2)
        {
            return 0;
        }

        var mean = valuesList.Average();
        var squaredDifferences = valuesList.Select(v => Math.Pow(v - mean, 2));
        var variance = squaredDifferences.Average();

        return Math.Sqrt(variance);
    }
}
