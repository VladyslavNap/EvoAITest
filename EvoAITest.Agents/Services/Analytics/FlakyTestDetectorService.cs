using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Analytics;
using EvoAITest.Core.Models.Execution;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Agents.Services.Analytics;

/// <summary>
/// Service for detecting and analyzing flaky test behavior
/// </summary>
public sealed class FlakyTestDetectorService : IFlakyTestDetector
{
    private readonly ILogger<FlakyTestDetectorService> _logger;
    private readonly ITestResultStorage _resultStorage;

    public FlakyTestDetectorService(
        ILogger<FlakyTestDetectorService> logger,
        ITestResultStorage resultStorage)
    {
        _logger = logger;
        _resultStorage = resultStorage;
    }

    public async Task<FlakyTestAnalysis> AnalyzeRecordingAsync(
        Guid recordingSessionId,
        FlakyCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        criteria ??= FlakyCriteria.Default;

        _logger.LogInformation(
            "Analyzing recording {RecordingId} for flakiness",
            recordingSessionId);

        // Get all execution results for this recording
        var results = await _resultStorage.GetResultsByRecordingAsync(
            recordingSessionId,
            cancellationToken);

        var resultsList = results.ToList();

        if (resultsList.Count < criteria.MinimumExecutions)
        {
            _logger.LogWarning(
                "Insufficient executions ({Count}) for recording {RecordingId}",
                resultsList.Count,
                recordingSessionId);

            return CreateInsufficientDataAnalysis(recordingSessionId, resultsList);
        }

        return await AnalyzeExecutionResultsAsync(resultsList, criteria, cancellationToken);
    }

    public async Task<FlakyTestAnalysis> AnalyzeExecutionResultsAsync(
        IEnumerable<TestExecutionResult> results,
        FlakyCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        criteria ??= FlakyCriteria.Default;
        var resultsList = results.ToList();

        if (!resultsList.Any())
        {
            throw new ArgumentException("No execution results provided", nameof(results));
        }

        var firstResult = resultsList.First();

        _logger.LogInformation(
            "Analyzing {Count} execution results for test {TestName}",
            resultsList.Count,
            firstResult.TestName);

        // Calculate basic statistics
        var totalExecutions = resultsList.Count;
        var passedExecutions = resultsList.Count(r => r.Status == TestExecutionStatus.Passed);
        var failedExecutions = resultsList.Count(r => r.Status == TestExecutionStatus.Failed);
        var passRate = totalExecutions > 0 ? (double)passedExecutions / totalExecutions * 100 : 0;

        // Identify flaky failures (failures followed by passes)
        var flakyFailureCount = CountFlakyFailures(resultsList);
        var consistentPassCount = CountConsecutivePasses(resultsList);
        var consistentFailureCount = CountConsecutiveFailures(resultsList);

        // Calculate duration variability
        var durationVariability = CalculateDurationVariability(resultsList);

        // Calculate flakiness score
        var flakinessScore = CalculateFlakinessScore(resultsList, criteria);

        // Determine severity
        var severity = DetermineSeverity(flakinessScore, passRate, flakyFailureCount);

        // Detect patterns
        var patterns = await DetectPatternsAsync(resultsList, cancellationToken);

        // Generate recommendations
        var recommendations = GenerateRecommendations(
            flakinessScore,
            passRate,
            flakyFailureCount,
            patterns);

        // Identify root causes
        var rootCauses = IdentifyRootCauses(patterns, resultsList);

        // Calculate analysis confidence
        var confidence = CalculateAnalysisConfidence(totalExecutions, criteria);

        // Calculate average time to failure
        var avgTimeToFailure = CalculateAverageTimeToFailure(resultsList);

        // Calculate execution duration standard deviation
        var durationStdDev = CalculateStandardDeviation(
            resultsList.Select(r => (double)r.DurationMs));

        var analysis = new FlakyTestAnalysis
        {
            RecordingSessionId = firstResult.RecordingSessionId,
            TestName = firstResult.TestName,
            FlakinessScore = flakinessScore,
            Severity = severity,
            TotalExecutions = totalExecutions,
            FlakyFailureCount = flakyFailureCount,
            ConsistentPassCount = consistentPassCount,
            ConsistentFailureCount = consistentFailureCount,
            PassRate = passRate,
            DurationVariability = durationVariability,
            Patterns = patterns,
            LastExecutionAt = resultsList.Max(r => r.StartedAt),
            Recommendations = recommendations,
            RootCauses = rootCauses,
            AnalysisConfidence = confidence,
            AverageTimeToFailure = avgTimeToFailure,
            ExecutionDurationStdDev = (long?)durationStdDev
        };

        _logger.LogInformation(
            "Analysis complete: Score={Score:F1}, Severity={Severity}, Flaky={IsFlaky}",
            flakinessScore,
            severity,
            analysis.IsFlaky);

        return analysis;
    }

