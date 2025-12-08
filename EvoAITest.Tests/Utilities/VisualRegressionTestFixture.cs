using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using EvoAITest.Core.Repositories;
using EvoAITest.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskStatus = EvoAITest.Core.Models.TaskStatus; // Resolve ambiguity

namespace EvoAITest.Tests.Utilities;

/// <summary>
/// Test fixture for visual regression tests with shared setup and teardown.
/// </summary>
public sealed class VisualRegressionTestFixture : IDisposable
{
    private readonly string _testDirectory;
    private bool _disposed;

    public EvoAIDbContext DbContext { get; }
    public IAutomationTaskRepository TaskRepository { get; }
    public IFileStorageService FileStorageService { get; }
    public VisualComparisonEngine ComparisonEngine { get; }
    public VisualComparisonService ComparisonService { get; }
    public Mock<ILogger<VisualComparisonEngine>> MockEngineLogger { get; }
    public Mock<ILogger<VisualComparisonService>> MockServiceLogger { get; }

    public VisualRegressionTestFixture()
    {
        // Create test directory
        _testDirectory = Path.Combine(Path.GetTempPath(), "EvoAITest_Visual_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<EvoAIDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        DbContext = new EvoAIDbContext(options);
        DbContext.Database.EnsureCreated();

        // Setup repository
        TaskRepository = new AutomationTaskRepository(
            DbContext,
            Mock.Of<ILogger<AutomationTaskRepository>>());

        // Setup file storage - FIXED constructor order
        FileStorageService = new LocalFileStorageService(
            Mock.Of<ILogger<LocalFileStorageService>>(),
            _testDirectory);

        // Setup comparison engine
        MockEngineLogger = new Mock<ILogger<VisualComparisonEngine>>();
        ComparisonEngine = new VisualComparisonEngine(MockEngineLogger.Object);

        // Setup comparison service - FIXED constructor order
        MockServiceLogger = new Mock<ILogger<VisualComparisonService>>();
        ComparisonService = new VisualComparisonService(
            DbContext,
            FileStorageService,
            ComparisonEngine,
            MockServiceLogger.Object);
    }

    /// <summary>
    /// Creates a test automation task.
    /// </summary>
    public async Task<AutomationTask> CreateTestTaskAsync(string name = "Test Task")
    {
        var task = new AutomationTask
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test task for visual regression",
            Status = TaskStatus.Pending, // FIXED: Changed from Draft to Pending
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        DbContext.AutomationTasks.Add(task);
        await DbContext.SaveChangesAsync();

        return task;
    }

    /// <summary>
    /// Creates a test visual baseline.
    /// </summary>
    public async Task<VisualBaseline> CreateTestBaselineAsync(
        Guid taskId,
        string checkpointName,
        byte[] imageData,
        string environment = "test",
        string browser = "chromium",
        string viewport = "1920x1080")
    {
        var baseline = new VisualBaseline
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            CheckpointName = checkpointName,
            Environment = environment,
            Browser = browser,
            Viewport = viewport,
            BaselinePath = $"baselines/{checkpointName}.png", // FIXED: Changed from ImagePath
            ImageHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(imageData)),
            // REMOVED: Width, Height properties don't exist
            CreatedAt = DateTimeOffset.UtcNow,
            // REMOVED: ApprovedAt property doesn't exist
            ApprovedBy = "test-user",
            // REMOVED: IsApproved property doesn't exist
            Metadata = "{}"
        };

        // Use DbContext directly instead of repository method
        DbContext.VisualBaselines.Add(baseline);
        await DbContext.SaveChangesAsync();

        // Save image to storage - FIXED method name
        await FileStorageService.SaveImageAsync(imageData, baseline.BaselinePath, CancellationToken.None);

        return baseline;
    }

    /// <summary>
    /// Creates a test visual checkpoint.
    /// </summary>
    public VisualCheckpoint CreateTestCheckpoint(
        string name = "TestCheckpoint",
        CheckpointType type = CheckpointType.FullPage,
        double tolerance = 0.01)
    {
        return new VisualCheckpoint
        {
            Name = name,
            Type = type,
            Tolerance = tolerance,
            Selector = type == CheckpointType.Element ? "#test-element" : null,
            Region = type == CheckpointType.Region ? new ScreenshotRegion { X = 0, Y = 0, Width = 100, Height = 100 } : null,
            IgnoreSelectors = new List<string>(),
            IsRequired = false,
            Tags = new List<string>()
        };
    }

    /// <summary>
    /// Clears all test data from database.
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        DbContext.VisualComparisonResults.RemoveRange(DbContext.VisualComparisonResults);
        DbContext.VisualBaselines.RemoveRange(DbContext.VisualBaselines);
        DbContext.AutomationTasks.RemoveRange(DbContext.AutomationTasks);
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the full path for a test file.
    /// </summary>
    public string GetTestFilePath(string relativePath)
    {
        return Path.Combine(_testDirectory, relativePath);
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            DbContext?.Dispose();
            
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }

        _disposed = true;
    }
}
