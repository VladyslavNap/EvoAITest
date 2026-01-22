using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Analytics;
using EvoAITest.Agents.Services.Analytics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace EvoAITest.Agents.Services;

/// <summary>
/// Background service for periodic analytics persistence and cache refresh
/// </summary>
public sealed class AnalyticsPersistenceHostedService : BackgroundService
{
    private readonly ILogger<AnalyticsPersistenceHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AnalyticsPersistenceOptions _options;

    public AnalyticsPersistenceHostedService(
        ILogger<AnalyticsPersistenceHostedService> logger,
        IServiceProvider serviceProvider,
        IOptions<AnalyticsPersistenceOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Analytics Persistence Service starting (Enabled={Enabled}, Interval={Interval}min)",
            _options.Enabled,
            _options.IntervalMinutes);

        if (!_options.Enabled)
        {
            _logger.LogInformation("Analytics Persistence Service is disabled");
            return;
        }

        // Wait for application startup to complete
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformPeriodicTasksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in analytics persistence cycle");
            }

            // Wait for next interval
            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Analytics Persistence Service stopped");
    }

    private async Task PerformPeriodicTasksAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting analytics persistence cycle");
        var startTime = DateTimeOffset.UtcNow;

        using var scope = _serviceProvider.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<ITestAnalyticsService>();
        var flakyDetector = scope.ServiceProvider.GetRequiredService<IFlakyTestDetector>();
        var cacheService = scope.ServiceProvider.GetService<AnalyticsCacheService>();

        int tasksCompleted = 0;
        int tasksSkipped = 0;
        int tasksFailed = 0;

        // Task 1: Calculate and persist hourly trends (if enabled)
        if (_options.PersistHourlyTrends)
        {
            try
            {
                await PersistHourlyTrendsAsync(analyticsService, cancellationToken);
                tasksCompleted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist hourly trends");
                tasksFailed++;
            }
        }
        else
        {
            tasksSkipped++;
        }

        // Task 2: Calculate and persist daily trends
        if (_options.PersistDailyTrends)
        {
            try
            {
                await PersistDailyTrendsAsync(analyticsService, cancellationToken);
                tasksCompleted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist daily trends");
                tasksFailed++;
            }
        }
        else
        {
            tasksSkipped++;
        }

        // Task 3: Refresh flaky test analysis (every 6 hours by default)
        if (ShouldRefreshFlakyAnalysis())
        {
            try
            {
                await RefreshFlakyTestAnalysisAsync(flakyDetector, analyticsService, cancellationToken);
                tasksCompleted++;
                _lastFlakyRefresh = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh flaky test analysis");
                tasksFailed++;
            }
        }
        else
        {
            tasksSkipped++;
        }

        // Task 4: Invalidate and rebuild caches
        if (_options.RefreshCaches && cacheService != null)
        {
            try
            {
                cacheService.ClearAll();
                _logger.LogInformation("Analytics caches cleared");
                tasksCompleted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear caches");
                tasksFailed++;
            }
        }
        else
        {
            tasksSkipped++;
        }

        var duration = DateTimeOffset.UtcNow - startTime;
        _logger.LogInformation(
            "Analytics persistence cycle completed in {Duration}ms - Completed={Completed}, Skipped={Skipped}, Failed={Failed}",
            duration.TotalMilliseconds,
            tasksCompleted,
            tasksSkipped,
            tasksFailed);
    }

    private DateTimeOffset _lastFlakyRefresh = DateTimeOffset.MinValue;

    private bool ShouldRefreshFlakyAnalysis()
    {
        var hoursSinceLastRefresh = (DateTimeOffset.UtcNow - _lastFlakyRefresh).TotalHours;
        return hoursSinceLastRefresh >= _options.FlakyAnalysisRefreshHours;
    }

    private async Task PersistHourlyTrendsAsync(
        ITestAnalyticsService analyticsService,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating and persisting hourly trends");

        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddHours(-_options.HourlyTrendLookbackHours);

        var trends = await analyticsService.CalculateTrendsAsync(
            TrendInterval.Hourly,
            startDate,
            endDate,
            null,
            cancellationToken);

        if (trends.Any())
        {
            await analyticsService.SaveTrendsAsync(trends, cancellationToken);
            _logger.LogInformation("Persisted {Count} hourly trends", trends.Count);
        }
        else
        {
            _logger.LogInformation("No hourly trends to persist");
        }
    }

    private async Task PersistDailyTrendsAsync(
        ITestAnalyticsService analyticsService,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating and persisting daily trends");

        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddDays(-_options.DailyTrendLookbackDays);

        var trends = await analyticsService.CalculateTrendsAsync(
            TrendInterval.Daily,
            startDate,
            endDate,
            null,
            cancellationToken);

        if (trends.Any())
        {
            await analyticsService.SaveTrendsAsync(trends, cancellationToken);
            _logger.LogInformation("Persisted {Count} daily trends", trends.Count);
        }
        else
        {
            _logger.LogInformation("No daily trends to persist");
        }
    }

    private async Task RefreshFlakyTestAnalysisAsync(
        IFlakyTestDetector flakyDetector,
        ITestAnalyticsService analyticsService,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing flaky test analysis");

        // Get all recordings that have recent executions
        var recordings = await flakyDetector.GetRecordingsWithRecentExecutionsAsync(
            days: 30,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Found {Count} recordings with recent executions", recordings.Count);

        int analysisCount = 0;
        int savedCount = 0;

        foreach (var recordingId in recordings)
        {
            try
            {
                var analysis = await flakyDetector.AnalyzeRecordingAsync(
                    recordingId,
                    cancellationToken: cancellationToken);

                analysisCount++;

                // Only save if flakiness is detected or changed
                if (analysis.IsFlaky || analysis.FlakinessScore > 0)
                {
                    await analyticsService.SaveFlakyTestAnalysisAsync(analysis, cancellationToken);
                    savedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to analyze recording {RecordingId}",
                    recordingId);
            }
        }

        _logger.LogInformation(
            "Flaky test analysis complete: Analyzed={Analyzed}, Saved={Saved}",
            analysisCount,
            savedCount);
    }
}

/// <summary>
/// Configuration options for analytics persistence service
/// </summary>
public sealed class AnalyticsPersistenceOptions
{
    /// <summary>
    /// Whether the service is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between persistence cycles in minutes
    /// </summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to persist hourly trends
    /// </summary>
    public bool PersistHourlyTrends { get; set; } = true;

    /// <summary>
    /// How many hours back to calculate hourly trends
    /// </summary>
    public int HourlyTrendLookbackHours { get; set; } = 24;

    /// <summary>
    /// Whether to persist daily trends
    /// </summary>
    public bool PersistDailyTrends { get; set; } = true;

    /// <summary>
    /// How many days back to calculate daily trends
    /// </summary>
    public int DailyTrendLookbackDays { get; set; } = 7;

    /// <summary>
    /// How often to refresh flaky test analysis (hours)
    /// </summary>
    public double FlakyAnalysisRefreshHours { get; set; } = 6.0;

    /// <summary>
    /// Whether to refresh caches after persistence
    /// </summary>
    public bool RefreshCaches { get; set; } = true;

    /// <summary>
    /// Batch size for bulk operations
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}
