using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Execution;
using EvoAITest.Core.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PlaywrightPage = Microsoft.Playwright.IPage;

namespace EvoAITest.Agents.Services.Execution;

/// <summary>
/// Service for executing generated test code
/// </summary>
public sealed class TestExecutorService : ITestExecutor
{
    private readonly ILogger<TestExecutorService> _logger;
    private readonly ITestResultCollector _resultCollector;
    private readonly IRecordingRepository _recordingRepository;
    private readonly ITestGenerator _testGenerator;

    private static readonly string[] SupportedFrameworks = { "xUnit", "NUnit", "MSTest" };

    public TestExecutorService(
        ILogger<TestExecutorService> logger,
        ITestResultCollector resultCollector,
        IRecordingRepository recordingRepository,
        ITestGenerator testGenerator)
    {
        _logger = logger;
        _resultCollector = resultCollector;
        _recordingRepository = recordingRepository;
        _testGenerator = testGenerator;
    }

    public async Task<TestExecutionResult> ExecuteTestAsync(
        string testCode,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new TestExecutionOptions();

        _logger.LogInformation("Starting test execution with framework {Framework}", options.TestFramework);

        // Create a temporary session for tracking
        var session = await _resultCollector.StartSessionAsync(
            Guid.Empty, // No associated recording
            new Dictionary<string, string>
            {
                ["Framework"] = options.TestFramework,
                ["Browser"] = options.Browser,
                ["Headless"] = options.Headless.ToString()
            },
            cancellationToken);

        session.TestCode = testCode;
        session.TestFramework = options.TestFramework;

        try
        {
            // Extract test name from code
            var testName = ExtractTestName(testCode) ?? "UnnamedTest";

            // Step 1: Validate compilation
            var validation = await ValidateTestAsync(testCode, cancellationToken);
            if (!validation.IsValid)
            {
                return await _resultCollector.CompleteSessionAsync(
                    session.Id,
                    TestExecutionStatus.CompilationError,
                    $"Compilation failed: {string.Join(", ", validation.Errors)}",
                    cancellationToken);
            }

            // Step 2: Execute the test using Playwright
            await ExecutePlaywrightTestAsync(session, testCode, options, cancellationToken);

            // Step 3: Complete session
            var result = await _resultCollector.CompleteSessionAsync(
                session.Id,
                session.Status,
                session.ErrorMessage,
                cancellationToken);

            result.TestName = testName;

            _logger.LogInformation(
                "Test execution completed with status {Status}",
                result.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test execution failed with exception");
            return await _resultCollector.CompleteSessionAsync(
                session.Id,
                TestExecutionStatus.Failed,
                ex.Message,
                cancellationToken);
        }
    }

    public async Task<TestExecutionResult> ExecuteFromRecordingAsync(
        Guid recordingSessionId,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing test from recording {RecordingId}", recordingSessionId);

        // Retrieve the recording session
        var recordingSession = await _recordingRepository.GetSessionByIdAsync(recordingSessionId, cancellationToken);
        if (recordingSession == null)
        {
            throw new InvalidOperationException($"Recording session {recordingSessionId} not found");
        }

        // Generate test code if not already generated
        string testCode;
        if (!string.IsNullOrEmpty(recordingSession.GeneratedTestCode))
        {
            testCode = recordingSession.GeneratedTestCode;
        }
        else
        {
            _logger.LogInformation("Generating test code for recording {RecordingId}", recordingSessionId);
            var generatedTest = await _testGenerator.GenerateTestAsync(recordingSession, null, cancellationToken);
            testCode = generatedTest.Code;
        }

        // Create execution session with recording association
        var session = await _resultCollector.StartSessionAsync(
            recordingSessionId,
            new Dictionary<string, string>
            {
                ["RecordingName"] = recordingSession.Name,
                ["Framework"] = options?.TestFramework ?? "xUnit"
            },
            cancellationToken);

        session.TestCode = testCode;
        session.TestFramework = options?.TestFramework ?? "xUnit";

        // Execute the test
        return await ExecuteTestAsync(testCode, options, cancellationToken);
    }

    public async Task<TestValidationResult> ValidateTestAsync(
        string testCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating test code");

        try
        {
            // Basic syntax validation
            if (string.IsNullOrWhiteSpace(testCode))
            {
                return new TestValidationResult
                {
                    IsValid = false,
                    Errors = ["Test code is empty"]
                };
            }

            var errors = new List<string>();
            var warnings = new List<string>();

            // Detect framework
            var detectedFramework = DetectFramework(testCode);
            if (detectedFramework == null)
            {
                warnings.Add("Unable to detect test framework");
            }

            // Count test methods
            var testMethodCount = CountTestMethods(testCode);
            if (testMethodCount == 0)
            {
                warnings.Add("No test methods found");
            }

            // Check for required Playwright setup
            if (!testCode.Contains("IPage") && !testCode.Contains("Page"))
            {
                warnings.Add("No Playwright Page object found");
            }

            // For now, we'll do basic validation
            // Full Roslyn compilation would require Microsoft.CodeAnalysis packages
            var isValid = !errors.Any();

            _logger.LogInformation(
                "Validation completed: {IsValid}, {ErrorCount} errors, {WarningCount} warnings",
                isValid,
                errors.Count,
                warnings.Count);

            return new TestValidationResult
            {
                IsValid = isValid,
                Errors = errors,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed with exception");
            return new TestValidationResult
            {
                IsValid = false,
                Errors = [$"Validation error: {ex.Message}"]
            };
        }
    }

    public async Task<IEnumerable<TestExecutionResult>> ExecuteBatchAsync(
        IEnumerable<string> testCodes,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing batch of {Count} tests", testCodes.Count());

        var results = new List<TestExecutionResult>();

        foreach (var testCode in testCodes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Batch execution cancelled");
                break;
            }

            try
            {
                var result = await ExecuteTestAsync(testCode, options, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute test in batch");
                results.Add(new TestExecutionResult
                {
                    TestName = "BatchTest",
                    TestFramework = options?.TestFramework ?? "xUnit",
                    Status = TestExecutionStatus.Failed,
                    ErrorMessage = ex.Message,
                    CompletedAt = DateTimeOffset.UtcNow,
                    RecordingSessionId = Guid.Empty
                });
            }
        }

        _logger.LogInformation(
            "Batch execution completed: {Total} tests, {Passed} passed, {Failed} failed",
            results.Count,
            results.Count(r => r.Status == TestExecutionStatus.Passed),
            results.Count(r => r.Status == TestExecutionStatus.Failed));

        return results;
    }

    public IEnumerable<string> GetSupportedFrameworks()
    {
        return SupportedFrameworks;
    }

    private async Task ExecutePlaywrightTestAsync(
        TestExecutionSession session,
        string testCode,
        TestExecutionOptions options,
        CancellationToken cancellationToken)
    {
        session.Status = TestExecutionStatus.Running;
        session.TotalSteps = CountTestSteps(testCode);
        session.CurrentStep = 0;

        using var playwright = await Playwright.CreateAsync();
        IBrowser? browser = null;

        try
        {
            // Launch browser
            var browserType = options.Browser.ToLowerInvariant() switch
            {
                "firefox" => playwright.Firefox,
                "webkit" => playwright.Webkit,
                _ => playwright.Chromium
            };

            browser = await browserType.LaunchAsync(new()
            {
                Headless = options.Headless
            });

            var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize
                {
                    Width = options.ViewportSize.Width,
                    Height = options.ViewportSize.Height
                }
            });

            var page = await context.NewPageAsync();

            // Parse and execute test steps
            var steps = ExtractTestSteps(testCode);
            
            foreach (var step in steps)
            {
                session.CurrentStep++;

                var stepResult = new TestStepResult
                {
                    StepNumber = session.CurrentStep,
                    Description = step.Description
                };

                try
                {
                    // Execute the step
                    await ExecuteStepAsync(page, step, options, cancellationToken);

                    stepResult.Status = TestExecutionStatus.Passed;
                    stepResult.CompletedAt = DateTimeOffset.UtcNow;

                    // Capture screenshot if enabled
                    if (options.CaptureScreenshots)
                    {
                        var screenshotPath = $"screenshots/step_{session.CurrentStep}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                        await page.ScreenshotAsync(new() { Path = screenshotPath });
                        stepResult.ScreenshotPath = screenshotPath;

                        await _resultCollector.AttachArtifactAsync(
                            session.Id,
                            "screenshot",
                            screenshotPath,
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Step {StepNumber} failed: {Description}", session.CurrentStep, step.Description);
                    
                    stepResult.Status = TestExecutionStatus.Failed;
                    stepResult.ErrorMessage = ex.Message;
                    stepResult.StackTrace = ex.StackTrace;
                    stepResult.CompletedAt = DateTimeOffset.UtcNow;

                    session.Status = TestExecutionStatus.Failed;
                    session.ErrorMessage = $"Step {session.CurrentStep} failed: {ex.Message}";

                    if (options.StopOnFailure)
                    {
                        await _resultCollector.RecordStepResultAsync(session.Id, stepResult, cancellationToken);
                        break;
                    }
                }

                await _resultCollector.RecordStepResultAsync(session.Id, stepResult, cancellationToken);
            }

            // Set final status if not already failed
            if (session.Status != TestExecutionStatus.Failed)
            {
                session.Status = TestExecutionStatus.Passed;
            }

            await context.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playwright execution failed");
            session.Status = TestExecutionStatus.Failed;
            session.ErrorMessage = ex.Message;
        }
        finally
        {
            if (browser != null)
            {
                await browser.CloseAsync();
            }
        }

        session.CompletedAt = DateTimeOffset.UtcNow;
    }

    private async Task ExecuteStepAsync(
        PlaywrightPage page,
        TestStep step,
        TestExecutionOptions options,
        CancellationToken cancellationToken)
    {
        switch (step.Type)
        {
            case "Navigate":
                await page.GotoAsync(step.Parameter);
                break;

            case "Click":
                await page.Locator(step.Parameter).ClickAsync();
                break;

            case "Fill":
                await page.Locator(step.Selector!).FillAsync(step.Parameter);
                break;

            case "Assert":
                await page.Locator(step.Parameter).WaitForAsync(new() { State = WaitForSelectorState.Visible });
                break;

            case "Wait":
                if (int.TryParse(step.Parameter, out var milliseconds))
                {
                    await Task.Delay(milliseconds, cancellationToken);
                }
                break;

            default:
                _logger.LogWarning("Unknown step type: {Type}", step.Type);
                break;
        }
    }

    private List<TestStep> ExtractTestSteps(string testCode)
    {
        var steps = new List<TestStep>();

        // Extract GotoAsync calls
        var gotoPattern = @"GotoAsync\(""([^""]+)""\)";
        foreach (Match match in Regex.Matches(testCode, gotoPattern))
        {
            steps.Add(new TestStep
            {
                Type = "Navigate",
                Description = $"Navigate to {match.Groups[1].Value}",
                Parameter = match.Groups[1].Value
            });
        }

        // Extract ClickAsync calls
        var clickPattern = @"Locator\(""([^""]+)""\)\.ClickAsync\(\)";
        foreach (Match match in Regex.Matches(testCode, clickPattern))
        {
            steps.Add(new TestStep
            {
                Type = "Click",
                Description = $"Click element: {match.Groups[1].Value}",
                Parameter = match.Groups[1].Value
            });
        }

        // Extract FillAsync calls
        var fillPattern = @"Locator\(""([^""]+)""\)\.FillAsync\(""([^""]+)""\)";
        foreach (Match match in Regex.Matches(testCode, fillPattern))
        {
            steps.Add(new TestStep
            {
                Type = "Fill",
                Description = $"Fill {match.Groups[1].Value} with value",
                Selector = match.Groups[1].Value,
                Parameter = match.Groups[2].Value
            });
        }

        // Extract assertions (ToBeVisibleAsync)
        var assertPattern = @"Expect\(_page!\.Locator\(""([^""]+)""\)\)\.ToBeVisibleAsync\(\)";
        foreach (Match match in Regex.Matches(testCode, assertPattern))
        {
            steps.Add(new TestStep
            {
                Type = "Assert",
                Description = $"Assert element visible: {match.Groups[1].Value}",
                Parameter = match.Groups[1].Value
            });
        }

        return steps;
    }

    private string? ExtractTestName(string testCode)
    {
        // Try to extract method name with [Fact], [Test], or [TestMethod] attribute
        var patterns = new[]
        {
            @"\[Fact\]\s+public\s+async\s+Task\s+(\w+)\(",
            @"\[Test\]\s+public\s+async\s+Task\s+(\w+)\(",
            @"\[TestMethod\]\s+public\s+async\s+Task\s+(\w+)\("
        };

        var testName = patterns
            .Select(pattern => Regex.Match(testCode, pattern))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .FirstOrDefault();

        return testName;
    }

    private string? DetectFramework(string testCode)
    {
        if (testCode.Contains("[Fact]") || testCode.Contains("using Xunit"))
            return "xUnit";
        if (testCode.Contains("[Test]") || testCode.Contains("using NUnit"))
            return "NUnit";
        if (testCode.Contains("[TestMethod]") || testCode.Contains("using Microsoft.VisualStudio.TestTools"))
            return "MSTest";

        return null;
    }

    private int CountTestMethods(string testCode)
    {
        var count = 0;
        count += Regex.Matches(testCode, @"\[Fact\]").Count;
        count += Regex.Matches(testCode, @"\[Test\]").Count;
        count += Regex.Matches(testCode, @"\[TestMethod\]").Count;
        return count;
    }

    private int CountTestSteps(string testCode)
    {
        // Count major operations
        var count = 0;
        count += Regex.Matches(testCode, @"GotoAsync").Count;
        count += Regex.Matches(testCode, @"ClickAsync").Count;
        count += Regex.Matches(testCode, @"FillAsync").Count;
        count += Regex.Matches(testCode, @"ToBeVisibleAsync").Count;
        return count;
    }

    private sealed class TestStep
    {
        public required string Type { get; init; }
        public required string Description { get; init; }
        public required string Parameter { get; init; }
        public string? Selector { get; init; }
    }
}
