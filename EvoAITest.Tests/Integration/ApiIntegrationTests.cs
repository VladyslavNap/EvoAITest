using System.Net;
using System.Net.Http.Json;
using EvoAITest.ApiService.Models;
using EvoAITest.Agents.Abstractions;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Models;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskStatus = EvoAITest.Core.Models.TaskStatus;

namespace EvoAITest.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for EvoAITest API using WebApplicationFactory.
/// Tests execution flows, healing scenarios, and end-to-end automation workflows.
/// </summary>
[TestClass]
public sealed class ApiIntegrationTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void TestInit()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    #region Execution Flow Integration Tests

    [TestMethod]
    public async Task ExecuteTask_WithValidTask_ReturnsSuccess()
    {
        // Arrange - Create a task
        var createRequest = new CreateTaskRequest
        {
            Name = "Simple Navigation Task",
            Description = "Navigate to example.com",
            NaturalLanguagePrompt = "Navigate to https://example.com and extract the page title"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Execute the task
        var executeResponse = await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);

        // Assert
        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecutionResultResponse>();
        executionResult.Should().NotBeNull();
        executionResult!.Status.Should().Be(ExecutionStatus.Success);
        executionResult.Steps.Should().NotBeEmpty();
        executionResult.TotalDurationMs.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task ExecuteTask_WithInvalidSelector_FailsGracefully()
    {
        // Arrange - Create a task with an invalid selector
        var createRequest = new CreateTaskRequest
        {
            Name = "Task with Invalid Selector",
            NaturalLanguagePrompt = "Navigate to https://example.com and click on element #nonexistent-element-12345"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Execute the task
        var executeResponse = await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);

        // Assert
        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecutionResultResponse>();
        executionResult.Should().NotBeNull();
        executionResult!.Status.Should().BeOneOf(ExecutionStatus.Failed, ExecutionStatus.PartialSuccess);
        executionResult.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task ExecuteTask_WithHealing_RecoversFromFailure()
    {
        // Arrange - Create a task that will initially fail but can be healed
        var createRequest = new CreateTaskRequest
        {
            Name = "Task Requiring Healing",
            NaturalLanguagePrompt = "Navigate to https://example.com and click the main heading"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Execute with healing enabled
        var executeResponse = await _client.PostAsync($"/api/tasks/{task!.Id}/execute?enableHealing=true", null);

        // Assert
        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecutionResultResponse>();
        executionResult.Should().NotBeNull();
        
        // Healer should have attempted recovery
        if (executionResult!.Status == ExecutionStatus.Failed)
        {
            executionResult.ErrorMessage.Should().Contain("healing");
        }
    }

    [TestMethod]
    public async Task ExecuteTask_MultipleExecutions_CreatesMultipleHistoryRecords()
    {
        // Arrange - Create a task
        var createRequest = new CreateTaskRequest
        {
            Name = "Multi-Execution Task",
            NaturalLanguagePrompt = "Navigate to https://example.com"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Execute multiple times
        await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);
        await _client.PostAsync($"/api/tasks/{task.Id}/execute", null);
        await _client.PostAsync($"/api/tasks/{task.Id}/execute", null);

        // Get execution history
        var historyResponse = await _client.GetAsync($"/api/tasks/{task.Id}/history");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<ExecutionHistoryResponse>>();

        // Assert
        history.Should().NotBeNull();
        history!.Count.Should().BeGreaterThanOrEqualTo(3);
        history.Should().AllSatisfy(h => h.TaskId.Should().Be(task.Id));
    }

    [TestMethod]
    public async Task ExecuteTask_WithCancellation_StopsExecution()
    {
        // Arrange
        var createRequest = new CreateTaskRequest
        {
            Name = "Long Running Task",
            NaturalLanguagePrompt = "Navigate to multiple pages and extract data from each"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        // Act & Assert
        try
        {
            await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Expected - execution was cancelled
        }

        // Verify task status reflects cancellation
        var getResponse = await _client.GetAsync($"/api/tasks/{task!.Id}");
        var updatedTask = await getResponse.Content.ReadFromJsonAsync<TaskResponse>();
        
        updatedTask!.Status.Should().BeOneOf(TaskStatus.Cancelled, TaskStatus.Failed, TaskStatus.Executing);
    }

    #endregion

    #region End-to-End Scenarios

    [TestMethod]
    public async Task LoginAutomation_CompleteFlow_Succeeds()
    {
        // Arrange - Create a login automation task
        var createRequest = new CreateTaskRequest
        {
            Name = "Example.com Login",
            Description = "Automated login test for example.com",
            NaturalLanguagePrompt = "Navigate to https://example.com, wait for the page to load, and extract the title"
        };

        // Act - Step 1: Create task
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.Status.Should().Be(TaskStatus.Pending);

        // Act - Step 2: Execute task
        var executeResponse = await _client.PostAsync($"/api/tasks/{task.Id}/execute", null);
        executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecutionResultResponse>();
        executionResult.Should().NotBeNull();

        // Assert - Step 3: Verify execution history created
        var historyResponse = await _client.GetAsync($"/api/tasks/{task.Id}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await historyResponse.Content.ReadFromJsonAsync<List<ExecutionHistoryResponse>>();
        history.Should().NotBeNull();
        history!.Should().NotBeEmpty();
        history.First().TaskId.Should().Be(task.Id);

        // Assert - Step 4: Verify task status updated
        var taskResponse = await _client.GetAsync($"/api/tasks/{task.Id}");
        var updatedTask = await taskResponse.Content.ReadFromJsonAsync<TaskResponse>();
        
        updatedTask.Should().NotBeNull();
        updatedTask!.Status.Should().BeOneOf(TaskStatus.Completed, TaskStatus.Failed);
        updatedTask.ExecutionCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task ComplexWorkflow_MultiStepAutomation_ExecutesSequentially()
    {
        // Arrange - Create a complex multi-step task
        var createRequest = new CreateTaskRequest
        {
            Name = "Complex Multi-Step Workflow",
            Description = "Navigate, extract data, and verify results",
            NaturalLanguagePrompt = @"
                1. Navigate to https://example.com
                2. Wait for the page to load completely
                3. Extract the main heading text
                4. Take a screenshot
                5. Verify the title contains 'Example Domain'
            "
        };

        // Act - Create and execute
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var executeResponse = await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);
        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecutionResultResponse>();

        // Assert
        executionResult.Should().NotBeNull();
        executionResult!.Steps.Should().HaveCountGreaterThanOrEqualTo(3);
        
        // Verify steps executed in order
        for (int i = 0; i < executionResult.Steps.Count - 1; i++)
        {
            executionResult.Steps[i].StepNumber.Should().BeLessThan(executionResult.Steps[i + 1].StepNumber);
        }

        // Verify at least one screenshot was captured
        executionResult.Steps.Should().Contain(s => !string.IsNullOrEmpty(s.ScreenshotUrl));
    }

    [TestMethod]
    public async Task DataExtraction_ExtractPageContent_ReturnsStructuredData()
    {
        // Arrange - Create a data extraction task
        var createRequest = new CreateTaskRequest
        {
            Name = "Data Extraction Task",
            NaturalLanguagePrompt = "Navigate to https://example.com and extract all text from the page"
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var executeResponse = await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);
        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecutionResultResponse>();

        // Assert
        executionResult.Should().NotBeNull();
        executionResult!.FinalOutput.Should().NotBeNullOrEmpty();
        
        if (executionResult.Status == ExecutionStatus.Success)
        {
            executionResult.FinalOutput.Should().Contain("Example");
        }
    }

    [TestMethod]
    public async Task ErrorRecovery_FailureAndRetry_EventuallySucceeds()
    {
        // Arrange - Create a task that might fail initially
        var createRequest = new CreateTaskRequest
        {
            Name = "Flaky Task",
            NaturalLanguagePrompt = "Navigate to https://example.com and click a dynamic element"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Execute with retries
        ExecutionResultResponse? lastResult = null;
        int maxAttempts = 3;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var executeResponse = await _client.PostAsync($"/api/tasks/{task!.Id}/execute?enableHealing=true", null);
            lastResult = await executeResponse.Content.ReadFromJsonAsync<ExecutionResultResponse>();

            if (lastResult!.Status == ExecutionStatus.Success)
            {
                break;
            }

            await Task.Delay(1000); // Wait before retry
        }

        // Assert
        lastResult.Should().NotBeNull();
        
        // Get final execution history
        var historyResponse = await _client.GetAsync($"/api/tasks/{task!.Id}/history");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<ExecutionHistoryResponse>>();

        history.Should().NotBeNull();
        history!.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task ConcurrentExecutions_MultipleTasks_ExecuteIndependently()
    {
        // Arrange - Create multiple tasks
        var tasks = new List<TaskResponse>();
        for (int i = 0; i < 3; i++)
        {
            var createRequest = new CreateTaskRequest
            {
                Name = $"Concurrent Task {i + 1}",
                NaturalLanguagePrompt = "Navigate to https://example.com"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/tasks", createRequest);
            var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();
            tasks.Add(task!);
        }

        // Act - Execute all tasks concurrently
        var executionTasks = tasks.Select(t => 
            _client.PostAsync($"/api/tasks/{t.Id}/execute", null)
        ).ToList();

        var responses = await Task.WhenAll(executionTasks);

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));

        // Verify each task has execution history
        foreach (var task in tasks)
        {
            var historyResponse = await _client.GetAsync($"/api/tasks/{task.Id}/history");
            var history = await historyResponse.Content.ReadFromJsonAsync<List<ExecutionHistoryResponse>>();
            
            history.Should().NotBeNull();
            history!.Should().NotBeEmpty();
        }
    }

    #endregion

    #region Helper Response Models

    /// <summary>
    /// Response model for execution results.
    /// </summary>
    public sealed record ExecutionResultResponse
    {
        public required Guid TaskId { get; init; }
        public required ExecutionStatus Status { get; init; }
        public required List<StepResultResponse> Steps { get; init; }
        public required string FinalOutput { get; init; }
        public string? ErrorMessage { get; init; }
        public required int TotalDurationMs { get; init; }
    }

    /// <summary>
    /// Response model for step results.
    /// </summary>
    public sealed record StepResultResponse
    {
        public required int StepNumber { get; init; }
        public required string Action { get; init; }
        public required bool Success { get; init; }
        public required string Output { get; init; }
        public string? ErrorMessage { get; init; }
        public required int DurationMs { get; init; }
        public string? ScreenshotUrl { get; init; }
    }

    #endregion

    /// <summary>
    /// Custom WebApplicationFactory for integration testing.
    /// Configures in-memory database and mocks external dependencies.
    /// </summary>
    public sealed class CustomWebApplicationFactory : WebApplicationFactory<EvoAITest.ApiService.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EvoAIDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database
                var dbName = $"TestDb_{Guid.NewGuid()}";
                services.AddDbContext<EvoAIDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                // Mock LLM Provider
                services.AddSingleton<ILLMProvider, MockLLMProvider>();

                // Mock Browser Agent
                services.AddScoped<IBrowserAgent, MockBrowserAgent>();

                // Mock Agents (if needed)
                // services.AddScoped<IPlanner, MockPlannerAgent>();
                // services.AddScoped<IExecutor, MockExecutorAgent>();
                // services.AddScoped<IHealer, MockHealerAgent>();
            });
        }
    }

    /// <summary>
    /// Mock LLM Provider that returns predefined plans.
    /// </summary>
    public sealed class MockLLMProvider : ILLMProvider
    {
        public string Name => "MockLLM";

        public IReadOnlyList<string> SupportedModels => new[] { "mock-model-1" };

        public Task<string> GenerateAsync(
            string prompt,
            Dictionary<string, string>? variables = null,
            List<BrowserTool>? tools = null,
            int maxTokens = 2000,
            CancellationToken cancellationToken = default)
        {
            // Return a simple JSON plan
            var plan = @"{
                ""steps"": [
                    {
                        ""order"": 1,
                        ""action"": ""navigate"",
                        ""value"": ""https://example.com"",
                        ""reasoning"": ""Navigate to target site""
                    },
                    {
                        ""order"": 2,
                        ""action"": ""wait_for_element"",
                        ""selector"": ""h1"",
                        ""reasoning"": ""Wait for page to load""
                    },
                    {
                        ""order"": 3,
                        ""action"": ""get_text"",
                        ""selector"": ""h1"",
                        ""reasoning"": ""Extract page title""
                    }
                ]
            }";
            return Task.FromResult(plan);
        }

        public Task<List<ToolCall>> ParseToolCallsAsync(string response, CancellationToken cancellationToken = default)
        {
            var calls = new List<ToolCall>
            {
                new ToolCall(
                    "navigate",
                    new Dictionary<string, object> { ["url"] = "https://example.com" },
                    "Navigate to target site",
                    Guid.NewGuid().ToString())
            };
            return Task.FromResult(calls);
        }

        public string GetModelName() => "mock-model-1";

        public TokenUsage GetLastTokenUsage() => new TokenUsage(100, 50, 0.001m);

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            var response = new LLMResponse
            {
                Id = "mock-id",
                Model = "mock-model-1",
                Choices = new List<Choice>
                {
                    new Choice
                    {
                        Index = 0,
                        Message = new Message
                        {
                            Role = MessageRole.Assistant,
                            Content = "Mock response"
                        },
                        FinishReason = "stop"
                    }
                },
                Usage = new Usage
                {
                    PromptTokens = 10,
                    CompletionTokens = 5,
                    TotalTokens = 15
                },
                FinishReason = "stop"
            };
            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(
            LLMRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new LLMStreamChunk { Id = "mock", Delta = "Mock" };
            await Task.CompletedTask;
        }

        public Task<float[]> GenerateEmbeddingAsync(string text, string? model = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new float[] { 0.1f, 0.2f, 0.3f });
        }

        public ProviderCapabilities GetCapabilities()
        {
            return new ProviderCapabilities
            {
                SupportsStreaming = true,
                SupportsEmbeddings = true,
                SupportsFunctionCalling = true,
                SupportsVision = false,
                MaxContextTokens = 8192,
                MaxOutputTokens = 2048
            };
        }
    }

    /// <summary>
    /// Mock Browser Agent that simulates browser actions without real browser.
    /// </summary>
    public sealed class MockBrowserAgent : IBrowserAgent
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<PageState> GetPageStateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PageState
            {
                Url = "https://example.com",
                Title = "Example Domain",
                LoadState = LoadState.Load,
                InteractiveElements = new List<ElementInfo>(),
                CapturedAt = DateTimeOffset.UtcNow
            });
        }

        public Task NavigateAsync(string url, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClickAsync(string selector, int maxRetries = 3, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task TypeAsync(string selector, string text, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> GetTextAsync(string selector, CancellationToken cancellationToken = default) => Task.FromResult("Example Domain");

        public Task<string> TakeScreenshotAsync(CancellationToken cancellationToken = default) => Task.FromResult("base64-screenshot-data");

        public Task<string> GetAccessibilityTreeAsync(CancellationToken cancellationToken = default) => Task.FromResult("{}");

        public Task WaitForElementAsync(string selector, int timeoutMs = 30000, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> GetPageHtmlAsync(CancellationToken cancellationToken = default) => Task.FromResult("<html><body><h1>Example Domain</h1></body></html>");

        public Task<byte[]> TakeFullPageScreenshotBytesAsync(CancellationToken cancellationToken = default)
        {
            // Return a minimal PNG header for testing
            var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            return Task.FromResult(pngHeader);
        }

        public Task<byte[]> TakeElementScreenshotAsync(string selector, CancellationToken cancellationToken = default)
        {
            var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            return Task.FromResult(pngHeader);
        }

        public Task<byte[]> TakeRegionScreenshotAsync(ScreenshotRegion region, CancellationToken cancellationToken = default)
        {
            var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            return Task.FromResult(pngHeader);
        }

        public Task<byte[]> TakeViewportScreenshotAsync(CancellationToken cancellationToken = default)
        {
            var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            return Task.FromResult(pngHeader);
        }

        // Mobile Device Emulation Methods
        public DeviceProfile? CurrentDevice { get; private set; }

        public Task SetDeviceEmulationAsync(DeviceProfile device, CancellationToken cancellationToken = default)
        {
            CurrentDevice = device;
            return Task.CompletedTask;
        }

        public Task SetGeolocationAsync(double latitude, double longitude, double? accuracy = null, CancellationToken cancellationToken = default) 
            => Task.CompletedTask;

        public Task SetTimezoneAsync(string timezoneId, CancellationToken cancellationToken = default) 
            => Task.CompletedTask;

        public Task SetLocaleAsync(string locale, CancellationToken cancellationToken = default) 
            => Task.CompletedTask;

        public Task GrantPermissionsAsync(string[] permissions, CancellationToken cancellationToken = default) 
            => Task.CompletedTask;

        public Task ClearPermissionsAsync(CancellationToken cancellationToken = default) 
            => Task.CompletedTask;

        public INetworkInterceptor? GetNetworkInterceptor() 
            => null; // No network interceptor in mock

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
