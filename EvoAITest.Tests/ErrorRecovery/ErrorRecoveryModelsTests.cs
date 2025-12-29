using Xunit;
using FluentAssertions;
using EvoAITest.Core.Models.ErrorRecovery;

namespace EvoAITest.Tests.ErrorRecovery;

/// <summary>
/// Unit tests for ErrorRecovery models
/// </summary>
public class ErrorRecoveryModelsTests
{
    [Fact]
    public void ErrorClassification_IsRecoverable_WhenConfidenceAboveThreshold()
    {
        // Arrange
        var classification = new ErrorClassification
        {
            ErrorType = ErrorType.SelectorNotFound,
            Confidence = 0.85,
            Exception = new Exception("Test"),
            Message = "Test error"
        };

        // Assert
        classification.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void ErrorClassification_IsNotRecoverable_WhenConfidenceBelowThreshold()
    {
        // Arrange
        var classification = new ErrorClassification
        {
            ErrorType = ErrorType.SelectorNotFound,
            Confidence = 0.65,
            Exception = new Exception("Test"),
            Message = "Test error"
        };

        // Assert
        classification.IsRecoverable.Should().BeFalse();
    }

    [Fact]
    public void ErrorClassification_IsNotRecoverable_WhenUnknownType()
    {
        // Arrange
        var classification = new ErrorClassification
        {
            ErrorType = ErrorType.Unknown,
            Confidence = 0.95, // Even with high confidence
            Exception = new Exception("Test"),
            Message = "Test error"
        };

        // Assert
        classification.IsRecoverable.Should().BeFalse();
    }

    [Fact]
    public void RecoveryResult_CanBeCreatedWithSuccess()
    {
        // Arrange
        var classification = new ErrorClassification
        {
            ErrorType = ErrorType.Transient,
            Confidence = 0.9,
            Exception = new Exception("Test"),
            Message = "Test error"
        };

        var result = new RecoveryResult
        {
            Success = true,
            ActionsAttempted = new List<RecoveryActionType> { RecoveryActionType.WaitAndRetry },
            AttemptNumber = 2,
            Duration = TimeSpan.FromSeconds(3),
            ErrorClassification = classification,
            Strategy = "Adaptive"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.ActionsAttempted.Should().ContainSingle();
        result.AttemptNumber.Should().Be(2);
        result.Duration.Should().Be(TimeSpan.FromSeconds(3));
        result.FinalException.Should().BeNull();
    }

    [Fact]
    public void RecoveryResult_CanBeCreatedWithFailure()
    {
        // Arrange
        var classification = new ErrorClassification
        {
            ErrorType = ErrorType.PageCrash,
            Confidence = 0.9,
            Exception = new Exception("Original"),
            Message = "Original error"
        };

        var finalException = new Exception("Final error");

        var result = new RecoveryResult
        {
            Success = false,
            ActionsAttempted = new List<RecoveryActionType> 
            { 
                RecoveryActionType.RestartContext,
                RecoveryActionType.NavigationRetry
            },
            AttemptNumber = 3,
            Duration = TimeSpan.FromSeconds(10),
            ErrorClassification = classification,
            FinalException = finalException,
            Strategy = "Adaptive"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ActionsAttempted.Should().HaveCount(2);
        result.FinalException.Should().Be(finalException);
    }

    [Fact]
    public void ErrorClassification_ContextIsInitialized()
    {
        // Arrange & Act
        var classification = new ErrorClassification
        {
            ErrorType = ErrorType.NetworkError,
            Confidence = 0.85,
            Exception = new Exception("Test"),
            Message = "Test error"
        };

        // Assert
        classification.Context.Should().NotBeNull();
        classification.Context.Should().BeEmpty();
    }

    [Fact]
    public void ErrorClassification_SuggestedActionsIsInitialized()
    {
        // Arrange & Act
        var classification = new ErrorClassification
        {
            ErrorType = ErrorType.NetworkError,
            Confidence = 0.85,
            Exception = new Exception("Test"),
            Message = "Test error"
        };

        // Assert
        classification.SuggestedActions.Should().NotBeNull();
        classification.SuggestedActions.Should().BeEmpty();
    }

    [Fact]
    public void RecoveryResult_MetadataIsInitialized()
    {
        // Arrange & Act
        var result = new RecoveryResult
        {
            Success = true,
            AttemptNumber = 1,
            Duration = TimeSpan.FromSeconds(1),
            ErrorClassification = new ErrorClassification
            {
                ErrorType = ErrorType.Transient,
                Confidence = 0.8,
                Exception = new Exception("Test"),
                Message = "Test"
            },
            Strategy = "Adaptive"
        };

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ErrorType.Unknown)]
    [InlineData(ErrorType.Transient)]
    [InlineData(ErrorType.SelectorNotFound)]
    [InlineData(ErrorType.NavigationTimeout)]
    [InlineData(ErrorType.JavaScriptError)]
    [InlineData(ErrorType.PermissionDenied)]
    [InlineData(ErrorType.NetworkError)]
    [InlineData(ErrorType.PageCrash)]
    [InlineData(ErrorType.ElementNotInteractable)]
    [InlineData(ErrorType.TimingIssue)]
    public void ErrorType_AllValuesAreDefined(ErrorType errorType)
    {
        // Assert
        Enum.IsDefined(typeof(ErrorType), errorType).Should().BeTrue();
    }

    [Theory]
    [InlineData(RecoveryActionType.None)]
    [InlineData(RecoveryActionType.WaitAndRetry)]
    [InlineData(RecoveryActionType.PageRefresh)]
    [InlineData(RecoveryActionType.AlternativeSelector)]
    [InlineData(RecoveryActionType.NavigationRetry)]
    [InlineData(RecoveryActionType.ClearCookies)]
    [InlineData(RecoveryActionType.ClearCache)]
    [InlineData(RecoveryActionType.WaitForStability)]
    [InlineData(RecoveryActionType.RestartContext)]
    public void RecoveryActionType_AllValuesAreDefined(RecoveryActionType actionType)
    {
        // Assert
        Enum.IsDefined(typeof(RecoveryActionType), actionType).Should().BeTrue();
    }
}
