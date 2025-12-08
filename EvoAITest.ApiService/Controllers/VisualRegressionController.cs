using System.Text.Json;
using EvoAITest.ApiService.Models;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EvoAITest.ApiService.Controllers;

/// <summary>
/// API controller for visual regression testing operations.
/// </summary>
[ApiController]
[Route("api/visual")]
[Produces("application/json")]
public sealed class VisualRegressionController : ControllerBase
{
    private readonly IAutomationTaskRepository _taskRepository;
    private readonly IVisualComparisonService _visualService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<VisualRegressionController> _logger;

    public VisualRegressionController(
        IAutomationTaskRepository taskRepository,
        IVisualComparisonService visualService,
        IFileStorageService fileStorage,
        ILogger<VisualRegressionController> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _visualService = visualService ?? throw new ArgumentNullException(nameof(visualService));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all checkpoints for a task.
    /// </summary>
    [HttpGet("tasks/{taskId:guid}/checkpoints")]
    [ProducesResponseType<TaskCheckpointsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskCheckpoints(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting checkpoints for task {TaskId}", taskId);

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found", taskId);
            return NotFound(new { error = "Task not found" });
        }

        var checkpoints = new List<CheckpointSummaryDto>();

        if (!string.IsNullOrEmpty(task.VisualCheckpoints))
        {
            try
            {
                var visualCheckpoints = JsonSerializer.Deserialize<List<VisualCheckpoint>>(task.VisualCheckpoints);
                if (visualCheckpoints != null)
                {
                    foreach (var checkpoint in visualCheckpoints)
                    {
                        var baseline = await _taskRepository.GetBaselineAsync(
                            taskId,
                            checkpoint.Name,
                            "dev", // Default environment
                            "chromium",
                            "1920x1080",
                            cancellationToken);

                        var history = await _taskRepository.GetComparisonHistoryAsync(
                            taskId,
                            checkpoint.Name,
                            limit: 1,
                            cancellationToken);

                        checkpoints.Add(new CheckpointSummaryDto
                        {
                            Name = checkpoint.Name,
                            Type = checkpoint.Type.ToString(),
                            Tolerance = checkpoint.Tolerance,
                            HasBaseline = baseline != null,
                            LatestComparison = history.Count > 0 ? MapToDto(history[0]) : null
                        });
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse visual checkpoints for task {TaskId}", taskId);
            }
        }

        return Ok(new TaskCheckpointsResponse
        {
            TaskId = taskId,
            Checkpoints = checkpoints
        });
    }

    /// <summary>
    /// Gets comparison history for a checkpoint.
    /// </summary>
    [HttpGet("tasks/{taskId:guid}/checkpoints/{checkpointName}/history")]
    [ProducesResponseType<ComparisonHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComparisonHistory(
        Guid taskId,
        string checkpointName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting comparison history for task {TaskId}, checkpoint {CheckpointName} (page {Page}, size {PageSize})",
            taskId, checkpointName, page, pageSize);

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var history = await _taskRepository.GetComparisonHistoryAsync(
            taskId,
            checkpointName,
            limit: pageSize * 2, // Get extra to check if there's a next page
            cancellationToken);

        var skip = (page - 1) * pageSize;
        var pageResults = history.Skip(skip).Take(pageSize).ToList();
        var hasNextPage = history.Count > skip + pageSize;

        return Ok(new ComparisonHistoryResponse
        {
            Comparisons = pageResults.Select(MapToDto).ToList(),
            TotalCount = history.Count,
            PageSize = pageSize,
            CurrentPage = page,
            HasNextPage = hasNextPage
        });
    }

    /// <summary>
    /// Gets a specific comparison result.
    /// </summary>
    [HttpGet("comparisons/{comparisonId:guid}")]
    [ProducesResponseType<VisualComparisonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComparison(
        Guid comparisonId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting comparison {ComparisonId}", comparisonId);

        // Query from database
        var comparison = await _taskRepository.GetComparisonResultAsync(comparisonId, cancellationToken);
        
        if (comparison == null)
        {
            _logger.LogWarning("Comparison {ComparisonId} not found", comparisonId);
            return NotFound(new { error = "Comparison not found" });
        }

        return Ok(MapToDto(comparison));
    }

    /// <summary>
    /// Gets baseline for a checkpoint.
    /// </summary>
    [HttpGet("tasks/{taskId:guid}/checkpoints/{checkpointName}/baseline")]
    [ProducesResponseType<VisualBaselineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBaseline(
        Guid taskId,
        string checkpointName,
        [FromQuery] string environment = "dev",
        [FromQuery] string browser = "chromium",
        [FromQuery] string viewport = "1920x1080",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting baseline for task {TaskId}, checkpoint {CheckpointName} ({Environment}/{Browser}/{Viewport})",
            taskId, checkpointName, environment, browser, viewport);

        var baseline = await _taskRepository.GetBaselineAsync(
            taskId,
            checkpointName,
            environment,
            browser,
            viewport,
            cancellationToken);

        if (baseline == null)
        {
            return NotFound(new { error = "Baseline not found" });
        }

        return Ok(MapToDto(baseline));
    }

    /// <summary>
    /// Updates tolerance for a checkpoint.
    /// </summary>
    [HttpPut("tasks/{taskId:guid}/checkpoints/{checkpointName}/tolerance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTolerance(
        Guid taskId,
        string checkpointName,
        [FromBody] UpdateToleranceRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Updating tolerance for task {TaskId}, checkpoint {CheckpointName} to {Tolerance:P2} (ApplyToAll: {ApplyToAll})",
            taskId, checkpointName, request.NewTolerance, request.ApplyToAll);

        if (request.NewTolerance < 0.0 || request.NewTolerance > 1.0)
        {
            return BadRequest(new { error = "Tolerance must be between 0.0 and 1.0" });
        }

        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            return NotFound(new { error = "Task not found" });
        }

        try
        {
            // Update task visual checkpoints
            if (!string.IsNullOrEmpty(task.VisualCheckpoints))
            {
                var checkpoints = JsonSerializer.Deserialize<List<VisualCheckpoint>>(task.VisualCheckpoints);
                if (checkpoints != null)
                {
                    if (request.ApplyToAll)
                    {
                        // Update all checkpoints
                        foreach (var cp in checkpoints)
                        {
                            // Create new checkpoint with updated tolerance
                            var updated = new VisualCheckpoint
                            {
                                Name = cp.Name,
                                Type = cp.Type,
                                Tolerance = request.NewTolerance,
                                Selector = cp.Selector,
                                Region = cp.Region,
                                IgnoreSelectors = cp.IgnoreSelectors,
                                IsRequired = cp.IsRequired,
                                Tags = cp.Tags,
                                Metadata = cp.Metadata
                            };
                            
                            var index = checkpoints.IndexOf(cp);
                            checkpoints[index] = updated;
                        }

                        _logger.LogInformation("Updated tolerance for all {Count} checkpoints", checkpoints.Count);
                    }
                    else
                    {
                        // Update specific checkpoint
                        var checkpoint = checkpoints.FirstOrDefault(cp => cp.Name == checkpointName);
                        if (checkpoint != null)
                        {
                            var updated = new VisualCheckpoint
                            {
                                Name = checkpoint.Name,
                                Type = checkpoint.Type,
                                Tolerance = request.NewTolerance,
                                Selector = checkpoint.Selector,
                                Region = checkpoint.Region,
                                IgnoreSelectors = checkpoint.IgnoreSelectors,
                                IsRequired = checkpoint.IsRequired,
                                Tags = checkpoint.Tags,
                                Metadata = checkpoint.Metadata
                            };
                            
                            var index = checkpoints.IndexOf(checkpoint);
                            checkpoints[index] = updated;

                            _logger.LogInformation("Updated tolerance for checkpoint {CheckpointName}", checkpointName);
                        }
                        else
                        {
                            return NotFound(new { error = "Checkpoint not found" });
                        }
                    }

                    task.VisualCheckpoints = JsonSerializer.Serialize(checkpoints);
                    await _taskRepository.UpdateAsync(task, cancellationToken);
                }
            }

            return Ok(new { success = true, message = "Tolerance updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tolerance");
            return StatusCode(500, new { error = "Failed to update tolerance", details = ex.Message });
        }
    }

    /// <summary>
    /// Serves an image file.
    /// </summary>
    [HttpGet("images/{*imagePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Serving image: {ImagePath}", imagePath);

        // Sanitize path to prevent directory traversal
        imagePath = imagePath.Replace("..", string.Empty).Replace("\\\\", "/");

        var exists = await _fileStorage.FileExistsAsync(imagePath, cancellationToken);
        if (!exists)
        {
            _logger.LogWarning("Image not found: {ImagePath}", imagePath);
            return NotFound();
        }

        var imageBytes = await _fileStorage.ReadFileAsync(imagePath, cancellationToken);
        
        // Determine content type from file extension
        var contentType = imagePath.ToLowerInvariant() switch
        {
            var p when p.EndsWith(".png") => "image/png",
            var p when p.EndsWith(".jpg") || p.EndsWith(".jpeg") => "image/jpeg",
            var p when p.EndsWith(".gif") => "image/gif",
            var p when p.EndsWith(".webp") => "image/webp",
            _ => "application/octet-stream"
        };

        return File(imageBytes, contentType);
    }

    /// <summary>
    /// Gets failed comparisons for a task.
    /// </summary>
    [HttpGet("tasks/{taskId:guid}/failures")]
    [ProducesResponseType<List<VisualComparisonDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedComparisons(
        Guid taskId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting failed comparisons for task {TaskId} (limit: {Limit})", taskId, limit);

        var failures = await _taskRepository.GetFailedComparisonsAsync(taskId, limit, cancellationToken);
        
        return Ok(failures.Select(MapToDto).ToList());
    }

    // Helper methods

    private VisualComparisonDto MapToDto(VisualComparisonResult result)
    {
        var dto = new VisualComparisonDto
        {
            Id = result.Id,
            TaskId = result.TaskId,
            CheckpointName = result.CheckpointName,
            Passed = result.Passed,
            DifferencePercentage = result.DifferencePercentage,
            Tolerance = result.Tolerance,
            PixelsDifferent = result.PixelsDifferent,
            TotalPixels = result.TotalPixels,
            SsimScore = result.SsimScore,
            DifferenceType = result.DifferenceType,
            BaselineUrl = $"/api/visual/images/{result.BaselinePath}",
            ActualUrl = $"/api/visual/images/{result.ActualPath}",
            DiffUrl = $"/api/visual/images/{result.DiffPath}",
            ComparedAt = result.ComparedAt
        };

        // Parse regions if available
        if (!string.IsNullOrEmpty(result.Regions))
        {
            try
            {
                var regions = JsonSerializer.Deserialize<List<DifferenceRegion>>(result.Regions);
                if (regions != null)
                {
                    dto.Regions = regions.Select(r => new DifferenceRegionDto
                    {
                        X = r.X,
                        Y = r.Y,
                        Width = r.Width,
                        Height = r.Height,
                        DifferenceScore = r.DifferenceScore
                    }).ToList();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse regions for comparison {ComparisonId}", result.Id);
            }
        }

        return dto;
    }

    private VisualBaselineDto MapToDto(VisualBaseline baseline)
    {
        return new VisualBaselineDto
        {
            Id = baseline.Id,
            TaskId = baseline.TaskId,
            CheckpointName = baseline.CheckpointName,
            Environment = baseline.Environment,
            Browser = baseline.Browser,
            Viewport = baseline.Viewport,
            BaselineUrl = $"/api/visual/images/{baseline.BaselinePath}",
            ImageHash = baseline.ImageHash,
            CreatedAt = baseline.CreatedAt,
            ApprovedBy = baseline.ApprovedBy,
            GitCommit = baseline.GitCommit,
            GitBranch = baseline.GitBranch
        };
    }
}
