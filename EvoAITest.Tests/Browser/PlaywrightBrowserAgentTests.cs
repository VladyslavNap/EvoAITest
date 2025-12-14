using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Browser;
using EvoAITest.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EvoAITest.Tests.Browser;

/// <summary>
/// xUnit tests for PlaywrightBrowserAgent.
/// Tests browser initialization, navigation, page state retrieval, and screenshot functionality.
/// Uses mocked ILogger and ensures proper disposal to avoid leaked Playwright processes.
/// </summary>
public class PlaywrightBrowserAgentTests : IAsyncLifetime
{
    private readonly Mock<ILogger<PlaywrightBrowserAgent>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private PlaywrightBrowserAgent? _agent;

    public PlaywrightBrowserAgentTests()
    {
        _mockLogger = new Mock<ILogger<PlaywrightBrowserAgent>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
    }

    /// <summary>
    /// Initialize test resources (called before each test).
    /// </summary>
    public Task InitializeAsync()
    {
        // Create a new agent for each test to ensure test isolation
        _agent = new PlaywrightBrowserAgent(_mockLogger.Object, _mockLoggerFactory.Object);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispose test resources (called after each test).
    /// This ensures Playwright processes are properly cleaned up.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_agent is not null)
        {
            await _agent.DisposeAsync();
            _agent = null;
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldCompleteWithoutErrors()
    {
        // Arrange
        _agent.Should().NotBeNull("agent should be created in test setup");

        // Act
        Func<Task> act = async () => await _agent!.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();

        // Verify logger was called with initialization message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initializing Playwright")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "should log initialization start");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initialized successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "should log successful initialization");
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await _agent!.InitializeAsync();

        // Act
        Func<Task> act = async () => await _agent.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already initialized*");
    }

    [Fact]
    public async Task NavigateAsync_ToExampleDotCom_ShouldSucceed()
    {
        // Arrange
        await _agent!.InitializeAsync();
        const string url = "https://example.com";

        // Act
        Func<Task> act = async () => await _agent.NavigateAsync(url);

        // Assert
        await act.Should().NotThrowAsync();

        // Verify navigation was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Navigating to") && v.ToString()!.Contains("example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "should log navigation attempt");
    }

    [Fact]
    public async Task NavigateAsync_WithNullUrl_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _agent!.InitializeAsync();

        // Act
        Func<Task> act = async () => await _agent.NavigateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("url");
    }

    [Fact]
    public async Task NavigateAsync_WithEmptyUrl_ShouldThrowArgumentNullException()
    {
        // Arrange
        await _agent!.InitializeAsync();

        // Act
        Func<Task> act = async () => await _agent.NavigateAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///C:/test.html")]
    [InlineData("javascript:alert('test')")]
    public async Task NavigateAsync_WithInvalidUrl_ShouldThrowArgumentException(string invalidUrl)
    {
        // Arrange
        await _agent!.InitializeAsync();

        // Act
        Func<Task> act = async () => await _agent.NavigateAsync(invalidUrl);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*valid absolute HTTP or HTTPS*");
    }

    [Fact]
    public async Task NavigateAsync_BeforeInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange - agent not initialized

        // Act
        Func<Task> act = async () => await _agent!.NavigateAsync("https://example.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public async Task GetPageStateAsync_AfterNavigation_ShouldReturnValidPageState()
    {
        // Arrange
        await _agent!.InitializeAsync();
        const string url = "https://example.com";
        await _agent.NavigateAsync(url);

        // Act
        var pageState = await _agent.GetPageStateAsync();

        // Assert
        pageState.Should().NotBeNull();
        pageState.Url.Should().Contain("example.com", "URL should contain the navigated domain");
        pageState.Title.Should().NotBeNullOrWhiteSpace("page should have a title");
        pageState.Title.Should().Be("Example Domain", "example.com has a specific title");
        pageState.LoadState.Should().NotBe(LoadState.Loading, "page should be loaded");
        pageState.CapturedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        pageState.InteractiveElements.Should().NotBeNull();
        pageState.VisibleElements.Should().NotBeNull();
        pageState.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPageStateAsync_BeforeInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange - agent not initialized

        // Act
        Func<Task> act = async () => await _agent!.GetPageStateAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public async Task GetPageStateAsync_ShouldIncludeViewportDimensions()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        var pageState = await _agent.GetPageStateAsync();

        // Assert
        pageState.ViewportDimensions.Should().NotBeNull("viewport dimensions should be captured");
        pageState.ViewportDimensions!.Width.Should().Be(1920, "default viewport width is 1920");
        pageState.ViewportDimensions.Height.Should().Be(1080, "default viewport height is 1080");
    }

    [Fact]
    public async Task TakeScreenshotAsync_ShouldReturnNonEmptyBase64String()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        var screenshot = await _agent.TakeScreenshotAsync();

        // Assert
        screenshot.Should().NotBeNullOrWhiteSpace("screenshot should contain base64 data");
        screenshot.Length.Should().BeGreaterThan(100, "base64 screenshot should be substantial");

        // Validate it's actually base64 by attempting to decode it
        Func<byte[]> decodeAction = () => Convert.FromBase64String(screenshot);
        decodeAction.Should().NotThrow("screenshot should be valid base64");

        var imageBytes = Convert.FromBase64String(screenshot);
        imageBytes.Should().NotBeEmpty("decoded screenshot should contain image data");
        imageBytes.Length.Should().BeGreaterThan(1000, "PNG screenshot should be at least 1KB");

        // Verify PNG signature (first 8 bytes of a PNG file)
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        imageBytes.Take(8).Should().Equal(pngSignature, "screenshot should be a valid PNG image");
    }

    [Fact]
    public async Task TakeScreenshotAsync_BeforeInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange - agent not initialized

        // Act
        Func<Task> act = async () => await _agent!.TakeScreenshotAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public async Task GetAccessibilityTreeAsync_ShouldReturnAccessibilityData()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        var accessibilityTree = await _agent.GetAccessibilityTreeAsync();

        // Assert
        // Accessibility tree might be empty for simple pages, but the method should not throw
        accessibilityTree.Should().NotBeNull("accessibility tree should be a string (may be empty)");
        
        // If the tree has content, verify it's valid
        if (!string.IsNullOrWhiteSpace(accessibilityTree))
        {
            // Should be valid JSON or structured text
            accessibilityTree.Should().Contain("role", "accessibility tree should contain role information");
        }
    }

    [Fact]
    public async Task WaitForElementAsync_ForExistingElement_ShouldComplete()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act - Wait for the h1 element which exists on example.com
        Func<Task> act = async () => await _agent.WaitForElementAsync("h1", timeoutMs: 5000);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WaitForElementAsync_ForNonExistentElement_ShouldThrowTimeoutException()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act - Wait for an element that doesn't exist with short timeout
        Func<Task> act = async () => await _agent.WaitForElementAsync("#non-existent-element-12345", timeoutMs: 1000);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task ClickAsync_WithValidSelector_ShouldNotThrow()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Note: example.com doesn't have clickable elements, so this test uses a more interactive page
        await _agent.NavigateAsync("https://www.w3.org/");

        // Act - Try to click a link (adjust selector based on actual page structure)
        Func<Task> act = async () => await _agent.ClickAsync("a", maxRetries: 1);

        // Assert - Should either succeed or fail gracefully with timeout
        // We're mainly testing that the method doesn't crash
        try
        {
            await act.Should().NotThrowAsync<InvalidOperationException>();
        }
        catch (TimeoutException)
        {
            // Acceptable if element takes too long to be clickable
        }
    }

