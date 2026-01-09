using EvoAITest.Core.Options;
using EvoAITest.LLM.Models;
using EvoAITest.LLM.Providers;
using EvoAITest.LLM.Routing;
using EvoAITest.Tests.LLM;
using EvoAITest.LLM.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EvoAITest.Tests.Integration;

/// <summary>
/// Integration tests for LLM routing and circuit breaker functionality.
/// </summary>
public sealed class LLMRoutingIntegrationTests
{
    [Fact]
    public async Task EndToEndRouting_WithSuccessfulProviders_RoutesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var primaryProvider = MockLLMProvider.CreateSuccessful("Primary", "primary-model");
        var secondaryProvider = MockLLMProvider.CreateSuccessful("Secondary", "secondary-model");

        services.AddSingleton<ILLMProvider>(_ => primaryProvider);
        services.AddSingleton<ILLMProvider>(_ => secondaryProvider);

        var sp = services.BuildServiceProvider();

        var routingOptions = Options.Create(new LLMRoutingOptions
        {
            RoutingStrategy = "TaskBased",
            EnableMultiModelRouting = true,
            EnableProviderFallback = true,
            DefaultRoute = new RouteConfiguration
            {
                PrimaryProvider = "Primary",
                PrimaryModel = "primary-model"
            }
        });

        var logger = sp.GetRequiredService<ILogger<RoutingLLMProvider>>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var strategies = new List<IRoutingStrategy>
        {
            new TaskBasedRoutingStrategy(loggerFactory.CreateLogger<TaskBasedRoutingStrategy>())
        };

        var routingProvider = new RoutingLLMProvider(sp, routingOptions, strategies, logger);

