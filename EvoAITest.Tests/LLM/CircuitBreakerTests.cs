using EvoAITest.LLM.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EvoAITest.Tests.LLM;

/// <summary>
/// Unit tests for circuit breaker functionality.
/// </summary>
public sealed class CircuitBreakerTests
{
    private readonly Mock<ILogger<CircuitBreaker>> _mockLogger;
    private readonly CircuitBreakerOptions _options;

    public CircuitBreakerTests()
    {
        _mockLogger = new Mock<ILogger<CircuitBreaker>>();
        _options = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            OpenDuration = TimeSpan.FromSeconds(5),
            RequestTimeout = TimeSpan.FromSeconds(10)
        };
    }

    [Fact]
    public void NewCircuitBreaker_StartsInClosedState()
    {
        // Arrange & Act
        var breaker = new CircuitBreaker("TestProvider", _options, _mockLogger.Object);

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.IsRequestAllowed().Should().BeTrue();
    }

    [Fact]
    public void RecordSuccess_InClosedState_KeepsCircuitClosed()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _options, _mockLogger.Object);

        // Act
        breaker.RecordSuccess();

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.IsRequestAllowed().Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_BelowThreshold_KeepsCircuitClosed()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _options, _mockLogger.Object);

        // Act
        breaker.RecordFailure(new Exception("Test failure 1"));
        breaker.RecordFailure(new Exception("Test failure 2"));

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.IsRequestAllowed().Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_AtThreshold_OpensCircuit()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _options, _mockLogger.Object);

        // Act
        breaker.RecordFailure(new Exception("Failure 1"));
        breaker.RecordFailure(new Exception("Failure 2"));
        breaker.RecordFailure(new Exception("Failure 3"));

        // Assert
        breaker.State.Should().Be(CircuitState.Open);
        breaker.IsRequestAllowed().Should().BeFalse();
    }

    [Fact]
    public void OpenCircuit_AfterDuration_TransitionsToHalfOpen()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            OpenDuration = TimeSpan.FromMilliseconds(100)
        };
        var breaker = new CircuitBreaker("TestProvider", options, _mockLogger.Object);

        // Act
        breaker.RecordFailure(new Exception("Failure 1"));
        breaker.RecordFailure(new Exception("Failure 2"));
        
        breaker.State.Should().Be(CircuitState.Open);
        
        // Wait for open duration
        Thread.Sleep(150);
        
        // Trigger state check
        var allowed = breaker.IsRequestAllowed();

        // Assert
        allowed.Should().BeTrue();
        breaker.State.Should().Be(CircuitState.HalfOpen);
    }

    [Fact]
    public void HalfOpenCircuit_OnSuccess_ClosesCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            OpenDuration = TimeSpan.FromMilliseconds(100)
        };
        var breaker = new CircuitBreaker("TestProvider", options, _mockLogger.Object);

        // Open the circuit
        breaker.RecordFailure(new Exception("Failure 1"));
        breaker.RecordFailure(new Exception("Failure 2"));
        
        // Wait for half-open
        Thread.Sleep(150);
        breaker.IsRequestAllowed(); // Trigger transition to half-open

        // Act
        breaker.RecordSuccess();

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.IsRequestAllowed().Should().BeTrue();
    }

    [Fact]
    public void HalfOpenCircuit_OnFailure_ReopensCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            OpenDuration = TimeSpan.FromMilliseconds(100)
        };
        var breaker = new CircuitBreaker("TestProvider", options, _mockLogger.Object);

        // Open the circuit
        breaker.RecordFailure(new Exception("Failure 1"));
        breaker.RecordFailure(new Exception("Failure 2"));
        
        // Wait for half-open
        Thread.Sleep(150);
        breaker.IsRequestAllowed(); // Trigger transition to half-open

        // Act
        breaker.RecordFailure(new Exception("Half-open test failure"));

        // Assert
        breaker.State.Should().Be(CircuitState.Open);
        breaker.IsRequestAllowed().Should().BeFalse();
    }

    [Fact]
    public void Reset_OpensClosedCircuit()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _options, _mockLogger.Object);
        
        // Open the circuit
        breaker.RecordFailure(new Exception("Failure 1"));
        breaker.RecordFailure(new Exception("Failure 2"));
        breaker.RecordFailure(new Exception("Failure 3"));

        breaker.State.Should().Be(CircuitState.Open);

        // Act
        breaker.Reset();

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.IsRequestAllowed().Should().BeTrue();
    }

    [Fact]
    public void GetStats_ReturnsCorrectInformation()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _options, _mockLogger.Object);
        
        breaker.RecordFailure(new Exception("Failure 1"));
        breaker.RecordFailure(new Exception("Failure 2"));

        // Act
        var stats = breaker.GetStats();

        // Assert
        stats.Should().NotBeNull();
        stats.ProviderName.Should().Be("TestProvider");
        stats.State.Should().Be(CircuitState.Closed);
        stats.ConsecutiveFailures.Should().Be(2);
        stats.LastFailureTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CircuitBreakerRegistry_CreatesAndTracksBreakers()
    {
        // Arrange
        var registry = new CircuitBreakerRegistry(_options, _mockLogger.Object);

        // Act
        var breaker1 = registry.GetOrCreateBreaker("Provider1");
        var breaker2 = registry.GetOrCreateBreaker("Provider2");
        var breaker1Again = registry.GetOrCreateBreaker("Provider1");

        // Assert
        breaker1.Should().NotBeNull();
        breaker2.Should().NotBeNull();
        breaker1Again.Should().BeSameAs(breaker1); // Same instance
        breaker1.Should().NotBeSameAs(breaker2);
    }

    [Fact]
    public void CircuitBreakerRegistry_GetAllStats_ReturnsAllBreakers()
    {
        // Arrange
        var registry = new CircuitBreakerRegistry(_options, _mockLogger.Object);
        
        var breaker1 = registry.GetOrCreateBreaker("Provider1");
        var breaker2 = registry.GetOrCreateBreaker("Provider2");
        
        breaker1.RecordFailure(new Exception("Test"));
        breaker2.RecordFailure(new Exception("Test"));
        breaker2.RecordFailure(new Exception("Test"));

        // Act
        var allStats = registry.GetAllStats();

        // Assert
        allStats.Should().HaveCount(2);
        allStats.Should().Contain(s => s.ProviderName == "Provider1" && s.ConsecutiveFailures == 1);
        allStats.Should().Contain(s => s.ProviderName == "Provider2" && s.ConsecutiveFailures == 2);
    }

    [Fact]
    public void CircuitBreakerRegistry_ResetAll_ResetsAllBreakers()
    {
        // Arrange
        var registry = new CircuitBreakerRegistry(_options, _mockLogger.Object);
        
        var breaker1 = registry.GetOrCreateBreaker("Provider1");
        var breaker2 = registry.GetOrCreateBreaker("Provider2");
        
        // Open both circuits
        for (int i = 0; i < 3; i++)
        {
            breaker1.RecordFailure(new Exception("Test"));
            breaker2.RecordFailure(new Exception("Test"));
        }

        breaker1.State.Should().Be(CircuitState.Open);
        breaker2.State.Should().Be(CircuitState.Open);

        // Act
        registry.ResetAll();

        // Assert
        breaker1.State.Should().Be(CircuitState.Closed);
        breaker2.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public void ClosedCircuit_SuccessAfterFailure_ResetsFailureCount()
    {
        // Arrange
        var breaker = new CircuitBreaker("TestProvider", _options, _mockLogger.Object);
        
        breaker.RecordFailure(new Exception("Failure 1"));
        breaker.RecordFailure(new Exception("Failure 2"));

        var stats1 = breaker.GetStats();
        stats1.ConsecutiveFailures.Should().Be(2);

        // Act
        breaker.RecordSuccess();
        var stats2 = breaker.GetStats();

        // Assert
        stats2.ConsecutiveFailures.Should().Be(0);
        breaker.State.Should().Be(CircuitState.Closed);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void CircuitBreaker_RespectsConfiguredThreshold(int threshold)
    {
        // Arrange
        var options = new CircuitBreakerOptions { FailureThreshold = threshold };
        var breaker = new CircuitBreaker("TestProvider", options, _mockLogger.Object);

        // Act
        for (int i = 0; i < threshold - 1; i++)
        {
            breaker.RecordFailure(new Exception($"Failure {i + 1}"));
        }

        // Assert
        breaker.State.Should().Be(CircuitState.Closed); // Still closed, just under threshold

        // Act - push over threshold
        breaker.RecordFailure(new Exception($"Failure {threshold}"));

        // Assert
        breaker.State.Should().Be(CircuitState.Open); // Now open
    }
}
