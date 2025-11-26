using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using EvoAITest.Core.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TaskStatus = EvoAITest.Core.Models.TaskStatus;

namespace EvoAITest.Tests.Repositories;

/// <summary>
/// Unit tests for AutomationTaskRepository.
/// Uses InMemory database for fast, isolated testing.
/// </summary>
public sealed class AutomationTaskRepositoryTests : IDisposable
{
    private readonly EvoAIDbContext _context;
    private readonly AutomationTaskRepository _repository;
    private readonly Mock<ILogger<AutomationTaskRepository>> _mockLogger;

    public AutomationTaskRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<EvoAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EvoAIDbContext(options);
        _mockLogger = new Mock<ILogger<AutomationTaskRepository>>();
        _repository = new AutomationTaskRepository(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AutomationTaskRepository(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*context*");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new AutomationTaskRepository(_context, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*logger*");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingTask_ReturnsTask()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Test Task",
            NaturalLanguagePrompt = "Test prompt",
            Status = TaskStatus.Pending
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.Name.Should().Be("Test Task");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentTask_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_IncludesExecutionHistory()
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
        var result = await _repository.GetByIdAsync(task.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Executions.Should().HaveCount(1);
        result.Executions.First().Id.Should().Be(history.Id);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_WithExistingTasks_ReturnsTasks()
    {
        // Arrange
        var task1 = new AutomationTask { UserId = "user-1", Name = "Task 1", NaturalLanguagePrompt = "Test" };
        var task2 = new AutomationTask { UserId = "user-1", Name = "Task 2", NaturalLanguagePrompt = "Test" };
        var task3 = new AutomationTask { UserId = "user-2", Name = "Task 3", NaturalLanguagePrompt = "Test" };

        _context.AutomationTasks.AddRange(task1, task2, task3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync("user-1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.UserId.Should().Be("user-1"));
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetByUserIdAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByUserIdAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var task1 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Older Task",
            NaturalLanguagePrompt = "Test",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var task2 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Newer Task",
            NaturalLanguagePrompt = "Test",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _context.AutomationTasks.AddRange(task1, task2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync("user-1");

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Newer Task");
        result[1].Name.Should().Be("Older Task");
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithMatchingTasks_ReturnsTasks()
    {
        // Arrange
        var task1 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Pending Task 1",
            NaturalLanguagePrompt = "Test",
            Status = TaskStatus.Pending
        };

        var task2 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Pending Task 2",
            NaturalLanguagePrompt = "Test",
            Status = TaskStatus.Pending
        };

        var task3 = new AutomationTask
        {
            UserId = "user-1",
            Name = "Completed Task",
            NaturalLanguagePrompt = "Test",
            Status = TaskStatus.Completed
        };

        _context.AutomationTasks.AddRange(task1, task2, task3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(TaskStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.Status.Should().Be(TaskStatus.Pending));
    }

    #endregion

    #region GetByUserIdAndStatusAsync Tests

    [Fact]
    public async Task GetByUserIdAndStatusAsync_WithMatchingTasks_ReturnsTasks()
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

        // Act
        var result = await _repository.GetByUserIdAndStatusAsync("user-1", TaskStatus.Pending);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be("user-1");
        result[0].Status.Should().Be(TaskStatus.Pending);
    }

    [Fact]
    public async Task GetByUserIdAndStatusAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetByUserIdAndStatusAsync(null!, TaskStatus.Pending);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidTask_CreatesTask()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "New Task",
            NaturalLanguagePrompt = "Test prompt",
            Status = TaskStatus.Pending
        };

        // Act
        var result = await _repository.CreateAsync(task);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));

        // Verify in database
        var dbTask = await _context.AutomationTasks.FindAsync(result.Id);
        dbTask.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateAsync_SetsTimestampsIfNotSet()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "New Task",
            NaturalLanguagePrompt = "Test"
        };

        // Act
        var result = await _repository.CreateAsync(task);

        // Assert
        result.CreatedAt.Should().NotBe(default);
        result.UpdatedAt.Should().NotBe(default);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingTask_UpdatesTask()
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

        var originalUpdatedAt = task.UpdatedAt;
        await Task.Delay(10);

        // Act
        task.Name = "Updated Name";
        await _repository.UpdateAsync(task);

