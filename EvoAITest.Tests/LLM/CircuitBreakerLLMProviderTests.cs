using EvoAITest.Core.Options;
using EvoAITest.LLM.CircuitBreaker;
using EvoAITest.LLM.Models;
using EvoAITest.LLM.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EvoAITest.Tests.LLM;

/// <summary>
/// Unit tests for <see cref="CircuitBreakerLLMProvider"/>.
/// </summary>
public sealed class CircuitBreakerLLMProviderTests
{
    private readonly ILogger<CircuitBreakerLLMProvider> _logger;

    public CircuitBreakerLLMProviderTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<CircuitBreakerLLMProvider>();
    }

    [Fact]
    public void Constructor_WithNullPrimaryProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var fallbackProvider = MockLLMProvider.CreateSuccessful();
        var options = CreateDefaultOptions();

        // Act & Assert
        Xunit.Assert.Throws<ArgumentNullException>(() =>
            new CircuitBreakerLLMProvider(null!, fallbackProvider, options, _logger));
    }

    [Fact]
    public void Constructor_WithNullFallbackProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful();
        var options = CreateDefaultOptions();

        // Act & Assert
        Xunit.Assert.Throws<ArgumentNullException>(() =>
            new CircuitBreakerLLMProvider(primaryProvider, null!, options, _logger));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful();
        var fallbackProvider = MockLLMProvider.CreateSuccessful();
        var options = CreateDefaultOptions();

        // Act
        var provider = new CircuitBreakerLLMProvider(primaryProvider, fallbackProvider, options, _logger);

        // Assert
        provider.Should().NotBeNull();
        provider.Name.Should().Contain("CircuitBreaker");
    }

    [Fact]
    public async Task CompleteAsync_WithSuccessfulPrimary_UsesPrimaryProvider()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");
        var provider = CreateCircuitBreakerProvider(primaryProvider, fallbackProvider);

        var request = CreateTestRequest();

        // Act
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        primaryProvider.CallCount.Should().Be(1);
        fallbackProvider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task CompleteAsync_WhenPrimaryFails_UsesFallbackProvider()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateFailing("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");
        var options = CreateOptionsWithLowThreshold(failureThreshold: 1);

        var provider = new CircuitBreakerLLMProvider(primaryProvider, fallbackProvider, options, _logger);

        var request = CreateTestRequest();

        // Act - First call should fail primary and open circuit
        try
        {
            await provider.CompleteAsync(request);
        }
        catch
        {
            // Expected on first failure
        }

        // Second call should use fallback
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        fallbackProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CircuitBreaker_AfterFailureThreshold_OpensCircuit()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateFailing("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");
        var options = CreateOptionsWithLowThreshold(failureThreshold: 3);

        var provider = new CircuitBreakerLLMProvider(primaryProvider, fallbackProvider, options, _logger);

        var request = CreateTestRequest();

        // Act - Fail 3 times to open circuit
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await provider.CompleteAsync(request);
            }
            catch
            {
                // Expected failures
            }
        }

        // Circuit should now be open
        var status = provider.GetStatus();

        // Assert
        status.State.Should().Be(CircuitBreakerState.Open);
        status.FailureCount.Should().Be(3);
    }

    [Fact]
    public async Task CircuitBreaker_WhenOpen_UsesFallbackProvider()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateFailing("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");
        var options = CreateOptionsWithLowThreshold(failureThreshold: 2);

        var provider = new CircuitBreakerLLMProvider(primaryProvider, fallbackProvider, options, _logger);

        var request = CreateTestRequest();

        // Act - Fail twice to open circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await provider.CompleteAsync(request);
            }
            catch
            {
                // Expected failures
            }
        }

        // Now circuit is open, should use fallback
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        fallbackProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CircuitBreaker_AfterOpenDuration_TransitionsToHalfOpen()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateFailing("Primary", "primary-model")
            .FailUntilCall(3); // Fail first 2, succeed on 3rd
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");
        var options = CreateOptionsWithShortOpenDuration(
            failureThreshold: 2,
            openDurationSeconds: 1);

        var provider = new CircuitBreakerLLMProvider(primaryProvider, fallbackProvider, options, _logger);

        var request = CreateTestRequest();

        // Act - Fail twice to open circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await provider.CompleteAsync(request);
            }
            catch
            {
                // Expected failures
            }
        }

        // Wait for circuit to transition to half-open
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Next call should try primary again (half-open)
        var response = await provider.CompleteAsync(request);

        // Assert
        response.Should().NotBeNull();
        primaryProvider.CallCount.Should().BeGreaterThan(2); // Should have tried again
    }

    [Fact]
    public async Task StreamCompleteAsync_WithSuccessfulPrimary_StreamsFromPrimary()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");
        var provider = CreateCircuitBreakerProvider(primaryProvider, fallbackProvider);

        var request = CreateTestRequest();

        // Act
        var chunks = new List<LLMStreamChunk>();
        await foreach (var chunk in provider.StreamCompleteAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().NotBeEmpty();
        primaryProvider.CallCount.Should().Be(1);
        fallbackProvider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task StreamCompleteAsync_WhenCircuitOpen_StreamsFromFallback()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateFailing("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");
        var options = CreateOptionsWithLowThreshold(failureThreshold: 1);

        var provider = new CircuitBreakerLLMProvider(primaryProvider, fallbackProvider, options, _logger);

        var request = CreateTestRequest();

        // Act - Fail once to open circuit
        try
        {
            await provider.CompleteAsync(request);
        }
        catch
        {
            // Expected failure
        }

        // Stream should now use fallback
        var chunks = new List<LLMStreamChunk>();
        await foreach (var chunk in provider.StreamCompleteAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().NotBeEmpty();
        fallbackProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task IsAvailableAsync_WithBothProvidersAvailable_ReturnsTrue()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful().WithAvailability(true);
        var fallbackProvider = MockLLMProvider.CreateSuccessful().WithAvailability(true);
        var provider = CreateCircuitBreakerProvider(primaryProvider, fallbackProvider);

        // Act
        var isAvailable = await provider.IsAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WithOnlyFallbackAvailable_ReturnsTrue()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful().WithAvailability(false);
        var fallbackProvider = MockLLMProvider.CreateSuccessful().WithAvailability(true);
        var provider = CreateCircuitBreakerProvider(primaryProvider, fallbackProvider);

        // Act
        var isAvailable = await provider.IsAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WithNeitherAvailable_ReturnsFalse()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful().WithAvailability(false);
        var fallbackProvider = MockLLMProvider.CreateSuccessful().WithAvailability(false);
        var provider = CreateCircuitBreakerProvider(primaryProvider, fallbackProvider);

        // Act
        var isAvailable = await provider.IsAvailableAsync();

        // Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public void GetStatus_InitialState_IsClosed()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful();
        var fallbackProvider = MockLLMProvider.CreateSuccessful();
        var provider = CreateCircuitBreakerProvider(primaryProvider, fallbackProvider);

        // Act
        var status = provider.GetStatus();

        // Assert
        status.State.Should().Be(CircuitBreakerState.Closed);
        status.FailureCount.Should().Be(0);
        status.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task GetStatus_AfterSuccesses_UpdatesSuccessCount()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful();
        var fallbackProvider = MockLLMProvider.CreateSuccessful();
        var provider = CreateCircuitBreakerProvider(primaryProvider, fallbackProvider);

        var request = CreateTestRequest();

        // Act - Make 3 successful calls
        for (int i = 0; i < 3; i++)
        {
            await provider.CompleteAsync(request);
        }

        var status = provider.GetStatus();

        // Assert
        status.SuccessCount.Should().Be(3);
        status.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void GetCapabilities_ReturnsValidCapabilities()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateSuccessful();
        var fallbackProvider = MockLLMProvider.CreateSuccessful();
        var provider = CreateCircuitBreakerProvider(primaryProvider, fallbackProvider);

        // Act
        var capabilities = provider.GetCapabilities();

        // Assert
        capabilities.Should().NotBeNull();
        capabilities.SupportsStreaming.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentRequests_WithFailures_HandlesThreadSafety()
    {
        // Arrange
        var primaryProvider = MockLLMProvider.CreateFailing("Primary", "primary-model");
        var fallbackProvider = MockLLMProvider.CreateSuccessful("Fallback", "fallback-model");
        var options = CreateOptionsWithLowThreshold(failureThreshold: 5);

        var provider = new CircuitBreakerLLMProvider(primaryProvider, fallbackProvider, options, _logger);

        var request = CreateTestRequest();

        // Act - Make 10 concurrent requests
        var tasks = Enumerable.Range(0, 10)
            .Select(async _ =>
            {
                try
                {
                    return await provider.CompleteAsync(request);
                }
                catch
                {
                    return null;
                }
            })
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // Assert
        // Should not throw and circuit status should be consistent
        var status = provider.GetStatus();
        status.Should().NotBeNull();
        // Some responses should be from fallback
        responses.Count(r => r != null).Should().BeGreaterThan(0);
    }

    // Helper methods

    private CircuitBreakerLLMProvider CreateCircuitBreakerProvider(
        MockLLMProvider primaryProvider,
        MockLLMProvider fallbackProvider)
    {
        var options = CreateDefaultOptions();
        return new CircuitBreakerLLMProvider(primaryProvider, fallbackProvider, options, _logger);
    }

    private IOptions<CircuitBreakerOptions> CreateDefaultOptions()
    {
        return Options.Create(new CircuitBreakerOptions
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromSeconds(30)
        });
    }

    private IOptions<CircuitBreakerOptions> CreateOptionsWithLowThreshold(int failureThreshold)
    {
        return Options.Create(new CircuitBreakerOptions
        {
            FailureThreshold = failureThreshold,
            OpenDuration = TimeSpan.FromSeconds(30)
        });
    }

    private IOptions<CircuitBreakerOptions> CreateOptionsWithShortOpenDuration(
        int failureThreshold,
        int openDurationSeconds)
    {
        return Options.Create(new CircuitBreakerOptions
        {
            FailureThreshold = failureThreshold,
            OpenDuration = TimeSpan.FromSeconds(openDurationSeconds)
        });
    }

    private LLMRequest CreateTestRequest()
    {
        return new LLMRequest
        {
            Model = "test-model",
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = "Test prompt" }
            }
        };
    }
}