    public async Task<List<FlakyTestPattern>> DetectPatternsAsync(
        IEnumerable<TestExecutionResult> results,
        CancellationToken cancellationToken = default)
    {
        var resultsList = results.ToList();
        var patterns = new List<FlakyTestPattern>();

        if (resultsList.Count < 3)
        {
            return patterns; // Not enough data for pattern detection
        }

        // Detect intermittent failures
        var intermittentPattern = DetectIntermittentPattern(resultsList);
        if (intermittentPattern != null)
        {
            patterns.Add(intermittentPattern);
        }

        // Detect temporal patterns (time-based)
        var temporalPattern = DetectTemporalPattern(resultsList);
        if (temporalPattern != null)
        {
            patterns.Add(temporalPattern);
        }

        // Detect timing-dependent failures
        var timingPattern = DetectTimingPattern(resultsList);
        if (timingPattern != null)
        {
            patterns.Add(timingPattern);
        }

        // Detect sequential patterns
        var sequentialPattern = DetectSequentialPattern(resultsList);
        if (sequentialPattern != null)
        {
            patterns.Add(sequentialPattern);
        }

        // Detect degrading performance pattern
        var degradingPattern = DetectDegradingPattern(resultsList);
        if (degradingPattern != null)
        {
            patterns.Add(degradingPattern);
        }

        _logger.LogInformation("Detected {Count} patterns", patterns.Count);

        return await Task.FromResult(patterns);
    }

    public async Task<TestStabilityMetrics> CalculateStabilityMetricsAsync(
        Guid recordingSessionId,
        int windowDays = 30,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating stability metrics for recording {RecordingId}",
            recordingSessionId);

        // Get execution results
        var allResults = await _resultStorage.GetResultsByRecordingAsync(
            recordingSessionId,
            cancellationToken);

        var resultsList = allResults.ToList();
        var windowStart = DateTimeOffset.UtcNow.AddDays(-windowDays);
        var windowResults = resultsList
            .Where(r => r.StartedAt >= windowStart)
            .OrderBy(r => r.StartedAt)
            .ToList();

        if (!windowResults.Any())
        {
            return CreateDefaultStabilityMetrics(recordingSessionId, windowStart);
        }

        var testName = windowResults.First().TestName;

        // Calculate pass rates
        var totalExecutions = windowResults.Count;
        var passedExecutions = windowResults.Count(r => r.Status == TestExecutionStatus.Passed);
        var passRate = (double)passedExecutions / totalExecutions * 100;

        // Calculate last 7 days metrics
        var last7Days = DateTimeOffset.UtcNow.AddDays(-7);
        var results7Days = windowResults.Where(r => r.StartedAt >= last7Days).ToList();
        var passRate7Days = results7Days.Any()
            ? (double)results7Days.Count(r => r.Status == TestExecutionStatus.Passed) / results7Days.Count * 100
            : 0;

        // Calculate stability score
        var stabilityScore = CalculateStabilityScore(windowResults);

        // Determine stability class
        var stabilityClass = DetermineStabilityClass(stabilityScore, passRate);

