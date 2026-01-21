using EvoAITest.ApiService.Hubs;
using EvoAITest.Core.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace EvoAITest.ApiService.Services;

/// <summary>
/// Background service that periodically broadcasts analytics updates via SignalR
/// </summary>
public sealed class AnalyticsBroadcastService : BackgroundService
{
    private readonly ILogger<AnalyticsBroadcastService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<AnalyticsHub> _hubContext;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30); // Update every 30 seconds
    private readonly TimeSpan _trendCalculationInterval = TimeSpan.FromMinutes(5); // Calculate trends every 5 minutes
    
    // Throttling
    private DateTimeOffset _lastDashboardUpdate = DateTimeOffset.MinValue;
    private DateTimeOffset _lastTrendCalculation = DateTimeOffset.MinValue;
    private const int MinSecondsBetweenUpdates = 10; // Minimum 10 seconds between updates

    public AnalyticsBroadcastService(
        ILogger<AnalyticsBroadcastService> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<AnalyticsHub> hubContext)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalyticsBroadcastService started");

        var lastTrendCalculation = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BroadcastDashboardUpdate(stoppingToken);

                // Calculate and broadcast trends periodically
                if (DateTimeOffset.UtcNow - lastTrendCalculation >= _trendCalculationInterval)
                {
                    await CalculateAndBroadcastTrends(stoppingToken);
                    lastTrendCalculation = DateTimeOffset.UtcNow;
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in analytics broadcast loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before retry
            }
        }

        _logger.LogInformation("AnalyticsBroadcastService stopped");
    }

    private async Task BroadcastDashboardUpdate(CancellationToken cancellationToken)
    {
        // Throttle updates
        var timeSinceLastUpdate = DateTimeOffset.UtcNow - _lastDashboardUpdate;
        if (timeSinceLastUpdate.TotalSeconds < MinSecondsBetweenUpdates)
        {
            _logger.LogDebug("Skipping dashboard update due to throttling");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<ITestAnalyticsService>();

            var statistics = await analyticsService.GetDashboardStatisticsAsync(cancellationToken);

            await _hubContext.SendDashboardUpdate(statistics);

            _lastDashboardUpdate = DateTimeOffset.UtcNow;

            // Check for pass rate alerts
            if (statistics.PassRateLast24Hours < 90)
            {
                await _hubContext.SendPassRateAlert(
                    statistics.PassRateLast24Hours,
                    90,
                    true);
            }

            _logger.LogDebug("Dashboard update broadcasted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast dashboard update");
        }
    }

    private async Task CalculateAndBroadcastTrends(CancellationToken cancellationToken)
    {
        // Throttle trend calculations
        var timeSinceLastCalculation = DateTimeOffset.UtcNow - _lastTrendCalculation;
        if (timeSinceLastCalculation < _trendCalculationInterval)
        {
            _logger.LogDebug("Skipping trend calculation due to throttling");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var analyticsService = scope.ServiceProvider.GetRequiredService<ITestAnalyticsService>();

            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddDays(-30);

            var trends = await analyticsService.CalculateTrendsAsync(
                Core.Models.Analytics.TrendInterval.Daily,
                startDate,
                endDate,
                null,
                cancellationToken);

            // Only broadcast if there are trends and we have clients
            if (trends.Any())
            {
                await _hubContext.SendTrendCalculated(trends);
                _lastTrendCalculation = DateTimeOffset.UtcNow;
                _logger.LogInformation("Trends calculated and broadcasted: {Count} data points", trends.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate and broadcast trends");
        }
    }
}
