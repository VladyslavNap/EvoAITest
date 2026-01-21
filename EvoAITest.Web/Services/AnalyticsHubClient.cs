using EvoAITest.Core.Models.Analytics;
using Microsoft.AspNetCore.SignalR.Client;

namespace EvoAITest.Web.Services;

/// <summary>
/// Service for managing SignalR connections to the analytics hub
/// </summary>
public sealed class AnalyticsHubClient : IAsyncDisposable
{
    private readonly ILogger<AnalyticsHubClient> _logger;
    private readonly string _hubUrl;
    private HubConnection? _connection;

    // Events for real-time updates
    public event Func<DashboardStatistics, Task>? OnDashboardUpdated;
    public event Func<FlakyTestAnalysis, Task>? OnFlakyTestDetected;
    public event Func<List<TestTrend>, Task>? OnTrendCalculated;
    public event Func<PassRateAlert, Task>? OnPassRateAlert;
    public event Func<Notification, Task>? OnNotification;

    public AnalyticsHubClient(
        ILogger<AnalyticsHubClient> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        
        // Get API base URL from configuration
        var apiUrl = configuration["ApiUrl"] ?? "https://localhost:7234";
        _hubUrl = $"{apiUrl}/hubs/analytics";
    }

    /// <summary>
    /// Gets whether the connection is active
    /// </summary>
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Starts the SignalR connection
    /// </summary>
    public async Task StartAsync()
    {
        if (_connection != null)
        {
            _logger.LogWarning("Connection already exists");
            return;
        }

        try
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect() // Auto-reconnect on disconnection
                .Build();

            // Register event handlers
            RegisterHandlers();

            // Start connection
            await _connection.StartAsync();

            _logger.LogInformation("Connected to AnalyticsHub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to AnalyticsHub");
            throw;
        }
    }

    /// <summary>
    /// Stops the SignalR connection
    /// </summary>
    public async Task StopAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.StopAsync();
                _logger.LogInformation("Disconnected from AnalyticsHub");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping connection");
            }
        }
    }

    /// <summary>
    /// Subscribe to dashboard updates
    /// </summary>
    public async Task SubscribeToDashboardAsync()
    {
        if (_connection == null || _connection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot subscribe: Not connected");
            return;
        }

        try
        {
            await _connection.InvokeAsync("SubscribeToDashboard");
            _logger.LogInformation("Subscribed to dashboard updates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to dashboard");
        }
    }

    /// <summary>
    /// Unsubscribe from dashboard updates
    /// </summary>
    public async Task UnsubscribeFromDashboardAsync()
    {
        if (_connection == null || _connection.State != HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _connection.InvokeAsync("UnsubscribeFromDashboard");
            _logger.LogInformation("Unsubscribed from dashboard updates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from dashboard");
        }
    }

    /// <summary>
    /// Subscribe to recording-specific updates
    /// </summary>
    public async Task SubscribeToRecordingAsync(Guid recordingId)
    {
        if (_connection == null || _connection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot subscribe: Not connected");
            return;
        }

        try
        {
            await _connection.InvokeAsync("SubscribeToRecording", recordingId);
            _logger.LogInformation("Subscribed to recording {RecordingId} updates", recordingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to recording");
        }
    }

    /// <summary>
    /// Unsubscribe from recording-specific updates
    /// </summary>
    public async Task UnsubscribeFromRecordingAsync(Guid recordingId)
    {
        if (_connection == null || _connection.State != HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _connection.InvokeAsync("UnsubscribeFromRecording", recordingId);
            _logger.LogInformation("Unsubscribed from recording {RecordingId} updates", recordingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from recording");
        }
    }

    private void RegisterHandlers()
    {
        if (_connection == null) return;

        // Dashboard updated
        _connection.On<DashboardStatistics>("DashboardUpdated", async (statistics) =>
        {
            _logger.LogDebug("Dashboard update received");
            if (OnDashboardUpdated != null)
            {
                await OnDashboardUpdated.Invoke(statistics);
            }
        });

        // Flaky test detected
        _connection.On<FlakyTestAnalysis>("FlakyTestDetected", async (analysis) =>
        {
            _logger.LogDebug("Flaky test detected: {TestName}", analysis.TestName);
            if (OnFlakyTestDetected != null)
            {
                await OnFlakyTestDetected.Invoke(analysis);
            }
        });

        // Trend calculated
        _connection.On<List<TestTrend>>("TrendCalculated", async (trends) =>
        {
            _logger.LogDebug("Trend calculation received: {Count} data points", trends.Count);
            if (OnTrendCalculated != null)
            {
                await OnTrendCalculated.Invoke(trends);
            }
        });

        // Pass rate alert
        _connection.On<PassRateAlert>("PassRateAlert", async (alert) =>
        {
            _logger.LogWarning("Pass rate alert: {Message}", alert.Message);
            if (OnPassRateAlert != null)
            {
                await OnPassRateAlert.Invoke(alert);
            }
        });

        // General notification
        _connection.On<Notification>("Notification", async (notification) =>
        {
            _logger.LogInformation("Notification received: {Title}", notification.Title);
            if (OnNotification != null)
            {
                await OnNotification.Invoke(notification);
            }
        });

        // Connection events
        _connection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "Reconnecting to AnalyticsHub...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            _logger.LogInformation("Reconnected to AnalyticsHub with connection ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            if (error != null)
            {
                _logger.LogError(error, "Connection closed with error");
            }
            else
            {
                _logger.LogInformation("Connection closed");
            }
            return Task.CompletedTask;
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}

/// <summary>
/// Pass rate alert data
/// </summary>
public sealed class PassRateAlert
{
    public double CurrentPassRate { get; set; }
    public double Threshold { get; set; }
    public bool IsBelow { get; set; }
    public string Message { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// General notification data
/// </summary>
public sealed class Notification
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "info"; // info, success, warning, danger
    public DateTimeOffset Timestamp { get; set; }
}