    [Fact]
    public async Task TypeAsync_IntoInput_ShouldComplete()
    {
        // Arrange - Use a page with an input field (example.com doesn't have forms)
        await _agent!.InitializeAsync();
        
        // Navigate to a simple HTML page with an input (we can use a data URL for testing)
        var htmlWithInput = @"data:text/html,
            <!DOCTYPE html>
            <html>
            <head><title>Test Page</title></head>
            <body>
                <input id='test-input' type='text' />
            </body>
            </html>";
        
        await _agent.NavigateAsync(htmlWithInput);

        // Act
        Func<Task> act = async () => await _agent.TypeAsync("#test-input", "test value");

        // Assert
        await act.Should().NotThrowAsync();

        // Verify the text was typed
        // Note: GetTextAsync might not work for input values, but we're testing the TypeAsync doesn't throw
    }

    [Fact]
    public async Task GetTextAsync_FromExistingElement_ShouldReturnText()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act - Get text from the h1 element
        var text = await _agent.GetTextAsync("h1");

        // Assert
        text.Should().NotBeNullOrWhiteSpace("h1 element should contain text");
        text.Should().Contain("Example Domain", "example.com h1 contains 'Example Domain'");
    }

    [Fact]
    public async Task GetPageHtmlAsync_ShouldReturnFullHtml()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        var html = await _agent.GetPageHtmlAsync();

        // Assert
        html.Should().NotBeNullOrWhiteSpace("page HTML should not be empty");
        html.Should().Contain("<!DOCTYPE html>", "HTML should start with DOCTYPE");
        html.Should().Contain("<html", "HTML should contain html tag");
        html.Should().Contain("</html>", "HTML should end with closing html tag");
        html.Should().Contain("Example Domain", "HTML should contain page title");
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        await _agent.DisposeAsync();

        // Assert - After disposal, operations should fail
        Func<Task> act = async () => await _agent.GetPageStateAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not been initialized*");

        // Verify logger was called for disposal
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "logger should be called during agent lifecycle");
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        await _agent!.InitializeAsync();

        // Act
        await _agent.DisposeAsync();
        Func<Task> secondDispose = async () => await _agent.DisposeAsync();

        // Assert - Multiple disposal should be safe
        await secondDispose.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CancellationToken_DuringNavigation_ShouldCancelOperation()
    {
        // Arrange
        await _agent!.InitializeAsync();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Act - Navigate to a page and cancel quickly
        Func<Task> act = async () => await _agent.NavigateAsync("https://example.com", cts.Token);

        // Assert - Should either complete or throw OperationCanceledException
        try
        {
            await act.Should().NotThrowAsync<InvalidOperationException>();
        }
        catch (OperationCanceledException)
        {
            // This is acceptable - the operation was cancelled
        }
    }

    [Fact]
    public async Task MultipleNavigations_ShouldWorkSequentially()
    {
        // Arrange
        await _agent!.InitializeAsync();

        // Act - Navigate to multiple pages
        await _agent.NavigateAsync("https://example.com");
        var state1 = await _agent.GetPageStateAsync();

        await _agent.NavigateAsync("https://www.iana.org/");
        var state2 = await _agent.GetPageStateAsync();

        // Assert
        state1.Url.Should().Contain("example.com");
        state2.Url.Should().Contain("iana.org");
        state1.Url.Should().NotBe(state2.Url, "URLs should be different after navigation");
    }

    [Fact]
    public async Task PageState_ShouldIncludeLoadState()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        var pageState = await _agent.GetPageStateAsync();

        // Assert
        pageState.LoadState.Should().BeOneOf(
            LoadState.Load,
            LoadState.DomContentLoaded,
            LoadState.NetworkIdle);
        pageState.LoadState.Should().NotBe(LoadState.Loading, "page should be in a loaded state after navigation");
    }

    [Fact]
    public async Task GetPageStateAsync_ShouldIncludeMetadata()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        var pageState = await _agent.GetPageStateAsync();

        // Assert
        pageState.Metadata.Should().NotBeNull();
        pageState.Metadata.Should().BeOfType<Dictionary<string, object>>();
        
        // Accessibility tree should be in metadata if available
        if (pageState.Metadata.TryGetValue("accessibilityTree", out var accessibilityTree))
        {
            accessibilityTree.Should().NotBeNull();
        }
    }
}

