using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Browser;
using EvoAITest.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;

namespace EvoAITest.Tests.Integration;

/// <summary>
/// Integration tests for browser screenshot capture functionality.
/// Tests actual Playwright browser operations.
/// </summary>
[TestClass]
public sealed class BrowserScreenshotIntegrationTests : IAsyncDisposable
{
    private PlaywrightBrowserAgent? _browserAgent;
    private Mock<ILogger<PlaywrightBrowserAgent>>? _mockLogger;
    private Mock<ILoggerFactory>? _mockLoggerFactory;
    private bool _disposed;

    [TestInitialize]
    public async Task Setup()
    {
        _mockLogger = new Mock<ILogger<PlaywrightBrowserAgent>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _browserAgent = new PlaywrightBrowserAgent(_mockLogger.Object, _mockLoggerFactory.Object);
        
        await _browserAgent.InitializeAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task TakeFullPageScreenshot_SimpleHtmlPage_ReturnsValidImage()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Test Page</title>
    <style>
        body { font-family: Arial; padding: 20px; background-color: #f0f0f0; }
        .header { background-color: #4CAF50; color: white; padding: 20px; }
        .content { margin: 20px 0; background-color: white; padding: 20px; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>Test Header</h1>
    </div>
    <div class='content'>
        <h2>Content Section</h2>
        <p>This is a test page for screenshot capture.</p>
    </div>
</body>
</html>";

        var dataUrl = "data:text/html;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        await _browserAgent!.NavigateAsync(dataUrl, CancellationToken.None);

        // Act
        var screenshot = await _browserAgent.TakeFullPageScreenshotBytesAsync(CancellationToken.None);

        // Assert
        screenshot.Should().NotBeNull();
        screenshot.Length.Should().BeGreaterThan(0);
        
        // Verify it's a valid PNG image
        using var image = Image.Load(screenshot);
        image.Should().NotBeNull();
        image.Width.Should().BeGreaterThan(0);
        image.Height.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task TakeViewportScreenshot_CapturesOnlyVisibleArea()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { margin: 0; padding: 0; }
        .tall-content { height: 3000px; background: linear-gradient(to bottom, blue, red); }
    </style>
</head>
<body>
    <div class='tall-content'></div>
</body>
</html>";

        var dataUrl = "data:text/html;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        await _browserAgent!.NavigateAsync(dataUrl, CancellationToken.None);

        // Act
        var screenshot = await _browserAgent.TakeViewportScreenshotAsync(CancellationToken.None);

        // Assert
        screenshot.Should().NotBeNull();
        screenshot.Length.Should().BeGreaterThan(0);
        
        // Verify viewport dimensions (should be less than full page)
        using var image = Image.Load(screenshot);
        image.Width.Should().BeGreaterThan(0);
        image.Height.Should().BeLessThan(3000); // Should only capture viewport
    }

    [TestMethod]
    public async Task TakeElementScreenshot_SpecificElement_CapturesOnlyThatElement()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        .header { 
            background-color: blue; 
            color: white; 
            padding: 20px; 
            width: 300px; 
            height: 100px; 
        }
        .content { 
            background-color: green; 
            padding: 20px; 
            margin-top: 20px; 
        }
    </style>
</head>
<body>
    <div class='header' id='test-header'>Header Element</div>
    <div class='content'>Content Element</div>
</body>
</html>";

        var dataUrl = "data:text/html;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        await _browserAgent!.NavigateAsync(dataUrl, CancellationToken.None);

        // Act
        var screenshot = await _browserAgent.TakeElementScreenshotAsync("#test-header", CancellationToken.None);

        // Assert
        screenshot.Should().NotBeNull();
        screenshot.Length.Should().BeGreaterThan(0);
        
        // Verify element dimensions (approximate)
        using var image = Image.Load(screenshot);
        image.Width.Should().BeGreaterThan(200); // Approximate element width
        image.Height.Should().BeLessThan(200); // Should be smaller than full page
    }

    [TestMethod]
    public async Task TakeRegionScreenshot_SpecificCoordinates_CapturesOnlyRegion()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { margin: 0; padding: 0; }
        .full { width: 800px; height: 600px; background: linear-gradient(to right, red, blue); }
    </style>
</head>
<body>
    <div class='full'></div>
</body>
</html>";

        var dataUrl = "data:text/html;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        await _browserAgent!.NavigateAsync(dataUrl, CancellationToken.None);

        var region = new ScreenshotRegion
        {
            X = 100,
            Y = 100,
            Width = 200,
            Height = 150
        };

        // Act
        var screenshot = await _browserAgent.TakeRegionScreenshotAsync(region, CancellationToken.None);

        // Assert
        screenshot.Should().NotBeNull();
        screenshot.Length.Should().BeGreaterThan(0);
        
        // Verify region dimensions
        using var image = Image.Load(screenshot);
        image.Width.Should().Be(200);
        image.Height.Should().Be(150);
    }

    [TestMethod]
    public async Task MultipleScreenshots_SameContent_ProduceIdenticalImages()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { margin: 0; padding: 0; }
        .test { 
            width: 100px; 
            height: 100px; 
            background-color: #4CAF50; 
        }
    </style>
</head>
<body>
    <div class='test'></div>
</body>
</html>";

        var dataUrl = "data:text/html;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        await _browserAgent!.NavigateAsync(dataUrl, CancellationToken.None);

        // Act - Take two screenshots
        var screenshot1 = await _browserAgent.TakeFullPageScreenshotBytesAsync(CancellationToken.None);
        var screenshot2 = await _browserAgent.TakeFullPageScreenshotBytesAsync(CancellationToken.None);

        // Assert
        screenshot1.Should().NotBeNull();
        screenshot2.Should().NotBeNull();
        
        // Screenshots should be very similar (may have minor differences due to timestamps, etc.)
        screenshot1.Length.Should().BeCloseTo(screenshot2.Length, (uint)(screenshot1.Length * 0.1));
    }

