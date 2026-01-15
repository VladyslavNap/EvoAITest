using EvoAITest.Core.Models.Analytics;

namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for exporting analytics data in various formats
/// </summary>
public interface IAnalyticsExportService
{
    /// <summary>
    /// Export dashboard statistics to JSON
    /// </summary>
    Task<byte[]> ExportDashboardToJsonAsync(
        DashboardStatistics statistics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export dashboard statistics to CSV
    /// </summary>
    Task<byte[]> ExportDashboardToCsvAsync(
        DashboardStatistics statistics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export flaky tests to JSON
    /// </summary>
    Task<byte[]> ExportFlakyTestsToJsonAsync(
        List<FlakyTestAnalysis> flakyTests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export flaky tests to CSV
    /// </summary>
    Task<byte[]> ExportFlakyTestsToCsvAsync(
        List<FlakyTestAnalysis> flakyTests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export trends to JSON
    /// </summary>
    Task<byte[]> ExportTrendsToJsonAsync(
        List<TestTrend> trends,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export trends to CSV
    /// </summary>
    Task<byte[]> ExportTrendsToCsvAsync(
        List<TestTrend> trends,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export recording insights to JSON
    /// </summary>
    Task<byte[]> ExportRecordingInsightsToJsonAsync(
        RecordingInsights insights,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export recording insights to CSV
    /// </summary>
    Task<byte[]> ExportRecordingInsightsToCsvAsync(
        RecordingInsights insights,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate comprehensive analytics report (HTML format)
    /// </summary>
    Task<byte[]> GenerateAnalyticsReportAsync(
        DashboardStatistics statistics,
        List<FlakyTestAnalysis> flakyTests,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Export format enumeration
/// </summary>
public enum ExportFormat
{
    Json,
    Csv,
    Html
}

/// <summary>
/// Export options
/// </summary>
public sealed class ExportOptions
{
    public ExportFormat Format { get; set; } = ExportFormat.Json;
    public bool IncludeTimestamp { get; set; } = true;
    public bool PrettyPrint { get; set; } = true;
    public string? CustomFileName { get; set; }
}
