using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Agents;
using EvoAITest.Agents.Models;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EvoAITest.Tests.Agents;

/// <summary>
/// Unit tests for HealerAgent.
/// Tests error analysis, LLM diagnostics, and healing strategy application.
/// </summary>
public sealed class HealerAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IBrowserAgent> _mockBrowserAgent;
    private readonly Mock<ILogger<HealerAgent>> _mockLogger;
    private readonly HealerAgent _sut;

    public HealerAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockBrowserAgent = new Mock<IBrowserAgent>();
        _mockLogger = new Mock<ILogger<HealerAgent>>();

        // Setup default mock behaviors
        SetupDefaultMocks();

        _sut = new HealerAgent(
            _mockLLMProvider.Object,
            _mockBrowserAgent.Object,
            _mockLogger.Object);
    }

    private void SetupDefaultMocks()
    {
        // Setup LLM provider
        _mockLLMProvider.Setup(p => p.GetModelName())
            .Returns("gpt-5");

        _mockLLMProvider.Setup(p => p.GetLastTokenUsage())
            .Returns(new TokenUsage(100, 200, 0.005m));

        // Setup browser agent with default page state
        _mockBrowserAgent.Setup(a => a.GetPageStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageState
            {
                Url = "https://example.com",
                Title = "Test Page",
                InteractiveElements = new List<ElementInfo>
                {
                    new()
                    {
                        TagName = "button",
                        Selector = ".btn-submit",
                        Text = "Submit",
                        IsVisible = true,
                        IsInteractable = true
                    }
                }
            });
    }

    [Fact]
    public async Task HealStepAsync_WithElementNotFoundError_ShouldSuggestAlternativeSelector()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-1",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Click,
                Target = ElementLocator.Css("#old-selector")
            },
            Reasoning = "Click the submit button",
            ExpectedOutcome = "Button clicked successfully"
        };

        var error = new Exception("Element not found: #old-selector");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-1"
        };

        var llmResponse = new LLMResponse
        {
            Id = "response-1",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "AlternativeLocator",
                              "strategy_name": "Use class selector instead",
                              "description": "The CSS selector '#old-selector' failed. Try using the class '.btn-submit' which is visible on the page.",
                              "confidence": 0.85,
                              "priority": 9,
                              "changes": {
                                "locator_type": "css",
                                "locator_value": ".btn-submit",
                                "reasoning": "Class selector is more resilient to ID changes"
                              }
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.HealedStep.Should().NotBeNull();
        result.Strategy.Should().NotBeNull();
        result.Strategy!.Type.Should().Be(HealingStrategyType.AlternativeLocator);
        result.Strategy.Name.Should().Be("Use class selector instead");
        result.Confidence.Should().Be(0.85);
        
        // Verify the healed step has the new selector
        result.HealedStep!.Action.Should().NotBeNull();
        result.HealedStep.Action!.Target.Should().NotBeNull();
        result.HealedStep.Action.Target!.Selector.Should().Be(".btn-submit");
        
        // Verify metadata tracking
        result.HealedStep.Metadata.Should().ContainKey("healing_applied");
        result.HealedStep.Metadata["healing_applied"].Should().Be(true);
        result.HealedStep.Metadata.Should().ContainKey("healing_strategy");
        result.HealedStep.Metadata.Should().ContainKey("original_step_id");
        result.HealedStep.Metadata["original_step_id"].Should().Be(failedStep.Id);
    }

    [Fact]
    public async Task HealStepAsync_WithTimeoutError_ShouldSuggestExtendedWait()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-timeout",
            StepNumber = 2,
            Action = new BrowserAction
            {
                Type = ActionType.WaitForElement,
                Target = ElementLocator.Css("#dynamic-content")
            },
            TimeoutMs = 5000,
            Reasoning = "Wait for dynamic content to load"
        };

        var error = new TimeoutException("Timeout waiting for element: #dynamic-content");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-2"
        };

        var llmResponse = new LLMResponse
        {
            Id = "response-timeout",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "ExtendedWait",
                              "strategy_name": "Double timeout duration",
                              "description": "The element timed out after 5000ms. Dynamic content may need more time to load. Increase timeout to 10000ms.",
                              "confidence": 0.75,
                              "priority": 8,
                              "changes": {
                                "timeout_multiplier": 2.0
                              }
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Strategy!.Type.Should().Be(HealingStrategyType.ExtendedWait);
        result.HealedStep!.TimeoutMs.Should().Be(10000); // Doubled from 5000
        result.Explanation.Should().Contain("timeout");
    }

    [Fact]
    public async Task HealStepAsync_WithElementNotInteractableError_ShouldSuggestScrollOrJavaScriptClick()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-not-interactable",
            StepNumber = 3,
            Action = new BrowserAction
            {
                Type = ActionType.Click,
                Target = ElementLocator.Css("#hidden-button")
            }
        };

        var error = new Exception("Element is not interactable: element is not visible");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-scroll",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "ScrollToElement",
                              "strategy_name": "Scroll element into view",
                              "description": "Element exists but is not visible in viewport. Scroll to make it visible before clicking.",
                              "confidence": 0.9,
                              "priority": 9,
                              "changes": {}
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Strategy!.Type.Should().Be(HealingStrategyType.ScrollToElement);
        result.HealedStep!.Action!.Options.Should().ContainKey("scroll_before_action");
        result.HealedStep.Action.Options["scroll_before_action"].Should().Be(true);
    }

    [Fact]
    public async Task HealStepAsync_AfterMaxAttempts_ShouldReturnFailure()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-max-attempts",
            StepNumber = 1,
            Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#btn") }
        };

        var error = new Exception("Element not found");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // First healing attempt
        await _sut.HealStepAsync(failedStep, error, context);
        
        // Second healing attempt
        await _sut.HealStepAsync(failedStep, error, context);
        
        // Third healing attempt
        await _sut.HealStepAsync(failedStep, error, context);

        // Act - Fourth attempt should fail
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Explanation.Should().Contain("Maximum healing attempts");
        result.Explanation.Should().Contain("3");
    }

    [Fact]
    public async Task HealStepAsync_IncludesPageStateInDiagnostic()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-with-state",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Click,
                Target = ElementLocator.Css("#missing-button")
            }
        };

        var error = new Exception("Element not found: #missing-button");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var pageState = new PageState
        {
            Url = "https://example.com/form",
            Title = "Contact Form",
            InteractiveElements = new List<ElementInfo>
            {
                new()
                {
                    TagName = "button",
                    Selector = ".submit-btn",
                    Text = "Submit Form",
                    IsVisible = true,
                    IsInteractable = true
                },
                new()
                {
                    TagName = "button",
                    Selector = "#send-button",
                    Text = "Send",
                    IsVisible = true,
                    IsInteractable = true
                }
            }
        };

        _mockBrowserAgent.Setup(a => a.GetPageStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageState);

        var llmResponse = new LLMResponse
        {
            Id = "response-state",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "AlternativeLocator",
                              "strategy_name": "Use visible submit button",
                              "description": "The selector '#missing-button' was not found. However, '.submit-btn' with text 'Submit Form' is visible and likely the intended target.",
                              "confidence": 0.8,
                              "priority": 8,
                              "changes": {
                                "locator_type": "css",
                                "locator_value": ".submit-btn"
                              }
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(
            It.Is<LLMRequest>(r => r.Messages.Any(m => m.Content.Contains("Contact Form"))),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeTrue();
        
        // Verify LLM was called with page state in prompt
        _mockLLMProvider.Verify(
            p => p.CompleteAsync(
                It.Is<LLMRequest>(r =>
                    r.Messages.Any(m =>
                        m.Content.Contains("Current Page State") &&
                        m.Content.Contains("Contact Form") &&
                        m.Content.Contains(".submit-btn"))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HealStepAsync_WithXPathSuggestion_ShouldUseXPathLocator()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-xpath",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Click,
                Target = ElementLocator.Css("#submit")
            }
        };

        var error = new Exception("Element not found");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-xpath",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "AlternativeLocator",
                              "strategy_name": "Use XPath selector",
                              "description": "CSS selector failed. Try XPath which can handle complex DOM structures better.",
                              "confidence": 0.75,
                              "priority": 7,
                              "changes": {
                                "locator_type": "xpath",
                                "locator_value": "//button[@type='submit']"
                              }
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeTrue();
        result.HealedStep!.Action!.Target!.Strategy.Should().Be(LocatorStrategy.XPath);
        result.HealedStep.Action.Target.Selector.Should().Be("//button[@type='submit']");
    }

    [Fact]
    public async Task HealStepAsync_WithTextLocatorSuggestion_ShouldUseTextLocator()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-text",
            StepNumber = 1,
            Action = new BrowserAction
            {
                Type = ActionType.Click,
                Target = ElementLocator.Css(".login-btn")
            }
        };

        var error = new Exception("Element not found");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-text",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "AlternativeLocator",
                              "strategy_name": "Find by text content",
                              "description": "Class selector may have changed. Use text content 'Login' which is more stable.",
                              "confidence": 0.82,
                              "priority": 8,
                              "changes": {
                                "locator_type": "text",
                                "locator_value": "Login"
                              }
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeTrue();
        result.HealedStep!.Action!.Target!.Strategy.Should().Be(LocatorStrategy.Text);
        result.HealedStep.Action.Target.Selector.Should().Be("Login");
    }

    [Fact]
    public async Task HealStepAsync_WithRetryWithDelaySuggestion_ShouldConfigureRetry()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-retry",
            StepNumber = 1,
            Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#btn") },
            RetryConfig = null
        };

        var error = new Exception("Temporary failure");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-retry",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "RetryWithDelay",
                              "strategy_name": "Add retry with 2 second delay",
                              "description": "Add retry logic with delay to handle transient failures.",
                              "confidence": 0.7,
                              "priority": 6,
                              "changes": {
                                "retry_delay_ms": 2000
                              }
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeTrue();
        result.HealedStep!.RetryConfig.Should().NotBeNull();
        result.HealedStep.RetryConfig!.RetryDelayMs.Should().Be(2000);
        result.HealedStep.RetryConfig.MaxRetries.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task AnalyzeErrorAsync_WithElementNotFoundError_ShouldReturnHealableAnalysis()
    {
        // Arrange
        var error = new Exception("Element not found: #username");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var analysis = await _sut.AnalyzeErrorAsync(error, context);

        // Assert
        analysis.Should().NotBeNull();
        analysis.Type.Should().Be(ErrorType.ElementNotFound);
        analysis.IsHealable.Should().BeTrue();
        analysis.RootCause.Should().Contain("Element could not be found");
        analysis.Severity.Should().Be(ErrorSeverity.Medium);
        analysis.SuggestedStrategies.Should().NotBeEmpty();
        analysis.SuggestedStrategies.Should().Contain(s => s.Type == HealingStrategyType.AlternativeLocator);
    }

    [Fact]
    public async Task AnalyzeErrorAsync_WithTimeoutError_ShouldSuggestExtendedWait()
    {
        // Arrange
        var error = new TimeoutException("Timeout waiting for element after 5000ms");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var analysis = await _sut.AnalyzeErrorAsync(error, context);

        // Assert
        analysis.Type.Should().Be(ErrorType.Timeout);
        analysis.IsHealable.Should().BeTrue();
        analysis.Severity.Should().Be(ErrorSeverity.Medium);
        analysis.SuggestedStrategies.Should().Contain(s => s.Type == HealingStrategyType.ExtendedWait);
    }

    [Fact]
    public async Task AnalyzeErrorAsync_WithElementNotInteractableError_ShouldSuggestScrollOrJavaScript()
    {
        // Arrange
        var error = new Exception("Element is not interactable: element not visible");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var analysis = await _sut.AnalyzeErrorAsync(error, context);

        // Assert
        analysis.Type.Should().Be(ErrorType.ElementNotInteractable);
        analysis.IsHealable.Should().BeTrue();
        analysis.Severity.Should().Be(ErrorSeverity.Low);
        analysis.SuggestedStrategies.Should().Contain(s => s.Type == HealingStrategyType.ScrollToElement);
        analysis.SuggestedStrategies.Should().Contain(s => s.Type == HealingStrategyType.InteractionMethodChange);
    }

    [Fact]
    public async Task AnalyzeErrorAsync_WithNetworkError_ShouldReturnNotHealable()
    {
        // Arrange
        var error = new Exception("Network connection failed: ERR_CONNECTION_REFUSED");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var analysis = await _sut.AnalyzeErrorAsync(error, context);

        // Assert
        analysis.Type.Should().Be(ErrorType.NetworkError);
        analysis.IsHealable.Should().BeFalse();
        analysis.Severity.Should().Be(ErrorSeverity.High);
    }

    [Fact]
    public async Task AnalyzeErrorAsync_WithAuthenticationError_ShouldReturnCriticalNotHealable()
    {
        // Arrange
        var error = new Exception("Authentication required: 401 Unauthorized");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var analysis = await _sut.AnalyzeErrorAsync(error, context);

        // Assert
        analysis.Type.Should().Be(ErrorType.AuthenticationRequired);
        analysis.IsHealable.Should().BeFalse();
        analysis.Severity.Should().Be(ErrorSeverity.Critical);
    }

    [Fact]
    public async Task SuggestAlternativesAsync_WithMultipleFailedAttempts_ShouldReturnPrioritizedStrategies()
    {
        // Arrange
        var failedAttempts = new List<AgentStepResult>
        {
            new()
            {
                StepId = "step-1",
                Success = false,
                Error = new Exception("Element not found: #btn1")
            },
            new()
            {
                StepId = "step-2",
                Success = false,
                Error = new TimeoutException("Timeout waiting for element")
            },
            new()
            {
                StepId = "step-3",
                Success = false,
                Error = new Exception("Element not interactable")
            }
        };

        // Act
        var strategies = await _sut.SuggestAlternativesAsync(failedAttempts);

        // Assert
        strategies.Should().NotBeEmpty();
        
        // Should include strategies for all error types
        strategies.Should().Contain(s => s.Type == HealingStrategyType.AlternativeLocator);
        strategies.Should().Contain(s => s.Type == HealingStrategyType.ExtendedWait);
        strategies.Should().Contain(s => s.Type == HealingStrategyType.ScrollToElement);
        
        // Should be sorted by priority (descending)
        strategies.Should().BeInDescendingOrder(s => s.Priority);
    }

    [Fact]
    public async Task SuggestAlternativesAsync_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var failedAttempts = new List<AgentStepResult>();

        // Act
        var strategies = await _sut.SuggestAlternativesAsync(failedAttempts);

        // Assert
        strategies.Should().BeEmpty();
    }

    [Fact]
    public async Task HealStepAsync_WithInvalidLLMJson_ShouldReturnFailure()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-invalid-json",
            StepNumber = 1,
            Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#btn") }
        };

        var error = new Exception("Element not found");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-invalid",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = "This is not valid JSON { invalid"
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Explanation.Should().Contain("parse");
    }

    [Fact]
    public async Task HealStepAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-cancel",
            StepNumber = 1,
            Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#btn") }
        };

        var error = new Exception("Element not found");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var act = async () => await _sut.HealStepAsync(failedStep, error, context, cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task HealStepAsync_IncludesPreviousStepsInContext()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-with-history",
            StepNumber = 3,
            Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#btn") }
        };

        var error = new Exception("Element not found");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            PreviousSteps = new List<AgentStepResult>
            {
                new() { StepId = "step-1", Success = true },
                new() { StepId = "step-2", Success = true }
            }
        };

        var llmResponse = new LLMResponse
        {
            Id = "response-history",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "AlternativeLocator",
                              "strategy_name": "Try alternative",
                              "description": "Use different selector",
                              "confidence": 0.7,
                              "priority": 7,
                              "changes": {
                                "locator_type": "css",
                                "locator_value": ".new-btn"
                              }
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(
            It.Is<LLMRequest>(r => r.Messages.Any(m => m.Content.Contains("Previous Steps"))),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeTrue();
        
        // Verify LLM prompt included previous steps
        _mockLLMProvider.Verify(
            p => p.CompleteAsync(
                It.Is<LLMRequest>(r =>
                    r.Messages.Any(m =>
                        m.Content.Contains("Previous Steps") &&
                        m.Content.Contains("step-1") &&
                        m.Content.Contains("step-2"))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullLLMProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new HealerAgent(null!, _mockBrowserAgent.Object, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("llmProvider");
    }

    [Fact]
    public void Constructor_WithNullBrowserAgent_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new HealerAgent(_mockLLMProvider.Object, null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("browserAgent");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new HealerAgent(_mockLLMProvider.Object, _mockBrowserAgent.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task HealStepAsync_WithNullStep_ThrowsArgumentNullException()
    {
        // Arrange
        var error = new Exception("Test error");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var act = async () => await _sut.HealStepAsync(null!, error, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HealStepAsync_WithNullError_ThrowsArgumentNullException()
    {
        // Arrange
        var step = new AgentStep();
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var act = async () => await _sut.HealStepAsync(step, null!, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HealStepAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var step = new AgentStep();
        var error = new Exception("Test error");

        // Act
        var act = async () => await _sut.HealStepAsync(step, error, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HealStepAsync_TracksTokenUsageInMetadata()
    {
        // Arrange
        var failedStep = new AgentStep
        {
            Id = "step-tokens",
            StepNumber = 1,
            Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#btn") }
        };

        var error = new Exception("Element not found");
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-tokens",
            Model = "gpt-5",
            Choices = new List<Choice>
            {
                new()
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = MessageRole.Assistant,
                        Content = """
                            {
                              "strategy_type": "SimpleFallback",
                              "strategy_name": "Simple approach",
                              "description": "Use fallback",
                              "confidence": 0.5,
                              "priority": 5,
                              "changes": {}
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        _mockLLMProvider.Setup(p => p.GetLastTokenUsage())
            .Returns(new TokenUsage(250, 180, 0.0086m));

        // Act
        var result = await _sut.HealStepAsync(failedStep, error, context);

        // Assert
        result.Success.Should().BeTrue();
        
        // Verify token usage was tracked (logged or used internally)
        _mockLLMProvider.Verify(p => p.GetLastTokenUsage(), Times.AtLeastOnce);
    }
}
