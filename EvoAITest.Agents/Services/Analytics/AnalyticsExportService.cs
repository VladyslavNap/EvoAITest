using System.Text;
using System.Text.Json;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Analytics;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Agents.Services.Analytics;

/// <summary>
/// Service for exporting analytics data in various formats
/// </summary>
public sealed class AnalyticsExportService : IAnalyticsExportService
{
    private readonly ILogger<AnalyticsExportService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AnalyticsExportService(ILogger<AnalyticsExportService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ExportDashboardToJsonAsync(
        DashboardStatistics statistics,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting dashboard statistics to JSON");

        var json = JsonSerializer.Serialize(statistics, JsonOptions);
        return await Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    public async Task<byte[]> ExportDashboardToCsvAsync(
        DashboardStatistics statistics,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting dashboard statistics to CSV");

        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Metric,Value");
        
        // Summary metrics
        csv.AppendLine($"Total Executions,{statistics.TotalExecutions}");
        csv.AppendLine($"Total Tests,{statistics.TotalTests}");
        csv.AppendLine($"Total Recordings,{statistics.TotalRecordings}");
        csv.AppendLine($"Overall Pass Rate,{statistics.OverallPassRate:F2}%");
        csv.AppendLine($"Flaky Test Count,{statistics.FlakyTestCount}");
        csv.AppendLine($"Stable Test Count,{statistics.StableTestCount}");
        csv.AppendLine($"Average Execution Duration (ms),{statistics.AverageExecutionDurationMs}");
        csv.AppendLine($"Total Execution Time (hours),{statistics.TotalExecutionTimeHours:F2}");
        csv.AppendLine($"Test Suite Health,{statistics.Health}");
        
        // Time-based metrics
        csv.AppendLine($"Executions Last 24 Hours,{statistics.ExecutionsLast24Hours}");
        csv.AppendLine($"Executions Last 7 Days,{statistics.ExecutionsLast7Days}");
        csv.AppendLine($"Executions Last 30 Days,{statistics.ExecutionsLast30Days}");
        csv.AppendLine($"Pass Rate Last 24 Hours,{statistics.PassRateLast24Hours:F2}%");
        csv.AppendLine($"Pass Rate Last 7 Days,{statistics.PassRateLast7Days:F2}%");
        csv.AppendLine($"Pass Rate Last 30 Days,{statistics.PassRateLast30Days:F2}%");

        return await Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public async Task<byte[]> ExportFlakyTestsToJsonAsync(
        List<FlakyTestAnalysis> flakyTests,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {Count} flaky tests to JSON", flakyTests.Count);

        var json = JsonSerializer.Serialize(flakyTests, JsonOptions);
        return await Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    public async Task<byte[]> ExportFlakyTestsToCsvAsync(
        List<FlakyTestAnalysis> flakyTests,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {Count} flaky tests to CSV", flakyTests.Count);

        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Test Name,Recording ID,Flakiness Score,Severity,Total Executions,Flaky Failures,Pass Rate,Duration Variability,Confidence,Analyzed At");
        
        // Data rows
        foreach (var test in flakyTests)
        {
            csv.AppendLine($"\"{EscapeCsv(test.TestName)}\",{test.RecordingSessionId},{test.FlakinessScore:F2},{test.Severity},{test.TotalExecutions},{test.FlakyFailureCount},{test.PassRate:F2},{test.DurationVariability:F4},{test.AnalysisConfidence:F2},{test.AnalyzedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return await Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public async Task<byte[]> ExportTrendsToJsonAsync(
        List<TestTrend> trends,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {Count} trends to JSON", trends.Count);

        var json = JsonSerializer.Serialize(trends, JsonOptions);
        return await Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    public async Task<byte[]> ExportTrendsToCsvAsync(
        List<TestTrend> trends,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {Count} trends to CSV", trends.Count);

        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Timestamp,Interval,Total Executions,Passed,Failed,Skipped,Pass Rate,Avg Duration (ms),Min Duration (ms),Max Duration (ms),Unique Tests,Flaky Tests");
        
        // Data rows
        foreach (var trend in trends.OrderBy(t => t.Timestamp))
        {
            csv.AppendLine($"{trend.Timestamp:yyyy-MM-dd HH:mm:ss},{trend.Interval},{trend.TotalExecutions},{trend.PassedExecutions},{trend.FailedExecutions},{trend.SkippedExecutions},{trend.PassRate:F2},{trend.AverageDurationMs},{trend.MinDurationMs},{trend.MaxDurationMs},{trend.UniqueTestCount},{trend.FlakyTestCount}");
        }

        return await Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public async Task<byte[]> ExportRecordingInsightsToJsonAsync(
        RecordingInsights insights,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting recording insights to JSON for {TestName}", insights.TestName);

        var json = JsonSerializer.Serialize(insights, JsonOptions);
        return await Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    public async Task<byte[]> ExportRecordingInsightsToCsvAsync(
        RecordingInsights insights,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting recording insights to CSV for {TestName}", insights.TestName);

        var csv = new StringBuilder();
        
        // Summary section
        csv.AppendLine("Recording Insights Summary");
        csv.AppendLine($"Test Name,{EscapeCsv(insights.TestName)}");
        csv.AppendLine($"Recording ID,{insights.RecordingSessionId}");
        csv.AppendLine($"Pass Rate,{insights.PassRate:F2}%");
        csv.AppendLine($"Baseline Duration (ms),{insights.BaselineDurationMs}");
        csv.AppendLine($"Performance Degrading,{insights.PerformanceDegrading}");
        csv.AppendLine("");

        // Statistics section
        csv.AppendLine("Execution Statistics");
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Total Executions,{insights.Statistics.TotalExecutions}");
        csv.AppendLine($"Last 7 Days,{insights.Statistics.Last7DaysExecutions}");
        csv.AppendLine($"Last 30 Days,{insights.Statistics.Last30DaysExecutions}");
        csv.AppendLine($"Average Duration (ms),{insights.Statistics.AverageDurationMs}");
        csv.AppendLine($"Fastest Duration (ms),{insights.Statistics.FastestDurationMs}");
        csv.AppendLine($"Slowest Duration (ms),{insights.Statistics.SlowestDurationMs}");
        csv.AppendLine("");

        // Issues section
        if (insights.Issues.Any())
        {
            csv.AppendLine("Identified Issues");
            foreach (var issue in insights.Issues)
            {
                csv.AppendLine($"\"{EscapeCsv(issue)}\"");
            }
            csv.AppendLine("");
        }

        // Recommendations section
        if (insights.Recommendations.Any())
        {
            csv.AppendLine("Recommendations");
            foreach (var recommendation in insights.Recommendations)
            {
                csv.AppendLine($"\"{EscapeCsv(recommendation)}\"");
            }
        }

        return await Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public async Task<byte[]> GenerateAnalyticsReportAsync(
        DashboardStatistics statistics,
        List<FlakyTestAnalysis> flakyTests,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating comprehensive analytics report");

        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("    <title>EvoAITest Analytics Report</title>");
        html.AppendLine("    <style>");
        html.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
        html.AppendLine("        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }");
        html.AppendLine("        h1 { color: #333; border-bottom: 3px solid #007bff; padding-bottom: 10px; }");
        html.AppendLine("        h2 { color: #555; margin-top: 30px; }");
        html.AppendLine("        .metric-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin: 20px 0; }");
        html.AppendLine("        .metric-card { background: #f8f9fa; padding: 20px; border-radius: 6px; border-left: 4px solid #007bff; }");
        html.AppendLine("        .metric-value { font-size: 32px; font-weight: bold; color: #007bff; }");
        html.AppendLine("        .metric-label { font-size: 14px; color: #666; text-transform: uppercase; margin-top: 5px; }");
        html.AppendLine("        .health-excellent { color: #28a745; }");
        html.AppendLine("        .health-good { color: #17a2b8; }");
        html.AppendLine("        .health-fair { color: #ffc107; }");
        html.AppendLine("        .health-poor { color: #dc3545; }");
        html.AppendLine("        table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        html.AppendLine("        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }");
        html.AppendLine("        th { background: #007bff; color: white; }");
        html.AppendLine("        .severity-critical { color: #dc3545; font-weight: bold; }");
        html.AppendLine("        .severity-high { color: #fd7e14; font-weight: bold; }");
        html.AppendLine("        .severity-medium { color: #ffc107; font-weight: bold; }");
        html.AppendLine("        .severity-low { color: #28a745; font-weight: bold; }");
        html.AppendLine("        .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 12px; text-align: center; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");
        
        // Header
        html.AppendLine("        <h1>📊 EvoAITest Analytics Report</h1>");
        html.AppendLine($"        <p>Generated on: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        
        // Dashboard Statistics
        html.AppendLine("        <h2>Dashboard Overview</h2>");
        html.AppendLine("        <div class=\"metric-grid\">");
        html.AppendLine($"            <div class=\"metric-card\">");
        html.AppendLine($"                <div class=\"metric-value\">{statistics.TotalExecutions:N0}</div>");
        html.AppendLine($"                <div class=\"metric-label\">Total Executions</div>");
        html.AppendLine($"            </div>");
        html.AppendLine($"            <div class=\"metric-card\">");
        html.AppendLine($"                <div class=\"metric-value\">{statistics.OverallPassRate:F1}%</div>");
        html.AppendLine($"                <div class=\"metric-label\">Overall Pass Rate</div>");
        html.AppendLine($"            </div>");
        html.AppendLine($"            <div class=\"metric-card\">");
        html.AppendLine($"                <div class=\"metric-value\">{statistics.TotalTests}</div>");
        html.AppendLine($"                <div class=\"metric-label\">Total Tests</div>");
        html.AppendLine($"            </div>");
        html.AppendLine($"            <div class=\"metric-card\">");
        html.AppendLine($"                <div class=\"metric-value\">{statistics.FlakyTestCount}</div>");
        html.AppendLine($"                <div class=\"metric-label\">Flaky Tests</div>");
        html.AppendLine($"            </div>");
        html.AppendLine("        </div>");
        
        html.AppendLine($"        <p><strong>Test Suite Health:</strong> <span class=\"health-{statistics.Health.ToString().ToLower()}\">{statistics.Health}</span></p>");
        
        // Flaky Tests Table
        if (flakyTests.Any())
        {
            html.AppendLine("        <h2>Flaky Tests Detected</h2>");
            html.AppendLine("        <table>");
            html.AppendLine("            <thead>");
            html.AppendLine("                <tr>");
            html.AppendLine("                    <th>Test Name</th>");
            html.AppendLine("                    <th>Severity</th>");
            html.AppendLine("                    <th>Score</th>");
            html.AppendLine("                    <th>Pass Rate</th>");
            html.AppendLine("                    <th>Executions</th>");
            html.AppendLine("                    <th>Flaky Failures</th>");
            html.AppendLine("                </tr>");
            html.AppendLine("            </thead>");
            html.AppendLine("            <tbody>");
            
            foreach (var test in flakyTests.OrderByDescending(t => t.FlakinessScore).Take(20))
            {
                html.AppendLine("                <tr>");
                html.AppendLine($"                    <td>{test.TestName}</td>");
                html.AppendLine($"                    <td class=\"severity-{test.Severity.ToString().ToLower()}\">{test.Severity}</td>");
                html.AppendLine($"                    <td>{test.FlakinessScore:F1}</td>");
                html.AppendLine($"                    <td>{test.PassRate:F1}%</td>");
                html.AppendLine($"                    <td>{test.TotalExecutions}</td>");
                html.AppendLine($"                    <td>{test.FlakyFailureCount}</td>");
                html.AppendLine("                </tr>");
            }
            
            html.AppendLine("            </tbody>");
            html.AppendLine("        </table>");
        }
        
        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine("            <p>EvoAITest - AI-Powered Test Automation Platform</p>");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return await Task.FromResult(Encoding.UTF8.GetBytes(html.ToString()));
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return value.Replace("\"", "\"\"");
        }

        return value;
    }
}
