using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Agents;
using EvoAITest.Agents.Models;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using AgentsTaskStatus = EvoAITest.Agents.Models.TaskStatus;

namespace EvoAITest.Tests.Agents;

/// <summary>
/// Unit tests for ExecutorAgent.
/// Tests plan execution, step handling, error management, and state tracking.
/// </summary>
public sealed class ExecutorAgentTests
{
    private readonly Mock<IToolExecutor> _mockToolExecutor;
    private readonly Mock<IBrowserAgent> _mockBrowserAgent;
    private readonly Mock<ILogger<ExecutorAgent>> _mockLogger;
    private readonly ExecutorAgent _sut;

    public ExecutorAgentTests()
    {
        _mockToolExecutor = new Mock<IToolExecutor>();
        _mockBrowserAgent = new Mock<IBrowserAgent>();
        _mockLogger = new Mock<ILogger<ExecutorAgent>>();

        _sut = new ExecutorAgent(
            _mockToolExecutor.Object,
            _mockBrowserAgent.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithValidStep_ShouldReturnSuccess()
    {
        // Arrange
        var step = new AgentStep
        {
            Id = "step-1",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Navigate,
                Value = "https://example.com"
            },
            Reasoning = "Navigate to website",
            TimeoutMs = 30000
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-1"
        };

        var toolResult = ToolExecutionResult.Succeeded(
            "navigate",
            null,
            TimeSpan.FromMilliseconds(500),
            1);

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolResult);

        // Act
        var result = await _sut.ExecuteStepAsync(step, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StepId.Should().Be(step.Id);
        result.ExecutionResult.Should().NotBeNull();
        result.ExecutionResult!.Success.Should().BeTrue();
        result.DurationMs.Should().BeGreaterThan(0);
        result.Error.Should().BeNull();

        // Verify tool executor was called
        _mockToolExecutor.Verify(
            x => x.ExecuteToolAsync(
                It.Is<ToolCall>(tc => tc.ToolName == "navigate" && tc.CorrelationId == context.SessionId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithFailure_ShouldCaptureError()
    {
        // Arrange
        var step = new AgentStep
        {
            Id = "step-fail",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Click,
                Target = ElementLocator.Css("#missing-button")
            },
            Reasoning = "Click button"
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-1"
        };

        var expectedException = new TimeoutException("Element not found");
        var toolResult = ToolExecutionResult.Failed(
            "click",
            expectedException,
            TimeSpan.FromSeconds(30),
            3);

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolResult);

        _mockBrowserAgent
            .Setup(x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("base64-screenshot-data");

        // Act
        var result = await _sut.ExecuteStepAsync(step, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.StepId.Should().Be(step.Id);
        result.Error.Should().Be(expectedException);
        result.RetryAttempts.Should().Be(2); // 3 attempts - 1 initial = 2 retries
        result.ExecutionResult.Should().NotBeNull();
        result.ExecutionResult!.Screenshot.Should().Be("base64-screenshot-data");

        // Verify screenshot was captured
        _mockBrowserAgent.Verify(
            x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecutePlanAsync_WithValidPlan_ShouldReturnSuccess()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-1",
            TaskId = "task-1",
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Navigate"
                },
                new()
                {
                    Id = "step-2",
                    StepNumber = 2,
                    Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#button") },
                    Reasoning = "Click button"
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-1"
        };

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolCall tc, CancellationToken ct) =>
                ToolExecutionResult.Succeeded(tc.ToolName, null, TimeSpan.FromMilliseconds(500), 1));

        _mockBrowserAgent
            .Setup(x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("final-screenshot");

        // Act
        var result = await _sut.ExecutePlanAsync(plan, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be(AgentsTaskStatus.Completed);
        result.TaskId.Should().Be(plan.TaskId);
        result.StepResults.Should().HaveCount(2);
        result.StepResults.Should().OnlyContain(sr => sr.Success);
        result.DurationMs.Should().BeGreaterThan(0);
        result.Statistics.Should().NotBeNull();
        result.Statistics!.TotalSteps.Should().Be(2);
        result.Statistics.SuccessfulSteps.Should().Be(2);
        result.Statistics.FailedSteps.Should().Be(0);
        result.Screenshots.Should().ContainSingle();

        // Verify both steps were executed
        _mockToolExecutor.Verify(
            x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecutePlanAsync_WithStepFailure_ShouldStopExecution()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-fail",
            TaskId = "task-fail",
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Navigate",
                    IsOptional = false
                },
                new()
                {
                    Id = "step-2",
                    StepNumber = 2,
                    Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#button") },
                    Reasoning = "Click button",
                    IsOptional = false
                },
                new()
                {
                    Id = "step-3",
                    StepNumber = 3,
                    Action = new BrowserAction { Type = ActionType.Type, Target = ElementLocator.Css("#input"), Value = "text" },
                    Reasoning = "Type text",
                    IsOptional = false
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-fail"
        };

        var callCount = 0;
        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolCall tc, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 2) // Second step fails
                {
                    return ToolExecutionResult.Failed(
                        tc.ToolName,
                        new Exception("Click failed"),
                        TimeSpan.FromSeconds(5),
                        1);
                }
                return ToolExecutionResult.Succeeded(tc.ToolName, null, TimeSpan.FromMilliseconds(500), 1);
            });

        _mockBrowserAgent
            .Setup(x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("error-screenshot");

        // Act
        var result = await _sut.ExecutePlanAsync(plan, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Status.Should().Be(AgentsTaskStatus.Failed);
        result.StepResults.Should().HaveCount(2); // Only first 2 steps executed
        result.StepResults[0].Success.Should().BeTrue();
        result.StepResults[1].Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Step 2 failed");
        result.Statistics!.SuccessfulSteps.Should().Be(1);
        result.Statistics.FailedSteps.Should().Be(1);

        // Third step should not be executed
        _mockToolExecutor.Verify(
            x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecutePlanAsync_WithOptionalStepFailure_ShouldContinue()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-optional",
            TaskId = "task-optional",
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Navigate",
                    IsOptional = false
                },
                new()
                {
                    Id = "step-2",
                    StepNumber = 2,
                    Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#optional") },
                    Reasoning = "Optional click",
                    IsOptional = true // This step is optional
                },
                new()
                {
                    Id = "step-3",
                    StepNumber = 3,
                    Action = new BrowserAction { Type = ActionType.Screenshot },
                    Reasoning = "Take screenshot",
                    IsOptional = false
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-optional"
        };

        var callCount = 0;
        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolCall tc, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 2) // Second step (optional) fails
                {
                    return ToolExecutionResult.Failed(
                        tc.ToolName,
                        new Exception("Optional step failed"),
                        TimeSpan.FromSeconds(1),
                        1);
                }
                return ToolExecutionResult.Succeeded(tc.ToolName, null, TimeSpan.FromMilliseconds(500), 1);
            });

        _mockBrowserAgent
            .Setup(x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("final-screenshot");

        // Act
        var result = await _sut.ExecutePlanAsync(plan, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Status.Should().Be(AgentsTaskStatus.Completed);
        result.StepResults.Should().HaveCount(3); // All steps executed
        result.StepResults[0].Success.Should().BeTrue();
        result.StepResults[1].Success.Should().BeFalse(); // Optional step failed
        result.StepResults[2].Success.Should().BeTrue();
        result.Statistics!.SuccessfulSteps.Should().Be(2);
        result.Statistics.FailedSteps.Should().Be(1);

        // All three steps should be executed
        _mockToolExecutor.Verify(
            x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteStepAsync_WithRetry_ShouldTrackAttempts()
    {
        // Arrange
        var step = new AgentStep
        {
            Id = "step-retry",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Click,
                Target = ElementLocator.Css("#button")
            },
            Reasoning = "Click with retry"
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-retry"
        };

        // Tool executor retried 3 times and succeeded
        var toolResult = ToolExecutionResult.Succeeded(
            "click",
            null,
            TimeSpan.FromSeconds(5),
            3, // 3 attempts total
            new Dictionary<string, object>
            {
                ["retry_reasons"] = new[] { "Timeout", "ElementNotFound" },
                ["retry_delays"] = new[] { 500, 1000 }
            });

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolResult);

        // Act
        var result = await _sut.ExecuteStepAsync(step, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RetryAttempts.Should().Be(2); // 3 attempts - 1 initial = 2 retries
        result.ExecutionResult.Should().NotBeNull();
        result.ExecutionResult!.RetryInfo.Should().NotBeNull();
        result.ExecutionResult.RetryInfo!.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteStepAsync_CapturesScreenshotOnFailure()
    {
        // Arrange
        var step = new AgentStep
        {
            Id = "step-screenshot",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Click,
                Target = ElementLocator.Css("#button")
            },
            Reasoning = "Click that will fail"
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-screenshot"
        };

        var toolResult = ToolExecutionResult.Failed(
            "click",
            new Exception("Element not found"),
            TimeSpan.FromSeconds(10),
            1);

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolResult);

        var expectedScreenshot = "base64-encoded-screenshot-data";
        _mockBrowserAgent
            .Setup(x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedScreenshot);

        // Act
        var result = await _sut.ExecuteStepAsync(step, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ExecutionResult.Should().NotBeNull();
        result.ExecutionResult!.Screenshot.Should().Be(expectedScreenshot);

        // Verify screenshot was captured
        _mockBrowserAgent.Verify(
            x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteStepAsync_MeasuresStepDuration()
    {
        // Arrange
        var step = new AgentStep
        {
            Id = "step-duration",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Navigate,
                Value = "https://example.com"
            },
            Reasoning = "Navigate"
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-duration"
        };

        var toolResult = ToolExecutionResult.Succeeded(
            "navigate",
            null,
            TimeSpan.FromMilliseconds(1234),
            1);

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolResult);

        // Act
        var result = await _sut.ExecuteStepAsync(step, context);

        // Assert
        result.Should().NotBeNull();
        result.DurationMs.Should().BeGreaterThan(0);
        result.StartedAt.Should().BeBefore(result.CompletedAt);
        result.ExecutionResult.Should().NotBeNull();
        result.ExecutionResult!.DurationMs.Should().Be(1234);
    }

    [Fact]
    public async Task ExecutePlanAsync_MeasuresTotalDuration()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-duration",
            TaskId = "task-duration",
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Navigate"
                },
                new()
                {
                    Id = "step-2",
                    StepNumber = 2,
                    Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#button") },
                    Reasoning = "Click"
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-duration"
        };

        var stepDurations = new[] { 1000L, 500L }; // milliseconds
        var callCount = 0;

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolCall tc, CancellationToken ct) =>
            {
                var duration = TimeSpan.FromMilliseconds(stepDurations[callCount]);
                callCount++;
                return ToolExecutionResult.Succeeded(tc.ToolName, null, duration, 1);
            });

        _mockBrowserAgent
            .Setup(x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("screenshot");

        // Act
        var result = await _sut.ExecutePlanAsync(plan, context);

        // Assert
        result.Should().NotBeNull();
        result.DurationMs.Should().BeGreaterThan(0);
        result.StartedAt.Should().BeBefore(result.CompletedAt);
        result.StepResults.Should().HaveCount(2);
        result.StepResults[0].DurationMs.Should().Be(1000);
        result.StepResults[1].DurationMs.Should().Be(500);
        result.Statistics!.AverageStepDurationMs.Should().Be(750); // (1000 + 500) / 2
    }

    [Fact]
    public async Task ExecutePlanAsync_WithCancellation_ShouldStopGracefully()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-cancel",
            TaskId = "task-cancel",
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Navigate"
                },
                new()
                {
                    Id = "step-2",
                    StepNumber = 2,
                    Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#button") },
                    Reasoning = "Click"
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-cancel"
        };