        // Calculate streaks
        var (longestPassStreak, currentPassStreak) = CalculatePassStreaks(windowResults);
        var currentFailureStreak = CalculateCurrentFailureStreak(windowResults);

        // Calculate MTBF and MTTR
        var mtbf = CalculateMeanTimeBetweenFailures(windowResults);
        var mttr = CalculateMeanTimeToRecovery(windowResults);

        // Calculate trend
        var trend = CalculateTrend(windowResults);

        // Calculate duration metrics
        var avgDuration = (long)windowResults.Average(r => r.DurationMs);
        var durationVariance = CalculateDurationVariability(windowResults);

        // Calculate retry rate
        var retryCount = windowResults.Count(r => r.Metadata.ContainsKey("Retried"));
        var retryRate = totalExecutions > 0 ? (double)retryCount / totalExecutions * 100 : 0;

        // Calculate assessment confidence
        var confidence = CalculateAnalysisConfidence(totalExecutions, FlakyCriteria.Default);

        var metrics = new TestStabilityMetrics
        {
            RecordingSessionId = recordingSessionId,
            TestName = testName,
            StabilityClass = stabilityClass,
            StabilityScore = stabilityScore,
            ConsecutivePassStreak = currentPassStreak,
            LongestPassStreak = longestPassStreak,
            ConsecutiveFailureStreak = currentFailureStreak,
            MeanTimeBetweenFailures = mtbf,
            MeanTimeToRecovery = mttr,
            PassRateStandardDeviation = CalculatePassRateStdDev(windowResults),
            TrendDirection = trend,
            StabilityChangeRate = CalculateStabilityChangeRate(windowResults),
            AverageDurationMs = avgDuration,
            DurationVariance = durationVariance,
            RetryRate = retryRate,
            TotalExecutions = totalExecutions,
            ExecutionsLast7Days = results7Days.Count,
            ExecutionsLast30Days = windowResults.Count,
            PassRateLast7Days = passRate7Days,
            PassRateLast30Days = passRate,
            WindowStart = windowStart,
            WindowEnd = DateTimeOffset.UtcNow,
            AssessmentConfidence = confidence
        };

        _logger.LogInformation(
            "Stability metrics calculated: Score={Score:F1}, Class={Class}",
            stabilityScore,
            stabilityClass);

