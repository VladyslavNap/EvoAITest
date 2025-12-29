using Xunit;
using FluentAssertions;
using EvoAITest.Core.Options;

namespace EvoAITest.Tests.ErrorRecovery;

/// <summary>
/// Unit tests for ErrorRecoveryOptions validation
/// </summary>
public class ErrorRecoveryOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new ErrorRecoveryOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.AutoRetry.Should().BeTrue();
        options.MaxRetries.Should().Be(3);
        options.InitialDelayMs.Should().Be(500);
        options.MaxDelayMs.Should().Be(10000);
        options.UseExponentialBackoff.Should().BeTrue();
        options.UseJitter.Should().BeTrue();
        options.BackoffMultiplier.Should().Be(2.0);
        options.EnabledActions.Should().HaveCount(5);
        options.EnabledActions.Should().Contain("WaitAndRetry");
        options.EnabledActions.Should().Contain("PageRefresh");
        options.EnabledActions.Should().Contain("AlternativeSelector");
        options.EnabledActions.Should().Contain("WaitForStability");
        options.EnabledActions.Should().Contain("ClearCookies");
    }

    [Fact]
    public void Validate_ValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            MaxRetries = 5,
            InitialDelayMs = 1000,
            MaxDelayMs = 15000,
            BackoffMultiplier = 2.5
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void Validate_NegativeMaxRetries_ThrowsException()
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            MaxRetries = -1
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("MaxRetries must be non-negative");
    }

    [Fact]
    public void Validate_NegativeInitialDelay_ThrowsException()
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            InitialDelayMs = -100
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("InitialDelayMs must be non-negative");
    }

    [Fact]
    public void Validate_MaxDelayLessThanInitial_ThrowsException()
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            InitialDelayMs = 5000,
            MaxDelayMs = 1000
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("MaxDelayMs must be greater than or equal to InitialDelayMs");
    }

    [Fact]
    public void Validate_ZeroBackoffMultiplier_ThrowsException()
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            BackoffMultiplier = 0
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("BackoffMultiplier must be positive");
    }

    [Fact]
    public void Validate_NegativeBackoffMultiplier_ThrowsException()
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            BackoffMultiplier = -1.5
        };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("BackoffMultiplier must be positive");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void Validate_ValidMaxRetries_DoesNotThrow(int maxRetries)
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            MaxRetries = maxRetries
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(60000)]
    public void Validate_ValidDelays_DoesNotThrow(int delayMs)
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            InitialDelayMs = delayMs,
            MaxDelayMs = delayMs + 1000
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(10.0)]
    public void Validate_ValidBackoffMultiplier_DoesNotThrow(double multiplier)
    {
        // Arrange
        var options = new ErrorRecoveryOptions
        {
            BackoffMultiplier = multiplier
        };

        // Act & Assert
        options.Invoking(o => o.Validate()).Should().NotThrow();
    }
}
