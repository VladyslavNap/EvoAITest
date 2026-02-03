# Dashboard Analytics Guide

**Real-Time Test Execution Monitoring and Performance Analytics**

## Overview

The Dashboard Analytics feature provides real-time monitoring and comprehensive analytics for test execution. Built with SignalR for live updates, it offers instant visibility into system performance, execution metrics, and health status.

## Features

### 🏃 Real-Time Execution Tracking
- **Live Progress Updates** - Watch tasks execute in real-time with progress bars
- **Active Execution Count** - See how many tasks are currently running
- **Step-by-Step Monitoring** - Track current action and completion percentage
- **Duration Tracking** - Monitor execution time as tasks run

### 📊 Performance Metrics
- **Success Rates** - Track success rates for last hour, 24 hours, and today
- **Average Duration** - Monitor execution times and performance trends
- **Execution Volume** - See task execution frequency and patterns
- **System Throughput** - Analyze executions per time unit

### 📈 Trend Analysis
- **Time Series Charts** - Visualize metrics over time (hourly/daily)
- **Success Rate Trends** - Identify improvements or degradations
- **Volume Trends** - Track execution frequency changes
- **Duration Trends** - Monitor performance improvements

### 🔧 Healing Analytics
- **Healing Success Rate** - Track self-healing effectiveness
- **Tasks with Healing** - Monitor healing-enabled task count
- **Recovery Patterns** - Analyze common failure recovery scenarios

### 💚 System Health Monitoring
- **Health Status** - Real-time system health indicator
- **Error Rate** - Track failure percentage
- **Uptime Percentage** - Monitor system availability
- **Consecutive Failures** - Early warning for systemic issues
- **Health Messages** - Contextual system status information

### 📋 Task Insights
- **Top Executed Tasks** - Most frequently run tasks
- **Top Failing Tasks** - Tasks with highest failure rates
- **Slowest Tasks** - Tasks with longest execution times

## Getting Started

### Prerequisites

1. **.NET 10 SDK** - Latest .NET runtime
2. **SQL Server** - For metrics persistence
3. **Azure OpenAI or Ollama** - For AI features
4. **Browser** - Modern browser with JavaScript enabled

### Setup

1. **Database Migration**

   Create the analytics tables:
   ```bash
   cd EvoAITest.Core
   dotnet ef migrations add AddExecutionAnalytics
   dotnet ef database update
   ```

2. **Configuration**

   Analytics are enabled by default. No additional configuration needed.

3. **Start the Application**

   ```bash
   dotnet run --project EvoAITest.AppHost
   ```

4. **Access the Dashboard**

   Navigate to `/execution-dashboard` in your browser.

## Dashboard UI

### Main Dashboard (`/execution-dashboard`)

The main dashboard provides a comprehensive view of all execution metrics:

#### Key Performance Indicators (KPIs)

Six metric cards display critical information:

1. **Active Executions** - Current running tasks
2. **Success Rate (24h)** - Last 24 hours success percentage
3. **Avg Duration (24h)** - Average execution time
4. **Executions Today** - Total tasks run today
5. **System Health** - Overall system status
6. **Healing Success** - Self-healing effectiveness

Each KPI card shows:
- Current value
- Trend indicator (↑ up, ↓ down, → stable)
- Percentage change from previous period
- Additional context (sub-values)

#### Active Executions Panel

Real-time view of running tasks showing:
- Task name and target URL
- Current action being performed
- Progress bar with completion percentage
- Current step / total steps
- Elapsed time
- Start timestamp

#### Time Series Charts

Two interactive charts visualize trends:

1. **Success Rate Trend (24 Hours)**
   - Hourly aggregation
   - Line chart with fill
   - Shows success rate percentage over time

2. **Execution Volume (7 Days)**
   - Daily aggregation
   - Bar/line chart
   - Shows execution count over time

#### Task Summary Tables

Three tables provide insights:

1. **Most Executed Tasks**
   - Task name
   - Execution count
   - Success rate
   - Last execution time

2. **Top Failing Tasks**
   - Tasks with highest failure rates
   - Highlighted in red for easy identification
   - Execution count and failure percentage

3. **Slowest Tasks**
   - Tasks with longest average duration
   - Execution count
   - Average duration (formatted)

#### System Health Panel

Detailed health metrics:
- **Error Rate** - Percentage of failed executions
- **Avg Response Time** - Average task duration
- **Consecutive Failures** - Current failure streak
- **Health Messages** - Contextual status information

### Real-Time Updates

Dashboard automatically updates via SignalR when:
- New execution starts
- Execution progress updates
- Execution completes
- System metrics change

Connection status indicator shows:
- 🟢 Live - Connected and receiving updates
- 🔴 Connecting... - Attempting connection

## API Endpoints

### Dashboard Analytics

```http
GET /api/execution-analytics/dashboard
```

Returns comprehensive dashboard analytics:
- Active executions
- Execution metrics (24h, last hour, today)
- Success rates and trends
- System health
- Time series data
- Top tasks

