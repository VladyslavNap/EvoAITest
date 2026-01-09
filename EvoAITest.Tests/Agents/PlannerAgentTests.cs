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
using AgentTaskType = EvoAITest.Agents.Models.TaskType;
using LLMTaskType = EvoAITest.LLM.Models.TaskType;

namespace EvoAITest.Tests.Agents;

/// <summary>
/// Unit tests for PlannerAgent.
/// Tests the planning logic, LLM integration, and error handling.
/// </summary>
public sealed class PlannerAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<IBrowserToolRegistry> _mockToolRegistry;
    private readonly Mock<ILogger<PlannerAgent>> _mockLogger;
    private readonly PlannerAgent _sut;

    public PlannerAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockToolRegistry = new Mock<IBrowserToolRegistry>();
        _mockLogger = new Mock<ILogger<PlannerAgent>>();

        // Setup default mock behaviors
        SetupDefaultMocks();

        _sut = new PlannerAgent(
            _mockLLMProvider.Object,
            _mockToolRegistry.Object,
            _mockLogger.Object);
    }

    private void SetupDefaultMocks()
    {
        // Setup tool registry
        _mockToolRegistry.Setup(r => r.GetToolNames())
            .Returns(new[] { "navigate", "click", "type", "wait_for_element", "extract_text", "take_screenshot" });

        _mockToolRegistry.Setup(r => r.GetAllTools())
            .Returns(new List<BrowserToolDefinition>
            {
                new(
                    "navigate",
                    "Navigate to a URL",
                    new Dictionary<string, ParameterDef>
                    {
                        ["url"] = new ParameterDef("string", true, "URL to navigate to", null)
                    }),
                new(
                    "click",
                    "Click on an element",
                    new Dictionary<string, ParameterDef>
                    {
                        ["selector"] = new ParameterDef("string", true, "CSS selector", null)
                    }),
                new(
                    "type",
                    "Type text into input",
                    new Dictionary<string, ParameterDef>
                    {
                        ["selector"] = new ParameterDef("string", true, "CSS selector", null),
                        ["text"] = new ParameterDef("string", true, "Text to type", null)
                    }),
                new(
                    "wait_for_element",
                    "Wait for element to appear",
                    new Dictionary<string, ParameterDef>
                    {
                        ["selector"] = new ParameterDef("string", true, "CSS selector", null)
                    })
            });

        _mockToolRegistry.Setup(r => r.ToolExists(It.IsAny<string>()))
            .Returns(true);

        // Setup LLM provider
        _mockLLMProvider.Setup(p => p.GetModelName())
            .Returns("gpt-5");

        _mockLLMProvider.Setup(p => p.GetLastTokenUsage())
            .Returns(new TokenUsage(100, 200, 0.005m));
    }

    [Fact]
    public async Task PlanAsync_WithValidPrompt_ShouldReturnExecutionPlan()
    {
        // Arrange
        var task = new AgentTask
        {
            Id = "task-1",
            Description = "Login to example.com with username test@example.com and password SecurePass123",
            StartUrl = "https://example.com/login",
            Type = AgentTaskType.Authentication
        };

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
                              "steps": [
                                {
                                  "order": 1,
                                  "action": "navigate",
                                  "selector": "",
                                  "value": "https://example.com/login",
                                  "reasoning": "Navigate to the login page",
                                  "expected_result": "Login page loads successfully"
                                },
                                {
                                  "order": 2,
                                  "action": "wait_for_element",
                                  "selector": "#username",
                                  "value": "",
                                  "reasoning": "Wait for username field to be visible",
                                  "expected_result": "Username field is ready for input"
                                },
                                {
                                  "order": 3,
                                  "action": "type",
                                  "selector": "#username",
                                  "value": "test@example.com",
                                  "reasoning": "Enter the username",
                                  "expected_result": "Username is filled in the field"
                                }
                              ]
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var plan = await _sut.CreatePlanAsync(task, context);

        // Assert
        plan.Should().NotBeNull();
        plan.TaskId.Should().Be(task.Id);
        plan.Steps.Should().HaveCount(3);
        plan.Steps.Should().OnlyContain(s => s.Action != null);
        
        // Verify sequential order
        plan.Steps[0].StepNumber.Should().Be(1);
        plan.Steps[1].StepNumber.Should().Be(2);
        plan.Steps[2].StepNumber.Should().Be(3);
        
        // Verify action types
        plan.Steps[0].Action!.Type.Should().Be(ActionType.Navigate);
        plan.Steps[1].Action!.Type.Should().Be(ActionType.WaitForElement);
        plan.Steps[2].Action!.Type.Should().Be(ActionType.Type);
        
        // Verify all actions are valid tool names
        plan.Steps.Should().AllSatisfy(step =>
        {
            step.Action.Should().NotBeNull();
            step.Reasoning.Should().NotBeNullOrEmpty();
            step.ExpectedOutcome.Should().NotBeNullOrEmpty();
        });
        
        plan.EstimatedDurationMs.Should().BeGreaterThan(0);
        plan.Confidence.Should().BeInRange(0.0, 1.0);
        plan.Metadata.Should().ContainKey("correlation_id");
        plan.Metadata.Should().ContainKey("llm_model");
        plan.Metadata.Should().ContainKey("step_count");
        plan.Metadata["step_count"].Should().Be(3);

        // Verify LLM was called correctly
        _mockLLMProvider.Verify(
            p => p.CompleteAsync(
                It.Is<LLMRequest>(r =>
                    r.Messages.Count == 2 &&
                    r.Messages[0].Role == MessageRole.System &&
                    r.Messages[1].Role == MessageRole.User &&
                    r.ResponseFormat != null &&
                    r.ResponseFormat.Type == "json_object"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanAsync_WithComplexPrompt_ShouldHandleMultipleSteps()
    {
        // Arrange
        var task = new AgentTask
        {
            Id = "task-complex",
            Description = "Complete a multi-step form with name, email, phone, address and submit",
            StartUrl = "https://example.com/form",
            Type = AgentTaskType.FormSubmission,
            Parameters = new Dictionary<string, object>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
                ["phone"] = "555-1234",
                ["address"] = "123 Main St"
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext
        {
            SessionId = "session-complex"
        };

        var llmResponse = new LLMResponse
        {
            Id = "response-complex",
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
                              "steps": [
                                {
                                  "order": 1,
                                  "action": "navigate",
                                  "selector": "",
                                  "value": "https://example.com/form",
                                  "reasoning": "Navigate to the form page",
                                  "expected_result": "Form page loads with all input fields"
                                },
                                {
                                  "order": 2,
                                  "action": "wait_for_element",
                                  "selector": "#name",
                                  "value": "",
                                  "reasoning": "Wait for form to be fully loaded",
                                  "expected_result": "Name field is visible"
                                },
                                {
                                  "order": 3,
                                  "action": "type",
                                  "selector": "#name",
                                  "value": "John Doe",
                                  "reasoning": "Enter name in the form",
                                  "expected_result": "Name field contains John Doe"
                                },
                                {
                                  "order": 4,
                                  "action": "type",
                                  "selector": "#email",
                                  "value": "john@example.com",
                                  "reasoning": "Enter email address",
                                  "expected_result": "Email field contains john@example.com"
                                },
                                {
                                  "order": 5,
                                  "action": "type",
                                  "selector": "#phone",
                                  "value": "555-1234",
                                  "reasoning": "Enter phone number",
                                  "expected_result": "Phone field contains 555-1234"
                                },
                                {
                                  "order": 6,
                                  "action": "type",
                                  "selector": "#address",
                                  "value": "123 Main St",
                                  "reasoning": "Enter address",
                                  "expected_result": "Address field contains 123 Main St"
                                },
                                {
                                  "order": 7,
                                  "action": "click",
                                  "selector": "button[type='submit']",
                                  "value": "",
                                  "reasoning": "Submit the completed form",
                                  "expected_result": "Form is submitted and success message appears"
                                }
                              ]
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var plan = await _sut.CreatePlanAsync(task, context);

        // Assert
        plan.Should().NotBeNull();
        plan.Steps.Should().HaveCount(7);
        
        // Verify correct sequence
        for (int i = 0; i < plan.Steps.Count; i++)
        {
            plan.Steps[i].StepNumber.Should().Be(i + 1, $"step at index {i} should have StepNumber {i + 1}");
        }
        
        // Verify all steps have reasoning populated
        plan.Steps.Should().AllSatisfy(step =>
        {
            step.Reasoning.Should().NotBeNullOrEmpty();
            step.ExpectedOutcome.Should().NotBeNullOrEmpty();
        });
        
        // Verify specific step types
        plan.Steps[0].Action!.Type.Should().Be(ActionType.Navigate);
        plan.Steps[1].Action!.Type.Should().Be(ActionType.WaitForElement);
        plan.Steps[2].Action!.Type.Should().Be(ActionType.Type);
        plan.Steps[3].Action!.Type.Should().Be(ActionType.Type);
        plan.Steps[4].Action!.Type.Should().Be(ActionType.Type);
        plan.Steps[5].Action!.Type.Should().Be(ActionType.Type);
        plan.Steps[6].Action!.Type.Should().Be(ActionType.Click);
        
        // Verify estimated duration scales with step count
        plan.EstimatedDurationMs.Should().BeGreaterThan(5000); // Multiple steps should take more time
        
        // Verify confidence is reasonable for complex plans
        plan.Confidence.Should().BeInRange(0.5, 1.0);
    }

    [Fact]
    public async Task PlanAsync_WithLLMFailure_ShouldThrowException()
    {
        // Arrange
        var task = new AgentTask
        {
            Description = "Test task"
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var expectedException = new InvalidOperationException("LLM service unavailable");
        
        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = async () => await _sut.CreatePlanAsync(task, context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to create execution plan*");

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create plan")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanAsync_WithInvalidJSON_ShouldThrowException()
    {
        // Arrange
        var task = new AgentTask
        {
            Description = "Test task with invalid JSON response"
        };

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
                        Content = "This is not valid JSON { invalid syntax"
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var act = async () => await _sut.CreatePlanAsync(task, context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*parse*");

        // Verify JSON parsing error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("parse")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PlanAsync_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        var task = new AgentTask
        {
            Description = "Test task that will be cancelled"
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        using (var cts = new CancellationTokenSource())
        {
            cts.Cancel(); // Cancel immediately

            _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException());

            // Act
            var act = async () => await _sut.CreatePlanAsync(task, context, cts.Token);

            // Assert
            await act.Should().ThrowAsync<TaskCanceledException>();

            // Verify cancellation was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cancelled")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task PlanAsync_WithEmptySteps_ShouldThrowException()
    {
        // Arrange
        var task = new AgentTask
        {
            Description = "Test task that returns no steps"
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-empty",
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
                              "steps": []
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var act = async () => await _sut.CreatePlanAsync(task, context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no steps*");
    }

    [Fact]
    public async Task RefinePlanAsync_WithFailedSteps_GeneratesRefinedPlan()
    {
        // Arrange
        var originalPlan = new ExecutionPlan
        {
            Id = "plan-1",
            TaskId = "task-1",
            Steps = new List<AgentStep>
            {
                new()
                {
                    Id = "step-1",
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#old-selector") },
                    Reasoning = "Click button"
                }
            }
        };

        var executionResults = new List<AgentStepResult>
        {
            new()
            {
                StepId = "step-1",
                Success = false,
                Error = new Exception("Element not found: #old-selector")
            }
        };

        var llmResponse = new LLMResponse
        {
            Id = "response-refined",
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
                              "steps": [
                                {
                                  "order": 1,
                                  "action": "wait_for_element",
                                  "selector": "#new-selector",
                                  "value": "",
                                  "reasoning": "Wait for element with alternative selector",
                                  "expected_result": "Element is visible"
                                },
                                {
                                  "order": 2,
                                  "action": "click",
                                  "selector": "#new-selector",
                                  "value": "",
                                  "reasoning": "Click button with corrected selector",
                                  "expected_result": "Button clicked successfully"
                                }
                              ]
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var refinedPlan = await _sut.RefinePlanAsync(originalPlan, executionResults);

        // Assert
        refinedPlan.Should().NotBeNull();
        refinedPlan.Id.Should().NotBe(originalPlan.Id);
        refinedPlan.TaskId.Should().Be(originalPlan.TaskId);
        refinedPlan.Steps.Should().HaveCount(2);
        refinedPlan.Metadata.Should().ContainKey("original_plan_id");
        refinedPlan.Metadata["original_plan_id"].Should().Be(originalPlan.Id);
        refinedPlan.Metadata.Should().ContainKey("refinement_reason");
    }

    [Fact]
    public async Task ValidatePlanAsync_WithValidPlan_ReturnsValid()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-valid",
            Steps = new List<AgentStep>
            {
                new()
                {
                    StepNumber = 1,
                    Action = new BrowserAction
                    {
                        Type = ActionType.Navigate,
                        Value = "https://example.com"
                    },
                    Reasoning = "Navigate to website",
                    ExpectedOutcome = "Page loads"
                },
                new()
                {
                    StepNumber = 2,
                    Action = new BrowserAction
                    {
                        Type = ActionType.Click,
                        Target = ElementLocator.Css("#button")
                    },
                    Reasoning = "Click button",
                    ExpectedOutcome = "Action completes"
                }
            },
            Confidence = 0.85
        };

        // Act
        var validation = await _sut.ValidatePlanAsync(plan);

        // Assert
        validation.Should().NotBeNull();
        validation.IsValid.Should().BeTrue();
        validation.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidatePlanAsync_WithEmptyPlan_ReturnsInvalid()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-empty",
            Steps = new List<AgentStep>()
        };

        // Act
        var validation = await _sut.ValidatePlanAsync(plan);

        // Assert
        validation.Should().NotBeNull();
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.Contains("no steps"));
    }

    [Fact]
    public async Task ValidatePlanAsync_WithMissingAction_ReturnsInvalid()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-missing-action",
            Steps = new List<AgentStep>
            {
                new()
                {
                    StepNumber = 1,
                    Action = null, // Missing action
                    Reasoning = "Test"
                }
            }
        };

        // Act
        var validation = await _sut.ValidatePlanAsync(plan);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.Contains("no action"));
    }

    [Fact]
    public async Task ValidatePlanAsync_WithNavigateWithoutURL_ReturnsInvalid()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-no-url",
            Steps = new List<AgentStep>
            {
                new()
                {
                    StepNumber = 1,
                    Action = new BrowserAction
                    {
                        Type = ActionType.Navigate,
                        Value = null // Missing URL
                    }
                }
            }
        };

        // Act
        var validation = await _sut.ValidatePlanAsync(plan);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.Contains("Navigate") && e.Contains("URL"));
    }

    [Fact]
    public async Task ValidatePlanAsync_WithClickWithoutTarget_ReturnsInvalid()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-no-target",
            Steps = new List<AgentStep>
            {
                new()
                {
                    StepNumber = 1,
                    Action = new BrowserAction
                    {
                        Type = ActionType.Click,
                        Target = null // Missing target
                    }
                }
            }
        };

        // Act
        var validation = await _sut.ValidatePlanAsync(plan);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.Contains("Click") && e.Contains("target"));
    }

    [Fact]
    public async Task ValidatePlanAsync_WithLowConfidence_AddsSuggestion()
    {
        // Arrange
        var plan = new ExecutionPlan
        {
            Id = "plan-low-confidence",
            Steps = new List<AgentStep>
            {
                new()
                {
                    StepNumber = 1,
                    Action = new BrowserAction { Type = ActionType.Navigate, Value = "https://example.com" },
                    Reasoning = "Test",
                    ExpectedOutcome = "Done"
                }
            },
            Confidence = 0.5 // Low confidence
        };

        // Act
        var validation = await _sut.ValidatePlanAsync(plan);

        // Assert
        validation.Suggestions.Should().Contain(s => s.Contains("confidence"));
    }

    [Fact]
    public async Task ValidatePlanAsync_WithManySteps_AddsWarning()
    {
        // Arrange
        var steps = Enumerable.Range(1, 101).Select(i => new AgentStep
        {
            StepNumber = i,
            Action = new BrowserAction { Type = ActionType.Click, Target = ElementLocator.Css("#btn") },
            Reasoning = $"Step {i}",
            ExpectedOutcome = "Done"
        }).ToList();

        var plan = new ExecutionPlan
        {
            Id = "plan-many-steps",
            Steps = steps
        };

        // Act
        var validation = await _sut.ValidatePlanAsync(plan);

        // Assert
        validation.Warnings.Should().Contain(w => w.Contains("101 steps"));
    }

    [Fact]
    public void Constructor_WithNullLLMProvider_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PlannerAgent(null!, _mockToolRegistry.Object, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("llmProvider");
    }

    [Fact]
    public void Constructor_WithNullToolRegistry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PlannerAgent(_mockLLMProvider.Object, null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("toolRegistry");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new PlannerAgent(_mockLLMProvider.Object, _mockToolRegistry.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CreatePlanAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        // Act
        var act = async () => await _sut.CreatePlanAsync(null!, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreatePlanAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var task = new AgentTask { Description = "Test" };

        // Act
        var act = async () => await _sut.CreatePlanAsync(task, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreatePlanAsync_WithTaskExpectations_IncludesExpectationsInPrompt()
    {
        // Arrange
        var task = new AgentTask
        {
            Id = "task-expectations",
            Description = "Navigate and verify",
            StartUrl = "https://example.com",
            Expectations = new TaskExpectations
            {
                ExpectedUrl = "*/success",
                ExpectedElements = new List<string> { "success-message", "user-profile" }
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-exp",
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
                              "steps": [
                                {
                                  "order": 1,
                                  "action": "navigate",
                                  "selector": "",
                                  "value": "https://example.com",
                                  "reasoning": "Navigate",
                                  "expected_result": "Page loads"
                                }
                              ]
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var plan = await _sut.CreatePlanAsync(task, context);

        // Assert
        plan.Should().NotBeNull();
        
        // Verify the prompt included expectations
        _mockLLMProvider.Verify(
            p => p.CompleteAsync(
                It.Is<LLMRequest>(r =>
                    r.Messages.Any(m => m.Content.Contains("Expected Outcomes"))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePlanAsync_WithTaskParameters_IncludesParametersInPrompt()
    {
        // Arrange
        var task = new AgentTask
        {
            Id = "task-params",
            Description = "Form submission",
            Parameters = new Dictionary<string, object>
            {
                ["username"] = "testuser",
                ["email"] = "test@example.com"
            }
        };

        var context = new EvoAITest.Agents.Abstractions.ExecutionContext();

        var llmResponse = new LLMResponse
        {
            Id = "response-params",
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
                              "steps": [
                                {
                                  "order": 1,
                                  "action": "type",
                                  "selector": "#username",
                                  "value": "testuser",
                                  "reasoning": "Enter username",
                                  "expected_result": "Username entered"
                                }
                              ]
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var plan = await _sut.CreatePlanAsync(task, context);

        // Assert
        plan.Should().NotBeNull();
        
        // Verify the prompt included parameters
        _mockLLMProvider.Verify(
            p => p.CompleteAsync(
                It.Is<LLMRequest>(r =>
                    r.Messages.Any(m => m.Content.Contains("Task Parameters"))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePlanAsync_TracksTokenUsageInMetadata()
    {
        // Arrange
        var task = new AgentTask
        {
            Description = "Simple task"
        };

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
                              "steps": [
                                {
                                  "order": 1,
                                  "action": "navigate",
                                  "selector": "",
                                  "value": "https://example.com",
                                  "reasoning": "Navigate",
                                  "expected_result": "Page loads"
                                }
                              ]
                            }
                            """
                    }
                }
            }
        };

        _mockLLMProvider.Setup(p => p.CompleteAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        _mockLLMProvider.Setup(p => p.GetLastTokenUsage())
            .Returns(new TokenUsage(150, 300, 0.0075m));

        // Act
        var plan = await _sut.CreatePlanAsync(task, context);

        // Assert
        plan.Metadata.Should().ContainKey("llm_tokens_input");
        plan.Metadata["llm_tokens_input"].Should().Be(150);
        plan.Metadata.Should().ContainKey("llm_tokens_output");
        plan.Metadata["llm_tokens_output"].Should().Be(300);
        plan.Metadata.Should().ContainKey("llm_cost_usd");
        plan.Metadata["llm_cost_usd"].Should().Be(0.0075m);
    }
}
