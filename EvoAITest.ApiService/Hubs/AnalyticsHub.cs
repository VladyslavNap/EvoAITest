using EvoAITest.Core.Models.Analytics;
using Microsoft.AspNetCore.SignalR;

namespace EvoAITest.ApiService.Hubs;

/// <summary>
/// SignalR hub for real-time analytics updates
/// </summary>
public sealed class AnalyticsHub : Hub
{
    private readonly ILogger<AnalyticsHub> _logger;

    public AnalyticsHub(ILogger<AnalyticsHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Client connected to AnalyticsHub: ConnectionId={ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client disconnected from AnalyticsHub: ConnectionId={ConnectionId}",
            Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to dashboard updates
    /// </summary>
    public async Task SubscribeToDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to Dashboard updates",
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from dashboard updates
    /// </summary>
    public async Task UnsubscribeFromDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from Dashboard updates",
            Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to recording-specific updates
    /// </summary>
    public async Task SubscribeToRecording(Guid recordingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Recording_{recordingId}");
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to Recording {RecordingId} updates",
            Context.ConnectionId,
            recordingId);
    }

    /// <summary>
    /// Unsubscribe from recording-specific updates
    /// </summary>
    public async Task UnsubscribeFromRecording(Guid recordingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Recording_{recordingId}");
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from Recording {RecordingId} updates",
            Context.ConnectionId,
            recordingId);
    }
}

/// <summary>
/// Extension methods for sending analytics updates via SignalR
/// </summary>
public static class AnalyticsHubExtensions
{
    /// <summary>
    /// Sends dashboard statistics update to all subscribed clients
    /// </summary>
    public static async Task SendDashboardUpdate(
        this IHubContext<AnalyticsHub> hubContext,
        DashboardStatistics statistics)
    {
        await hubContext.Clients.Group("Dashboard").SendAsync(
            "DashboardUpdated",
            statistics);
    }

    /// <summary>
    /// Sends flaky test detection notification
    /// </summary>
    public static async Task SendFlakyTestDetected(
        this IHubContext<AnalyticsHub> hubContext,
        FlakyTestAnalysis analysis)
    {
        await hubContext.Clients.Group("Dashboard").SendAsync(
            "FlakyTestDetected",
            analysis);
    }

    /// <summary>
    /// Sends test execution completion notification
    /// </summary>
    public static async Task SendTestExecutionCompleted(
        this IHubContext<AnalyticsHub> hubContext,
        Guid recordingId,
        string testName,
        bool passed)
    {
        await hubContext.Clients.Group($"Recording_{recordingId}").SendAsync(
            "TestExecutionCompleted",
            new
            {
                RecordingId = recordingId,
                TestName = testName,
                Passed = passed,
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    /// <summary>
    /// Sends trend calculation completed notification
    /// </summary>
    public static async Task SendTrendCalculated(
        this IHubContext<AnalyticsHub> hubContext,
        List<TestTrend> trends)
    {
        await hubContext.Clients.Group("Dashboard").SendAsync(
            "TrendCalculated",
            trends);
    }

    /// <summary>
    /// Sends recording insights update
    /// </summary>
    public static async Task SendRecordingInsightsUpdate(
        this IHubContext<AnalyticsHub> hubContext,
        Guid recordingId,
        object insights)
    {
        await hubContext.Clients.Group($"Recording_{recordingId}").SendAsync(
            "RecordingInsightsUpdated",
            insights);
    }

    /// <summary>
    /// Sends pass rate alert when threshold is crossed
    /// </summary>
    public static async Task SendPassRateAlert(
        this IHubContext<AnalyticsHub> hubContext,
        double currentPassRate,
        double threshold,
        bool isBelow)
    {
        await hubContext.Clients.Group("Dashboard").SendAsync(
            "PassRateAlert",
            new
            {
                CurrentPassRate = currentPassRate,
                Threshold = threshold,
                IsBelow = isBelow,
                Message = isBelow
                    ? $"Pass rate dropped to {currentPassRate:F1}% (below threshold {threshold}%)"
                    : $"Pass rate recovered to {currentPassRate:F1}% (above threshold {threshold}%)",
                Timestamp = DateTimeOffset.UtcNow
            });
    }

    /// <summary>
    /// Broadcasts a general analytics notification
    /// </summary>
    public static async Task BroadcastNotification(
        this IHubContext<AnalyticsHub> hubContext,
        string title,
        string message,
        string type = "info")
    {
        await hubContext.Clients.All.SendAsync(
            "Notification",
            new
            {
                Title = title,
                Message = message,
                Type = type, // info, success, warning, danger
                Timestamp = DateTimeOffset.UtcNow
            });
    }
}