**Response:**
```json
{
  "calculatedAt": "2025-01-20T10:30:00Z",
  "activeExecutionCount": 3,
  "executionsLast24Hours": 156,
  "successRateLast24Hours": 94.2,
  "averageDurationMsLast24Hours": 4532,
  "totalExecutionsToday": 42,
  "successRateToday": 95.2,
  "systemHealth": {
    "status": "Healthy",
    "errorRate": 5.8,
    "uptimePercentage": 99.8
  },
  "trends": {
    "successRateTrend": "Up",
    "executionVolumeTrend": "Stable",
    "durationTrend": "Down"
  }
}
```

### Active Executions

```http
GET /api/execution-analytics/active
```

Returns currently running executions:
```json
[
  {
    "taskId": "guid",
    "taskName": "Login Test",
    "currentStep": 3,
    "totalSteps": 5,
    "completionPercentage": 60.0,
    "durationMs": 2341,
    "currentAction": "Clicking submit button"
  }
]
```

### Time Series Data

```http
GET /api/execution-analytics/time-series?metricType=SuccessRate&interval=Hour&hours=24
```

Query parameters:
- `metricType` - SuccessRate, ExecutionCount, AverageDuration, etc.
- `interval` - Minute, Hour, Day, Week, Month
- `hours` - Number of hours to look back (1-720)
- `taskId` - Optional: filter by specific task

### System Health

```http
GET /api/execution-analytics/health
```

Returns system health metrics:
```json
{
  "status": "Healthy",
  "errorRate": 4.2,
  "averageResponseTimeMs": 3821,
  "consecutiveFailures": 0,
  "uptimePercentage": 99.9,
  "healthMessages": ["System operating normally"]
}
```

### Top Tasks

```http
GET /api/execution-analytics/top-executed?count=10
GET /api/execution-analytics/top-failing?count=10
GET /api/execution-analytics/slowest?count=10
```

### Execution Trends

```http
GET /api/execution-analytics/trends
```

Returns trend analysis:
```json
{
  "successRateTrend": "Up",
  "executionVolumeTrend": "Stable",
  "durationTrend": "Down",
  "successRateChangePercent": 5.2,
  "executionVolumeChangePercent": -2.1,
  "durationChangePercent": -8.4
}
```

### Task-Specific Metrics

```http
GET /api/execution-analytics/tasks/{taskId}/metrics?includeInactive=true
```

Returns all metrics for a specific task.

## SignalR Hub

### Connection

Connect to the analytics hub:
```javascript
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/analytics')
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### Subscriptions

Subscribe to updates:
```javascript
// Subscribe to dashboard updates
await connection.invoke('SubscribeToDashboard');

// Subscribe to metrics
await connection.invoke('SubscribeToMetrics');

// Subscribe to specific task
await connection.invoke('SubscribeToTask', taskId);
```

### Events

Listen for real-time events:
```javascript
// Dashboard analytics updated
connection.on('DashboardAnalyticsUpdated', (analytics) => {
  console.log('Dashboard updated:', analytics);
});

// Execution started
connection.on('ExecutionStarted', (data) => {
  console.log('Execution started:', data);
});

// Execution progress
connection.on('ExecutionProgress', (data) => {
  console.log('Progress:', data);
});

// Execution completed
connection.on('ExecutionCompleted', (data) => {
  console.log('Execution completed:', data);
});

// Active executions updated
connection.on('ActiveExecutionsUpdated', (data) => {
  console.log('Active executions:', data);
});

// System health updated
connection.on('SystemHealthUpdated', (health) => {
  console.log('System health:', health);
});
```

## Data Models

### ExecutionMetrics

Captures real-time execution state:
```csharp
public sealed class ExecutionMetrics
{
    public Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public ExecutionStatus Status { get; set; }
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string? CurrentAction { get; set; }
    public long DurationMs { get; set; }
    public bool IsActive { get; set; }
    public double CompletionPercentage { get; set; }
    public bool HealingAttempted { get; set; }
    public bool HealingSuccessful { get; set; }
}
```

### TimeSeriesDataPoint

Historical metric aggregation:
```csharp
public sealed class TimeSeriesDataPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public TimeInterval Interval { get; set; }
    public MetricType MetricType { get; set; }
    public double Value { get; set; }
    public int ExecutionCount { get; set; }
    public double SuccessRate { get; set; }
    public long AverageDurationMs { get; set; }
}
```

### DashboardAnalytics

Comprehensive dashboard data:
```csharp
public sealed class DashboardAnalytics
{
    public List<ActiveExecutionInfo> ActiveExecutions { get; set; }
    public int ActiveExecutionCount { get; set; }
    public int ExecutionsLast24Hours { get; set; }
    public double SuccessRateLast24Hours { get; set; }
    public long AverageDurationMsLast24Hours { get; set; }
    public SystemHealthMetrics SystemHealth { get; set; }
    public ExecutionTrends Trends { get; set; }
    public List<TimeSeriesDataPoint> TimeSeriesLast24Hours { get; set; }
    public List<TaskExecutionSummary> TopExecutedTasks { get; set; }
}
```

## Architecture

### Components

1. **AnalyticsService** (`IAnalyticsService`)
   - Core analytics logic
   - Metric calculation and aggregation
   - Database queries and persistence

2. **ExecutionTrackingService**
   - Coordinates analytics and SignalR
   - Tracks execution lifecycle
   - Broadcasts real-time updates

3. **ExecutionAnalyticsController**
   - REST API endpoints
   - Response caching
   - Error handling

4. **AnalyticsHub** (SignalR)
   - Real-time communication
   - Client subscriptions
   - Event broadcasting

5. **ExecutionDashboard** (Blazor)
   - UI components
   - SignalR client
   - Chart visualization

### Data Flow

```
Execution Starts
      ↓