        // Assert
        var dbTask = await _context.AutomationTasks.FindAsync(task.Id);
        dbTask.Should().NotBeNull();
        dbTask!.Name.Should().Be("Updated Name");
        dbTask.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentTask_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = new AutomationTask
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Name = "Task",
            NaturalLanguagePrompt = "Test"
        };

        // Act
        Func<Task> act = async () => await _repository.UpdateAsync(task);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingTask_DeletesTask()
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
        var taskId = task.Id;

        // Act
        await _repository.DeleteAsync(taskId);

        // Assert
        var dbTask = await _context.AutomationTasks.FindAsync(taskId);
        dbTask.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentTask_ThrowsInvalidOperationException()
    {
        // Act
        Func<Task> act = async () => await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_CascadeDeletesExecutionHistory()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task",
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
        await _repository.DeleteAsync(task.Id);

        // Assert
        var dbHistory = await _context.ExecutionHistory.FindAsync(historyId);
        dbHistory.Should().BeNull();
    }

    #endregion

    #region GetExecutionHistoryAsync Tests

    [Fact]
    public async Task GetExecutionHistoryAsync_WithExistingHistory_ReturnsHistory()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var history1 = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2)
        };

        var history2 = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Failed,
            DurationMs = 500,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _context.ExecutionHistory.AddRange(history1, history2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetExecutionHistoryAsync(task.Id);

        // Assert
        result.Should().HaveCount(2);
        result[0].StartedAt.Should().BeAfter(result[1].StartedAt); // Most recent first
    }

    [Fact]
    public async Task GetExecutionHistoryAsync_OrdersByStartedAtDescending()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var olderHistory = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-2)
        };

        var newerHistory = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        _context.ExecutionHistory.AddRange(olderHistory, newerHistory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetExecutionHistoryAsync(task.Id);

        // Assert
        result[0].Id.Should().Be(newerHistory.Id);
        result[1].Id.Should().Be(olderHistory.Id);
    }

    #endregion

    #region AddExecutionHistoryAsync Tests

    [Fact]
    public async Task AddExecutionHistoryAsync_WithValidHistory_AddsHistory()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        var history = new ExecutionHistory
        {
            TaskId = task.Id,
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000,
            FinalOutput = "Success"
        };

        // Act
        var result = await _repository.AddExecutionHistoryAsync(history);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));

        // Verify in database
        var dbHistory = await _context.ExecutionHistory.FindAsync(result.Id);
        dbHistory.Should().NotBeNull();
    }

    [Fact]
    public async Task AddExecutionHistoryAsync_WithNullHistory_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _repository.AddExecutionHistoryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddExecutionHistoryAsync_WithNonExistentTask_ThrowsInvalidOperationException()
    {
        // Arrange
        var history = new ExecutionHistory
        {
            TaskId = Guid.NewGuid(),
            ExecutionStatus = ExecutionStatus.Success,
            DurationMs = 1000
        };

        // Act
        Func<Task> act = async () => await _repository.AddExecutionHistoryAsync(history);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingTask_ReturnsTrue()
    {
        // Arrange
        var task = new AutomationTask
        {
            UserId = "user-1",
            Name = "Task",
            NaturalLanguagePrompt = "Test"
        };

        _context.AutomationTasks.Add(task);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(task.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentTask_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetTaskCountByUserAsync Tests

    [Fact]
    public async Task GetTaskCountByUserAsync_WithExistingTasks_ReturnsCount()
    {
        // Arrange
        var task1 = new AutomationTask { UserId = "user-1", Name = "Task 1", NaturalLanguagePrompt = "Test" };
        var task2 = new AutomationTask { UserId = "user-1", Name = "Task 2", NaturalLanguagePrompt = "Test" };
        var task3 = new AutomationTask { UserId = "user-2", Name = "Task 3", NaturalLanguagePrompt = "Test" };

        _context.AutomationTasks.AddRange(task1, task2, task3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTaskCountByUserAsync("user-1");

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetTaskCountByUserAsync_WithNoTasks_ReturnsZero()
    {
        // Act
        var result = await _repository.GetTaskCountByUserAsync("user-1");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetTaskCountByUserAsync_WithNullUserId_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _repository.GetTaskCountByUserAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
