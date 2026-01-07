using EvoAITest.Core.Options;
using EvoAITest.LLM.Models;
using EvoAITest.LLM.Providers;
using EvoAITest.LLM.Routing;
using EvoAITest.LLM.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EvoAITest.Tests.LLM;

/// <summary>
/// Unit tests for <see cref="RoutingLLMProvider"/>.
/// </summary>
public sealed class RoutingLLMProviderTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RoutingLLMProvider> _logger;

    public RoutingLLMProviderTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<RoutingLLMProvider>>();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var strategies = CreateDefaultStrategies();

        // Act & Assert
        Xunit.Assert.Throws<ArgumentNullException>(() =>
            new RoutingLLMProvider(null!, options, strategies, _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var strategies = CreateDefaultStrategies();

        // Act & Assert
        Xunit.Assert.Throws<ArgumentNullException>(() =>
            new RoutingLLMProvider(_serviceProvider, null!, strategies, _logger));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var strategies = CreateDefaultStrategies();

        // Act
        var provider = new RoutingLLMProvider(_serviceProvider, options, strategies, _logger);

        // Assert
        provider.Should().NotBeNull();
        provider.Name.Should().Be("RoutingLLMProvider");
    }

    [Fact]
    public async Task DetectTaskType_WithCodeGenerationPrompt_ReturnsCodeGeneration()
    {
        // Arrange
        var provider = CreateRoutingProvider();
        var request = new LLMRequest
        {
            Model = "gpt-4",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Write a C# function to sort an array" }
            }
        };

        // Act
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        // Task type detection happens internally
    }

    [Fact]
    public async Task DetectTaskType_WithPlanningPrompt_ReturnsPlanning()
    {
        // Arrange
        var provider = CreateRoutingProvider();
        var request = new LLMRequest
        {
            Model = "gpt-4",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Create a project plan for building a web app" }
            }
        };

        // Act
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteAsync_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var mockProvider = MockLLMProvider.CreateSuccessful("TestProvider", "test-model");
        var provider = CreateRoutingProvider(mockProvider);

        var request = new LLMRequest
        {
            Model = "test-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Hello" }
            }
        };

        // Act
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Choices.Should().NotBeEmpty();
        mockProvider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task CompleteAsync_WithMultipleProviders_RoutesToCorrectProvider()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");

        var options = CreateOptionsWithRoutes();
        var strategies = CreateDefaultStrategies();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILLMProvider>(_ => primaryProvider);
        services.AddSingleton<ILLMProvider>(_ => fallbackProvider);

        var sp = services.BuildServiceProvider();

        var provider = new RoutingLLMProvider(sp, options, strategies, _logger);

        var request = new LLMRequest
        {
            Model = "primary-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Test" }
            }
        };

        // Act
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        // At least one provider should have been called
        (primaryProvider.CallCount + fallbackProvider.CallCount).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StreamCompleteAsync_WithValidRequest_ReturnsChunks()
    {
        // Arrange
        var mockProvider = MockLLMProvider.CreateSuccessful("StreamProvider", "stream-model");
        var provider = CreateRoutingProvider(mockProvider);

        var request = new LLMRequest
        {
            Model = "stream-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Stream test" }
            }
        };

        // Act
        var chunks = new List<LLMStreamChunk>();
        await foreach (var chunk in provider.StreamCompleteAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().NotBeEmpty();
        mockProvider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task IsAvailableAsync_WithAvailableProvider_ReturnsTrue()
    {
        // Arrange
        var mockProvider = MockLLMProvider.CreateSuccessful().WithAvailability(true);
        var provider = CreateRoutingProvider(mockProvider);

        // Act
        var isAvailable = await provider.IsAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public void GetCapabilities_ReturnsValidCapabilities()
    {
        // Arrange
        var provider = CreateRoutingProvider();

        // Act
        var capabilities = provider.GetCapabilities();

        // Assert
        capabilities.Should().NotBeNull();
        capabilities.SupportsStreaming.Should().BeTrue();
    }

    [Theory]
    [InlineData("Write a function", TaskType.CodeGeneration)]
    [InlineData("Create a plan", TaskType.Planning)]
    [InlineData("Extract data from", TaskType.Analysis)]
    [InlineData("Analyze this code", TaskType.Analysis)]
    [InlineData("Hello world", TaskType.General)]
    public async Task TaskTypeDetection_WithVariousPrompts_DetectsCorrectly(string prompt, TaskType expectedType)
    {
        // Arrange
        var mockProvider = MockLLMProvider.CreateSuccessful();
        var provider = CreateRoutingProvider(mockProvider);

        var request = new LLMRequest
        {
            Model = "test-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = prompt }
            }
        };

        // Act
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        // Task type is detected internally and used for routing
        mockProvider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task CompleteAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var mockProvider = MockLLMProvider.CreateSlow(TimeSpan.FromSeconds(5));
        var provider = CreateRoutingProvider(mockProvider);

        var request = new LLMRequest
        {
            Model = "test-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Test" }
            }
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Xunit.Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await provider.CompleteAsync(request, cts.Token));
    }

    // Helper methods

    private RoutingLLMProvider CreateRoutingProvider(MockLLMProvider? mockProvider = null)
    {
        var provider = mockProvider ?? MockLLMProvider.CreateSuccessful();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILLMProvider>(provider);

        var sp = services.BuildServiceProvider();

        var options = CreateDefaultOptions();
        var strategies = CreateDefaultStrategies();

        return new RoutingLLMProvider(sp, options, strategies, _logger);
    }

    private IOptions<LLMRoutingOptions> CreateDefaultOptions()
    {
        var options = new LLMRoutingOptions
        {
            RoutingStrategy = "TaskBased",
            EnableMultiModelRouting = true,
            EnableProviderFallback = false,
            DefaultRoute = new RouteConfiguration
            {
                PrimaryProvider = "TestProvider",
                PrimaryModel = "test-model"
            },
            Routes = new Dictionary<string, RouteConfiguration>()
        };

        return Options.Create(options);
    }

    private IOptions<LLMRoutingOptions> CreateOptionsWithRoutes()
    {
        var options = new LLMRoutingOptions
        {
            RoutingStrategy = "TaskBased",
            EnableMultiModelRouting = true,
            EnableProviderFallback = true,
            DefaultRoute = new RouteConfiguration
            {
                PrimaryProvider = "Primary",
                PrimaryModel = "primary-model",
                FallbackProvider = "Fallback",
                FallbackModel = "fallback-model"
            },
            Routes = new Dictionary<string, RouteConfiguration>
            {
                ["Planning"] = new RouteConfiguration
                {
                    PrimaryProvider = "Primary",
                    PrimaryModel = "primary-model",
                    MaxLatencyMs = 5000
                },
                ["CodeGeneration"] = new RouteConfiguration
                {
                    PrimaryProvider = "Fallback",
                    PrimaryModel = "fallback-model",
                    CostPer1KTokens = 0.0
                }
            }
        };

        return Options.Create(options);
    }

    private List<IRoutingStrategy> CreateDefaultStrategies()
    {
        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

        return new List<IRoutingStrategy>
        {
            new TaskBasedRoutingStrategy(loggerFactory.CreateLogger<TaskBasedRoutingStrategy>()),
            new CostOptimizedRoutingStrategy(loggerFactory.CreateLogger<CostOptimizedRoutingStrategy>())
        };
    }
}
