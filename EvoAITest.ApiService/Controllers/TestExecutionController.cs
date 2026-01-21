using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Execution;
using Microsoft.AspNetCore.Mvc;

namespace EvoAITest.ApiService.Controllers;

/// <summary>
/// API controller for test execution operations
/// </summary>
[ApiController]
[Route("api/test-execution")]
[Produces("application/json")]
public sealed class TestExecutionController : ControllerBase
{
    private readonly ITestExecutor _testExecutor;
    private readonly ITestResultStorage _resultStorage;
    private readonly ILogger<TestExecutionController> _logger;

    public TestExecutionController(
        ITestExecutor testExecutor,
        ITestResultStorage resultStorage,
        ILogger<TestExecutionController> logger)
    {
        _testExecutor = testExecutor ?? throw new ArgumentNullException(nameof(testExecutor));
        _resultStorage = resultStorage ?? throw new ArgumentNullException(nameof(resultStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a test from a recording session
    /// </summary>
    /// <param name="recordingId">The recording session ID</param>
    /// <param name="request">Execution options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test execution result</returns>
    [HttpPost("recordings/{recordingId:guid}/execute")]
    [ProducesResponseType<TestExecutionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteFromRecording(
        Guid recordingId,
        [FromBody] ExecuteTestRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing test from recording {RecordingId}", recordingId);

        try
        {
            var options = request?.ToExecutionOptions() ?? new TestExecutionOptions();

            var result = await _testExecutor.ExecuteFromRecordingAsync(
                recordingId,
                options,
                cancellationToken);

            // Save result to database
            await _resultStorage.SaveResultAsync(result, cancellationToken);

            _logger.LogInformation(
                "Test execution completed: {ResultId} with status {Status}",
                result.Id,
                result.Status);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Recording {RecordingId} not found", recordingId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute test from recording {RecordingId}", recordingId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Executes test code directly
    /// </summary>
    /// <param name="request">Test code and execution options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test execution result</returns>
    [HttpPost("execute")]
    [ProducesResponseType<TestExecutionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteTest(
        [FromBody] ExecuteTestCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TestCode))
        {
            return BadRequest(new { error = "Test code is required" });
        }

        _logger.LogInformation("Executing test code directly");

        try
        {
            var options = request.ToExecutionOptions();

            var result = await _testExecutor.ExecuteTestAsync(
                request.TestCode,
                options,
                cancellationToken);

            // Save result to database
            await _resultStorage.SaveResultAsync(result, cancellationToken);

            _logger.LogInformation(
                "Test execution completed: {ResultId} with status {Status}",
                result.Id,
                result.Status);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute test code");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validates test code without executing it
    /// </summary>
    /// <param name="request">Test code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [ProducesResponseType<TestValidationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateTest(
        [FromBody] ValidateTestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TestCode))
        {
            return BadRequest(new { error = "Test code is required" });
        }

        _logger.LogInformation("Validating test code");

        try
        {
            var result = await _testExecutor.ValidateTestAsync(
                request.TestCode,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate test code");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific test execution result
    /// </summary>
    /// <param name="resultId">The execution result ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test execution result</returns>
    [HttpGet("results/{resultId:guid}")]
    [ProducesResponseType<TestExecutionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(
        Guid resultId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting execution result {ResultId}", resultId);

        var result = await _resultStorage.GetResultAsync(resultId, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("Execution result {ResultId} not found", resultId);
            return NotFound(new { error = "Execution result not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all execution results for a recording session
    /// </summary>
    /// <param name="recordingId">The recording session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of test execution results</returns>
    [HttpGet("recordings/{recordingId:guid}/results")]
    [ProducesResponseType<IEnumerable<TestExecutionResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResultsByRecording(
        Guid recordingId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting execution results for recording {RecordingId}", recordingId);

        var results = await _resultStorage.GetResultsByRecordingAsync(
            recordingId,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Gets execution history with pagination
    /// </summary>
    /// <param name="skip">Number of results to skip</param>
    /// <param name="take">Number of results to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of test execution results</returns>
    [HttpGet("history")]
    [ProducesResponseType<IEnumerable<TestExecutionResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutionHistory(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (take > 100)
        {
            take = 100; // Limit max results
        }

        _logger.LogInformation("Getting execution history: skip={Skip}, take={Take}", skip, take);

        var results = await _resultStorage.GetExecutionHistoryAsync(
            skip,
            take,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Gets execution statistics for a recording session
    /// </summary>
    /// <param name="recordingId">The recording session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test execution statistics</returns>
    [HttpGet("recordings/{recordingId:guid}/statistics")]
    [ProducesResponseType<TestExecutionStatistics>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(
        Guid recordingId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting statistics for recording {RecordingId}", recordingId);

        var statistics = await _resultStorage.GetStatisticsAsync(
            recordingId,
            cancellationToken);

        return Ok(statistics);
    }

    /// <summary>
    /// Searches execution results with advanced filtering
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching test execution results</returns>
    [HttpPost("search")]
    [ProducesResponseType<IEnumerable<TestExecutionResult>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchResults(
        [FromBody] TestExecutionSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        if (criteria.Take > 100)
        {
            criteria.Take = 100; // Limit max results
        }

        _logger.LogInformation("Searching execution results");

        var results = await _resultStorage.SearchResultsAsync(
            criteria,
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Gets supported test frameworks
    /// </summary>
    /// <returns>List of supported test framework names</returns>
    [HttpGet("frameworks")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public IActionResult GetSupportedFrameworks()
    {
        _logger.LogInformation("Getting supported test frameworks");

        var frameworks = _testExecutor.GetSupportedFrameworks();

        return Ok(frameworks);
    }

    /// <summary>
    /// Deletes old execution results
    /// </summary>
    /// <param name="olderThanDays">Delete results older than this many days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted results</returns>
    [HttpDelete("cleanup")]
    [ProducesResponseType<CleanupResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupOldResults(
        [FromQuery] int olderThanDays = 30,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up execution results older than {Days} days", olderThanDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        var deletedCount = await _resultStorage.DeleteOldResultsAsync(
            cutoffDate,
            cancellationToken);

        _logger.LogInformation("Deleted {Count} old execution results", deletedCount);

        return Ok(new CleanupResponse
        {
            DeletedCount = deletedCount,
            CutoffDate = cutoffDate
        });
    }
}

/// <summary>
/// Request model for executing a test from a recording
/// </summary>
public sealed class ExecuteTestRequest
{
    /// <summary>
    /// Test framework to use (xUnit, NUnit, MSTest)
    /// </summary>
    public string? TestFramework { get; set; }

    /// <summary>
    /// Browser to use for execution
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// Whether to run in headless mode
    /// </summary>
    public bool? Headless { get; set; }

    /// <summary>
    /// Whether to capture screenshots
    /// </summary>
    public bool? CaptureScreenshots { get; set; }

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Converts to TestExecutionOptions
    /// </summary>
    public TestExecutionOptions ToExecutionOptions()
    {
        return new TestExecutionOptions
        {
            TestFramework = TestFramework ?? "xUnit",
            Browser = Browser ?? "chromium",
            Headless = Headless ?? true,
            CaptureScreenshots = CaptureScreenshots ?? true,
            TimeoutSeconds = TimeoutSeconds ?? 300
        };
    }
}

/// <summary>
/// Request model for executing test code directly
/// </summary>
public sealed class ExecuteTestCodeRequest
{
    /// <summary>
    /// Test code to execute
    /// </summary>
    public required string TestCode { get; set; }

    /// <summary>
    /// Test framework to use (xUnit, NUnit, MSTest)
    /// </summary>
    public string? TestFramework { get; set; }

    /// <summary>
    /// Browser to use for execution
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// Whether to run in headless mode
    /// </summary>
    public bool? Headless { get; set; }

    /// <summary>
    /// Whether to capture screenshots
    /// </summary>
    public bool? CaptureScreenshots { get; set; }

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Converts to TestExecutionOptions
    /// </summary>
    public TestExecutionOptions ToExecutionOptions()
    {
        return new TestExecutionOptions
        {
            TestFramework = TestFramework ?? "xUnit",
            Browser = Browser ?? "chromium",
            Headless = Headless ?? true,
            CaptureScreenshots = CaptureScreenshots ?? true,
            TimeoutSeconds = TimeoutSeconds ?? 300
        };
    }
}

/// <summary>
/// Request model for validating test code
/// </summary>
public sealed class ValidateTestRequest
{
    /// <summary>
    /// Test code to validate
    /// </summary>
    public required string TestCode { get; set; }
}

/// <summary>
/// Response model for cleanup operation
/// </summary>
public sealed class CleanupResponse
{
    /// <summary>
    /// Number of results deleted
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// Cutoff date used for cleanup
    /// </summary>
    public DateTime CutoffDate { get; set; }
}
