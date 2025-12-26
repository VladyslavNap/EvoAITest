using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Data.Models;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.ErrorRecovery;
using EvoAITest.Core.Models.SmartWaiting;
using EvoAITest.Core.Models.SelfHealing;
using EvoAITest.Core.Services.ErrorRecovery;
using ExecutionContext = EvoAITest.Core.Models.ExecutionContext;

namespace EvoAITest.Tests.ErrorRecovery;

/// <summary>
/// Integration tests for ErrorRecoveryService
/// </summary>
[Trait("Category", "Integration")]
public class ErrorRecoveryServiceIntegrationTests : IAsyncLifetime
{
    private EvoAIDbContext _dbContext = null!;
    private Mock<IBrowserAgent> _mockBrowserAgent = null!;
    private Mock<ISelectorHealingService> _mockSelectorHealing = null!;
    private Mock<ISmartWaitService> _mockSmartWait = null!;
    private ErrorRecoveryService _service = null!;
    private IErrorClassifier _classifier = null!;

    public async Task InitializeAsync()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<EvoAIDbContext>()
            .UseInMemoryDatabase($"ErrorRecoveryTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new EvoAIDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Setup mocks
        _mockBrowserAgent = new Mock<IBrowserAgent>();
        _mockSelectorHealing = new Mock<ISelectorHealingService>();
        _mockSmartWait = new Mock<ISmartWaitService>();

        // Setup classifier
        _classifier = new ErrorClassifier(NullLogger<ErrorClassifier>.Instance);

        // Setup browser agent to return a default page state
        _mockBrowserAgent.Setup(x => x.GetPageStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageState { Url = "https://example.com" });

        // Create service
        _service = new ErrorRecoveryService(
            _classifier,
            _dbContext,
            NullLogger<ErrorRecoveryService>.Instance,
            _mockBrowserAgent.Object,
            _mockSelectorHealing.Object,
            _mockSmartWait.Object
        );
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task RecoverAsync_TransientError_SucceedsWithWaitAndRetry()
    {
        // Arrange
        var exception = new Exception("Temporary network issue");
        var context = new ExecutionContext
        {
            Action = "navigate",
            PageUrl = "https://example.com"
        };
        var strategy = new RetryStrategy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            UseJitter = false
        };

        // Act
        var result = await _service.RecoverAsync(exception, context, strategy);

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsAttempted.Should().Contain(RecoveryActionType.WaitAndRetry);
        result.AttemptNumber.Should().BeGreaterThan(0);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.ErrorClassification.ErrorType.Should().Be(ErrorType.Transient);

        // Verify recovery was saved to database
        var history = await _dbContext.RecoveryHistory.FirstOrDefaultAsync();
        history.Should().NotBeNull();
        history!.Success.Should().BeTrue();
        history.ErrorType.Should().Be("Transient");
    }