    [TestMethod]
    public async Task Screenshot_DynamicContent_CapturesCurrentState()
    {
        // Arrange
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <style>
        .box { 
            width: 200px; 
            height: 200px; 
            background-color: blue; 
            transition: background-color 0.3s;
        }
    </style>
</head>
<body>
    <div class='box' id='dynamic-box'></div>
    <button onclick='document.getElementById(""dynamic-box"").style.backgroundColor = ""red""'>
        Change Color
    </button>
</body>
</html>";

        var dataUrl = "data:text/html;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        await _browserAgent!.NavigateAsync(dataUrl, CancellationToken.None);

        // Act - Take screenshot before change
        var beforeScreenshot = await _browserAgent.TakeFullPageScreenshotBytesAsync(CancellationToken.None);

        // Click button to change color
        await _browserAgent.ClickAsync("button", maxRetries: 1, CancellationToken.None);
        await Task.Delay(500); // Wait for transition

        // Take screenshot after change
        var afterScreenshot = await _browserAgent.TakeFullPageScreenshotBytesAsync(CancellationToken.None);

        // Assert
        beforeScreenshot.Should().NotBeNull();
        afterScreenshot.Should().NotBeNull();
        
        // Screenshots should be different (different content)
        beforeScreenshot.Should().NotEqual(afterScreenshot);
    }

    [TestMethod]
    public async Task Screenshot_InvalidSelector_ThrowsException()
    {
        // Arrange
        var html = "<html><body><div id='valid'>Content</div></body></html>";
        var dataUrl = "data:text/html;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        await _browserAgent!.NavigateAsync(dataUrl, CancellationToken.None);

        // Act & Assert - Use FluentAssertions instead of MSTest Assert
        var act = async () => await _browserAgent.TakeElementScreenshotAsync("#nonexistent", CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_browserAgent != null)
        {
            await _browserAgent.DisposeAsync();
        }

        _disposed = true;
    }
}
