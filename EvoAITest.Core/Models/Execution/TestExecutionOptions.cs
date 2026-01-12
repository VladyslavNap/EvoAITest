namespace EvoAITest.Core.Models.Execution;

/// <summary>
/// Options for test execution
/// </summary>
public sealed class TestExecutionOptions
{
    /// <summary>
    /// Test framework to use (xUnit, NUnit, MSTest)
    /// </summary>
    public string TestFramework { get; set; } = "xUnit";

    /// <summary>
    /// Maximum execution time in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to capture screenshots during execution
    /// </summary>
    public bool CaptureScreenshots { get; set; } = true;

    /// <summary>
    /// Whether to capture video of execution
    /// </summary>
    public bool CaptureVideo { get; set; } = false;

    /// <summary>
    /// Whether to capture browser console logs
    /// </summary>
    public bool CaptureConsoleLogs { get; set; } = true;

    /// <summary>
    /// Whether to capture network traffic
    /// </summary>
    public bool CaptureNetworkTraffic { get; set; } = false;

    /// <summary>
    /// Browser to use for execution
    /// </summary>
    public string Browser { get; set; } = "chromium";

    /// <summary>
    /// Whether to run in headless mode
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Browser viewport size
    /// </summary>
    public (int Width, int Height) ViewportSize { get; set; } = (1920, 1080);

    /// <summary>
    /// Additional environment variables for test execution
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// Additional metadata to attach to the execution
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Whether to stop on first failure
    /// </summary>
    public bool StopOnFailure { get; set; } = false;

    /// <summary>
    /// Maximum number of retries on failure
    /// </summary>
    public int MaxRetries { get; set; } = 0;
}