        return metrics;
    }

    public async Task<List<FlakyTestAnalysis>> GetAllFlakyTestsAsync(
        FlakyCriteria? criteria = null,
        CancellationToken cancellationToken = default)
    {
        criteria ??= FlakyCriteria.Default;

        _logger.LogInformation("Scanning all tests for flakiness");

        // Get recent execution history
        var recentResults = await _resultStorage.GetExecutionHistoryAsync(0, 1000, cancellationToken);

        // Group by recording session ID
        var groupedResults = recentResults
            .GroupBy(r => r.RecordingSessionId)
            .Where(g => g.Count() >= criteria.MinimumExecutions);

        var flakyTests = new List<FlakyTestAnalysis>();

        foreach (var group in groupedResults)
        {
            try
            {
                var analysis = await AnalyzeExecutionResultsAsync(
                    group,
                    criteria,
                    cancellationToken);

                if (analysis.IsFlaky)
                {
                    flakyTests.Add(analysis);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to analyze recording {RecordingId}",
                    group.Key);
            }
        }

        _logger.LogInformation("Found {Count} flaky tests", flakyTests.Count);

        return flakyTests.OrderByDescending(f => f.FlakinessScore).ToList();
    }

    public double CalculateFlakinessScore(
        IEnumerable<TestExecutionResult> results,
        FlakyCriteria criteria)
    {
        var resultsList = results.ToList();

        if (!resultsList.Any())
        {
            return 0;
        }

        var totalExecutions = resultsList.Count;
        var failedExecutions = resultsList.Count(r => r.Status == TestExecutionStatus.Failed);
        var passRate = (double)(totalExecutions - failedExecutions) / totalExecutions * 100;

        // Component 1: Pass rate deviation from 100%
        var passRateScore = Math.Max(0, 100 - passRate);

        // Component 2: Flaky failures (failures followed by passes)
        var flakyFailures = CountFlakyFailures(resultsList);
        var flakyScore = Math.Min(100, flakyFailures * 10.0);

        // Component 3: Duration variability
        var durationVariability = CalculateDurationVariability(resultsList);
        var variabilityScore = Math.Min(100, durationVariability * 100);

        // Component 4: Consecutive failure patterns
        var consecutiveFailures = CountConsecutiveFailures(resultsList);
        var consecutiveScore = consecutiveFailures >= criteria.ConsistentFailureThreshold
            ? 0 // Consistently failing, not flaky
            : Math.Min(50, consecutiveFailures * 5.0);

        // Weighted combination
        var score = (passRateScore * 0.3) +
                    (flakyScore * 0.4) +
                    (variabilityScore * 0.2) +
                    (consecutiveScore * 0.1);

        return Math.Min(100, Math.Max(0, score));
    }

    public FlakinessSeverity DetermineSeverity(
        double score,
        double passRate,
        int retryCount)
    {
        // Critical: Very high flakiness or very low pass rate
        if (score >= 60 || passRate < 40)
        {
            return FlakinessSeverity.Critical;
        }

        // High: Significant flakiness or multiple retries
        if (score >= 40 || retryCount >= 5)
        {
            return FlakinessSeverity.High;
        }

        // Medium: Moderate flakiness
        if (score >= 20 || retryCount >= 3)
        {
            return FlakinessSeverity.Medium;
        }

        // Low: Minor flakiness
        if (score >= 10 || retryCount >= 1)
        {
            return FlakinessSeverity.Low;
        }

        return FlakinessSeverity.None;
    }

    // Private helper methods

    private int CountFlakyFailures(List<TestExecutionResult> results)
    {
        var count = 0;
        var orderedResults = results.OrderBy(r => r.StartedAt).ToList();

        for (int i = 0; i < orderedResults.Count - 1; i++)
        {
            if (orderedResults[i].Status == TestExecutionStatus.Failed &&
                orderedResults[i + 1].Status == TestExecutionStatus.Passed)
            {
                count++;
            }
        }

        return count;
    }

    private int CountConsecutivePasses(List<TestExecutionResult> results)
    {
        return CountConsecutiveStatus(results, TestExecutionStatus.Passed);
    }

    private int CountConsecutiveFailures(List<TestExecutionResult> results)
    {
        return CountConsecutiveStatus(results, TestExecutionStatus.Failed);
    }

    private int CountConsecutiveStatus(List<TestExecutionResult> results, TestExecutionStatus status)
    {
        var orderedResults = results.OrderBy(r => r.StartedAt).ToList();
        var maxCount = 0;
        var currentCount = 0;

        foreach (var result in orderedResults)
        {
            if (result.Status == status)
            {
                currentCount++;
                maxCount = Math.Max(maxCount, currentCount);
            }
            else
            {
                currentCount = 0;
            }
        }

        return maxCount;
    }

    private double CalculateDurationVariability(List<TestExecutionResult> results)
    {
        if (results.Count < 2)
        {
            return 0;
        }

        var durations = results.Select(r => (double)r.DurationMs).ToList();
        var mean = durations.Average();

        if (mean == 0)
        {
            return 0;
        }

        var stdDev = CalculateStandardDeviation(durations);
        return stdDev / mean; // Coefficient of variation
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

    private FlakyTestPattern? DetectIntermittentPattern(List<TestExecutionResult> results)
    {
        var orderedResults = results.OrderBy(r => r.StartedAt).ToList();
        var transitions = 0;

        for (int i = 0; i < orderedResults.Count - 1; i++)
        {
            if (orderedResults[i].Status != orderedResults[i + 1].Status)
            {
                transitions++;
            }
        }

        if (transitions >= 3 && transitions >= orderedResults.Count * 0.3)
        {
            return new FlakyTestPattern
            {
                Type = PatternType.Intermittent,
                Description = $"Test alternates between pass and fail states frequently ({transitions} transitions)",
                Confidence = Math.Min(100, transitions * 10.0),
                Occurrences = transitions,
                FirstDetectedAt = orderedResults.First().StartedAt,
                LastObservedAt = orderedResults.Last().StartedAt,
                SuggestedFix = "Review test for race conditions or timing dependencies"
            };
        }

        return null;
    }

    private FlakyTestPattern? DetectTemporalPattern(List<TestExecutionResult> results)
    {
        // Group failures by hour of day
        var failuresByHour = results
            .Where(r => r.Status == TestExecutionStatus.Failed)
            .GroupBy(r => r.StartedAt.Hour)
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (failuresByHour != null && failuresByHour.Count() >= 3)
        {
            return new FlakyTestPattern
            {
                Type = PatternType.Temporal,
                Description = $"Test frequently fails around {failuresByHour.Key}:00",
                Confidence = Math.Min(100, failuresByHour.Count() * 15.0),
                Occurrences = failuresByHour.Count(),
                FirstDetectedAt = failuresByHour.Min(r => r.StartedAt),
                LastObservedAt = failuresByHour.Max(r => r.StartedAt),
                SuggestedFix = "Check for time-dependent logic or scheduled external processes"
            };
        }

        return null;
    }

    private FlakyTestPattern? DetectTimingPattern(List<TestExecutionResult> results)
    {
        var durations = results.Select(r => r.DurationMs).ToList();
        var avgDuration = durations.Average();
        var stdDev = CalculateStandardDeviation(durations.Select(d => (double)d));

        if (stdDev > avgDuration * 0.5) // High variability
        {
            var failures = results.Where(r => r.Status == TestExecutionStatus.Failed).ToList();
            var avgFailureDuration = failures.Any() ? failures.Average(r => r.DurationMs) : 0;
            var passes = results.Where(r => r.Status == TestExecutionStatus.Passed).ToList();
            var avgPassDuration = passes.Any() ? passes.Average(r => r.DurationMs) : 0;

            if (Math.Abs(avgFailureDuration - avgPassDuration) > avgDuration * 0.3)
            {
                return new FlakyTestPattern
                {
                    Type = PatternType.TimingDependent,
                    Description = $"Execution duration varies significantly (?={stdDev:F0}ms)",
                    Confidence = Math.Min(100, (stdDev / avgDuration) * 100),
                    Occurrences = results.Count,
                    FirstDetectedAt = results.Min(r => r.StartedAt),
                    LastObservedAt = results.Max(r => r.StartedAt),
                    SuggestedFix = "Add explicit waits or increase timeout values"
                };
            }
        }

        return null;
    }

    private FlakyTestPattern? DetectSequentialPattern(List<TestExecutionResult> results)
    {
        var orderedResults = results.OrderBy(r => r.StartedAt).ToList();

        // Look for pattern where every Nth execution fails
        for (int n = 2; n <= 5; n++)
        {
            var matchingFailures = 0;

            for (int i = n - 1; i < orderedResults.Count; i += n)
            {
                if (orderedResults[i].Status == TestExecutionStatus.Failed)
                {
                    matchingFailures++;
                }
            }

            if (matchingFailures >= 3)
            {
                return new FlakyTestPattern
                {
                    Type = PatternType.Sequential,
                    Description = $"Test fails approximately every {n} executions",
                    Confidence = Math.Min(100, matchingFailures * 20.0),
                    Occurrences = matchingFailures,
                    FirstDetectedAt = orderedResults.First().StartedAt,
                    LastObservedAt = orderedResults.Last().StartedAt,
                    SuggestedFix = "Check for state pollution or resource cleanup issues"
                };
            }
        }

        return null;
    }

    private FlakyTestPattern? DetectDegradingPattern(List<TestExecutionResult> results)
    {
        if (results.Count < 10)
        {
            return null;
        }

        var orderedResults = results.OrderBy(r => r.StartedAt).ToList();
        var firstHalf = orderedResults.Take(orderedResults.Count / 2);
        var secondHalf = orderedResults.Skip(orderedResults.Count / 2);

        var firstHalfPassRate = firstHalf.Count(r => r.Status == TestExecutionStatus.Passed) / (double)firstHalf.Count() * 100;
        var secondHalfPassRate = secondHalf.Count(r => r.Status == TestExecutionStatus.Passed) / (double)secondHalf.Count() * 100;

        if (firstHalfPassRate - secondHalfPassRate >= 20)
        {
            return new FlakyTestPattern
            {
                Type = PatternType.Degrading,
                Description = $"Pass rate decreased from {firstHalfPassRate:F1}% to {secondHalfPassRate:F1}%",
                Confidence = Math.Min(100, (firstHalfPassRate - secondHalfPassRate) * 2),
                Occurrences = results.Count,
                FirstDetectedAt = orderedResults.First().StartedAt,
                LastObservedAt = orderedResults.Last().StartedAt,
                SuggestedFix = "Investigate recent changes or accumulating technical debt"
            };
        }

        return null;
    }

    private List<string> GenerateRecommendations(
        double flakinessScore,
        double passRate,
        int flakyFailureCount,
        List<FlakyTestPattern> patterns)
    {
        var recommendations = new List<string>();

        if (flakinessScore > 50)
        {
            recommendations.Add("Consider quarantining this test until stability improves");
        }

        if (passRate < 70)
        {
            recommendations.Add("Review test logic and assertions for correctness");
        }

        if (flakyFailureCount > 3)
        {
            recommendations.Add("Add retry logic or increase timeout values");
            recommendations.Add("Check for race conditions and timing dependencies");
        }

        foreach (var pattern in patterns)
        {
            if (!string.IsNullOrEmpty(pattern.SuggestedFix))
            {
                recommendations.Add(pattern.SuggestedFix);
            }
        }

        if (!recommendations.Any())
        {
            recommendations.Add("Continue monitoring test stability");
        }

        return recommendations.Distinct().ToList();
    }

    private List<string> IdentifyRootCauses(List<FlakyTestPattern> patterns, List<TestExecutionResult> results)
    {
        var causes = new HashSet<string>();

        foreach (var pattern in patterns)
        {
            switch (pattern.Type)
            {
                case PatternType.Intermittent:
                    causes.Add("Race conditions");
                    causes.Add("Timing issues");
                    break;
                case PatternType.Temporal:
                    causes.Add("Time-dependent logic");
                    causes.Add("External scheduled processes");
                    break;
                case PatternType.TimingDependent:
                    causes.Add("Insufficient waits");
                    causes.Add("Variable response times");
                    break;
                case PatternType.Sequential:
                    causes.Add("State pollution");
                    causes.Add("Resource cleanup issues");
                    break;
                case PatternType.Degrading:
                    causes.Add("Performance degradation");
                    causes.Add("Accumulating technical debt");
                    break;
            }
        }

        // Analyze error messages for common causes
        var failedResults = results.Where(r => r.Status == TestExecutionStatus.Failed).ToList();
        var errorMessages = failedResults.Select(r => r.ErrorMessage?.ToLower() ?? "").ToList();

        if (errorMessages.Any(e => e.Contains("timeout")))
        {
            causes.Add("Timeout issues");
        }

        if (errorMessages.Any(e => e.Contains("element not found") || e.Contains("selector")))
        {
            causes.Add("Selector stability");
        }

        if (errorMessages.Any(e => e.Contains("stale element")))
        {
            causes.Add("DOM manipulation timing");
        }

        return causes.ToList();
    }

    private double CalculateAnalysisConfidence(int executionCount, FlakyCriteria criteria)
    {
        if (executionCount < criteria.MinimumExecutions)
        {
            return 0;
        }

        // Confidence increases with more executions, up to 100
        var baseConfidence = Math.Min(100, (double)executionCount / criteria.MinimumExecutions * 50);

        // Bonus for having good sample size
        if (executionCount >= criteria.MinimumExecutions * 3)
        {
            baseConfidence += 25;
        }

        if (executionCount >= criteria.MinimumExecutions * 5)
        {
            baseConfidence += 25;
        }

        return Math.Min(100, baseConfidence);
    }

    private long? CalculateAverageTimeToFailure(List<TestExecutionResult> results)
    {
        var failures = results
            .Where(r => r.Status == TestExecutionStatus.Failed && r.CompletedAt.HasValue)
            .ToList();

        if (!failures.Any())
        {
            return null;
        }

        return (long)failures.Average(r => r.DurationMs);
    }

    private (int longestStreak, int currentStreak) CalculatePassStreaks(List<TestExecutionResult> results)
    {
        var orderedResults = results.OrderBy(r => r.StartedAt).ToList();
        var longestStreak = 0;
        var currentStreak = 0;

        foreach (var result in orderedResults)
        {
            if (result.Status == TestExecutionStatus.Passed)
            {
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        // Current streak is only valid if last execution passed
        if (orderedResults.Any() && orderedResults.Last().Status != TestExecutionStatus.Passed)
        {
            currentStreak = 0;
        }

        return (longestStreak, currentStreak);
    }

    private int CalculateCurrentFailureStreak(List<TestExecutionResult> results)
    {
        var orderedResults = results.OrderByDescending(r => r.StartedAt).ToList();
        var streak = 0;

        foreach (var result in orderedResults)
        {
            if (result.Status == TestExecutionStatus.Failed)
            {
                streak++;
            }
            else
            {
                break;
            }
        }

        return streak;
    }

    private double? CalculateMeanTimeBetweenFailures(List<TestExecutionResult> results)
    {
        var failures = results
            .Where(r => r.Status == TestExecutionStatus.Failed)
            .OrderBy(r => r.StartedAt)
            .ToList();

        if (failures.Count < 2)
        {
            return null;
        }

        var intervals = new List<double>();

        for (int i = 1; i < failures.Count; i++)
        {
            var interval = (failures[i].StartedAt - failures[i - 1].StartedAt).TotalHours;
            intervals.Add(interval);
        }

        return intervals.Average();
    }

    private double? CalculateMeanTimeToRecovery(List<TestExecutionResult> results)
    {
        var orderedResults = results.OrderBy(r => r.StartedAt).ToList();
        var recoveryTimes = new List<double>();

        for (int i = 0; i < orderedResults.Count - 1; i++)
        {
            if (orderedResults[i].Status == TestExecutionStatus.Failed &&
                orderedResults[i + 1].Status == TestExecutionStatus.Passed)
            {
                var recoveryTime = (orderedResults[i + 1].StartedAt - orderedResults[i].StartedAt).TotalHours;
                recoveryTimes.Add(recoveryTime);
            }
        }

        return recoveryTimes.Any() ? recoveryTimes.Average() : null;
    }

    private double CalculateStabilityScore(List<TestExecutionResult> results)
    {
        var passRate = results.Count(r => r.Status == TestExecutionStatus.Passed) / (double)results.Count * 100;
        var durationVariability = CalculateDurationVariability(results);
        var flakyFailures = CountFlakyFailures(results);

        // Higher pass rate = higher stability
        var passRateComponent = passRate;

        // Lower variability = higher stability
        var variabilityComponent = Math.Max(0, 100 - (durationVariability * 100));

        // Fewer flaky failures = higher stability
        var flakyComponent = Math.Max(0, 100 - (flakyFailures * 10));

        return (passRateComponent * 0.5) + (variabilityComponent * 0.3) + (flakyComponent * 0.2);
    }

    private StabilityClass DetermineStabilityClass(double stabilityScore, double passRate)
    {
        if (stabilityScore >= 95 && passRate >= 95)
        {
            return StabilityClass.Stable;
        }

        if (stabilityScore >= 85 && passRate >= 85)
        {
            return StabilityClass.MostlyStable;
        }

        if (stabilityScore >= 70 && passRate >= 70)
        {
            return StabilityClass.Unstable;
        }

        return StabilityClass.HighlyUnstable;
    }

    private double CalculatePassRateStdDev(List<TestExecutionResult> results)
    {
        if (results.Count < 10)
        {
            return 0;
        }

        // Calculate pass rate for rolling windows
        var windowSize = Math.Max(5, results.Count / 5);
        var passRates = new List<double>();

        for (int i = 0; i <= results.Count - windowSize; i += windowSize / 2)
        {
            var window = results.Skip(i).Take(windowSize);
            var passRate = window.Count(r => r.Status == TestExecutionStatus.Passed) / (double)windowSize * 100;
            passRates.Add(passRate);
        }

        return CalculateStandardDeviation(passRates);
    }

    private int CalculateTrend(List<TestExecutionResult> results)
    {
        if (results.Count < 4)
        {
            return 0;
        }

        var firstQuarter = results.Take(results.Count / 4);
        var lastQuarter = results.Skip(results.Count * 3 / 4);

        var firstPassRate = firstQuarter.Count(r => r.Status == TestExecutionStatus.Passed) / (double)firstQuarter.Count();
        var lastPassRate = lastQuarter.Count(r => r.Status == TestExecutionStatus.Passed) / (double)lastQuarter.Count();

        var diff = lastPassRate - firstPassRate;

        if (diff > 0.1) return 1;  // Improving
        if (diff < -0.1) return -1; // Degrading
        return 0; // Stable
    }

    private double CalculateStabilityChangeRate(List<TestExecutionResult> results)
    {
        if (results.Count < 2)
        {
            return 0;
        }

        var orderedResults = results.OrderBy(r => r.StartedAt).ToList();
        var timeSpan = (orderedResults.Last().StartedAt - orderedResults.First().StartedAt).TotalDays;

        if (timeSpan < 7)
        {
            return 0; // Not enough time to calculate weekly rate
        }

        var firstWeekResults = orderedResults.Where(r =>
            r.StartedAt < orderedResults.First().StartedAt.AddDays(7)).ToList();
        var lastWeekResults = orderedResults.Where(r =>
            r.StartedAt > orderedResults.Last().StartedAt.AddDays(-7)).ToList();

        if (!firstWeekResults.Any() || !lastWeekResults.Any())
        {
            return 0;
        }

        var firstWeekPassRate = firstWeekResults.Count(r => r.Status == TestExecutionStatus.Passed) /
                                (double)firstWeekResults.Count * 100;
        var lastWeekPassRate = lastWeekResults.Count(r => r.Status == TestExecutionStatus.Passed) /
                               (double)lastWeekResults.Count * 100;

        return lastWeekPassRate - firstWeekPassRate;
    }

    private FlakyTestAnalysis CreateInsufficientDataAnalysis(
        Guid recordingSessionId,
        List<TestExecutionResult> results)
    {
        var testName = results.Any() ? results.First().TestName : "Unknown";

        return new FlakyTestAnalysis
        {
            RecordingSessionId = recordingSessionId,
            TestName = testName,
            FlakinessScore = 0,
            Severity = FlakinessSeverity.None,
            TotalExecutions = results.Count,
            AnalysisConfidence = 0,
            Recommendations = ["Insufficient execution data for flakiness analysis. Run more tests."]
        };
    }

    private TestStabilityMetrics CreateDefaultStabilityMetrics(
        Guid recordingSessionId,
        DateTimeOffset windowStart)
    {
        return new TestStabilityMetrics
        {
            RecordingSessionId = recordingSessionId,
            TestName = "Unknown",
            StabilityClass = StabilityClass.Unknown,
            StabilityScore = 0,
            WindowStart = windowStart,
            WindowEnd = DateTimeOffset.UtcNow,
            AssessmentConfidence = 0
        };
    }
}
