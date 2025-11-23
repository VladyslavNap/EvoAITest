using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TaskStatus = EvoAITest.Core.Models.TaskStatus;

namespace EvoAITest.Tests.Data;

/// <summary>
/// Unit tests for EvoAIDbContext.
/// Tests entity configuration, relationships, and database operations.
/// </summary>
public sealed class EvoAIDbContextTests : IDisposable
{
    private readonly EvoAIDbContext _context;

    public EvoAIDbContextTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<EvoAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EvoAIDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task DbContext_CanAddAutomationTask()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Test Task",
            Description = "Test description",
            NaturalLanguagePrompt = "Do something automated",
            Status = TaskStatus.Pending
        };

        // Act
        _context.AutomationTasks.Add(task);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        var saved = await _context.AutomationTasks.FindAsync(task.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Task");
        saved.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task DbContext_CanAddExecutionHistory()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Parent Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var history = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 5000,
            FinalOutput = "Success",
            StepResults = "[{\"step\":1}]",
            ScreenshotUrls = "[\"url1\",\"url2\"]"
        };

        // Act
        _context.ExecutionHistory.Add(history);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        var saved = await _context.ExecutionHistory.FindAsync(history.Id);
        saved.Should().NotBeNull();
        saved!.TaskId.Should().Be(task.Id);
        saved.ExecutionStatus.Should().Be(ExecutionStatus.Success);
        saved.DurationMs.Should().Be(5000);
    }

    [Fact]
    public async Task DbContext_CascadeDeleteRemovesExecutionHistory()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task to Delete",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var history = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000
        };

        _context.ExecutionHistory.Add(history);
        await _context.SaveChangesAsync();

        var historyId = history.Id;

        // Act
        _context.AutomationTasks.Remove(task);
        await _context.SaveChangesAsync();

        // Assert
        var deletedHistory = await _context.ExecutionHistory.FindAsync(historyId);
        deletedHistory.Should().BeNull();
    }

    [Fact]
    public async Task DbContext_AutomaticallyUpdatesTimestamp()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Original Name",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var originalUpdateTime = task.UpdatedAt;
        await Task.Delay(10); // Ensure time difference

        // Act
        task.Name = "Updated Name";
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.AutomationTasks.FindAsync(task.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public async Task DbContext_CanQueryTasksByUserId()
    {
        // Arrange
        var task1 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task 1",
            NaturalLanguagePrompt = "Test 1"
        };

        var task2 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task 2",
            NaturalLanguagePrompt = "Test 2"
        };

        var task3 = new AutomationTask
        {
            UserId = "user-2",
            Name = "Task 3",
            NaturalLanguagePrompt = "Test 3"
        };

        _context.AutomationTasks.AddRange(task1, task2, task3);
        await _context.SaveChangesAsync();

        // Act
        var userTasks = await _context.AutomationTasks
            .Where(t => t.UserId == "user-1")
            .ToListAsync();

        // Assert
        userTasks.Should().HaveCount(2);
        userTasks.Should().AllSatisfy(t => t.UserId.Should().Be("user-1"));
    }

    [Fact]
    public async Task DbContext_CanQueryTasksByStatus()
    {
        // Arrange
        var task1 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Pending Task",
            NaturalLanguagePrompt = "Test",
            Status = TaskStatus.Pending
        };

        var task2 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Completed Task",
            NaturalLanguagePrompt = "Test",
            Status = TaskStatus.Completed
        };

        _context.AutomationTasks.AddRange(task1, task2);
        await _context.SaveChangesAsync();

        // Act
        var pendingTasks = await _context.AutomationTasks
            .Where(t => t.Status == TaskStatus.Pending)
            .ToListAsync();

        // Assert
        pendingTasks.Should().HaveCount(1);
        pendingTasks[0].Name.Should().Be("Pending Task");
    }

    [Fact]
    public async Task DbContext_CanIncludeExecutionHistory()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task with History",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var history1 = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000
        };

        var history2 = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Failed,
            DurationMs = 500,
            ErrorMessage = "Test error"
        };

        _context.ExecutionHistory.AddRange(history1, history2);
        await _context.SaveChangesAsync();

        // Act
        var taskWithHistory = await _context.AutomationTasks
            .Include(t => t.Executions)
            .FirstOrDefaultAsync(t => t.Id == task.Id);

        // Assert
        taskWithHistory.Should().NotBeNull();
        taskWithHistory!.Executions.Should().HaveCount(2);
        taskWithHistory.Executions.Should().Contain(e => e.ExecutionStatus == ExecutionStatus.Success);
        taskWithHistory.Executions.Should().Contain(e => e.ExecutionStatus == ExecutionStatus.Failed);
    }

    [Fact]
    public async Task DbContext_CanQueryExecutionHistoryByTaskId()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Test Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var history = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 2000
        };

        _context.ExecutionHistory.Add(history);
        await _context.SaveChangesAsync();

        // Act
        var histories = await _context.ExecutionHistory
            .Where(h => h.TaskId == task.Id)
            .ToListAsync();

        // Assert
        histories.Should().HaveCount(1);
        histories[0].TaskId.Should().Be(task.Id);
    }

    [Fact]
    public async Task DbContext_CanQueryExecutionHistoryByStatus()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Test Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var successHistory = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000
        };

        var failedHistory = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Failed,
            DurationMs = 500,
            ErrorMessage = "Error"
        };

        _context.ExecutionHistory.AddRange(successHistory, failedHistory);
        await _context.SaveChangesAsync();

        // Act
        var failedHistories = await _context.ExecutionHistory
            .Where(h => h.ExecutionStatus == ExecutionStatus.Failed)
            .ToListAsync();

        // Assert
        failedHistories.Should().HaveCount(1);
        failedHistories[0].ExecutionStatus.Should().Be(ExecutionStatus.Failed);
        failedHistories[0].ErrorMessage.Should().Be("Error");
    }

    [Fact]
    public async Task DbContext_ExecutionHistoryNavigationPropertyWorks()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Test Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var history = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000
        };

        _context.ExecutionHistory.Add(history);
        await _context.SaveChangesAsync();

        // Act
        var historyWithTask = await _context.ExecutionHistory
            .Include(h => h.Task)
            .FirstOrDefaultAsync(h => h.Id == history.Id);

        // Assert
        historyWithTask.Should().NotBeNull();
        historyWithTask!.Task.Should().NotBeNull();
        historyWithTask.Task!.Name.Should().Be("Test Task");
    }

    [Fact]
    public async Task DbContext_CanFilterByCompositeIndex()
    {
        // Arrange
        var task1 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task 1",
            NaturalLanguagePrompt = "Test",
            Status = TaskStatus.Pending
        };

        var task2 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task 2",
            NaturalLanguagePrompt = "Test",
            Status = TaskStatus.Completed
        };

        var task3 = new AutomationTask
        {
            UserId = "user-2",
            Name = "Task 3",
            NaturalLanguagePrompt = "Test",
            Status = TaskStatus.Pending
        };

        _context.AutomationTasks.AddRange(task1, task2, task3);
        await _context.SaveChangesAsync();

        // Act - Query using composite index (UserId + Status)
        var results = await _context.AutomationTasks
            .Where(t => t.UserId == "user-1" && t.Status == TaskStatus.Pending)
            .ToListAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Task 1");
    }

    [Fact]
    public async Task DbContext_ExecutionHistoryStoresJsonData()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Test Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var stepResultsJson = "[{\"step\":1,\"success\":true},{\"step\":2,\"success\":false}]";
        var screenshotUrlsJson = "[\"https://example.com/screenshot1.png\",\"https://example.com/screenshot2.png\"]";
        var metadataJson = "{\"key\":\"value\",\"number\":42}";

        var history = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.PartialSuccess,
            DurationMs = 3000,
            StepResults = stepResultsJson,
            ScreenshotUrls = screenshotUrlsJson,
            Metadata = metadataJson
        };

        // Act
        _context.ExecutionHistory.Add(history);
        await _context.SaveChangesAsync();

        var saved = await _context.ExecutionHistory.FindAsync(history.Id);

        // Assert
        saved.Should().NotBeNull();
        saved!.StepResults.Should().Be(stepResultsJson);
        saved.ScreenshotUrls.Should().Be(screenshotUrlsJson);
        saved.Metadata.Should().Be(metadataJson);
    }
}