ExecutionTrackingService
      ↓
AnalyticsService.RecordMetrics
      ↓
Database (ExecutionMetrics)
      ↓
SignalR Broadcast
      ↓
Dashboard UI Update
```

### Background Processing

**AnalyticsBroadcastService** - Background service that:
- Broadcasts dashboard updates every 30 seconds
- Calculates trends every 5 minutes
- Monitors system health
- Throttles updates to prevent overload

**Time Series Calculation** - Periodic aggregation:
- Runs hourly for hourly intervals
- Runs daily for daily intervals
- Stores aggregated metrics
- Enables efficient trend queries

## Best Practices

### Performance

1. **Enable Response Caching**
   - Dashboard endpoint: 10 seconds
   - Time series: 60 seconds
   - Trends: 5 minutes

2. **Use SignalR for Real-Time**
   - Subscribe only to needed updates
   - Unsubscribe when component unmounts
   - Use automatic reconnection

3. **Optimize Queries**
   - Use indexed columns
   - Limit result sets
   - Aggregate in database

### Monitoring

1. **Track Key Metrics**
   - Success rates trending down
   - Error rates trending up
   - Consecutive failures > 3
   - Duration increases

2. **Set Up Alerts**
   - Success rate drops below 90%
   - Error rate exceeds 10%
   - System health degraded
   - Consecutive failures detected

3. **Regular Review**
   - Weekly trend analysis
   - Monthly performance review
   - Identify optimization opportunities

### Troubleshooting

**Dashboard not updating:**
- Check SignalR connection status
- Verify CORS configuration
- Check browser console for errors
- Ensure AnalyticsBroadcastService is running

**Metrics not recording:**
- Verify database connection
- Check ExecutionTrackingService registration
- Review application logs
- Confirm migrations applied

**Slow performance:**
- Check database indexes
- Review query execution plans
- Enable response caching
- Consider data retention policy

## Integration Examples

### C# Client

```csharp
using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7001/hubs/analytics")
    .WithAutomaticReconnect()
    .Build();

connection.On<DashboardAnalytics>("DashboardAnalyticsUpdated", analytics =>
{
    Console.WriteLine($"Active: {analytics.ActiveExecutionCount}");
    Console.WriteLine($"Success Rate: {analytics.SuccessRateLast24Hours}%");
});

await connection.StartAsync();
await connection.InvokeAsync("SubscribeToDashboard");
```

### JavaScript Client

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/analytics')
    .withAutomaticReconnect()
    .build();

connection.on('ExecutionProgress', (data) => {
    updateProgressBar(data.taskId, data.completionPercentage);
});

await connection.start();
await connection.invoke('SubscribeToMetrics');
```

### HTTP Client

```csharp
using HttpClient client = new();
client.BaseAddress = new Uri("https://localhost:7001");

// Get dashboard analytics
var response = await client.GetAsync("/api/execution-analytics/dashboard");
var analytics = await response.Content.ReadFromJsonAsync<DashboardAnalytics>();

// Get active executions
var activeResponse = await client.GetAsync("/api/execution-analytics/active");
var activeExecutions = await activeResponse.Content
    .ReadFromJsonAsync<List<ActiveExecutionInfo>>();
```

## Future Enhancements

Planned features for future releases:

- **Custom Dashboards** - Create personalized views
- **Alert Rules** - Configure custom alerts
- **Export Functionality** - Export metrics to CSV/Excel
- **Comparative Analysis** - Compare periods
- **Predictive Analytics** - ML-powered predictions
- **Mobile App** - Native mobile dashboard
- **Slack/Teams Integration** - Real-time notifications
- **Historical Replay** - Review past executions

## Resources

- [API Reference](API_REFERENCE.md) - Complete API documentation
- [Architecture Guide](ARCHITECTURE.md) - System architecture details
- [SignalR Documentation](https://learn.microsoft.com/aspnet/core/signalr/introduction) - Official SignalR docs
- [Chart.js](https://www.chartjs.org/) - Charting library documentation

## Support

For issues, questions, or feature requests:
- **GitHub Issues**: [Report an issue](https://github.com/VladyslavNap/EvoAITest/issues)
- **Documentation**: [Browse all docs](../DOCUMENTATION_INDEX.md)
- **Examples**: Check integration tests in `EvoAITest.Tests/Integration/DashboardAnalyticsIntegrationTests.cs`

---

**Built with .NET 10, Blazor, SignalR, and Chart.js**
