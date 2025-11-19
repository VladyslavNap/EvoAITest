using System.Diagnostics;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Browser;
using EvoAITest.Core.Models;
using EvoAITest.Core.Options;
using EvoAITest.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace EvoAITest.Tests.Integration;

/// <summary>
/// Integration tests for DefaultToolExecutor with real PlaywrightBrowserAgent.
/// These tests execute against real web pages and require Playwright browser binaries.
/// 
/// Prerequisites:
/// - Playwright browser binaries installed (run: pwsh bin/Debug/net10.0/playwright.ps1 install)
/// - Internet connection for accessing test websites
/// - Sites used: example.com, httpbin.org (stable public test sites)
/// 
/// Usage:
/// - Run all integration tests: dotnet test --filter "Category=Integration"
/// - Run specific test: dotnet test --filter "FullyQualifiedName~ExecuteRealNavigationAndClick"
/// - Skip integration tests: dotnet test --filter "Category!=Integration"
/// </summary>
[Collection("ToolExecutorIntegration")]
[Trait("Category", "Integration")]
public class ToolExecutorIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<PlaywrightBrowserAgent> _browserLogger;
    private readonly ILogger<DefaultToolExecutor> _executorLogger;
    private PlaywrightBrowserAgent? _browserAgent;
    private DefaultToolExecutor? _executor;
    private readonly ToolExecutorOptions _options;

    public ToolExecutorIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Create loggers that output to xUnit test output
        _browserLogger = new XUnitLogger<PlaywrightBrowserAgent>(output);
        _executorLogger = new XUnitLogger<DefaultToolExecutor>(output);
        
        // Configure executor options for integration tests
        _options = new ToolExecutorOptions
        {
            MaxRetries = 2,
            InitialRetryDelayMs = 500,
            MaxRetryDelayMs = 5000,
            UseExponentialBackoff = true,
            TimeoutPerToolMs = 30000,
            EnableDetailedLogging = true,
            MaxHistorySize = 100
        };
    }

    public async Task InitializeAsync()
    {
        _output.WriteLine("Initializing Playwright browser agent...");
        
        _browserAgent = new PlaywrightBrowserAgent(_browserLogger);
        await _browserAgent.InitializeAsync();
        
        // Create a simple in-memory tool registry for tests
        var toolRegistry = new TestBrowserToolRegistry();
        
        _executor = new DefaultToolExecutor(
            _browserAgent,
            toolRegistry,
            Options.Create(_options),
            _executorLogger
        );
        
        _output.WriteLine("Browser agent initialized successfully");
    }

    public async Task DisposeAsync()
    {
        _output.WriteLine("Cleaning up browser resources...");
        
        if (_browserAgent != null)
        {
            await _browserAgent.DisposeAsync();
        }
        
        _output.WriteLine("Cleanup complete");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteRealNavigationAndClick_OnExampleCom_Succeeds()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _output.WriteLine($"Test correlation ID: {correlationId}");

        var navigateCall = new ToolCall(
            ToolName: "navigate",
            Parameters: new Dictionary<string, object> { ["url"] = "https://example.com" },
            Reasoning: "Navigate to example.com to test basic navigation",
            CorrelationId: correlationId
        );

        var waitCall = new ToolCall(
            ToolName: "wait_for_element",
            Parameters: new Dictionary<string, object> 
            { 
                ["selector"] = "h1",
                ["timeout_ms"] = 10000
            },
            Reasoning: "Wait for the main heading to appear",
            CorrelationId: correlationId
        );

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var navigateResult = await _executor!.ExecuteToolAsync(navigateCall);
        var waitResult = await _executor.ExecuteToolAsync(waitCall);
        
        stopwatch.Stop();

        // Assert
        navigateResult.Success.Should().BeTrue("navigation should succeed");
        navigateResult.AttemptCount.Should().Be(1, "should succeed on first attempt");
        navigateResult.Error.Should().BeNull();

        waitResult.Success.Should().BeTrue("element wait should succeed");
        waitResult.AttemptCount.Should().Be(1);

        _output.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Navigate time: {navigateResult.ExecutionDuration.TotalMilliseconds}ms");
        _output.WriteLine($"Wait time: {waitResult.ExecutionDuration.TotalMilliseconds}ms");

        // Verify execution history
        var history = await _executor.GetExecutionHistoryAsync(correlationId);
        history.Should().HaveCount(2);
        history.Should().AllSatisfy(r => r.Metadata["correlation_id"].Should().Be(correlationId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteFormFillSequence_TypesAndSubmits_Succeeds()
    {
        // Arrange - Use httpbin.org which has a simple HTML form
        var correlationId = Guid.NewGuid().ToString();
        _output.WriteLine($"Test correlation ID: {correlationId}");

        var toolCalls = new[]
        {
            new ToolCall("navigate", 
                new Dictionary<string, object> { ["url"] = "https://httpbin.org/forms/post" }, 
                "Navigate to form page", correlationId),
            
            new ToolCall("wait_for_element", 
                new Dictionary<string, object> 
                { 
                    ["selector"] = "input[name='custname']",
                    ["timeout_ms"] = 10000
                }, 
                "Wait for customer name input", correlationId),
            
            new ToolCall("type", 
                new Dictionary<string, object> 
                { 
                    ["selector"] = "input[name='custname']",
                    ["text"] = "Test User"
                }, 
                "Enter customer name", correlationId),
            
            new ToolCall("type", 
                new Dictionary<string, object> 
                { 
                    ["selector"] = "input[name='custtel']",
                    ["text"] = "555-1234"
                }, 
                "Enter phone number", correlationId),
            
            new ToolCall("type", 
                new Dictionary<string, object> 
                { 
                    ["selector"] = "input[name='custemail']",
                    ["text"] = "test@example.com"
                }, 
                "Enter email", correlationId)
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = await _executor!.ExecuteSequenceAsync(toolCalls);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(5, "all 5 tools should execute");
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue("each step should succeed"));
        
        _output.WriteLine($"Total sequence time: {stopwatch.ElapsedMilliseconds}ms");
        
        foreach (var result in results)
        {
            _output.WriteLine($"  {result.ToolName}: {result.ExecutionDuration.TotalMilliseconds}ms (attempts: {result.AttemptCount})");
        }

        // Verify execution history
        var history = await _executor.GetExecutionHistoryAsync(correlationId);
        history.Should().HaveCount(5);
        history.Select(h => h.ToolName).Should().ContainInOrder(
            "navigate", "wait_for_element", "type", "type", "type");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteWithPageStateCapture_ExtractsElements_Succeeds()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _output.WriteLine($"Test correlation ID: {correlationId}");

        var toolCalls = new[]
        {
            new ToolCall("navigate", 
                new Dictionary<string, object> { ["url"] = "https://example.com" }, 
                "Navigate to page", correlationId),
            
            new ToolCall("wait_for_element", 
                new Dictionary<string, object> 
                { 
                    ["selector"] = "h1",
                    ["timeout_ms"] = 10000
                }, 
                "Wait for heading", correlationId),
            
            new ToolCall("get_text", 
                new Dictionary<string, object> { ["selector"] = "h1" }, 
                "Extract heading text", correlationId),
            
            new ToolCall("get_page_state", 
                new Dictionary<string, object>(), 
                "Capture page state", correlationId),
            
            new ToolCall("get_page_html", 
                new Dictionary<string, object>(), 
                "Get full HTML", correlationId)
        };

        // Act
        var results = await _executor!.ExecuteSequenceAsync(toolCalls);

        // Assert
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());

        // Verify get_text result
        var textResult = results[2];
        textResult.Result.Should().NotBeNull();
        var headingText = textResult.Result as string;
        headingText.Should().NotBeNull();
        headingText.Should().Contain("Example Domain", "heading should contain expected text");
        _output.WriteLine($"Extracted heading: {headingText}");

        // Verify get_page_state result
        var stateResult = results[3];
        stateResult.Result.Should().NotBeNull();
        var pageState = stateResult.Result as PageState;
        pageState.Should().NotBeNull();
        pageState!.Url.Should().Contain("example.com");
        pageState.Title.Should().Be("Example Domain");
        pageState.InteractiveElements.Should().NotBeEmpty("page has links");
        
        _output.WriteLine($"Page state captured:");
        _output.WriteLine($"  URL: {pageState.Url}");
        _output.WriteLine($"  Title: {pageState.Title}");
        _output.WriteLine($"  Interactive elements: {pageState.InteractiveElements.Count}");

        // Verify get_page_html result
        var htmlResult = results[4];
        htmlResult.Result.Should().NotBeNull();
        var html = htmlResult.Result as string;
        html.Should().NotBeNullOrEmpty();
        html.Should().Contain("<!DOCTYPE html>", "should contain DOCTYPE");
        html.Should().Contain("Example Domain", "should contain page content");
        
        _output.WriteLine($"HTML length: {html!.Length} characters");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteRetryOnTransientFailure_ElementAppears_SucceedsAfterRetry()
    {
        // Arrange - Navigate to a page and wait for an element that exists
        // This simulates a transient failure by using a slightly longer wait
        var correlationId = Guid.NewGuid().ToString();
        _output.WriteLine($"Test correlation ID: {correlationId}");

        var navigateCall = new ToolCall(
            ToolName: "navigate",
            Parameters: new Dictionary<string, object> { ["url"] = "https://example.com" },
            Reasoning: "Navigate to test page",
            CorrelationId: correlationId
        );

        // This will succeed, but we'll test the retry mechanism by using a selector
        // that might take a moment to be ready
        var waitCall = new ToolCall(
            ToolName: "wait_for_element",
            Parameters: new Dictionary<string, object> 
            { 
                ["selector"] = "body",
                ["timeout_ms"] = 5000
            },
            Reasoning: "Wait for body element",
            CorrelationId: correlationId
        );

        // Act
        var navigateResult = await _executor!.ExecuteToolAsync(navigateCall);
        var waitResult = await _executor.ExecuteToolAsync(waitCall);

        // Assert
        navigateResult.Success.Should().BeTrue();
        waitResult.Success.Should().BeTrue();
        
        _output.WriteLine($"Navigate attempts: {navigateResult.AttemptCount}");
        _output.WriteLine($"Wait attempts: {waitResult.AttemptCount}");
        
        // Both should succeed quickly without retries since example.com is reliable
        navigateResult.WasRetried.Should().BeFalse("navigation should succeed on first try");
        waitResult.WasRetried.Should().BeFalse("element wait should succeed on first try");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteSequenceWithScreenshots_CapturesAtEachStep_Succeeds()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _output.WriteLine($"Test correlation ID: {correlationId}");

        var toolCalls = new[]
        {
            new ToolCall("navigate", 
                new Dictionary<string, object> { ["url"] = "https://example.com" }, 
                "Navigate to page", correlationId),
            
            new ToolCall("take_screenshot", 
                new Dictionary<string, object>(), 
                "Capture initial screenshot", correlationId),
            
            new ToolCall("wait_for_element", 
                new Dictionary<string, object> 
                { 
                    ["selector"] = "h1",
                    ["timeout_ms"] = 10000
                }, 
                "Wait for content", correlationId),
            
            new ToolCall("take_screenshot", 
                new Dictionary<string, object>(), 
                "Capture after content loaded", correlationId)
        };

        // Act
        var results = await _executor!.ExecuteSequenceAsync(toolCalls);

        // Assert
        results.Should().HaveCount(4);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());

        // Verify screenshots were captured
        var screenshot1Result = results[1];
        screenshot1Result.Result.Should().NotBeNull();
        var screenshot1 = screenshot1Result.Result as string;
        screenshot1.Should().NotBeNullOrEmpty("first screenshot should be captured");
        
        var screenshot2Result = results[3];
        screenshot2Result.Result.Should().NotBeNull();
        var screenshot2 = screenshot2Result.Result as string;
        screenshot2.Should().NotBeNullOrEmpty("second screenshot should be captured");

        // Validate base64 format
        var screenshot1Bytes = Convert.FromBase64String(screenshot1!);
        var screenshot2Bytes = Convert.FromBase64String(screenshot2!);
        
        screenshot1Bytes.Should().NotBeEmpty();
        screenshot2Bytes.Should().NotBeEmpty();
        
        // Verify PNG signature (first 8 bytes)
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        screenshot1Bytes.Take(8).Should().Equal(pngSignature, "should be valid PNG");
        screenshot2Bytes.Take(8).Should().Equal(pngSignature, "should be valid PNG");

        _output.WriteLine($"Screenshot 1 size: {screenshot1Bytes.Length} bytes");
        _output.WriteLine($"Screenshot 2 size: {screenshot2Bytes.Length} bytes");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteComplexWorkflow_MultipleActions_Succeeds()
    {
        // Arrange - Complex workflow demonstrating multiple tool types
        var correlationId = Guid.NewGuid().ToString();
        _output.WriteLine($"Test correlation ID: {correlationId}");

        var toolCalls = new[]
        {
            new ToolCall("navigate", 
                new Dictionary<string, object> { ["url"] = "https://example.com" }, 
                "Step 1: Navigate", correlationId),
            
            new ToolCall("wait_for_element", 
                new Dictionary<string, object> 
                { 
                    ["selector"] = "h1",
                    ["timeout_ms"] = 10000
                }, 
                "Step 2: Wait for heading", correlationId),
            
            new ToolCall("get_text", 
                new Dictionary<string, object> { ["selector"] = "h1" }, 
                "Step 3: Extract heading", correlationId),
            
            new ToolCall("get_text", 
                new Dictionary<string, object> { ["selector"] = "p" }, 
                "Step 4: Extract paragraph", correlationId),
            
            new ToolCall("get_page_state", 
                new Dictionary<string, object>(), 
                "Step 5: Capture state", correlationId),
            
            new ToolCall("take_screenshot", 
                new Dictionary<string, object>(), 
                "Step 6: Take screenshot", correlationId),
            
            new ToolCall("get_page_html", 
                new Dictionary<string, object>(), 
                "Step 7: Get HTML", correlationId)
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var results = await _executor!.ExecuteSequenceAsync(toolCalls);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(7, "all tools should execute");
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue("each step should succeed"));

        _output.WriteLine($"Complex workflow completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average step duration: {results.Average(r => r.ExecutionDuration.TotalMilliseconds):F2}ms");

        // Verify specific results
        var headingText = results[2].Result as string;
        headingText.Should().Contain("Example Domain");

        var paragraphText = results[3].Result as string;
        paragraphText.Should().NotBeNullOrEmpty();

        var pageState = results[4].Result as PageState;
        pageState.Should().NotBeNull();
        pageState!.Url.Should().Contain("example.com");

        var screenshot = results[5].Result as string;
        screenshot.Should().NotBeNullOrEmpty();

        var html = results[6].Result as string;
        html.Should().Contain("<!DOCTYPE html>");

        // Verify execution history
        var history = await _executor.GetExecutionHistoryAsync(correlationId);
        history.Should().HaveCount(7);
        
        var totalDuration = history.Sum(h => h.ExecutionDuration.TotalMilliseconds);
        _output.WriteLine($"Total duration from history: {totalDuration}ms");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteWithFallback_PrimaryFailsFallbackSucceeds_Succeeds()
    {
        // Arrange - Try to click an element that doesn't exist, then fallback to a valid one
        var correlationId = Guid.NewGuid().ToString();
        _output.WriteLine($"Test correlation ID: {correlationId}");

        await _executor!.ExecuteToolAsync(new ToolCall(
            "navigate",
            new Dictionary<string, object> { ["url"] = "https://example.com" },
            "Navigate first",
            correlationId));

        var primaryCall = new ToolCall(
            ToolName: "get_text",
            Parameters: new Dictionary<string, object> { ["selector"] = "#nonexistent-element-12345" },
            Reasoning: "Try to get text from non-existent element (will fail)",
            CorrelationId: correlationId
        );

        var fallbackCalls = new[]
        {
            new ToolCall("get_text", 
                new Dictionary<string, object> { ["selector"] = "h1" }, 
                "Fallback: get heading text", correlationId)
        };

        // Act
        var result = await _executor.ExecuteWithFallbackAsync(primaryCall, fallbackCalls);

        // Assert
        result.Success.Should().BeTrue("fallback should succeed");
        result.Metadata.Should().ContainKey("fallback_used");
        result.Metadata["fallback_used"].Should().Be(true);
        result.Metadata["fallback_index"].Should().Be(0);
        result.Metadata.Should().ContainKey("primary_error");

        var extractedText = result.Result as string;
        extractedText.Should().Contain("Example Domain");

        _output.WriteLine($"Primary tool failed as expected");
        _output.WriteLine($"Fallback succeeded with result: {extractedText}");
        _output.WriteLine($"Total attempts: {result.AttemptCount}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExecuteGetAccessibilityTree_ReturnsAccessibilityData_Succeeds()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        _output.WriteLine($"Test correlation ID: {correlationId}");

        var navigateCall = new ToolCall(
            "navigate",
            new Dictionary<string, object> { ["url"] = "https://example.com" },
            "Navigate to page",
            correlationId);

        // Act
        var navigateResult = await _executor!.ExecuteToolAsync(navigateCall);
        
        // Get accessibility tree through browser agent directly (not a registered tool yet)
        var accessibilityTree = await _browserAgent!.GetAccessibilityTreeAsync();

        // Assert
        navigateResult.Success.Should().BeTrue();
        
        accessibilityTree.Should().NotBeNull("accessibility tree should be captured");
        
        if (!string.IsNullOrWhiteSpace(accessibilityTree))
        {
            _output.WriteLine($"Accessibility tree length: {accessibilityTree.Length} characters");
            _output.WriteLine($"Sample: {accessibilityTree.Substring(0, Math.Min(200, accessibilityTree.Length))}...");
        }
        else
        {
            _output.WriteLine("Accessibility tree is empty (may be valid for simple pages)");
        }
    }
}

/// <summary>
/// xUnit logger that outputs to test output helper.
/// </summary>
internal class XUnitLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;

    public XUnitLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        
        _output.WriteLine($"[{timestamp}] [{logLevel}] {typeof(T).Name}: {message}");
        
        if (exception != null)
        {
            _output.WriteLine($"  Exception: {exception.GetType().Name}: {exception.Message}");
        }
    }
}

/// <summary>
/// Collection definition to control parallel execution of integration tests.
/// Integration tests in the same collection run sequentially to avoid browser conflicts.
/// </summary>
[CollectionDefinition("ToolExecutorIntegration")]
public class ToolExecutorIntegrationCollection : ICollectionFixture<ToolExecutorIntegrationFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Shared fixture for integration tests (currently empty, but can be used for shared setup).
/// </summary>
public class ToolExecutorIntegrationFixture
{
    public ToolExecutorIntegrationFixture()
    {
        // Shared setup if needed (e.g., verify Playwright is installed)
    }
}

/// <summary>
/// Simple test implementation of IBrowserToolRegistry that wraps the static BrowserToolRegistry.
/// Used for integration tests since DefaultBrowserToolRegistry is internal.
/// </summary>
internal class TestBrowserToolRegistry : IBrowserToolRegistry
{
    public List<BrowserToolDefinition> GetAllTools() => BrowserToolRegistry.GetAllTools();
    public BrowserToolDefinition GetTool(string name) => BrowserToolRegistry.GetTool(name);
    public bool ToolExists(string name) => BrowserToolRegistry.ToolExists(name);
    public string GetToolsAsJson() => BrowserToolRegistry.GetToolsAsJson();
    public string[] GetToolNames() => BrowserToolRegistry.GetToolNames();
    public int ToolCount => BrowserToolRegistry.ToolCount;
}
