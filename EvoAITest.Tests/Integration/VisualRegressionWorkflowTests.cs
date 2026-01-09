using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using EvoAITest.Core.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EvoAITest.Tests.Integration;

/// <summary>
/// End-to-end tests for complete visual regression workflows.
/// Tests the integration of all components from screenshot to comparison to healing.
/// </summary>
[TestClass]
public sealed class VisualRegressionWorkflowTests : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private EvoAIDbContext? _dbContext;
    private IAutomationTaskRepository? _taskRepository;
    private IVisualComparisonService? _visualService;
    private IFileStorageService? _fileStorage;
    private string _testDirectory;
    private bool _disposed;

    public VisualRegressionWorkflowTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "EvoAITest_Workflow_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Add in-memory database
        services.AddDbContext<EvoAIDbContext>(options =>
        {
            options.UseInMemoryDatabase($"WorkflowTestDb_{Guid.NewGuid()}");
        });

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add repository
        services.AddScoped<IAutomationTaskRepository, AutomationTaskRepository>();

        // Add file storage
        services.AddSingleton<IFileStorageService>(sp =>
            new EvoAITest.Core.Services.LocalFileStorageService(
                sp.GetRequiredService<ILogger<EvoAITest.Core.Services.LocalFileStorageService>>(),
                _testDirectory));

        // Add visual comparison services
        services.AddScoped<EvoAITest.Core.Services.VisualComparisonEngine>();
        services.AddScoped<IVisualComparisonService, EvoAITest.Core.Services.VisualComparisonService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<EvoAIDbContext>();
        _taskRepository = _serviceProvider.GetRequiredService<IAutomationTaskRepository>();
        _visualService = _serviceProvider.GetRequiredService<IVisualComparisonService>();
        _fileStorage = _serviceProvider.GetRequiredService<IFileStorageService>();
    }

    [TestMethod]
    public async Task CompleteWorkflow_FirstRunCreatesBaseline_SecondRunCompares()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var checkpoint = new VisualCheckpoint
        {
            Name = "HomePage",
            Type = CheckpointType.FullPage,
            Tolerance = 0.01
        };

        var screenshot = CreateTestImage(1920, 1080, Color.Blue);

        // Act - First run (creates baseline)
        var firstRun = await _visualService!.CompareAsync(
            checkpoint,
            screenshot,
            task.Id,
            "dev",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert first run
        firstRun.Should().NotBeNull();
        firstRun.Passed.Should().BeTrue();
        firstRun.BaselinePath.Should().NotBeNullOrEmpty();

        // Verify baseline was created in database
        var baseline = await _taskRepository!.GetBaselineAsync(
            task.Id,
            "HomePage",
            "dev",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        baseline.Should().NotBeNull();
        baseline!.CheckpointName.Should().Be("HomePage");

        // Act - Second run with identical screenshot (should pass)
        var secondRun = await _visualService.CompareAsync(
            checkpoint,
            screenshot,
            task.Id,
            "dev",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert second run
        secondRun.Should().NotBeNull();
        secondRun.Passed.Should().BeTrue();
        secondRun.DifferencePercentage.Should().Be(0.0);
        secondRun.PixelsDifferent.Should().Be(0);
    }

    [TestMethod]
    public async Task CompleteWorkflow_DetectsDifferences_GeneratesDiff()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var checkpoint = new VisualCheckpoint
        {
            Name = "DynamicPage",
            Type = CheckpointType.FullPage,
            Tolerance = 0.01
        };

        var baselineImage = CreateTestImage(100, 100, Color.Blue);
        var differentImage = CreateTestImage(100, 100, Color.Red);

        // Act - Create baseline
        var firstRun = await _visualService!.CompareAsync(
            checkpoint,
            baselineImage,
            task.Id,
            "dev",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        firstRun.Passed.Should().BeTrue();

        // Act - Compare with different image
        var secondRun = await _visualService.CompareAsync(
            checkpoint,
            differentImage,
            task.Id,
            "dev",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert
        secondRun.Should().NotBeNull();
        secondRun.Passed.Should().BeFalse(); // Should fail due to differences
        secondRun.DifferencePercentage.Should().BeGreaterThan(0.01);
        secondRun.PixelsDifferent.Should().BeGreaterThan(0);
        secondRun.DiffPath.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task CompleteWorkflow_MultipleCheckpoints_IndependentBaselines()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        
        var checkpoint1 = new VisualCheckpoint
        {
            Name = "Header",
            Type = CheckpointType.Element,
            Tolerance = 0.01,
            Selector = "#header"
        };

        var checkpoint2 = new VisualCheckpoint
        {
            Name = "Footer",
            Type = CheckpointType.Element,
            Tolerance = 0.01,
            Selector = "#footer"
        };

        var headerImage = CreateTestImage(1920, 100, Color.Blue);
        var footerImage = CreateTestImage(1920, 80, Color.Green);

        // Act - Create baselines for both checkpoints
        var headerResult = await _visualService!.CompareAsync(
            checkpoint1,
            headerImage,
            task.Id,
            "dev",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        var footerResult = await _visualService.CompareAsync(
            checkpoint2,
            footerImage,
            task.Id,
            "dev",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert
        headerResult.Passed.Should().BeTrue();
        footerResult.Passed.Should().BeTrue();

        // Verify separate baselines
        var baselines = await _taskRepository!.GetBaselinesByTaskAsync(task.Id, CancellationToken.None);
        baselines.Should().HaveCount(2);
        baselines.Should().Contain(b => b.CheckpointName == "Header");
        baselines.Should().Contain(b => b.CheckpointName == "Footer");
    }

    [TestMethod]
    public async Task CompleteWorkflow_DifferentEnvironments_SeparateBaselines()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var checkpoint = new VisualCheckpoint
        {
            Name = "HomePage",
            Type = CheckpointType.FullPage,
            Tolerance = 0.01
        };

        var devImage = CreateTestImage(100, 100, Color.Blue);
        var prodImage = CreateTestImage(100, 100, Color.Green);

        // Act - Create baselines for different environments
        var devResult = await _visualService!.CompareAsync(
            checkpoint,
            devImage,
            task.Id,
            "dev",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        var prodResult = await _visualService.CompareAsync(
            checkpoint,
            prodImage,
            task.Id,
            "prod",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert
        devResult.Passed.Should().BeTrue();
        prodResult.Passed.Should().BeTrue();

        // Verify separate baselines
        var devBaseline = await _taskRepository!.GetBaselineAsync(
            task.Id, "HomePage", "dev", "chromium", "1920x1080", CancellationToken.None);
        
        var prodBaseline = await _taskRepository.GetBaselineAsync(
            task.Id, "HomePage", "prod", "chromium", "1920x1080", CancellationToken.None);

        devBaseline.Should().NotBeNull();
        prodBaseline.Should().NotBeNull();
        devBaseline!.Id.Should().NotBe(prodBaseline!.Id);
    }

    [TestMethod]
    public async Task CompleteWorkflow_ComparisonHistory_OrderedByDateDescending()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var checkpoint = new VisualCheckpoint
        {
            Name = "TestPage",
            Type = CheckpointType.FullPage,
            Tolerance = 0.01
        };

        var image = CreateTestImage(100, 100, Color.Blue);

        // Act - Create multiple comparisons
        var results = new List<VisualComparisonResult>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _visualService!.CompareAsync(
                checkpoint,
                image,
                task.Id,
                "dev",
                "chromium",
                "1920x1080",
                CancellationToken.None);
            
            results.Add(result);
            await Task.Delay(100); // Ensure different timestamps
        }

        // Act - Get history
        var history = await _taskRepository!.GetComparisonHistoryAsync(
            task.Id,
            "TestPage",
            limit: 10,
            CancellationToken.None);

        // Assert
        history.Should().HaveCount(5);
        history.Should().BeInDescendingOrder(r => r.ComparedAt);
        
        // Most recent should be first
        history.First().Id.Should().Be(results.Last().Id);
    }

    [TestMethod]
    public async Task CompleteWorkflow_GetFailedComparisons_ReturnsOnlyFailures()
    {
        // Arrange
        var task = await CreateTestTaskAsync();
        var checkpoint = new VisualCheckpoint
        {
            Name = "TestPage",
            Type = CheckpointType.FullPage,
            Tolerance = 0.001 // Very strict
        };

        var baselineImage = CreateTestImage(100, 100, Color.Blue);
        var identicalImage = CreateTestImage(100, 100, Color.Blue);
        var differentImage = CreateTestImage(100, 100, Color.Red);

        // Create baseline
        await _visualService!.CompareAsync(
            checkpoint, baselineImage, task.Id, "dev", "chromium", "1920x1080", CancellationToken.None);

        // Act - Create passed comparison
        var passedResult = await _visualService.CompareAsync(
            checkpoint, identicalImage, task.Id, "dev", "chromium", "1920x1080", CancellationToken.None);

        // Act - Create failed comparison
        var failedResult = await _visualService.CompareAsync(
            checkpoint, differentImage, task.Id, "dev", "chromium", "1920x1080", CancellationToken.None);

        // Get failed comparisons
        var failures = await _taskRepository!.GetFailedComparisonsAsync(
            task.Id, limit: 50, CancellationToken.None);

        // Assert
        passedResult.Passed.Should().BeTrue();
        failedResult.Passed.Should().BeFalse();
        
        failures.Should().HaveCount(1);
        failures.First().Id.Should().Be(failedResult.Id);
        failures.First().Passed.Should().BeFalse();
    }

    // Helper methods

    private async Task<AutomationTask> CreateTestTaskAsync()
    {
        var task = new AutomationTask
        {
            Id = Guid.NewGuid(),
            Name = "Workflow Test Task",
            Description = "Test task for workflow testing",
            UserId = "test-user",
            Status = EvoAITest.Core.Models.TaskStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _taskRepository!.CreateAsync(task, CancellationToken.None);
    }

    private byte[] CreateTestImage(int width, int height, Color color)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.BackgroundColor(color));
        
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _serviceProvider?.Dispose();
            _dbContext?.Dispose();
            
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        _disposed = true;
    }
}
