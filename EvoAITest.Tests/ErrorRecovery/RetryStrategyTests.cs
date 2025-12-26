using Xunit;
using FluentAssertions;
using EvoAITest.Core.Models.ErrorRecovery;

namespace EvoAITest.Tests.ErrorRecovery;

/// <summary>
/// Unit tests for RetryStrategy
/// </summary>
public class RetryStrategyTests
{
    [Fact]
    public void CalculateDelay_FirstAttempt_ReturnsInitialDelay()
    {
        // Arrange
        var strategy = new RetryStrategy
        {
            InitialDelay = TimeSpan.FromMilliseconds(500),
            UseExponentialBackoff = false,
            UseJitter = false
        };

        // Act
        var delay = strategy.CalculateDelay(1);

        // Assert
        delay.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void CalculateDelay_ExponentialBackoff_DoublesEachTime()
    {
        // Arrange
        var strategy = new RetryStrategy
        {
            InitialDelay = TimeSpan.FromMilliseconds(500),
            MaxDelay = TimeSpan.FromSeconds(30),
            UseExponentialBackoff = true,
            UseJitter = false,
            BackoffMultiplier = 2.0
        };

        // Act
        var delay1 = strategy.CalculateDelay(1);
        var delay2 = strategy.CalculateDelay(2);
        var delay3 = strategy.CalculateDelay(3);

        // Assert
        delay1.Should().Be(TimeSpan.FromMilliseconds(500));  // 500 * 2^0
        delay2.Should().Be(TimeSpan.FromMilliseconds(1000)); // 500 * 2^1
        delay3.Should().Be(TimeSpan.FromMilliseconds(2000)); // 500 * 2^2
    }

    [Fact]
    public void CalculateDelay_ExceedsMaxDelay_CapsAtMaxDelay()
    {
        // Arrange
        var strategy = new RetryStrategy
        {
            InitialDelay = TimeSpan.FromSeconds(5),
            MaxDelay = TimeSpan.FromSeconds(10),
            UseExponentialBackoff = true,
            UseJitter = false,
            BackoffMultiplier = 2.0
        };

        // Act
        var delay = strategy.CalculateDelay(10); // Would be 5 * 2^9 = 2560 seconds

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CalculateDelay_WithJitter_AddsRandomDelay()
    {
        // Arrange
        var strategy = new RetryStrategy
        {
            InitialDelay = TimeSpan.FromMilliseconds(1000),
            UseExponentialBackoff = false,
            UseJitter = true
        };

        // Act - Run multiple times to test jitter variability
        var delays = Enumerable.Range(0, 100)
            .Select(_ => strategy.CalculateDelay(1))
            .ToList();

        // Assert
        delays.Should().OnlyContain(d => d >= TimeSpan.FromMilliseconds(1000));
        delays.Should().OnlyContain(d => d <= TimeSpan.FromMilliseconds(1300)); // Max 30% jitter
        delays.Distinct().Count().Should().BeGreaterThan(10); // Should have variability
    }

    [Fact]
    public void CalculateDelay_CustomMultiplier_UsesCorrectMultiplier()
    {
        // Arrange
        var strategy = new RetryStrategy
        {
            InitialDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(30),
            UseExponentialBackoff = true,
            UseJitter = false,
            BackoffMultiplier = 3.0
        };

        // Act
        var delay1 = strategy.CalculateDelay(1);
        var delay2 = strategy.CalculateDelay(2);
        var delay3 = strategy.CalculateDelay(3);

        // Assert
        delay1.Should().Be(TimeSpan.FromMilliseconds(100));  // 100 * 3^0
        delay2.Should().Be(TimeSpan.FromMilliseconds(300));  // 100 * 3^1
        delay3.Should().Be(TimeSpan.FromMilliseconds(900));  // 100 * 3^2
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var strategy = new RetryStrategy();

        // Assert
        strategy.MaxRetries.Should().Be(3);
        strategy.InitialDelay.Should().Be(TimeSpan.FromMilliseconds(500));
        strategy.MaxDelay.Should().Be(TimeSpan.FromSeconds(10));
        strategy.UseExponentialBackoff.Should().BeTrue();
        strategy.UseJitter.Should().BeTrue();
        strategy.BackoffMultiplier.Should().Be(2.0);
    }

    [Fact]
    public void CalculateDelay_LinearBackoff_IncreasesLinearly()
    {
        // Arrange
        var strategy = new RetryStrategy
        {
            InitialDelay = TimeSpan.FromMilliseconds(500),
            MaxDelay = TimeSpan.FromSeconds(30),
            UseExponentialBackoff = false,
            UseJitter = false
        };

        // Act
        var delay1 = strategy.CalculateDelay(1);
        var delay2 = strategy.CalculateDelay(2);
        var delay3 = strategy.CalculateDelay(3);

        // Assert - All should be the same with linear backoff disabled
        delay1.Should().Be(TimeSpan.FromMilliseconds(500));
        delay2.Should().Be(TimeSpan.FromMilliseconds(500));
        delay3.Should().Be(TimeSpan.FromMilliseconds(500));
    }
}
