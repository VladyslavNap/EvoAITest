namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for the recording feature
/// </summary>
public sealed class RecordingOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Recording";
    
    /// <summary>
    /// Whether recording feature is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maximum duration for a recording session in minutes (0 = unlimited)
    /// </summary>
    public int MaxRecordingDurationMinutes { get; set; } = 120;
    
    /// <summary>
    /// Maximum number of actions per recording session (0 = unlimited)
    /// </summary>
    public int MaxActionsPerSession { get; set; } = 1000;
    
    /// <summary>
    /// Whether to automatically save recordings
    /// </summary>
    public bool AutoSave { get; set; } = true;
    
    /// <summary>
    /// Interval for auto-saving recordings in seconds
    /// </summary>
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Whether to capture screenshots for each action
    /// </summary>
    public bool CaptureScreenshots { get; set; } = true;
    
    /// <summary>
    /// Maximum screenshot size in KB
    /// </summary>
    public int MaxScreenshotSizeKb { get; set; } = 500;
    
    /// <summary>
    /// Screenshot quality (0-100)
    /// </summary>
    public int ScreenshotQuality { get; set; } = 80;
    
    /// <summary>
    /// Whether to use AI for action analysis
    /// </summary>
    public bool UseAiAnalysis { get; set; } = true;
    
    /// <summary>
    /// Minimum confidence threshold for AI predictions (0.0-1.0)
    /// </summary>
    public double MinimumConfidenceThreshold { get; set; } = 0.7;
    
    /// <summary>
    /// Target accuracy percentage for action recognition (0-100)
    /// </summary>
    public double TargetAccuracyPercentage { get; set; } = 90.0;
    
    /// <summary>
    /// Whether to automatically generate assertions
    /// </summary>
    public bool AutoGenerateAssertions { get; set; } = true;
    
    /// <summary>
    /// Default test framework for code generation
    /// </summary>
    public string DefaultTestFramework { get; set; } = "xUnit";
    
    /// <summary>
    /// Default programming language for generated tests
    /// </summary>
    public string DefaultLanguage { get; set; } = "C#";
    
    /// <summary>
    /// Default automation library
    /// </summary>
    public string DefaultAutomationLibrary { get; set; } = "Playwright";
    
    /// <summary>
    /// Storage path for recording data
    /// </summary>
    public string StoragePath { get; set; } = "./recordings";
    
    /// <summary>
    /// Whether to compress recording data
    /// </summary>
    public bool CompressRecordings { get; set; } = true;
    
    /// <summary>
    /// Number of days to retain recordings (0 = indefinite)
    /// </summary>
    public int RetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Actions to ignore during recording
    /// </summary>
    public List<string> IgnoredActions { get; set; } = ["Hover"];
    
    /// <summary>
    /// CSS selectors to ignore during recording
    /// </summary>
    public List<string> IgnoredSelectors { get; set; } = 
    [
        "[data-recording-ignore]",
        ".recording-ignore"
    ];
    
    /// <summary>
    /// Debounce time for rapid actions in milliseconds
    /// </summary>
    public int ActionDebounceMs { get; set; } = 100;
    
    /// <summary>
    /// Whether to record network traffic
    /// </summary>
    public bool RecordNetworkTraffic { get; set; } = false;
    
    /// <summary>
    /// Whether to record console logs
    /// </summary>
    public bool RecordConsoleLogs { get; set; } = false;
    
    /// <summary>
    /// LLM model to use for analysis
    /// </summary>
    public string AnalysisModel { get; set; } = "gpt-4";
    
    /// <summary>
    /// LLM model to use for test generation
    /// </summary>
    public string GenerationModel { get; set; } = "gpt-4";
    
    /// <summary>
    /// Temperature for LLM generation (0.0-2.0)
    /// </summary>
    public double LlmTemperature { get; set; } = 0.3;
    
    /// <summary>
    /// Maximum tokens for LLM responses
    /// </summary>
    public int MaxLlmTokens { get; set; } = 4000;
}