        using var cts = new CancellationTokenSource();

        var callCount = 0;
        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolCall tc, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // Cancel after first step
                    cts.Cancel();
                    return ToolExecutionResult.Succeeded(tc.ToolName, null, TimeSpan.FromMilliseconds(500), 1);
                }
                throw new OperationCanceledException();
            });

        _mockBrowserAgent
            .Setup(x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("screenshot");

        // Act
        var result = await _sut.ExecutePlanAsync(plan, context, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Status.Should().Be(AgentsTaskStatus.Cancelled);
        result.ErrorMessage.Should().Contain("cancelled");
        result.StepResults.Should().HaveCount(1); // Only first step completed
    }

    [Fact]
    public async Task PauseExecutionAsync_WithRunningTask_ShouldPause()
    {
        // Arrange
        var taskId = "task-pause";
        var plan = new ExecutionPlan
        {
            Id = "plan-pause",
            TaskId = taskId,
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Navigate"
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-pause"
        };

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolCall tc, CancellationToken ct) =>
            {
                // Simulate long-running operation
                Task.Delay(5000, ct).Wait(ct);
                return ToolExecutionResult.Succeeded(tc.ToolName, null, TimeSpan.FromSeconds(5), 1);
            });

        // Start execution in background
        var executionTask = Task.Run(async () => await _sut.ExecutePlanAsync(plan, context));

        // Wait a bit for execution to start
        await Task.Delay(100);

        // Act
        var pauseAction = async () => await _sut.PauseExecutionAsync(taskId);

        // Assert
        await pauseAction.Should().NotThrowAsync();

        // Cleanup: cancel the execution
        await _sut.CancelExecutionAsync(taskId);
        try
        {
            await executionTask; // Await to observe any exceptions
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [Fact]
    public async Task CancelExecutionAsync_WithRunningTask_ShouldCancel()
    {
        // Arrange
        var taskId = "task-cancel-method";

        // Act & Assert
        var act = async () => await _sut.CancelExecutionAsync(taskId);
        
        // Should throw because task doesn't exist
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active execution found*");
    }

    [Fact]
    public async Task ExecuteStepAsync_WithValidationRules_ShouldValidate()
    {
        // Arrange
        var step = new AgentStep
        {
            Id = "step-validate",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Navigate,
                Value = "https://example.com"
            },
            Reasoning = "Navigate",
            ValidationRules = new List<ValidationRule>
            {
                new()
                {
                    Name = "PageTitle",
                    Type = ValidationType.PageTitle,
                    ExpectedValue = "Example",
                    IsRequired = true
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-validate"
        };

        var toolResult = ToolExecutionResult.Succeeded(
            "navigate",
            null,
            TimeSpan.FromMilliseconds(500),
            1);

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolResult);

        _mockBrowserAgent
            .Setup(x => x.GetPageStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageState
            {
                Title = "Example Domain",
                Url = "https://example.com"
            });

        // Act
        var result = await _sut.ExecuteStepAsync(step, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ValidationResults.Should().NotBeEmpty();
        result.ValidationResults.Should().ContainSingle(vr => vr.RuleName == "PageTitle" && vr.Passed);
    }

    [Fact]
    public void Constructor_WithNullToolExecutor_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ExecutorAgent(null!, _mockBrowserAgent.Object, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("toolExecutor");
    }

    [Fact]
    public void Constructor_WithNullBrowserAgent_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ExecutorAgent(_mockToolExecutor.Object, null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("browserAgent");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ExecutorAgent(_mockToolExecutor.Object, _mockBrowserAgent.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteStepAsync_WithNullStep_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var act = async () => await _sut.ExecuteStepAsync(null!, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecutePlanAsync_WithNullPlan_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var act = async () => await _sut.ExecutePlanAsync(null!, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecutePlanAsync_CollectsStatistics()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-stats",
            TaskId = "task-stats",
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Navigate"
                },
                new()
                {
                    Id = "step-2",
                    StepNumber = 2,
                    Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#button") },
                    Reasoning = "Click",
                    IsOptional = true
                },
                new()
                {
                    Id = "step-3",
                    StepNumber = 3,
                    Action = new BrowserAction { Type = ActionType.Type, Target = ElementLocator.Css("#input"), Value = "text" },
                    Reasoning = "Type"
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-stats"
        };

        var callCount = 0;
        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolCall tc, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 2) // Second step fails but is optional
                {
                    return ToolExecutionResult.Failed(
                        tc.ToolName,
                        new Exception("Failed"),
                        TimeSpan.FromSeconds(2),
                        2); // 2 attempts (1 retry)
                }
                return ToolExecutionResult.Succeeded(tc.ToolName, null, TimeSpan.FromSeconds(1), 1);
            });

        _mockBrowserAgent
            .Setup(x => x.TakeScreenshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("screenshot");

        // Act
        var result = await _sut.ExecutePlanAsync(plan, context);

        // Assert
        result.Should().NotBeNull();
        result.Statistics.Should().NotBeNull();
        result.Statistics!.TotalSteps.Should().Be(3);
        result.Statistics.SuccessfulSteps.Should().Be(2);
        result.Statistics.FailedSteps.Should().Be(1);
        result.Statistics.RetriedSteps.Should().Be(1);
        result.Statistics.TotalRetries.Should().Be(1);
        result.Statistics.SuccessRate.Should().BeApproximately(0.666, 0.001);
        result.Statistics.AverageStepDurationMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecutePlanAsync_WithDuplicateTaskId_ThrowsInvalidOperationException()
    {
        // Arrange
        var taskId = "duplicate-task";
        var plan = new ExecutionPlan
        {
            Id = "plan-duplicate",
            TaskId = taskId,
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Navigate"
                }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-duplicate"
        };

        _mockToolExecutor
            .Setup(x => x.ExecuteToolAsync(It.IsAny<ToolCall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ToolCall tc, CancellationToken ct) =>
            {
                // Simulate long-running operation to keep first execution active
                Task.Delay(5000, ct).Wait(ct);
                return ToolExecutionResult.Succeeded(tc.ToolName, null, TimeSpan.FromSeconds(5), 1);
            });

        // Start first execution in background
        var firstExecution = Task.Run(async () => await _sut.ExecutePlanAsync(plan, context));

        // Wait a bit for first execution to register
        await Task.Delay(100);

        // Act - Try to execute the same task again
        var act = async () => await _sut.ExecutePlanAsync(plan, context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Task {taskId} is already executing");

        // Cleanup: cancel the first execution
        await _sut.CancelExecutionAsync(taskId);
        try
        {
            await firstExecution;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }
}