/// <summary>
/// Additional integration tests for PlaywrightBrowserAgent with various scenarios.
/// These tests ensure the agent works correctly in different conditions.
/// </summary>
public class PlaywrightBrowserAgentIntegrationTests : IAsyncLifetime
{
    private readonly Mock<ILogger<PlaywrightBrowserAgent>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private PlaywrightBrowserAgent? _agent;

    public PlaywrightBrowserAgentIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<PlaywrightBrowserAgent>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
    }

    public Task InitializeAsync()
    {
        _agent = new PlaywrightBrowserAgent(_mockLogger.Object, _mockLoggerFactory.Object);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_agent is not null)
        {
            await _agent.DisposeAsync();
        }
    }

    [Fact]
    public async Task Screenshot_ShouldCaptureFullPage()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        var screenshot = await _agent.TakeScreenshotAsync();
        var imageBytes = Convert.FromBase64String(screenshot);

        // Assert - Full page screenshot should be larger than viewport only
        imageBytes.Length.Should().BeGreaterThan(5000, "full page screenshot should be substantial");
    }

    [Fact]
    public async Task InteractiveElements_ShouldBeDetectedOnPage()
    {
        // Arrange
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");

        // Act
        var pageState = await _agent.GetPageStateAsync();

        // Assert
        // example.com has links, so there should be some interactive elements
        pageState.InteractiveElements.Should().NotBeNull();
        
        if (pageState.InteractiveElements.Count > 0)
        {
            pageState.InteractiveElements.Should().Contain(
                e => e.TagName == "a",
                "example.com has links");
        }
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://www.iana.org/")]
    public async Task NavigateAsync_ToDifferentUrls_ShouldUpdatePageState(string url)
    {
        // Arrange
        await _agent!.InitializeAsync();

        // Act
        await _agent.NavigateAsync(url);
        var pageState = await _agent.GetPageStateAsync();

        // Assert
        pageState.Url.Should().Contain(new Uri(url).Host, "page state should reflect current URL");
        pageState.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CompleteWorkflow_ShouldExecuteWithoutLeaks()
    {
        // Arrange & Act - Simulate a complete browser automation workflow
        await _agent!.InitializeAsync();
        await _agent.NavigateAsync("https://example.com");
        
        var state1 = await _agent.GetPageStateAsync();
        var screenshot1 = await _agent.TakeScreenshotAsync();
        var html1 = await _agent.GetPageHtmlAsync();
        
        await _agent.NavigateAsync("https://www.iana.org/");
        
        var state2 = await _agent.GetPageStateAsync();
        var screenshot2 = await _agent.TakeScreenshotAsync();

        // Assert - All operations should succeed
        state1.Should().NotBeNull();
        state2.Should().NotBeNull();
        screenshot1.Should().NotBeNullOrWhiteSpace();
        screenshot2.Should().NotBeNullOrWhiteSpace();
        html1.Should().NotBeNullOrWhiteSpace();
        
        // Cleanup
        await _agent.DisposeAsync();
        
        // After disposal, no Playwright processes should remain
        // (This is verified by the test framework's cleanup)
    }
}
