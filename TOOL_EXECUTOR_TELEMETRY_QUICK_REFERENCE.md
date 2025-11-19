# Tool Executor Telemetry - Quick Reference

## Configuration

Enable detailed telemetry in `appsettings.json`:
```json
{
  "EvoAITest": {
    "Core": {
      "ToolExecutor": {
        "EnableDetailedLogging": true
      }
    }
  }
}
```

## Activity Tags

### Tool Execution
- `tool.name` - Tool name (e.g., "navigate")
- `tool.correlation_id` - Correlation ID
- `tool.success` - Success status (true/false)
- `tool.duration_ms` - Duration in milliseconds
- `tool.attempt_count` - Total attempts made
- `tool.retried` - Whether retries occurred
- `error.type` - Error type (if failed)

### Sequence Execution
- `sequence.tool_count` - Number of tools
- `sequence.success_count` - Successful tools
- `sequence.duration_ms` - Total duration
- `sequence.success` - Overall success

### Fallback Execution
- `fallback.used` - Whether fallback was used
- `fallback.success_tool` - Successful fallback tool
- `fallback.all_failed` - All fallbacks failed

## Metrics

### Counter: tool_executions_total
```promql
# Total executions
tool_executions_total{tool_name="navigate"}

# Success rate
rate(tool_executions_total{tool_status="success"}[5m]) /
rate(tool_executions_total[5m])
```

### Histogram: tool_execution_duration_ms
```promql
# P95 latency
histogram_quantile(0.95, 
  rate(tool_execution_duration_ms_bucket[5m]))

# Average duration
rate(tool_execution_duration_ms_sum[5m]) /
rate(tool_execution_duration_ms_count[5m])
```

### Gauge: active_tool_executions
```promql
# Current active executions
active_tool_executions
```

## Structured Logs

### Log Levels
- **Information**: Success, start, completion
- **Warning**: Retries, transient errors, fallbacks
- **Error**: Failures, terminal errors, validation issues
- **Debug**: Detailed operation info

### Event IDs
| EventId | Event | Level |
|---------|-------|-------|
| 1000 | ExecutingTool | Information |
| 1001 | ToolExecutionSucceeded | Information |
| 1002 | ToolExecutionFailed | Error |
| 1003 | RetryingToolExecution | Warning |
| 2000 | StartingSequenceExecution | Information |
| 2001 | SequenceExecutionCompleted | Information |
| 3000 | StartingFallbackExecution | Information |
| 3001 | FallbackSucceeded | Information |
| 4000 | ToolNotFound | Error |
| 4001 | ParameterValidationFailed | Error |

## Querying

### Aspire Dashboard
**URL:** http://localhost:15888

**Views:**
- Traces ? Distributed trace visualization
- Metrics ? Real-time charts
- Logs ? Structured log stream

### Kusto (Azure Monitor)
```kusto
// Tool success rate
traces
| where customDimensions.EventId == 1001
| summarize count() by ToolName = tostring(customDimensions.ToolName)

// Retry analysis
traces
| where customDimensions.EventId == 1003
| extend Reason = tostring(customDimensions.Reason)
| summarize count() by Reason
```

### Prometheus
```promql
# Success rate by tool
sum(rate(tool_executions_total{tool_status="success"}[5m])) by (tool_name) /
sum(rate(tool_executions_total[5m])) by (tool_name)

# P99 latency
histogram_quantile(0.99, rate(tool_execution_duration_ms_bucket[5m]))
```

## Common Queries

### Find Slow Executions
```kusto
traces
| where customDimensions.EventId == 1001
| extend DurationMs = tolong(customDimensions.DurationMs)
| where DurationMs > 5000
| project timestamp, ToolName = tostring(customDimensions.ToolName), DurationMs
| order by DurationMs desc
```

### Analyze Failures
```kusto
traces
| where customDimensions.EventId == 1002
| extend ToolName = tostring(customDimensions.ToolName)
| extend ErrorType = tostring(customDimensions["error.type"])
| summarize FailureCount = count() by ToolName, ErrorType
| order by FailureCount desc
```

### Track Retry Patterns
```kusto
traces
| where customDimensions.EventId == 1003
| extend ToolName = tostring(customDimensions.ToolName)
| extend Attempt = toint(customDimensions.Attempt)
| summarize RetryCount = count() by ToolName, Attempt
```

## Grafana Dashboard Panels

### 1. Execution Rate
```promql
sum(rate(tool_executions_total[5m])) by (tool_name)
```

### 2. Success Rate (%)
```promql
sum(rate(tool_executions_total{tool_status="success"}[5m])) by (tool_name) /
sum(rate(tool_executions_total[5m])) by (tool_name) * 100
```

### 3. Latency Percentiles
```promql
histogram_quantile(0.50, rate(tool_execution_duration_ms_bucket[5m])) # P50
histogram_quantile(0.95, rate(tool_execution_duration_ms_bucket[5m])) # P95
histogram_quantile(0.99, rate(tool_execution_duration_ms_bucket[5m])) # P99
```

### 4. Active Executions
```promql
active_tool_executions
```

### 5. Error Rate
```promql
sum(rate(tool_executions_total{tool_status="failure"}[5m])) by (tool_name)
```

## Alerts

### High Failure Rate
```promql
(sum(rate(tool_executions_total{tool_status="failure"}[5m])) /
 sum(rate(tool_executions_total[5m]))) > 0.1
```

### High Latency
```promql
histogram_quantile(0.95, rate(tool_execution_duration_ms_bucket[5m])) > 5000
```

### Too Many Retries
```kusto
traces
| where customDimensions.EventId == 1003
| where timestamp > ago(5m)
| summarize RetryCount = count()
| where RetryCount > 100
```

## OpenTelemetry Setup

### Program.cs
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("EvoAITest.ToolExecutor")
        .AddAspireInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter("EvoAITest.ToolExecutor")
        .AddAspireInstrumentation());
```

### Azure Monitor
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAzureMonitorTraceExporter(options =>
        {
            options.ConnectionString = connectionString;
        }))
    .WithMetrics(metrics => metrics
        .AddAzureMonitorMetricExporter(options =>
        {
            options.ConnectionString = connectionString;
        }));
```

## Troubleshooting

### No Traces in Aspire Dashboard
```csharp
// Ensure ActivitySource is registered
.WithTracing(tracing => tracing.AddSource("EvoAITest.ToolExecutor"))
```

### No Metrics Showing
```csharp
// Ensure Meter is registered
.WithMetrics(metrics => metrics.AddMeter("EvoAITest.ToolExecutor"))
```

### Logs Not Structured
- Check LoggerMessage source generators are running
- Verify `Microsoft.Extensions.Logging.Abstractions` >= 6.0

## Related Documentation
- [Full Telemetry Summary](TOOL_EXECUTOR_TELEMETRY_SUMMARY.md)
- [Tool Executor Summary](DEFAULT_TOOL_EXECUTOR_SUMMARY.md)
- [OpenTelemetry Docs](https://opentelemetry.io/docs/)
