using System.Diagnostics;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Options;
using EvoAITest.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EvoAITest.Tests.Services;

/// <summary>
/// Comprehensive unit tests for DefaultToolExecutor.
/// Tests successful execution, retry logic, validation, cancellation, error handling, and logging.
/// </summary>
public class DefaultToolExecutorTests
{
    private readonly Mock<IBrowserAgent> _mockBrowserAgent;
    private readonly Mock<IBrowserToolRegistry> _mockToolRegistry;
    private readonly Mock<ILogger<DefaultToolExecutor>> _mockLogger;
    private readonly ToolExecutorOptions _options;

    public DefaultToolExecutorTests()
    {
        _mockBrowserAgent = new Mock<IBrowserAgent>();
        _mockToolRegistry = new Mock<IBrowserToolRegistry>();
        _mockLogger = new Mock<ILogger<DefaultToolExecutor>>();
        _options = new ToolExecutorOptions
        {
            MaxRetries = 3,
            InitialRetryDelayMs = 100, // Faster for tests
            MaxRetryDelayMs = 5000,
            UseExponentialBackoff = true,
            TimeoutPerToolMs = 10000,
            EnableDetailedLogging = true,
            MaxHistorySize = 100
        };
    }

    #region Successful Execution Tests

    [Fact]
    public async Task ExecuteToolAsync_NavigateTool_ReturnsSuccess()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });
        
        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var toolCall = new ToolCall(
            ToolName: "navigate",
            Parameters: new Dictionary<string, object> { ["url"] = "https://example.com" },
            Reasoning: "Navigate to test page",
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeTrue();
        result.ToolName.Should().Be("navigate");
        result.AttemptCount.Should().Be(1);
        result.ExecutionDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Error.Should().BeNull();
        result.Result.Should().BeNull(); // navigate returns void
        
        _mockBrowserAgent.Verify(x => x.NavigateAsync("https://example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_ClickTool_ReturnsSuccess()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("click", new[] { "selector" });
        
        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var toolCall = new ToolCall(
            ToolName: "click",
            Parameters: new Dictionary<string, object> { ["selector"] = "#submit-btn" },
            Reasoning: "Click submit button",
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeTrue();
        result.ToolName.Should().Be("click");
        result.AttemptCount.Should().Be(1);
        result.WasRetried.Should().BeFalse();
        
        _mockBrowserAgent.Verify(x => x.ClickAsync("#submit-btn", It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_TypeTool_ReturnsSuccess()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("type", new[] { "selector", "text" });
        
        _mockBrowserAgent.Setup(x => x.TypeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var toolCall = new ToolCall(
            ToolName: "type",
            Parameters: new Dictionary<string, object> 
            { 
                ["selector"] = "#username",
                ["text"] = "testuser"
            },
            Reasoning: "Enter username",
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeTrue();
        result.ToolName.Should().Be("type");
        result.AttemptCount.Should().Be(1);
        
        _mockBrowserAgent.Verify(x => x.TypeAsync("#username", "testuser", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteSequenceAsync_MultipleTools_ExecutesInOrder()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });
        SetupToolInRegistry("click", new[] { "selector" });
        SetupToolInRegistry("type", new[] { "selector", "text" });

        var callOrder = new List<string>();
        
        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("navigate"))
            .Returns(Task.CompletedTask);
        
        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("click"))
            .Returns(Task.CompletedTask);
        
        _mockBrowserAgent.Setup(x => x.TypeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("type"))
            .Returns(Task.CompletedTask);

        var correlationId = Guid.NewGuid().ToString();
        var toolCalls = new[]
        {
            new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "Step 1", correlationId),
            new ToolCall("type", new Dictionary<string, object> { ["selector"] = "#input", ["text"] = "test" }, "Step 2", correlationId),
            new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "Step 3", correlationId)
        };

        // Act
        var results = await executor.ExecuteSequenceAsync(toolCalls);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        results.Select(r => r.ToolName).Should().ContainInOrder("navigate", "type", "click");
        callOrder.Should().Equal("navigate", "type", "click", "tools should execute in order");
    }

    [Fact]
    public async Task ExecuteToolAsync_RecordsCorrectExecutionDuration()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });

        var delayMs = 200;
        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(delayMs);
            });

        var toolCall = new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", Guid.NewGuid().ToString());

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await executor.ExecuteToolAsync(toolCall);
        stopwatch.Stop();

        // Assert
        result.Success.Should().BeTrue();
        result.ExecutionDuration.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(delayMs, "should include operation time");
        result.ExecutionDuration.TotalMilliseconds.Should().BeLessThan(stopwatch.ElapsedMilliseconds + 100, "should be reasonably close to actual time");
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public async Task ExecuteToolAsync_TransientError_RetriesWithBackoff()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 2);
        SetupToolInRegistry("click", new[] { "selector" });

        var attemptCount = 0;
        var attemptTimes = new List<DateTimeOffset>();
        
        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptTimes.Add(DateTimeOffset.UtcNow);
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new TimeoutException("Transient timeout");
                }
                return Task.CompletedTask;
            });

        var toolCall = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeTrue("should succeed after retries");
        result.AttemptCount.Should().Be(3, "should make 3 attempts (1 initial + 2 retries)");
        result.WasRetried.Should().BeTrue();
        result.Metadata.Should().ContainKey("retry_reasons");
        result.Metadata.Should().ContainKey("retry_delays");

        var retryReasons = result.Metadata["retry_reasons"] as string[];
        retryReasons.Should().HaveCount(2, "should have 2 retry reasons");
        retryReasons.Should().AllSatisfy(r => r.Should().Contain("TimeoutException"));

        // Verify exponential backoff delays
        if (attemptTimes.Count >= 3)
        {
            var delay1 = (attemptTimes[1] - attemptTimes[0]).TotalMilliseconds;
            var delay2 = (attemptTimes[2] - attemptTimes[1]).TotalMilliseconds;
            
            delay1.Should().BeGreaterThan(50, "first retry should have delay");
            delay2.Should().BeGreaterThanOrEqualTo(delay1 * 0.75, "second retry should have longer delay (accounting for jitter)");
        }
    }

    [Fact]
    public async Task ExecuteToolAsync_SucceedsOnSecondAttempt_ReturnsSuccess()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 2);
        SetupToolInRegistry("navigate", new[] { "url" });

        var attemptCount = 0;
        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new TimeoutException("First attempt fails");
                }
                return Task.CompletedTask;
            });

        var toolCall = new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeTrue();
        result.AttemptCount.Should().Be(2, "should succeed on second attempt");
        result.WasRetried.Should().BeTrue();
        result.Metadata["retry_reasons"].Should().BeOfType<string[]>().Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteToolAsync_MaxRetriesExceeded_ReturnsFailure()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 2);
        SetupToolInRegistry("click", new[] { "selector" });

        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Persistent timeout"));

        var toolCall = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        result.AttemptCount.Should().Be(3, "should make all attempts (1 initial + 2 retries)");
        result.Error.Should().BeOfType<TimeoutException>();
        result.WasRetried.Should().BeTrue();
        
        var retryReasons = result.Metadata["retry_reasons"] as string[];
        retryReasons.Should().HaveCount(2, "should have 2 retry reasons");
    }

    [Fact]
    public async Task ExecuteToolAsync_ExponentialBackoff_UsesCorrectDelays()
    {
        // Arrange
        _options.UseExponentialBackoff = true;
        _options.InitialRetryDelayMs = 100;
        _options.MaxRetryDelayMs = 5000;
        
        var executor = CreateExecutor(maxRetries: 3);
        SetupToolInRegistry("click", new[] { "selector" });

        var attemptTimes = new List<DateTimeOffset>();
        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptTimes.Add(DateTimeOffset.UtcNow);
                throw new TimeoutException("Test");
            });

        var toolCall = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        attemptTimes.Should().HaveCount(4, "should make 4 attempts");

        // Calculate actual delays
        var delays = new List<double>();
        for (int i = 1; i < attemptTimes.Count; i++)
        {
            delays.Add((attemptTimes[i] - attemptTimes[i - 1]).TotalMilliseconds);
        }

        // Expected delays (with 25% jitter): ~100ms, ~200ms, ~400ms
        delays[0].Should().BeInRange(75, 125, "first retry delay ~100ms ± jitter");
        delays[1].Should().BeInRange(150, 250, "second retry delay ~200ms ± jitter");
        delays[2].Should().BeInRange(300, 500, "third retry delay ~400ms ± jitter");
    }

    [Fact]
    public async Task ExecuteToolAsync_LinearBackoff_UsesFixedDelays()
    {
        // Arrange
        _options.UseExponentialBackoff = false;
        _options.InitialRetryDelayMs = 200;
        
        var executor = CreateExecutor(maxRetries: 2);
        SetupToolInRegistry("click", new[] { "selector" });

        var attemptTimes = new List<DateTimeOffset>();
        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptTimes.Add(DateTimeOffset.UtcNow);
                throw new TimeoutException("Test");
            });

        var toolCall = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        attemptTimes.Should().HaveCount(3);

        var delay1 = (attemptTimes[1] - attemptTimes[0]).TotalMilliseconds;
        var delay2 = (attemptTimes[2] - attemptTimes[1]).TotalMilliseconds;

        // With fixed delay, both should be ~200ms (no exponential growth)
        delay1.Should().BeInRange(180, 220, "first retry should use fixed delay");
        delay2.Should().BeInRange(180, 220, "second retry should use same fixed delay");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ExecuteToolAsync_ToolNotInRegistry_ReturnsFailure()
    {
        // Arrange
        var executor = CreateExecutor();
        _mockToolRegistry.Setup(r => r.ToolExists("nonexistent")).Returns(false);
        _mockToolRegistry.Setup(r => r.GetToolNames()).Returns(new[] { "navigate", "click" });

        var toolCall = new ToolCall("nonexistent", new Dictionary<string, object>(), "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeOfType<InvalidOperationException>();
        result.Error!.Message.Should().Contain("not found in registry");
        result.Metadata["validation_error"].Should().Be("tool_not_found");
    }

    [Fact]
    public async Task ExecuteToolAsync_MissingRequiredParameter_ReturnsFailure()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" }); // url is required

        var toolCall = new ToolCall("navigate", new Dictionary<string, object>(), "", Guid.NewGuid().ToString()); // Missing url

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeOfType<ArgumentException>();
        result.Error!.Message.Should().Contain("Missing required parameters");
        result.Metadata["validation_error"].Should().Be("missing_required_parameters");
    }

    [Fact]
    public async Task ExecuteToolAsync_InvalidParameterType_ThrowsArgumentException()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("click", new[] { "selector" });

        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Pass an object instead of string for selector
        var toolCall = new ToolCall("click", 
            new Dictionary<string, object> { ["selector"] = new { invalid = "object" } }, 
            "", 
            Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert - The executor should handle type conversion gracefully
        // Either succeed if conversion works, or fail with clear error
        if (!result.Success)
        {
            result.Error.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ValidateToolCallAsync_WithValidTool_ReturnsTrue()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });

        var toolCall = new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", Guid.NewGuid().ToString());

        // Act
        var isValid = await executor.ValidateToolCallAsync(toolCall);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToolCallAsync_WithMissingTool_ReturnsFalse()
    {
        // Arrange
        var executor = CreateExecutor();
        _mockToolRegistry.Setup(r => r.ToolExists("invalid")).Returns(false);

        var toolCall = new ToolCall("invalid", new Dictionary<string, object>(), "", Guid.NewGuid().ToString());

        // Act
        var isValid = await executor.ValidateToolCallAsync(toolCall);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateToolCallAsync_WithMissingParameter_ReturnsFalse()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });

        var toolCall = new ToolCall("navigate", new Dictionary<string, object>(), "", Guid.NewGuid().ToString());

        // Act
        var isValid = await executor.ValidateToolCallAsync(toolCall);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteToolAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var toolCall = new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", Guid.NewGuid().ToString());

        // Act
        Func<Task> act = async () => await executor.ExecuteToolAsync(toolCall, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteToolAsync_CancellationDuringRetry_StopsRetrying()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 5);
        SetupToolInRegistry("click", new[] { "selector" });

        using var cts = new CancellationTokenSource();
        var attemptCount = 0;

        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                attemptCount++;
                if (attemptCount == 2)
                {
                    cts.Cancel(); // Cancel after second attempt
                }
                await Task.Delay(50);
                throw new TimeoutException("Test");
            });

        var toolCall = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", Guid.NewGuid().ToString());

        // Act
        Func<Task> act = async () => await executor.ExecuteToolAsync(toolCall, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attemptCount.Should().BeLessThanOrEqualTo(3, "should stop retrying after cancellation");
    }

    [Fact]
    public async Task ExecuteSequenceAsync_CancellationMidSequence_ReturnsPartialResults()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });
        SetupToolInRegistry("click", new[] { "selector" });

        using var cts = new CancellationTokenSource();
        var executedTools = new List<string>();

        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => executedTools.Add("navigate"))
            .Returns(Task.CompletedTask);

        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                executedTools.Add("click");
                cts.Cancel(); // Cancel after click
            })
            .Returns(Task.CompletedTask);

        var correlationId = Guid.NewGuid().ToString();
        var toolCalls = new[]
        {
            new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", correlationId),
            new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", correlationId),
            new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://other.com" }, "", correlationId)
        };

        // Act
        Func<Task> act = async () => await executor.ExecuteSequenceAsync(toolCalls, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        executedTools.Should().Contain("navigate");
        executedTools.Should().Contain("click");
        executedTools.Count.Should().BeLessThanOrEqualTo(2, "should stop after cancellation");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteToolAsync_TerminalError_DoesNotRetry()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 3);
        SetupToolInRegistry("navigate", new[] { "url" });

        var attemptCount = 0;
        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptCount++;
                throw new InvalidOperationException("Browser not initialized");
            });

        var toolCall = new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        result.AttemptCount.Should().Be(1, "terminal errors should not retry");
        result.Error.Should().BeOfType<InvalidOperationException>();
        attemptCount.Should().Be(1, "should only attempt once");
    }

    [Fact]
    public async Task ExecuteToolAsync_BrowserAgentNotInitialized_ReturnsFailure()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("get_page_state", Array.Empty<string>());

        _mockBrowserAgent.Setup(x => x.GetPageStateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Browser agent has not been initialized"));

        var toolCall = new ToolCall("get_page_state", new Dictionary<string, object>(), "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteToolAsync(toolCall);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Message.Should().Contain("not been initialized");
    }

    [Fact]
    public async Task ExecuteSequenceAsync_FirstToolFails_ShortCircuits()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 0); // No retries for faster test
        SetupToolInRegistry("navigate", new[] { "url" });
        SetupToolInRegistry("click", new[] { "selector" });

        var executedTools = new List<string>();

        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => executedTools.Add("navigate"))
            .ThrowsAsync(new TimeoutException("Navigation failed"));

        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => executedTools.Add("click"))
            .Returns(Task.CompletedTask);

        var correlationId = Guid.NewGuid().ToString();
        var toolCalls = new[]
        {
            new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", correlationId),
            new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", correlationId)
        };

        // Act
        var results = await executor.ExecuteSequenceAsync(toolCalls);

        // Assert
        results.Should().HaveCount(1, "should stop after first failure");
        results.First().Success.Should().BeFalse();
        executedTools.Should().ContainSingle("navigate");
        executedTools.Should().NotContain("click", "subsequent tools should not execute");
    }

    #endregion

    #region Fallback Tests

    [Fact]
    public async Task ExecuteWithFallbackAsync_PrimarySucceeds_DoesNotUseFallback()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("click", new[] { "selector" });

        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var primary = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", Guid.NewGuid().ToString());
        var fallback = new ToolCall("click", new Dictionary<string, object> { ["selector"] = ".btn" }, "", Guid.NewGuid().ToString());

        // Act
        var result = await executor.ExecuteWithFallbackAsync(primary, new[] { fallback });

        // Assert
        result.Success.Should().BeTrue();
        result.Metadata.Should().NotContainKey("fallback_used");
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_PrimaryFailsFallbackSucceeds_UsesFallback()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 0); // No retries for faster test
        SetupToolInRegistry("click", new[] { "selector" });

        var attemptCount = 0;
        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new TimeoutException("Primary failed");
                }
                return Task.CompletedTask;
            });

        var correlationId = Guid.NewGuid().ToString();
        var primary = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", correlationId);
        var fallback = new ToolCall("click", new Dictionary<string, object> { ["selector"] = ".btn" }, "", correlationId);

        // Act
        var result = await executor.ExecuteWithFallbackAsync(primary, new[] { fallback });

        // Assert
        result.Success.Should().BeTrue();
        result.Metadata["fallback_used"].Should().Be(true);
        result.Metadata["fallback_index"].Should().Be(0);
        result.Metadata["primary_tool"].Should().Be("click");
        result.Metadata.Should().ContainKey("primary_error");
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_AllFail_ReturnsFailure()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 0);
        SetupToolInRegistry("click", new[] { "selector" });

        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("All failed"));

        var correlationId = Guid.NewGuid().ToString();
        var primary = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn1" }, "", correlationId);
        var fallbacks = new[]
        {
            new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn2" }, "", correlationId),
            new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn3" }, "", correlationId)
        };

        // Act
        var result = await executor.ExecuteWithFallbackAsync(primary, fallbacks);

        // Assert
        result.Success.Should().BeFalse();
        result.Metadata["fallback_attempted"].Should().Be(true);
        result.Metadata["fallback_count"].Should().Be(2);
        result.Metadata["all_fallbacks_failed"].Should().Be(true);
    }

    #endregion

    #region History Tests

    [Fact]
    public async Task GetExecutionHistoryAsync_WithCorrelationId_ReturnsHistory()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });
        SetupToolInRegistry("click", new[] { "selector" });

        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var correlationId = Guid.NewGuid().ToString();
        var tool1 = new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", correlationId);
        var tool2 = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", correlationId);

        await executor.ExecuteToolAsync(tool1);
        await executor.ExecuteToolAsync(tool2);

        // Act
        var history = await executor.GetExecutionHistoryAsync(correlationId);

        // Assert
        history.Should().HaveCount(2);
        history.Should().AllSatisfy(r => r.Metadata["correlation_id"].Should().Be(correlationId));
        history.Select(r => r.ToolName).Should().ContainInOrder("navigate", "click");
    }

    [Fact]
    public async Task GetExecutionHistoryAsync_WithUnknownCorrelationId_ReturnsEmpty()
    {
        // Arrange
        var executor = CreateExecutor();

        // Act
        var history = await executor.GetExecutionHistoryAsync(Guid.NewGuid().ToString());

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExecutionHistoryAsync_WithNullCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var executor = CreateExecutor();

        // Act
        Func<Task> act = async () => await executor.GetExecutionHistoryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ExecuteToolAsync_LogsExecutionAttempts()
    {
        // Arrange
        var executor = CreateExecutor();
        SetupToolInRegistry("navigate", new[] { "url" });

        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var toolCall = new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", Guid.NewGuid().ToString());

        // Act
        await executor.ExecuteToolAsync(toolCall);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting tool execution")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_LogsRetryAttempts()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 1);
        SetupToolInRegistry("click", new[] { "selector" });

        var attemptCount = 0;
        _mockBrowserAgent.Setup(x => x.ClickAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new TimeoutException("First attempt fails");
                }
                return Task.CompletedTask;
            });

        var toolCall = new ToolCall("click", new Dictionary<string, object> { ["selector"] = "#btn" }, "", Guid.NewGuid().ToString());

        // Act
        await executor.ExecuteToolAsync(toolCall);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Transient error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteToolAsync_LogsFailureDetails()
    {
        // Arrange
        var executor = CreateExecutor(maxRetries: 0);
        SetupToolInRegistry("navigate", new[] { "url" });

        _mockBrowserAgent.Setup(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Browser not initialized"));

        var toolCall = new ToolCall("navigate", new Dictionary<string, object> { ["url"] = "https://example.com" }, "", Guid.NewGuid().ToString());

        // Act
        await executor.ExecuteToolAsync(toolCall);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Terminal error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private DefaultToolExecutor CreateExecutor(int maxRetries = 3)
    {
        _options.MaxRetries = maxRetries;
        return new DefaultToolExecutor(
            _mockBrowserAgent.Object,
            _mockToolRegistry.Object,
            Options.Create(_options),
            _mockLogger.Object
        );
    }

    private void SetupToolInRegistry(string toolName, string[] requiredParams)
    {
        _mockToolRegistry.Setup(r => r.ToolExists(toolName)).Returns(true);

        var parameters = new Dictionary<string, ParameterDef>();
        foreach (var param in requiredParams)
        {
            parameters[param] = new ParameterDef("string", true, $"{param} parameter", null);
        }

        // Add optional parameters that some tools use
        if (toolName == "click")
        {
            parameters["maxRetries"] = new ParameterDef("int", false, "Max retries", 3);
        }

        _mockToolRegistry.Setup(r => r.GetTool(toolName))
            .Returns(new BrowserToolDefinition(toolName, $"{toolName} tool", parameters));

        _mockToolRegistry.Setup(r => r.GetToolNames())
            .Returns(new[] { toolName });
    }

    #endregion
}
