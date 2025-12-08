using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services;

/// <summary>
/// Service for comparing screenshots against baselines and managing visual regression testing.
/// </summary>
public sealed class VisualComparisonService : IVisualComparisonService
{
    private readonly EvoAIDbContext _dbContext;
    private readonly IFileStorageService _storage;
    private readonly VisualComparisonEngine _engine;
    private readonly ILogger<VisualComparisonService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualComparisonService"/> class.
    /// </summary>
    public VisualComparisonService(
        EvoAIDbContext dbContext,
        IFileStorageService storage,
        VisualComparisonEngine engine,
        ILogger<VisualComparisonService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<VisualComparisonResult> CompareAsync(
        VisualCheckpoint checkpoint,
        byte[] actualImage,
        Guid taskId,
        string environment,
        string browser,
        string viewport,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        ArgumentNullException.ThrowIfNull(actualImage);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(browser);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewport);

        _logger.LogInformation(
            "Comparing checkpoint '{CheckpointName}' for task {TaskId} in {Environment}/{Browser}/{Viewport}",
            checkpoint.Name, taskId, environment, browser, viewport);

        try
        {
            // Get or create baseline
            var baseline = await GetBaselineAsync(taskId, checkpoint.Name, environment, browser, viewport, cancellationToken);

            if (baseline == null)
            {
                _logger.LogWarning(
                    "No baseline found for checkpoint '{CheckpointName}'. Creating first-time baseline.",
                    checkpoint.Name);

                // Save actual as baseline for first run
                baseline = await CreateBaselineAsync(
                    taskId,
                    checkpoint.Name,
                    actualImage,
                    environment,
                    browser,
                    viewport,
                    "system",
                    "Initial baseline creation",
                    cancellationToken);

                // Return a passing result since this is the first run
                var actualPath = await SaveActualImageAsync(actualImage, taskId, checkpoint.Name, environment, browser, viewport);

                return new VisualComparisonResult
                {
                    TaskId = taskId,
                    ExecutionHistoryId = Guid.Empty, // Will be set by caller
                    CheckpointName = checkpoint.Name,
                    BaselineId = baseline.Id,
                    BaselinePath = baseline.BaselinePath,
                    ActualPath = actualPath,
                    DiffPath = actualPath, // No diff for first run
                    DifferencePercentage = 0.0,
                    Tolerance = checkpoint.Tolerance,
                    Passed = true,
                    PixelsDifferent = 0,
                    TotalPixels = 0,
                    Metadata = "{}",
                    Regions = "[]"
                };
            }

            // Load baseline image
            var baselineImage = await _storage.LoadImageAsync(baseline.BaselinePath, cancellationToken);

            // Perform comparison
            var metrics = await _engine.CompareImagesAsync(baselineImage, actualImage, checkpoint, cancellationToken);

            // Save actual and diff images
            var actualSavePath = await SaveActualImageAsync(actualImage, taskId, checkpoint.Name, environment, browser, viewport);
            var diffSavePath = await SaveDiffImageAsync(metrics.DiffImage!, taskId, checkpoint.Name, environment, browser, viewport);

            // Create comparison result
            var result = new VisualComparisonResult
            {
                TaskId = taskId,
                ExecutionHistoryId = Guid.Empty, // Will be set by caller
                CheckpointName = checkpoint.Name,
                BaselineId = baseline.Id,
                BaselinePath = baseline.BaselinePath,
                ActualPath = actualSavePath,
                DiffPath = diffSavePath,
                DifferencePercentage = metrics.DifferencePercentage,
                Tolerance = checkpoint.Tolerance,
                Passed = metrics.Passed,
                PixelsDifferent = metrics.PixelsDifferent,
                TotalPixels = metrics.TotalPixels,
                SsimScore = metrics.SsimScore,
                DifferenceType = metrics.DifferenceType.ToString(),
                Regions = JsonSerializer.Serialize(metrics.Regions),
                ComparedAt = DateTimeOffset.UtcNow,
                Metadata = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["environment"] = environment,
                    ["browser"] = browser,
                    ["viewport"] = viewport,
                    ["checkpoint_type"] = checkpoint.Type.ToString()
                })
            };

