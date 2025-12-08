using EvoAITest.Core.Models;
using EvoAITest.Tests.Utilities;
using FluentAssertions;

namespace EvoAITest.Tests.VisualRegression;

[TestClass]
public sealed class VisualComparisonServiceTests : IDisposable
{
    private VisualRegressionTestFixture? _fixture;

    [TestInitialize]
    public void Setup()
    {
        _fixture = new VisualRegressionTestFixture();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    public async Task CompareAsync_NoBaselineExists_CreatesNewBaseline()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header");
        var screenshot = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Blue);

        // Act
        var result = await _fixture.ComparisonService.CompareAsync(
            checkpoint,
            screenshot,
            task.Id,
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passed.Should().BeTrue();
        result.DifferencePercentage.Should().Be(0.0); // First run should have 0 difference
        result.CheckpointName.Should().Be("HomePage_Header");
        result.BaselinePath.Should().NotBeNullOrEmpty();

        // Verify baseline was saved to database
        var baseline = await _fixture.TaskRepository.GetBaselineAsync(
            task.Id,
            "HomePage_Header",
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);
        
        baseline.Should().NotBeNull();
        baseline!.ApprovedBy.Should().NotBeNullOrEmpty(); // Baseline exists
    }

    [TestMethod]
    public async Task CompareAsync_BaselineExists_ComparesImages()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var baselineImage = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Blue);
        await _fixture.CreateTestBaselineAsync(task.Id, "HomePage_Header", baselineImage);

        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header");
        var actualImage = TestImageGenerator.CloneImage(baselineImage);

        // Act
        var result = await _fixture.ComparisonService.CompareAsync(
            checkpoint,
            actualImage,
            task.Id,
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passed.Should().BeTrue();
        result.DifferencePercentage.Should().Be(0.0);
        result.PixelsDifferent.Should().Be(0);
        result.BaselinePath.Should().NotBeNullOrEmpty(); // Has baseline
    }

    [TestMethod]
    public async Task CompareAsync_DifferenceDetected_ReturnsFailedResult()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var baselineImage = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Blue);
        await _fixture.CreateTestBaselineAsync(task.Id, "HomePage_Header", baselineImage);

        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header", tolerance: 0.01);
        var actualImage = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Red);

        // Act
        var result = await _fixture.ComparisonService.CompareAsync(
            checkpoint,
            actualImage,
            task.Id,
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Passed.Should().BeFalse();
        result.DifferencePercentage.Should().BeGreaterThan(0.01);
        result.DiffPath.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task CompareAsync_SavesComparisonToDatabase()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var baselineImage = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Blue);
        await _fixture.CreateTestBaselineAsync(task.Id, "HomePage_Header", baselineImage);

        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header");
        var actualImage = TestImageGenerator.CloneImage(baselineImage);

        // Act
        var result = await _fixture.ComparisonService.CompareAsync(
            checkpoint,
            actualImage,
            task.Id,
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);

        // Verify saved to database
        var history = await _fixture.TaskRepository.GetComparisonHistoryAsync(
            task.Id,
            "HomePage_Header",
            limit: 10,
            CancellationToken.None);

        history.Should().NotBeEmpty();
        history.First().Id.Should().Be(result.Id);
    }

    [TestMethod]
    public async Task ApproveNewBaselineAsync_UpdatesBaselineStatus()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header");
        var screenshot = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Blue);

        // Create initial baseline (unapproved)
        var comparisonResult = await _fixture.ComparisonService.CompareAsync(
            checkpoint,
            screenshot,
            task.Id,
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Act - FIXED method name
        await _fixture.ComparisonService.ApproveNewBaselineAsync(
            comparisonResult.Id,
            "test-user",
            "Approved for testing",
            CancellationToken.None);

        // Assert
        var baseline = await _fixture.TaskRepository.GetBaselineAsync(
            task.Id,
            "HomePage_Header",
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        baseline.Should().NotBeNull();
        // REMOVED: baseline!.IsApproved.Should().BeTrue() - property doesn't exist
        baseline!.ApprovedBy.Should().Be("test-user");
        // FIXED: ApprovalReason ? UpdateReason
        baseline.UpdateReason.Should().Be("Approved for testing");
        // REMOVED: baseline.ApprovedAt.Should().NotBeNull() - property doesn't exist
        baseline.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [TestMethod]
    public async Task GetComparisonHistoryAsync_ReturnsOrderedHistory()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header");
        var screenshot = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Blue);

        // Create multiple comparisons
        for (int i = 0; i < 5; i++)
        {
            await _fixture.ComparisonService.CompareAsync(
                checkpoint,
                screenshot,
                task.Id,
                "test",
                "chromium",
                "1920x1080",
                CancellationToken.None);
            
            await Task.Delay(100); // Ensure different timestamps
        }

        // Act
        var history = await _fixture.TaskRepository.GetComparisonHistoryAsync(
            task.Id,
            "HomePage_Header",
            limit: 10,
            CancellationToken.None);

        // Assert
        history.Should().HaveCount(5);
        history.Should().BeInDescendingOrder(r => r.ComparedAt); // Most recent first
    }

    [TestMethod]
    public async Task CompareAsync_DifferentEnvironments_MaintainsSeparateBaselines()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header");
        
        var devImage = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Blue);
        var prodImage = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Green);

        // Act - Create baselines for different environments
        var devResult = await _fixture.ComparisonService.CompareAsync(
            checkpoint, devImage, task.Id, "dev", "chromium", "1920x1080", CancellationToken.None);
        
        var prodResult = await _fixture.ComparisonService.CompareAsync(
            checkpoint, prodImage, task.Id, "prod", "chromium", "1920x1080", CancellationToken.None);

        // Assert
        devResult.Passed.Should().BeTrue();
        prodResult.Passed.Should().BeTrue();
        devResult.DifferencePercentage.Should().Be(0.0);
        prodResult.DifferencePercentage.Should().Be(0.0);

        var devBaseline = await _fixture.TaskRepository.GetBaselineAsync(
            task.Id, "HomePage_Header", "dev", "chromium", "1920x1080", CancellationToken.None);
        
        var prodBaseline = await _fixture.TaskRepository.GetBaselineAsync(
            task.Id, "HomePage_Header", "prod", "chromium", "1920x1080", CancellationToken.None);

        devBaseline.Should().NotBeNull();
        prodBaseline.Should().NotBeNull();
        devBaseline!.Id.Should().NotBe(prodBaseline!.Id);
    }

    [TestMethod]
    public async Task CompareAsync_CalculatesImageHash()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header");
        var screenshot = TestImageGenerator.CreateSolidColorImage(1920, 1080, SixLabors.ImageSharp.Color.Blue);

        // Act
        var result = await _fixture.ComparisonService.CompareAsync(
            checkpoint,
            screenshot,
            task.Id,
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        // Assert
        var baseline = await _fixture.TaskRepository.GetBaselineAsync(
            task.Id,
            "HomePage_Header",
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        baseline.Should().NotBeNull();
        baseline!.ImageHash.Should().NotBeNullOrEmpty();
        baseline.ImageHash.Length.Should().BeGreaterThan(20); // SHA256 hash
    }

    [TestMethod]
    public async Task CompareAsync_MultipleCheckpoints_MaintainsSeparateBaselines()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        
        var checkpoint1 = _fixture.CreateTestCheckpoint("Header");
        var checkpoint2 = _fixture.CreateTestCheckpoint("Footer");
        
        var image1 = TestImageGenerator.CreateSolidColorImage(1920, 200, SixLabors.ImageSharp.Color.Blue);
        var image2 = TestImageGenerator.CreateSolidColorImage(1920, 100, SixLabors.ImageSharp.Color.Green);

        // Act
        var result1 = await _fixture.ComparisonService.CompareAsync(
            checkpoint1, image1, task.Id, "test", "chromium", "1920x1080", CancellationToken.None);
        
        var result2 = await _fixture.ComparisonService.CompareAsync(
            checkpoint2, image2, task.Id, "test", "chromium", "1920x1080", CancellationToken.None);

        // Assert
        result1.Passed.Should().BeTrue();
        result2.Passed.Should().BeTrue();
        result1.CheckpointName.Should().Be("Header");
        result2.CheckpointName.Should().Be("Footer");

        var baselines = await _fixture.TaskRepository.GetBaselinesByTaskAsync(
            task.Id, CancellationToken.None);
        
        baselines.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task CompareAsync_InvalidImageData_ThrowsException()
    {
        // Arrange
        var task = await _fixture!.CreateTestTaskAsync();
        var checkpoint = _fixture.CreateTestCheckpoint("HomePage_Header");
        var invalidImage = new byte[] { 1, 2, 3, 4, 5 }; // Not a valid image

        // Act & Assert
        var act = async () => await _fixture.ComparisonService.CompareAsync(
            checkpoint,
            invalidImage,
            task.Id,
            "test",
            "chromium",
            "1920x1080",
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    public void Dispose()
    {
        _fixture?.Dispose();
    }
}
