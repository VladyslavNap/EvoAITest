using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.ErrorRecovery;
using EvoAITest.Core.Services.ErrorRecovery;
using ExecutionContext = EvoAITest.Core.Models.ExecutionContext;

namespace EvoAITest.Tests.ErrorRecovery;

/// <summary>
/// Unit tests for ErrorClassifier service
/// </summary>
public class ErrorClassifierTests
{
    private readonly ErrorClassifier _classifier;

    public ErrorClassifierTests()
    {
        _classifier = new ErrorClassifier(NullLogger<ErrorClassifier>.Instance);
    }

    [Fact]
    public async Task ClassifyAsync_SelectorNotFound_ReturnsCorrectType()
    {
        // Arrange
        var exception = new Exception("Selector not found: #button");
        var context = new ExecutionContext
        {
            Action = "click",
            Selector = "#button",
            PageUrl = "https://example.com"
        };

        // Act
        var result = await _classifier.ClassifyAsync(exception, context);

        // Assert
        result.ErrorType.Should().Be(ErrorType.SelectorNotFound);
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.9);
        result.IsRecoverable.Should().BeTrue();
        result.SuggestedActions.Should().Contain(RecoveryActionType.AlternativeSelector);
        result.SuggestedActions.Should().Contain(RecoveryActionType.WaitForStability);
        result.Message.Should().Be(exception.Message);
        result.Exception.Should().Be(exception);
    }

    [Theory]
    [InlineData("Timeout exceeded", ErrorType.Transient, 0.75)]
    [InlineData("Navigation timeout 30000ms exceeded", ErrorType.NavigationTimeout, 0.95)]
    [InlineData("Element not visible on page", ErrorType.ElementNotInteractable, 0.9)]
    [InlineData("Element is not interactable", ErrorType.ElementNotInteractable, 0.9)]
    [InlineData("Network connection failed", ErrorType.NetworkError, 0.85)]
    [InlineData("Browser crashed unexpectedly", ErrorType.PageCrash, 0.9)]
    [InlineData("JavaScript evaluation failed", ErrorType.JavaScriptError, 0.85)]
    [InlineData("Permission denied by browser", ErrorType.PermissionDenied, 0.9)]
    public async Task ClassifyAsync_VariousErrors_ReturnsExpectedTypes(
        string errorMessage,
        ErrorType expectedType,
        double minimumConfidence)
    {
        // Arrange
        var exception = new Exception(errorMessage);

        // Act
        var result = await _classifier.ClassifyAsync(exception);

        // Assert
        result.ErrorType.Should().Be(expectedType);
        result.Confidence.Should().BeGreaterThanOrEqualTo(minimumConfidence);
        result.Message.Should().Be(errorMessage);
    }

    [Fact]
    public async Task ClassifyAsync_UnknownError_ReturnsUnknownType()
    {
        // Arrange
        var exception = new Exception("Some completely unknown error");

        // Act
        var result = await _classifier.ClassifyAsync(exception);

        // Assert
        result.ErrorType.Should().Be(ErrorType.Unknown);
        result.Confidence.Should().Be(0.5);
        result.IsRecoverable.Should().BeFalse();
        result.SuggestedActions.Should().Contain(RecoveryActionType.None);
    }

    [Theory]
    [InlineData(ErrorType.Transient, true)]
    [InlineData(ErrorType.NetworkError, true)]
    [InlineData(ErrorType.TimingIssue, true)]
    [InlineData(ErrorType.SelectorNotFound, false)]
    [InlineData(ErrorType.PageCrash, false)]
    [InlineData(ErrorType.Unknown, false)]
    public void IsTransient_VariousErrorTypes_ReturnsCorrectResult(
        ErrorType errorType,
        bool expectedTransient)
    {
        // Act
        var result = _classifier.IsTransient(errorType);

        // Assert
        result.Should().Be(expectedTransient);
    }

    [Fact]
    public void GetSuggestedActions_SelectorNotFound_ReturnsSelectorActions()
    {
        // Act
        var actions = _classifier.GetSuggestedActions(ErrorType.SelectorNotFound);

        // Assert
        actions.Should().Contain(RecoveryActionType.AlternativeSelector);
        actions.Should().Contain(RecoveryActionType.WaitForStability);
        actions.Should().Contain(RecoveryActionType.PageRefresh);
        actions.Should().HaveCount(3);
    }

    [Fact]
    public void GetSuggestedActions_NavigationTimeout_ReturnsNavigationActions()
    {
        // Act
        var actions = _classifier.GetSuggestedActions(ErrorType.NavigationTimeout);

        // Assert
        actions.Should().Contain(RecoveryActionType.NavigationRetry);
        actions.Should().Contain(RecoveryActionType.WaitAndRetry);
    }

    [Fact]
    public void GetSuggestedActions_ElementNotInteractable_ReturnsWaitActions()
    {
        // Act
        var actions = _classifier.GetSuggestedActions(ErrorType.ElementNotInteractable);

        // Assert
        actions.Should().Contain(RecoveryActionType.WaitForStability);
        actions.Should().Contain(RecoveryActionType.AlternativeSelector);
    }

    [Fact]
    public void GetSuggestedActions_Unknown_ReturnsNone()
    {
        // Act
        var actions = _classifier.GetSuggestedActions(ErrorType.Unknown);

        // Assert
        actions.Should().ContainSingle();
        actions.Should().Contain(RecoveryActionType.None);
    }

    [Fact]
    public async Task ClassifyAsync_WithContext_IncludesContextInResult()
    {
        // Arrange
        var exception = new Exception("Test error");
        var context = new ExecutionContext
        {
            TaskId = Guid.NewGuid(),
            Action = "click",
            Selector = "#button",
            PageUrl = "https://example.com",
            ExpectedText = "Submit"
        };

        // Act
        var result = await _classifier.ClassifyAsync(exception, context);

        // Assert
        result.Context.Should().ContainKey("PageUrl");
        result.Context.Should().ContainKey("Action");
        result.Context.Should().ContainKey("Selector");
        result.Context["PageUrl"].Should().Be("https://example.com");
        result.Context["Action"].Should().Be("click");
        result.Context["Selector"].Should().Be("#button");
    }

    [Fact]
    public async Task ClassifyAsync_WithInnerException_IncludesInnerExceptionInfo()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var exception = new Exception("Outer error", innerException);

        // Act
        var result = await _classifier.ClassifyAsync(exception);

        // Assert
        result.Context.Should().ContainKey("InnerExceptionType");
        result.Context.Should().ContainKey("InnerExceptionMessage");
        result.Context["InnerExceptionType"].Should().Be("InvalidOperationException");
        result.Context["InnerExceptionMessage"].Should().Be("Inner error");
    }

    [Theory]
    [InlineData("selector 'button' timeout waiting")]
    [InlineData("Timeout waiting for selector")]
    public async Task ClassifyAsync_TimeoutWithSelector_ClassifiesAsTimingIssue(string message)
    {
        // Arrange
        var exception = new TimeoutException(message);

        // Act
        var result = await _classifier.ClassifyAsync(exception);

        // Assert
        result.ErrorType.Should().Be(ErrorType.TimingIssue);
        result.Confidence.Should().Be(0.85);
    }

    [Fact]
    public async Task ClassifyAsync_LowConfidence_MarksAsNotRecoverable()
    {
        // Arrange
        var exception = new Exception("Random error message xyz123");

        // Act
        var result = await _classifier.ClassifyAsync(exception);

        // Assert
        result.ErrorType.Should().Be(ErrorType.Unknown);
        result.Confidence.Should().BeLessThan(0.7);
        result.IsRecoverable.Should().BeFalse();
    }
}