            _logger.LogInformation(
                "Comparison complete for checkpoint '{CheckpointName}'. Passed: {Passed}, Difference: {Difference:P2}",
                checkpoint.Name, result.Passed, result.DifferencePercentage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing checkpoint '{CheckpointName}' for task {TaskId}", checkpoint.Name, taskId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VisualBaseline> CreateBaselineAsync(
        Guid taskId,
        string checkpointName,
        byte[] image,
        string environment,
        string browser,
        string viewport,
        string approvedBy,
        string? updateReason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointName);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(browser);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewport);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);

        _logger.LogInformation(
            "Creating baseline for checkpoint '{CheckpointName}' in {Environment}/{Browser}/{Viewport}",
            checkpointName, environment, browser, viewport);

        try
        {
            // Check if baseline already exists
            var existingBaseline = await GetBaselineAsync(
                taskId, checkpointName, environment, browser, viewport, cancellationToken);

            // Save image to storage
            var relativePath = BuildBaselinePath(taskId, checkpointName, environment, browser, viewport);
            var storedPath = await _storage.SaveImageAsync(image, relativePath, cancellationToken);

            // Calculate image hash for integrity
            var imageHash = CalculateImageHash(image);

            // Create new baseline
            var baseline = new VisualBaseline
            {
                TaskId = taskId,
                CheckpointName = checkpointName,
                Environment = environment,
                Browser = browser,
                Viewport = viewport,
                BaselinePath = storedPath,
                ImageHash = imageHash,
                CreatedAt = DateTimeOffset.UtcNow,
                ApprovedBy = approvedBy,
                UpdateReason = updateReason,
                PreviousBaselineId = existingBaseline?.Id,
                Metadata = "{}"
            };

            _dbContext.VisualBaselines.Add(baseline);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created baseline {BaselineId} for checkpoint '{CheckpointName}'",
                baseline.Id, checkpointName);

            return baseline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating baseline for checkpoint '{CheckpointName}'", checkpointName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<VisualBaseline?> GetBaselineAsync(
        Guid taskId,
        string checkpointName,
        string environment,
        string browser,
        string viewport,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointName);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(browser);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewport);

        return await _dbContext.VisualBaselines
            .Where(b => b.TaskId == taskId
                     && b.CheckpointName == checkpointName
                     && b.Environment == environment
                     && b.Browser == browser
                     && b.Viewport == viewport)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VisualBaseline> ApproveNewBaselineAsync(
        Guid comparisonId,
        string approvedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        _logger.LogInformation("Approving new baseline from comparison {ComparisonId}", comparisonId);

        try
        {
            // Get the comparison result
            var comparison = await _dbContext.VisualComparisonResults
                .FirstOrDefaultAsync(c => c.Id == comparisonId, cancellationToken);

            if (comparison == null)
            {
                throw new InvalidOperationException($"Comparison result {comparisonId} not found");
            }

            // Load the actual image from the comparison
            var actualImage = await _storage.LoadImageAsync(comparison.ActualPath, cancellationToken);

            // Extract environment info from metadata
            var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(comparison.Metadata);
            var environment = metadata?["environment"]?.ToString() ?? "unknown";
            var browser = metadata?["browser"]?.ToString() ?? "unknown";
            var viewport = metadata?["viewport"]?.ToString() ?? "unknown";

            // Create new baseline from the actual image
            var baseline = await CreateBaselineAsync(
                comparison.TaskId,
                comparison.CheckpointName,
                actualImage,
                environment,
                browser,
                viewport,
                approvedBy,
                reason,
                cancellationToken);

            _logger.LogInformation(
                "Approved new baseline {BaselineId} from comparison {ComparisonId}",
                baseline.Id, comparisonId);

            return baseline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving new baseline from comparison {ComparisonId}", comparisonId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<VisualComparisonResult>> GetHistoryAsync(
        Guid taskId,
        string checkpointName,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointName);

        return await _dbContext.VisualComparisonResults
            .Where(c => c.TaskId == taskId && c.CheckpointName == checkpointName)
            .OrderByDescending(c => c.ComparedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Saves the actual screenshot to storage.
    /// </summary>
    private async Task<string> SaveActualImageAsync(
        byte[] image,
        Guid taskId,
        string checkpointName,
        string environment,
        string browser,
        string viewport)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var path = $"actual/{environment}/{browser}/{viewport}/{taskId}/{checkpointName}/{timestamp}.png";
        return await _storage.SaveImageAsync(image, path);
    }

    /// <summary>
    /// Saves the diff image to storage.
    /// </summary>
    private async Task<string> SaveDiffImageAsync(
        byte[] image,
        Guid taskId,
        string checkpointName,
        string environment,
        string browser,
        string viewport)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var path = $"diff/{environment}/{browser}/{viewport}/{taskId}/{checkpointName}/{timestamp}.png";
        return await _storage.SaveImageAsync(image, path);
    }

    /// <summary>
    /// Builds the storage path for a baseline image.
    /// </summary>
    private static string BuildBaselinePath(
        Guid taskId,
        string checkpointName,
        string environment,
        string browser,
        string viewport)
    {
        return $"baselines/{environment}/{browser}/{viewport}/{taskId}/{checkpointName}/baseline.png";
    }

    /// <summary>
    /// Calculates SHA256 hash of an image for integrity verification.
    /// </summary>
    private static string CalculateImageHash(byte[] image)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(image);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
