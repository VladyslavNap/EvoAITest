using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Analytics;
using EvoAITest.ApiService.Models;
using Microsoft.AspNetCore.Mvc;

namespace EvoAITest.ApiService.Controllers;

/// <summary>
/// API controller for test analytics and insights
/// </summary>
[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly ITestAnalyticsService _analyticsService;
    private readonly IFlakyTestDetector _flakyDetector;
    private readonly IAnalyticsExportService _exportService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        ITestAnalyticsService analyticsService,
        IFlakyTestDetector flakyDetector,
        IAnalyticsExportService exportService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _flakyDetector = flakyDetector ?? throw new ArgumentNullException(nameof(flakyDetector));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== QUERY ENDPOINTS ====================

    /// <summary>
    /// Gets comprehensive dashboard statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics including pass rates, flaky tests, and trends</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType<DashboardStatistics>(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByHeader = "User-Agent")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating dashboard statistics");

        try
        {
            var statistics = await _analyticsService.GetDashboardStatisticsAsync(cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate dashboard statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all flaky tests detected in the system
    /// </summary>
    /// <param name="minScore">Minimum flakiness score (0-100)</param>
    /// <param name="severity">Filter by severity level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of flaky test analyses</returns>
    [HttpGet("flaky-tests")]
    [ProducesResponseType<List<FlakyTestAnalysis>>(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "minScore", "severity" })]
    public async Task<IActionResult> GetFlakyTests(
        [FromQuery] double? minScore = null,
        [FromQuery] FlakinessSeverity? severity = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting flaky tests with minScore={MinScore}, severity={Severity}", minScore, severity);

        try
        {
            var flakyTests = await _flakyDetector.GetAllFlakyTestsAsync(cancellationToken: cancellationToken);

            // Apply filters
            if (minScore.HasValue)
            {
                flakyTests = flakyTests.Where(t => t.FlakinessScore >= minScore.Value).ToList();
            }

            if (severity.HasValue)
            {
                flakyTests = flakyTests.Where(t => t.Severity == severity.Value).ToList();
            }

            _logger.LogInformation("Found {Count} flaky tests", flakyTests.Count);
            return Ok(flakyTests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve flaky tests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets test execution trends for a time period
    /// </summary>
    /// <param name="interval">Trend interval (hourly, daily, weekly, monthly)</param>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <param name="recordingId">Optional recording session filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of trend data points</returns>
    [HttpGet("trends")]
    [ProducesResponseType<List<TestTrend>>(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "interval", "days", "recordingId" })]
    public async Task<IActionResult> GetTrends(
        [FromQuery] TrendInterval interval = TrendInterval.Daily,
        [FromQuery] int days = 30,
        [FromQuery] Guid? recordingId = null,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0 || days > 365)
        {
            return BadRequest(new { error = "Days must be between 1 and 365" });
        }

        _logger.LogInformation(
            "Getting {Interval} trends for last {Days} days, recordingId={RecordingId}",
            interval,
            days,
            recordingId);

        try
        {
            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddDays(-days);

            var trends = await _analyticsService.CalculateTrendsAsync(
                interval,
                startDate,
                endDate,
                recordingId,
                cancellationToken);

            _logger.LogInformation("Retrieved {Count} trend data points", trends.Count);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate trends");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets insights for a specific recording session
    /// </summary>
    /// <param name="recordingId">Recording session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recording-specific insights and recommendations</returns>
    [HttpGet("recordings/{recordingId:guid}/insights")]
    [ProducesResponseType<RecordingInsights>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecordingInsights(
        Guid recordingId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting insights for recording {RecordingId}", recordingId);

        try
        {
            var insights = await _analyticsService.GetRecordingInsightsAsync(recordingId, cancellationToken);
            return Ok(insights);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Recording {RecordingId} not found", recordingId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate insights for recording {RecordingId}", recordingId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets trends for a specific recording session
    /// </summary>
    /// <param name="recordingId">Recording session ID</param>
    /// <param name="interval">Trend interval</param>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recording-specific trends</returns>
    [HttpGet("recordings/{recordingId:guid}/trends")]
    [ProducesResponseType<List<TestTrend>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecordingTrends(
        Guid recordingId,
        [FromQuery] TrendInterval interval = TrendInterval.Daily,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0 || days > 365)
        {
            return BadRequest(new { error = "Days must be between 1 and 365" });
        }

        _logger.LogInformation(
            "Getting {Interval} trends for recording {RecordingId}, last {Days} days",
            interval,
            recordingId,
            days);

        try
        {
            var trends = await _analyticsService.GetRecordingTrendsAsync(
                recordingId,
                interval,
                days,
                cancellationToken);

            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trends for recording {RecordingId}", recordingId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets top failing tests
    /// </summary>
    /// <param name="count">Number of tests to return (max 50)</param>
    /// <param name="days">Days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top failing tests</returns>
    [HttpGet("top-failing")]
    [ProducesResponseType<List<TestExecutionSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopFailingTests(
        [FromQuery] int count = 10,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0 || count > 50)
        {
            return BadRequest(new { error = "Count must be between 1 and 50" });
        }

        _logger.LogInformation("Getting top {Count} failing tests for last {Days} days", count, days);

        try
        {
            var tests = await _analyticsService.GetTopFailingTestsAsync(count, days, cancellationToken);
            return Ok(tests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top failing tests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets slowest tests by average execution duration
    /// </summary>
    /// <param name="count">Number of tests to return (max 50)</param>
    /// <param name="days">Days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of slowest tests</returns>
    [HttpGet("slowest")]
    [ProducesResponseType<List<TestExecutionSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlowestTests(
        [FromQuery] int count = 10,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0 || count > 50)
        {
            return BadRequest(new { error = "Count must be between 1 and 50" });
        }

        _logger.LogInformation("Getting top {Count} slowest tests for last {Days} days", count, days);

        try
        {
            var tests = await _analyticsService.GetSlowestTestsAsync(count, days, cancellationToken);
            return Ok(tests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get slowest tests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets most executed tests
    /// </summary>
    /// <param name="count">Number of tests to return (max 50)</param>
    /// <param name="days">Days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of most executed tests</returns>
    [HttpGet("most-executed")]
    [ProducesResponseType<List<TestExecutionSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMostExecutedTests(
        [FromQuery] int count = 10,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0 || count > 50)
        {
            return BadRequest(new { error = "Count must be between 1 and 50" });
        }

        _logger.LogInformation("Getting top {Count} most executed tests for last {Days} days", count, days);

        try
        {
            var tests = await _analyticsService.GetMostExecutedTestsAsync(count, days, cancellationToken);
            return Ok(tests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get most executed tests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Analyzes a recording for flakiness
    /// </summary>
    /// <param name="recordingId">Recording session ID</param>
    /// <param name="request">Analysis criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Flaky test analysis</returns>
    [HttpPost("recordings/{recordingId:guid}/analyze-flakiness")]
    [ProducesResponseType<FlakyTestAnalysis>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeRecordingFlakiness(
        Guid recordingId,
        [FromBody] AnalyzeFlakinessRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing flakiness for recording {RecordingId}", recordingId);

        try
        {
            var criteria = request?.ToFlakyCriteria() ?? FlakyCriteria.Default;

            var analysis = await _flakyDetector.AnalyzeRecordingAsync(
                recordingId,
                criteria,
                cancellationToken);

            // Save analysis to database
            await _analyticsService.SaveFlakyTestAnalysisAsync(analysis, cancellationToken);

            return Ok(analysis);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Recording {RecordingId} not found", recordingId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze recording {RecordingId}", recordingId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets stability metrics for a recording
    /// </summary>
    /// <param name="recordingId">Recording session ID</param>
    /// <param name="windowDays">Number of days to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stability metrics</returns>
    [HttpGet("recordings/{recordingId:guid}/stability")]
    [ProducesResponseType<TestStabilityMetrics>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStabilityMetrics(
        Guid recordingId,
        [FromQuery] int windowDays = 30,
        CancellationToken cancellationToken = default)
    {
        if (windowDays <= 0 || windowDays > 365)
        {
            return BadRequest(new { error = "Window days must be between 1 and 365" });
        }

        _logger.LogInformation(
            "Getting stability metrics for recording {RecordingId}, window={Days} days",
            recordingId,
            windowDays);

        try
        {
            var metrics = await _flakyDetector.CalculateStabilityMetricsAsync(
                recordingId,
                windowDays,
                cancellationToken);

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate stability metrics for recording {RecordingId}", recordingId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculates and saves trends for a time period
    /// </summary>
    /// <param name="request">Trend calculation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Calculated trends</returns>
    [HttpPost("calculate-trends")]
    [ProducesResponseType<List<TestTrend>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateAndSaveTrends(
        [FromBody] CalculateTrendsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating {Interval} trends from {Start} to {End}",
            request.Interval,
            request.StartDate,
            request.EndDate);

        try
        {
            var trends = await _analyticsService.CalculateTrendsAsync(
                request.Interval,
                request.StartDate,
                request.EndDate,
                request.RecordingSessionId,
                cancellationToken);

            // Save trends to database
            await _analyticsService.SaveTrendsAsync(trends, cancellationToken);

            _logger.LogInformation("Calculated and saved {Count} trends", trends.Count);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate trends");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets historical trends from database
    /// </summary>
    /// <param name="interval">Trend interval</param>
    /// <param name="days">Number of days to retrieve</param>
    /// <param name="recordingId">Optional recording filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical trends</returns>
    [HttpGet("historical-trends")]
    [ProducesResponseType<List<TestTrend>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoricalTrends(
        [FromQuery] TrendInterval interval = TrendInterval.Daily,
        [FromQuery] int days = 30,
        [FromQuery] Guid? recordingId = null,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0 || days > 365)
        {
            return BadRequest(new { error = "Days must be between 1 and 365" });
        }

        _logger.LogInformation(
            "Getting historical {Interval} trends for last {Days} days",
            interval,
            days);

        try
        {
            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddDays(-days);

            var trends = await _analyticsService.GetHistoricalTrendsAsync(
                recordingId,
                interval,
                startDate,
                endDate,
                cancellationToken);

                    return Ok(trends);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get historical trends");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
                }
            }

            // ==================== HEALTH & COMPARISON ENDPOINTS ====================

            /// <summary>
            /// Gets the overall test suite health score
            /// </summary>
            /// <param name="cancellationToken">Cancellation token</param>
            /// <returns>Health score and status</returns>
            [HttpGet("health")]
            [ProducesResponseType<HealthScoreResponse>(StatusCodes.Status200OK)]
            [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
            public async Task<IActionResult> GetHealthScore(CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("Getting test suite health score");

                try
                {
                    var statistics = await _analyticsService.GetDashboardStatisticsAsync(cancellationToken);
                    var health = _analyticsService.DetermineHealth(statistics);

                    var response = new HealthScoreResponse
                    {
                        Health = health,
                        Score = CalculateNumericHealthScore(statistics),
                        PassRate = statistics.OverallPassRate,
                        FlakyTestPercentage = statistics.TotalTests > 0 
                            ? (double)statistics.FlakyTestCount / statistics.TotalTests * 100 
                            : 0,
                        TotalTests = statistics.TotalTests,
                        TotalExecutions = statistics.TotalExecutions,
                        CalculatedAt = DateTimeOffset.UtcNow,
                        Trends = new HealthTrends
                        {
                            PassRateTrend = CalculatePassRateTrend(statistics),
                            FlakyTestTrend = CalculateFlakyTestTrend(statistics)
                        }
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get health score");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
                }
            }

            /// <summary>
            /// Compares analytics between two time periods
            /// </summary>
            /// <param name="request">Comparison request with time periods</param>
            /// <param name="cancellationToken">Cancellation token</param>
            /// <returns>Period comparison results</returns>
            [HttpPost("compare")]
            [ProducesResponseType<PeriodComparisonResponse>(StatusCodes.Status200OK)]
            public async Task<IActionResult> ComparePeriods(
                [FromBody] PeriodComparisonRequest request,
                CancellationToken cancellationToken = default)
            {
                _logger.LogInformation(
                    "Comparing periods: Current({CurrentStart} to {CurrentEnd}) vs Previous({PrevStart} to {PrevEnd})",
                    request.CurrentPeriodStart,
                    request.CurrentPeriodEnd,
                    request.PreviousPeriodStart,
                    request.PreviousPeriodEnd);

                try
                {
                    // Calculate trends for both periods
                    var currentTrends = await _analyticsService.CalculateTrendsAsync(
                        TrendInterval.Daily,
                        request.CurrentPeriodStart,
                        request.CurrentPeriodEnd,
                        request.RecordingSessionId,
                        cancellationToken);

                    var previousTrends = await _analyticsService.CalculateTrendsAsync(
                        TrendInterval.Daily,
                        request.PreviousPeriodStart,
                        request.PreviousPeriodEnd,
                        request.RecordingSessionId,
                        cancellationToken);

                    var response = new PeriodComparisonResponse
                    {
                        CurrentPeriod = AggregateTrends(currentTrends),
                        PreviousPeriod = AggregateTrends(previousTrends),
                        Comparison = CalculateComparison(currentTrends, previousTrends)
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to compare periods");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
                }
            }

            /// <summary>
            /// Gets paginated list of flaky tests with filtering
            /// </summary>
            /// <param name="page">Page number (1-based)</param>
            /// <param name="pageSize">Items per page (max 100)</param>
            /// <param name="minScore">Minimum flakiness score</param>
            /// <param name="severity">Filter by severity</param>
            /// <param name="sortBy">Sort field (score, severity, analyzed)</param>
            /// <param name="sortDescending">Sort direction</param>
            /// <param name="cancellationToken">Cancellation token</param>
            /// <returns>Paginated flaky test results</returns>
            [HttpGet("flaky-tests/paged")]
            [ProducesResponseType<PaginatedResponse<FlakyTestAnalysis>>(StatusCodes.Status200OK)]
            public async Task<IActionResult> GetFlakyTestsPaged(
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 20,
                [FromQuery] double? minScore = null,
                [FromQuery] FlakinessSeverity? severity = null,
                [FromQuery] string sortBy = "score",
                [FromQuery] bool sortDescending = true,
                CancellationToken cancellationToken = default)
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                _logger.LogInformation(
                    "Getting flaky tests page {Page}, size {PageSize}, minScore={MinScore}, severity={Severity}",
                    page,
                    pageSize,
                    minScore,
                    severity);

                try
                {
                    var allTests = await _flakyDetector.GetAllFlakyTestsAsync(cancellationToken: cancellationToken);

                    // Apply filters
                    if (minScore.HasValue)
                    {
                        allTests = allTests.Where(t => t.FlakinessScore >= minScore.Value).ToList();
                    }

                    if (severity.HasValue)
                    {
                        allTests = allTests.Where(t => t.Severity == severity.Value).ToList();
                    }

                    // Apply sorting
                    allTests = sortBy.ToLowerInvariant() switch
                    {
                        "severity" => sortDescending 
                            ? allTests.OrderByDescending(t => t.Severity).ToList()
                            : allTests.OrderBy(t => t.Severity).ToList(),
                        "analyzed" => sortDescending
                            ? allTests.OrderByDescending(t => t.AnalyzedAt).ToList()
                            : allTests.OrderBy(t => t.AnalyzedAt).ToList(),
                        _ => sortDescending
                            ? allTests.OrderByDescending(t => t.FlakinessScore).ToList()
                            : allTests.OrderBy(t => t.FlakinessScore).ToList()
                    };

                    // Paginate
                    var totalCount = allTests.Count;
                    var pagedTests = allTests
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    var response = new PaginatedResponse<FlakyTestAnalysis>
                    {
                        Items = pagedTests,
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalCount = totalCount
                    };

                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get paginated flaky tests");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
                }
            }

            // ==================== EXPORT ENDPOINTS ====================

    /// <summary>
    /// Export dashboard statistics
    /// </summary>
    /// <param name="format">Export format (json, csv, html)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported file</returns>
    [HttpGet("export/dashboard")]
    public async Task<IActionResult> ExportDashboard(
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting dashboard in {Format} format", format);

        try
        {
            var statistics = await _analyticsService.GetDashboardStatisticsAsync(cancellationToken);

            byte[] fileBytes;
            string contentType;
            string fileName;

            switch (format.ToLower())
            {
                case "csv":
                    fileBytes = await _exportService.ExportDashboardToCsvAsync(statistics, cancellationToken);
                    contentType = "text/csv";
                    fileName = $"dashboard-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
                    break;

                case "html":
                    var flakyTests = await _flakyDetector.GetAllFlakyTestsAsync(cancellationToken: cancellationToken);
                    fileBytes = await _exportService.GenerateAnalyticsReportAsync(statistics, flakyTests, cancellationToken);
                    contentType = "text/html";
                    fileName = $"analytics-report-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.html";
                    break;

                case "json":
                default:
                    fileBytes = await _exportService.ExportDashboardToJsonAsync(statistics, cancellationToken);
                    contentType = "application/json";
                    fileName = $"dashboard-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
                    break;
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export dashboard");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Export flaky tests
    /// </summary>
    /// <param name="format">Export format (json, csv)</param>
    /// <param name="minScore">Minimum flakiness score</param>
    /// <param name="severity">Filter by severity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported file</returns>
    [HttpGet("export/flaky-tests")]
    public async Task<IActionResult> ExportFlakyTests(
        [FromQuery] string format = "json",
        [FromQuery] double? minScore = null,
        [FromQuery] FlakinessSeverity? severity = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting flaky tests in {Format} format", format);

        try
        {
            var flakyTests = await _flakyDetector.GetAllFlakyTestsAsync(cancellationToken: cancellationToken);

            // Apply filters
            if (minScore.HasValue)
            {
                flakyTests = flakyTests.Where(t => t.FlakinessScore >= minScore.Value).ToList();
            }

            if (severity.HasValue)
            {
                flakyTests = flakyTests.Where(t => t.Severity == severity.Value).ToList();
            }

            byte[] fileBytes;
            string contentType;
            string fileName;

            switch (format.ToLower())
            {
                case "csv":
                    fileBytes = await _exportService.ExportFlakyTestsToCsvAsync(flakyTests, cancellationToken);
                    contentType = "text/csv";
                    fileName = $"flaky-tests-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
                    break;

                case "json":
                default:
                    fileBytes = await _exportService.ExportFlakyTestsToJsonAsync(flakyTests, cancellationToken);
                    contentType = "application/json";
                    fileName = $"flaky-tests-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
                    break;
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export flaky tests");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Export trends
    /// </summary>
    /// <param name="format">Export format (json, csv)</param>
    /// <param name="interval">Trend interval</param>
    /// <param name="days">Number of days</param>
    /// <param name="recordingId">Optional recording filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported file</returns>
    [HttpGet("export/trends")]
    public async Task<IActionResult> ExportTrends(
        [FromQuery] string format = "json",
        [FromQuery] TrendInterval interval = TrendInterval.Daily,
        [FromQuery] int days = 30,
        [FromQuery] Guid? recordingId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting trends in {Format} format", format);

        try
        {
            var endDate = DateTimeOffset.UtcNow;
            var startDate = endDate.AddDays(-days);

            var trends = await _analyticsService.CalculateTrendsAsync(
                interval,
                startDate,
                endDate,
                recordingId,
                cancellationToken);

            byte[] fileBytes;
            string contentType;
            string fileName;

            switch (format.ToLower())
            {
                case "csv":
                    fileBytes = await _exportService.ExportTrendsToCsvAsync(trends, cancellationToken);
                    contentType = "text/csv";
                    fileName = $"trends-{interval}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
                    break;

                case "json":
                default:
                    fileBytes = await _exportService.ExportTrendsToJsonAsync(trends, cancellationToken);
                    contentType = "application/json";
                    fileName = $"trends-{interval}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
                    break;
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export trends");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Export recording insights
    /// </summary>
    /// <param name="recordingId">Recording session ID</param>
    /// <param name="format">Export format (json, csv)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported file</returns>
    [HttpGet("export/recordings/{recordingId:guid}/insights")]
    public async Task<IActionResult> ExportRecordingInsights(
        Guid recordingId,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting insights for recording {RecordingId} in {Format} format", recordingId, format);

        try
        {
            var insights = await _analyticsService.GetRecordingInsightsAsync(recordingId, cancellationToken);

            byte[] fileBytes;
            string contentType;
            string fileName;

            switch (format.ToLower())
            {
                case "csv":
                    fileBytes = await _exportService.ExportRecordingInsightsToCsvAsync(insights, cancellationToken);
                    contentType = "text/csv";
                    fileName = $"insights-{recordingId}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
                    break;

                case "json":
                default:
                    fileBytes = await _exportService.ExportRecordingInsightsToJsonAsync(insights, cancellationToken);
                    contentType = "application/json";
                    fileName = $"insights-{recordingId}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
                    break;
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Recording {RecordingId} not found", recordingId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
                        _logger.LogError(ex, "Failed to export insights");
                        return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
                    }
                }

                // ==================== HELPER METHODS ====================

                private static double CalculateNumericHealthScore(DashboardStatistics statistics)
                {
                    // Weighted score: Pass Rate (60%), Flaky % (20%), Execution Count (10%), Trend (10%)
                    var passRateScore = statistics.OverallPassRate;

                    var flakyPercentage = statistics.TotalTests > 0
                        ? (double)statistics.FlakyTestCount / statistics.TotalTests * 100
                        : 0;
                    var flakyScore = Math.Max(0, 100 - flakyPercentage * 5); // Each 1% flaky = -5 points

                    var executionScore = statistics.TotalExecutions >= 100 ? 100 :
                                       statistics.TotalExecutions >= 50 ? 80 :
                                       statistics.TotalExecutions >= 10 ? 60 : 40;

                    var trendScore = statistics.PassRateLast7Days >= statistics.OverallPassRate ? 100 : 80;

                    return (passRateScore * 0.6) + (flakyScore * 0.2) + (executionScore * 0.1) + (trendScore * 0.1);
                }

                private static string CalculatePassRateTrend(DashboardStatistics statistics)
                {
                    var diff = statistics.PassRateLast7Days - statistics.PassRateLast30Days;
                    return diff > 5 ? "improving" :
                           diff < -5 ? "degrading" : "stable";
                }

                private static string CalculateFlakyTestTrend(DashboardStatistics statistics)
                {
                    // Simplified - in real implementation, compare with historical data
                    return statistics.FlakyTestCount > statistics.StableTestCount * 0.1 ? "increasing" : "stable";
                }

                private static PeriodSummary AggregateTrends(List<TestTrend> trends)
                {
                    if (!trends.Any())
                    {
                        return new PeriodSummary();
                    }

                    return new PeriodSummary
                    {
                        TotalExecutions = trends.Sum(t => t.TotalExecutions),
                        PassedExecutions = trends.Sum(t => t.PassedExecutions),
                        FailedExecutions = trends.Sum(t => t.FailedExecutions),
                        PassRate = trends.Average(t => t.PassRate),
                        AverageDurationMs = (long)trends.Average(t => t.AverageDurationMs),
                        FlakyTestCount = trends.Sum(t => t.FlakyTestCount),
                        UniqueTestCount = trends.Max(t => t.UniqueTestCount),
                        DataPoints = trends.Count
                    };
                }

                private static ComparisonMetrics CalculateComparison(List<TestTrend> current, List<TestTrend> previous)
                {
                    var currentSummary = AggregateTrends(current);
                    var previousSummary = AggregateTrends(previous);

                    return new ComparisonMetrics
                    {
                        PassRateChange = currentSummary.PassRate - previousSummary.PassRate,
                        ExecutionCountChange = currentSummary.TotalExecutions - previousSummary.TotalExecutions,
                        FlakyTestCountChange = currentSummary.FlakyTestCount - previousSummary.FlakyTestCount,
                        AvgDurationChange = currentSummary.AverageDurationMs - previousSummary.AverageDurationMs,
                        PassRateChangePercent = previousSummary.PassRate > 0
                            ? ((currentSummary.PassRate - previousSummary.PassRate) / previousSummary.PassRate * 100)
                            : 0,
                        Verdict = DetermineVerdict(currentSummary, previousSummary)
                    };
                }

                private static string DetermineVerdict(PeriodSummary current, PeriodSummary previous)
                {
                    var passRateDiff = current.PassRate - previous.PassRate;
                    var flakyDiff = current.FlakyTestCount - previous.FlakyTestCount;

                    if (passRateDiff >= 5 && flakyDiff <= 0)
                        return "significantly_improved";
                    if (passRateDiff >= 2 && flakyDiff <= 1)
                        return "improved";
                    if (passRateDiff <= -5 || flakyDiff >= 5)
                        return "degraded";
                    if (passRateDiff <= -2 || flakyDiff >= 2)
                        return "slightly_degraded";

                    return "stable";
                }
            }

/// <summary>
/// Request model for analyzing flakiness
/// </summary>
public sealed class AnalyzeFlakinessRequest
{
    /// <summary>
    /// Minimum number of executions required
    /// </summary>
    public int? MinimumExecutions { get; set; }

    /// <summary>
    /// Minimum pass rate threshold
    /// </summary>
    public double? MinimumPassRate { get; set; }

    /// <summary>
    /// Use strict criteria
    /// </summary>
    public bool? UseStrictCriteria { get; set; }

    /// <summary>
    /// Converts to FlakyCriteria
    /// </summary>
    public FlakyCriteria ToFlakyCriteria()
    {
        if (UseStrictCriteria == true)
        {
            return FlakyCriteria.Strict;
        }

        var criteria = FlakyCriteria.Default;

        if (MinimumExecutions.HasValue)
        {
            criteria.MinimumExecutions = MinimumExecutions.Value;
        }

        if (MinimumPassRate.HasValue)
        {
            criteria.MinimumPassRate = MinimumPassRate.Value;
        }

        return criteria;
    }
}

/// <summary>
/// Request model for calculating trends
/// </summary>
public sealed class CalculateTrendsRequest
{
    /// <summary>
    /// Trend interval
    /// </summary>
    public TrendInterval Interval { get; set; } = TrendInterval.Daily;

    /// <summary>
    /// Start date for trend calculation
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// End date for trend calculation
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Optional recording session filter
    /// </summary>
    public Guid? RecordingSessionId { get; set; }
}