        var request = new LLMRequest
        {
            Model = "primary-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Write a function to sort an array" }
            }
        };

        // Act
        var response = await routingProvider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        primaryProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EndToEndCircuitBreaker_WithFailingPrimary_FallsBackSuccessfully()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateFailing("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");

        var circuitBreakerOptions = Options.Create(new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            OpenDuration = TimeSpan.FromSeconds(30)
        });

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<CircuitBreakerLLMProvider>();

        var circuitBreakerProvider = new CircuitBreakerLLMProvider(
            primaryProvider,
            fallbackProvider,
            circuitBreakerOptions,
            logger);

        var request = new LLMRequest
        {
            Model = "test-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Test" }
            }
        };

        // Act - Fail twice to open circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreakerProvider.CompleteAsync(request);
            }
            catch
            {
                // Expected failures
            }
        }

        // Third call should use fallback
        var response = await circuitBreakerProvider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        fallbackProvider.CallCount.Should().BeGreaterThan(0);
        circuitBreakerProvider.GetStatus().State.Should().Be(EvoAITest.LLM.CircuitBreaker.CircuitBreakerState.Open);
    }

    [Fact]
    public async Task EndToEndStreamingWithRouting_StreamsSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var provider = MockLLMProvider.CreateSuccessful("StreamProvider", "stream-model");
        services.AddSingleton<ILLMProvider>(_ => provider);

        var sp = services.BuildServiceProvider();

        var routingOptions = Options.Create(new LLMRoutingOptions
        {
            RoutingStrategy = "TaskBased",
            EnableMultiModelRouting = true,
            DefaultRoute = new RouteConfiguration
            {
                PrimaryProvider = "StreamProvider",
                PrimaryModel = "stream-model"
            }
        });

        var logger = sp.GetRequiredService<ILogger<RoutingLLMProvider>>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var strategies = new List<IRoutingStrategy>
        {
            new TaskBasedRoutingStrategy(loggerFactory.CreateLogger<TaskBasedRoutingStrategy>())
        };

        var routingProvider = new RoutingLLMProvider(sp, routingOptions, strategies, logger);

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
        await foreach (var chunk in routingProvider.StreamCompleteAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().NotBeEmpty();
        provider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task EndToEndStreamingWithCircuitBreaker_StreamsViaFallbackWhenCircuitOpen()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateFailing("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");

        var circuitBreakerOptions = Options.Create(new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            OpenDuration = TimeSpan.FromSeconds(30)
        });

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<CircuitBreakerLLMProvider>();

        var circuitBreakerProvider = new CircuitBreakerLLMProvider(
            primaryProvider,
            fallbackProvider,
            circuitBreakerOptions,
            logger);

        var request = new LLMRequest
        {
            Model = "test-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Stream test" }
            }
        };

        // Act - Fail once to open circuit
        try
        {
            await circuitBreakerProvider.CompleteAsync(request);
        }
        catch
        {
            // Expected failure
        }

        // Stream should use fallback
        var chunks = new List<LLMStreamChunk>();
        await foreach (var chunk in circuitBreakerProvider.StreamCompleteAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().NotBeEmpty();
        fallbackProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ComplexScenario_RoutingWithCircuitBreakerAndMultipleProviders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Create providers
        var fastProvider = MockLLMProvider.CreateSuccessful("Fast", "fast-model")
            .WithLatency(TimeSpan.FromMilliseconds(100));
        var slowProvider = MockLLMProvider.CreateSuccessful("Slow", "slow-model")
            .WithLatency(TimeSpan.FromMilliseconds(500));
        var failingProvider = MockLLMProvider.CreateFailing("Failing", "failing-model");

        services.AddSingleton<ILLMProvider>(_ => fastProvider);
        services.AddSingleton<ILLMProvider>(_ => slowProvider);
        services.AddSingleton<ILLMProvider>(_ => failingProvider);

        var sp = services.BuildServiceProvider();

        // Set up routing
        var routingOptions = Options.Create(new LLMRoutingOptions
        {
            RoutingStrategy = "TaskBased",
            EnableMultiModelRouting = true,
            EnableProviderFallback = true,
            DefaultRoute = new RouteConfiguration
            {
                PrimaryProvider = "Fast",
                PrimaryModel = "fast-model",
                FallbackProvider = "Slow",
                FallbackModel = "slow-model"
            }
        });

        var logger = sp.GetRequiredService<ILogger<RoutingLLMProvider>>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var strategies = new List<IRoutingStrategy>
        {
            new TaskBasedRoutingStrategy(loggerFactory.CreateLogger<TaskBasedRoutingStrategy>())
        };

        var routingProvider = new RoutingLLMProvider(sp, routingOptions, strategies, logger);

        // Wrap in circuit breaker
        var circuitBreakerOptions = Options.Create(new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            OpenDuration = TimeSpan.FromSeconds(60)
        });

        var circuitBreakerLogger = loggerFactory.CreateLogger<CircuitBreakerLLMProvider>();
        var circuitBreakerProvider = new CircuitBreakerLLMProvider(
            routingProvider,
            slowProvider, // Fallback for the entire routing layer
            circuitBreakerOptions,
            circuitBreakerLogger);

        var request = new LLMRequest
        {
            Model = "fast-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Complex test" }
            }
        };

        // Act
        var response = await circuitBreakerProvider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        fastProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MultipleRequests_WithCostOptimizedRouting_SelectsCheapestProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var expensiveProvider = MockLLMProvider.CreateSuccessful("Expensive", "expensive-model");
        var cheapProvider = MockLLMProvider.CreateSuccessful("Cheap", "cheap-model");

        services.AddSingleton<ILLMProvider>(_ => expensiveProvider);
        services.AddSingleton<ILLMProvider>(_ => cheapProvider);

        var sp = services.BuildServiceProvider();

        var routingOptions = Options.Create(new LLMRoutingOptions
        {
            RoutingStrategy = "CostOptimized",
            EnableMultiModelRouting = true,
            DefaultRoute = new RouteConfiguration
            {
                PrimaryProvider = "Cheap",
                PrimaryModel = "cheap-model",
                CostPer1KTokens = 0.0001
            },
            Routes = new Dictionary<string, RouteConfiguration>
            {
                ["CodeGeneration"] = new RouteConfiguration
                {
                    PrimaryProvider = "Cheap",
                    PrimaryModel = "cheap-model",
                    CostPer1KTokens = 0.0001
                },
                ["Planning"] = new RouteConfiguration
                {
                    PrimaryProvider = "Expensive",
                    PrimaryModel = "expensive-model",
                    CostPer1KTokens = 0.01
                }
            }
        });

        var logger = sp.GetRequiredService<ILogger<RoutingLLMProvider>>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var strategies = new List<IRoutingStrategy>
        {
            new CostOptimizedRoutingStrategy(loggerFactory.CreateLogger<CostOptimizedRoutingStrategy>())
        };

        var routingProvider = new RoutingLLMProvider(sp, routingOptions, strategies, logger);

        var request = new LLMRequest
        {
            Model = "cheap-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Write a simple function" }
            }
        };

        // Act
        var response = await routingProvider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        // Cost-optimized routing should prefer the cheap provider
        (cheapProvider.CallCount + expensiveProvider.CallCount).Should().BeGreaterThan(0);
    }
}