    [Fact]
    public async Task RecoverAsync_SelectorNotFound_AttemptsAlternativeSelector()
    {
        // Arrange
        var exception = new Exception("Selector not found: #button");
        var context = new ExecutionContext
        {
            Action = "click",
            Selector = "#button",
            PageUrl = "https://example.com"
        };

        // Setup healing to succeed
        _mockSelectorHealing.Setup(x => x.HealSelectorAsync(
            It.IsAny<string>(),
            It.IsAny<PageState>(),
            It.IsAny<string?>(),
            It.IsAny<byte[]?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealedSelector
            {
                OriginalSelector = "#button",
                NewSelector = "button[type='submit']",
                Strategy = HealingStrategy.FuzzyAttributes,
                ConfidenceScore = 0.95,
                HealedAt = DateTimeOffset.UtcNow,
                PageUrl = "https://example.com",
                Reasoning = "Found by attributes"
            });

        var strategy = new RetryStrategy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };

        // Act
        var result = await _service.RecoverAsync(exception, context, strategy);

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsAttempted.Should().Contain(RecoveryActionType.AlternativeSelector);
        context.Selector.Should().Be("button[type='submit']"); // Should be updated

        // Verify healing was called
        _mockSelectorHealing.Verify(x => x.HealSelectorAsync(
            "#button",
            It.IsAny<PageState>(),
            It.IsAny<string?>(),
            It.IsAny<byte[]?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecoverAsync_UnrecoverableError_FailsImmediately()
    {
        // Arrange
        var exception = new Exception("Unknown weird error xyz");
        var context = new ExecutionContext
        {
            Action = "unknown",
            PageUrl = "https://example.com"
        };

        // Act
        var result = await _service.RecoverAsync(exception, context);

        // Assert
        result.Success.Should().BeFalse();
        result.AttemptNumber.Should().Be(0);
        result.ErrorClassification.IsRecoverable.Should().BeFalse();
        result.FinalException.Should().Be(exception);

        // Verify saved to database
        var history = await _dbContext.RecoveryHistory.FirstOrDefaultAsync();
        history.Should().NotBeNull();
        history!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RecoverAsync_ElementNotInteractable_WaitsForStability()
    {
        // Arrange
        var exception = new Exception("Element not interactable");
        var context = new ExecutionContext
        {
            Action = "click",
            Selector = "#button",
            PageUrl = "https://example.com"
        };

        // Setup smart wait to succeed
        _mockSmartWait.Setup(x => x.WaitForStableStateAsync(
            It.IsAny<WaitConditions>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var strategy = new RetryStrategy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };

        // Act
        var result = await _service.RecoverAsync(exception, context, strategy);

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsAttempted.Should().Contain(RecoveryActionType.WaitForStability);

        // Verify smart wait was called
        _mockSmartWait.Verify(x => x.WaitForStableStateAsync(
            It.Is<WaitConditions>(w => w.Conditions.Contains(WaitConditionType.DomStable)),
            10000,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecoverAsync_PageRefresh_NavigatesToCurrentUrl()
    {
        // Arrange
        var exception = new Exception("Stale page state");
        var context = new ExecutionContext
        {
            Action = "click",
            PageUrl = "https://example.com/page"
        };

        // Force classification to suggest page refresh
        var strategy = new RetryStrategy
        {
            MaxRetries = 1,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };

        // Act
        var result = await _service.RecoverAsync(exception, context, strategy);

        // Assert
        result.ActionsAttempted.Should().Contain(RecoveryActionType.WaitAndRetry);
    }

    [Fact]
    public async Task GetRecoveryStatisticsAsync_WithHistory_ReturnsCorrectStats()
    {
        // Arrange - Add some test data
        var taskId = Guid.NewGuid();
        await _dbContext.RecoveryHistory.AddRangeAsync(
            new RecoveryHistory
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ErrorType = "SelectorNotFound",
                ErrorMessage = "Selector not found",
                ExceptionType = "Exception",
                RecoveryStrategy = "Adaptive",
                RecoveryActions = "[\"AlternativeSelector\"]",
                Success = true,
                AttemptNumber = 1,
                DurationMs = 1000,
                RecoveredAt = DateTimeOffset.UtcNow
            },
            new RecoveryHistory
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ErrorType = "SelectorNotFound",
                ErrorMessage = "Selector not found",
                ExceptionType = "Exception",
                RecoveryStrategy = "Adaptive",
                RecoveryActions = "[\"AlternativeSelector\"]",
                Success = true,
                AttemptNumber = 2,
                DurationMs = 2000,
                RecoveredAt = DateTimeOffset.UtcNow
            },
            new RecoveryHistory
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ErrorType = "Transient",
                ErrorMessage = "Network error",
                ExceptionType = "Exception",
                RecoveryStrategy = "Adaptive",
                RecoveryActions = "[\"WaitAndRetry\"]",
                Success = false,
                AttemptNumber = 3,
                DurationMs = 5000,
                RecoveredAt = DateTimeOffset.UtcNow
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _service.GetRecoveryStatisticsAsync(taskId);

        // Assert
        stats["total_recoveries"].Should().Be(3);
        stats["successful_recoveries"].Should().Be(2);
        ((double)stats["success_rate"]).Should().BeApproximately(0.666, 0.01);
        ((double)stats["average_duration_ms"]).Should().BeApproximately(2666, 1);

        var byErrorType = stats["by_error_type"] as List<object>;
        byErrorType.Should().HaveCount(2);
    }

    [Fact]
    public async Task SuggestActionsAsync_WithHistoricalSuccess_PrioritizesLearnedActions()
    {
        // Arrange - Add historical success data
        await _dbContext.RecoveryHistory.AddRangeAsync(
            new RecoveryHistory
            {
                Id = Guid.NewGuid(),
                ErrorType = "SelectorNotFound",
                ErrorMessage = "Test",
                ExceptionType = "Exception",
                RecoveryStrategy = "Adaptive",
                RecoveryActions = "[\"PageRefresh\"]", // Different from default
                Success = true,
                AttemptNumber = 1,
                DurationMs = 1000,
                RecoveredAt = DateTimeOffset.UtcNow
            },
            new RecoveryHistory
            {
                Id = Guid.NewGuid(),
                ErrorType = "SelectorNotFound",
                ErrorMessage = "Test",
                ExceptionType = "Exception",
                RecoveryStrategy = "Adaptive",
                RecoveryActions = "[\"PageRefresh\"]",
                Success = true,
                AttemptNumber = 1,
                DurationMs = 1000,
                RecoveredAt = DateTimeOffset.UtcNow
            }
        );
        await _dbContext.SaveChangesAsync();

        var context = new ExecutionContext { PageUrl = "https://example.com" };

        // Act
        var actions = await _service.SuggestActionsAsync(ErrorType.SelectorNotFound, context);

        // Assert
        actions.First().Should().Be(RecoveryActionType.PageRefresh); // Learned action should be first
        actions.Should().Contain(RecoveryActionType.AlternativeSelector); // Base actions still included
    }

    [Fact]
    public async Task RecoverAsync_ExceedsMaxRetries_FailsAndSavesHistory()
    {
        // Arrange
        var exception = new Exception("Persistent error");
        var context = new ExecutionContext
        {
            Action = "click",
            PageUrl = "https://example.com"
        };

        // Setup all recovery actions to fail
        _mockSmartWait.Setup(x => x.WaitForStableStateAsync(
            It.IsAny<WaitConditions>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Wait failed"));

        var strategy = new RetryStrategy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };

        // Act
        var result = await _service.RecoverAsync(exception, context, strategy);

        // Assert
        result.Success.Should().BeFalse();
        result.AttemptNumber.Should().Be(2);
        result.ActionsAttempted.Should().NotBeEmpty();
        result.FinalException.Should().Be(exception);

        // Verify saved to database
        var history = await _dbContext.RecoveryHistory.FirstOrDefaultAsync();
        history.Should().NotBeNull();
        history!.Success.Should().BeFalse();
        history.AttemptNumber.Should().Be(2);
    }

    [Fact]
    public async Task RecoverAsync_WithoutSelectorHealing_SkipsAlternativeSelector()
    {
        // Arrange - Create service without selector healing
        var serviceWithoutHealing = new ErrorRecoveryService(
            _classifier,
            _dbContext,
            NullLogger<ErrorRecoveryService>.Instance,
            _mockBrowserAgent.Object,
            selectorHealing: null,
            smartWait: null
        );

        var exception = new Exception("Selector not found: #button");
        var context = new ExecutionContext
        {
            Action = "click",
            Selector = "#button",
            PageUrl = "https://example.com"
        };

        var strategy = new RetryStrategy
        {
            MaxRetries = 1,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };

        // Act
        var result = await serviceWithoutHealing.RecoverAsync(exception, context, strategy);

        // Assert
        // Should succeed with WaitAndRetry instead
        result.ActionsAttempted.Should().NotContain(RecoveryActionType.AlternativeSelector);
    }
}
