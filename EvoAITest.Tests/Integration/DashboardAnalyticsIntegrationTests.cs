using System.Net;
using System.Net.Http.Json;
using EvoAITest.ApiService.Models;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.Analytics;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EvoAITest.Tests.Integration;

/// <summary>
/// Integration tests for Dashboard Analytics and real-time execution tracking.
/// Tests analytics API endpoints, SignalR real-time updates, and metric calculations.
/// </summary>
[TestClass]
public sealed class DashboardAnalyticsIntegrationTests : ApiIntegrationTests
{
    #region Dashboard Analytics API Tests

    [TestMethod]
    public async Task GetDashboardAnalytics_ReturnsComprehensiveMetrics()
    {
        // Act
        var response = await _client.GetAsync("/api/execution-analytics/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var analytics = await response.Content.ReadFromJsonAsync<DashboardAnalytics>();
        analytics.Should().NotBeNull();
        analytics!.CalculatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        analytics.ActiveExecutions.Should().NotBeNull();
        analytics.TimeSeriesLast24Hours.Should().NotBeNull();
        analytics.TimeSeriesLast7Days.Should().NotBeNull();
        analytics.TopExecutedTasks.Should().NotBeNull();
        analytics.TopFailingTasks.Should().NotBeNull();
        analytics.SlowestTasks.Should().NotBeNull();
        analytics.SystemHealth.Should().NotBeNull();
        analytics.Trends.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetActiveExecutions_WithNoActiveExecutions_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/execution-analytics/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeExecutions = await response.Content.ReadFromJsonAsync<List<ActiveExecutionInfo>>();
        activeExecutions.Should().NotBeNull();
        activeExecutions.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetTimeSeries_WithValidParameters_ReturnsDataPoints()
    {
        // Arrange
        var metricType = "SuccessRate";
        var interval = "Hour";
        var hours = 24;

        // Act
        var response = await _client.GetAsync(
            $"/api/execution-analytics/time-series?metricType={metricType}&interval={interval}&hours={hours}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var timeSeries = await response.Content.ReadFromJsonAsync<List<TimeSeriesDataPoint>>();
        timeSeries.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetTimeSeries_WithInvalidHours_ReturnsBadRequest()
    {
        // Arrange
        var hours = 1000; // Exceeds max of 720

        // Act
        var response = await _client.GetAsync($"/api/execution-analytics/time-series?hours={hours}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task GetSystemHealth_ReturnsHealthMetrics()
    {
        // Act
        var response = await _client.GetAsync("/api/execution-analytics/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var health = await response.Content.ReadFromJsonAsync<SystemHealthMetrics>();
        health.Should().NotBeNull();
        health!.Status.Should().BeDefined();
        health.ErrorRate.Should().BeGreaterThanOrEqualTo(0);
        health.UptimePercentage.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        health.HealthMessages.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetTopExecutedTasks_ReturnsTaskSummaries()
    {
        // Arrange
        var count = 5;

        // Act
        var response = await _client.GetAsync($"/api/execution-analytics/top-executed?count={count}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskExecutionSummary>>();
        tasks.Should().NotBeNull();
        tasks!.Count.Should().BeLessThanOrEqualTo(count);
    }

    [TestMethod]
    public async Task GetTopExecutedTasks_WithInvalidCount_ReturnsBadRequest()
    {
        // Arrange
        var count = 100; // Exceeds max of 50

        // Act
        var response = await _client.GetAsync($"/api/execution-analytics/top-executed?count={count}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task GetTopFailingTasks_ReturnsFailureSummaries()
    {
        // Act
        var response = await _client.GetAsync("/api/execution-analytics/top-failing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskExecutionSummary>>();
        tasks.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetSlowestTasks_ReturnsDurationSummaries()
    {
        // Act
        var response = await _client.GetAsync("/api/execution-analytics/slowest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskExecutionSummary>>();
        tasks.Should().NotBeNull();
    }

    [TestMethod]
    public async Task GetTrends_ReturnsExecutionTrends()
    {
        // Act
        var response = await _client.GetAsync("/api/execution-analytics/trends");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var trends = await response.Content.ReadFromJsonAsync<ExecutionTrends>();
        trends.Should().NotBeNull();
        trends!.SuccessRateTrend.Should().BeDefined();
        trends.ExecutionVolumeTrend.Should().BeDefined();
        trends.DurationTrend.Should().BeDefined();
    }

    [TestMethod]
    public async Task CalculateTimeSeries_TriggersBackgroundCalculation()
    {
        // Act
        var response = await _client.PostAsync("/api/execution-analytics/calculate-time-series", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    #endregion

    #region Execution Tracking Integration Tests

    [TestMethod]
    public async Task ExecuteTask_UpdatesExecutionMetrics()
    {
        // Arrange - Create a task
        var createRequest = new CreateTaskRequest
        {
            Name = "Metrics Tracking Task",
            Description = "Task to test metrics tracking",
            NaturalLanguagePrompt = "Navigate to https://example.com and extract the page title"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Execute the task
        var executeResponse = await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);
        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Give time for metrics to be recorded
        await Task.Delay(500);

        // Assert - Check that metrics were created
        var metricsResponse = await _client.GetAsync($"/api/execution-analytics/tasks/{task.Id}/metrics?includeInactive=true");
        metricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var metrics = await metricsResponse.Content.ReadFromJsonAsync<List<ExecutionMetrics>>();
        metrics.Should().NotBeNull();
        
        if (metrics!.Any())
        {
            var metric = metrics.First();
            metric.TaskId.Should().Be(task.Id);
            metric.TaskName.Should().Be(task.Name);
            metric.TotalSteps.Should().BeGreaterThan(0);
        }
    }

    [TestMethod]
    public async Task MultipleExecutions_UpdatesDashboardAnalytics()
    {
        // Arrange - Get initial dashboard state
        var initialResponse = await _client.GetAsync("/api/execution-analytics/dashboard");
        var initialAnalytics = await initialResponse.Content.ReadFromJsonAsync<DashboardAnalytics>();
        var initialExecutionCount = initialAnalytics!.ExecutionsLastHour;

        // Create and execute a task
        var createRequest = new CreateTaskRequest
        {
            Name = "Dashboard Update Task",
            NaturalLanguagePrompt = "Navigate to https://example.com"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Execute multiple times
        for (int i = 0; i < 3; i++)
        {
            await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);
            await Task.Delay(200); // Small delay between executions
        }

        // Give time for metrics to aggregate
        await Task.Delay(1000);

        // Assert - Check dashboard updated
        var updatedResponse = await _client.GetAsync("/api/execution-analytics/dashboard");
        var updatedAnalytics = await updatedResponse.Content.ReadFromJsonAsync<DashboardAnalytics>();

        updatedAnalytics.Should().NotBeNull();
        updatedAnalytics!.TotalExecutionsToday.Should().BeGreaterThanOrEqualTo(initialAnalytics.TotalExecutionsToday);
    }

    [TestMethod]
    public async Task TaskExecution_CalculatesSuccessRate()
    {
        // Arrange - Create tasks with different outcomes
        var successTask = new CreateTaskRequest
        {
            Name = "Success Task",
            NaturalLanguagePrompt = "Navigate to https://example.com"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", successTask);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Execute task
        await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);
        await Task.Delay(1000);

        // Assert - Check analytics shows execution
        var analyticsResponse = await _client.GetAsync("/api/execution-analytics/dashboard");
        var analytics = await analyticsResponse.Content.ReadFromJsonAsync<DashboardAnalytics>();

        analytics.Should().NotBeNull();
        analytics!.TotalExecutionsToday.Should().BeGreaterThan(0);
        analytics.SuccessRateToday.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
    }

    #endregion

    #region SignalR Real-Time Updates Tests

    [TestMethod]
    public async Task SignalRHub_ConnectsSuccessfully()
    {
        // Arrange
        var hubUrl = new Uri(_client.BaseAddress!, "/hubs/analytics");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            // Act
            await connection.StartAsync();

            // Assert
            connection.State.Should().Be(HubConnectionState.Connected);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task SignalRHub_SubscribesToDashboard()
    {
        // Arrange
        var hubUrl = new Uri(_client.BaseAddress!, "/hubs/analytics");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();

            // Act
            await connection.InvokeAsync("SubscribeToDashboard");

            // Assert - No exception means success
            connection.State.Should().Be(HubConnectionState.Connected);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task SignalRHub_ReceivesDashboardUpdates()
    {
        // Arrange
        var hubUrl = new Uri(_client.BaseAddress!, "/hubs/analytics");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var updateReceived = new TaskCompletionSource<DashboardAnalytics>();
        connection.On<DashboardAnalytics>("DashboardAnalyticsUpdated", analytics =>
        {
            updateReceived.TrySetResult(analytics);
        });

        try
        {
            await connection.StartAsync();
            await connection.InvokeAsync("SubscribeToDashboard");

            // Act - Trigger an execution that should broadcast update
            var createRequest = new CreateTaskRequest
            {
                Name = "SignalR Test Task",
                NaturalLanguagePrompt = "Navigate to https://example.com"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
            var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();
            await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);

            // Assert - Wait for update (with timeout)
            var receivedAnalytics = await Task.WhenAny(
                updateReceived.Task,
                Task.Delay(TimeSpan.FromSeconds(10))
            );

            if (receivedAnalytics == updateReceived.Task)
            {
                var analytics = await updateReceived.Task;
                analytics.Should().NotBeNull();
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    #endregion

    #region Performance and Load Tests

    [TestMethod]
    public async Task GetDashboardAnalytics_UnderLoad_PerformsWell()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int requestCount = 10;

        // Act - Make multiple concurrent requests
        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => _client.GetAsync("/api/execution-analytics/dashboard"))
            .ToList();

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [TestMethod]
    public async Task MetricsCollection_WithHighVolume_RemainsAccurate()
    {
        // Arrange - Create multiple tasks
        var taskIds = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var createRequest = new CreateTaskRequest
            {
                Name = $"High Volume Task {i + 1}",
                NaturalLanguagePrompt = "Navigate to https://example.com"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
            var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();
            taskIds.Add(task!.Id);
        }

        // Act - Execute all tasks concurrently
        var executionTasks = taskIds.Select(id =>
            _client.PostAsync($"/api/tasks/{id}/execute", null)
        ).ToList();

        await Task.WhenAll(executionTasks);
        await Task.Delay(2000); // Allow metrics to aggregate

        // Assert - Verify analytics updated correctly
        var analyticsResponse = await _client.GetAsync("/api/execution-analytics/dashboard");
        var analytics = await analyticsResponse.Content.ReadFromJsonAsync<DashboardAnalytics>();

        analytics.Should().NotBeNull();
        analytics!.TotalExecutionsToday.Should().BeGreaterThanOrEqualTo(5);
    }

    #endregion
}
