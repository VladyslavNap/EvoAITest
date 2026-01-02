namespace EvoAITest.Core.Models.Recording;

/// <summary>
/// Represents the status of a recording session
/// </summary>
public enum RecordingStatus
{
    /// <summary>
    /// Recording is currently active
    /// </summary>
    Recording,
    
    /// <summary>
    /// Recording is paused
    /// </summary>
    Paused,
    
    /// <summary>
    /// Recording has been stopped
    /// </summary>
    Stopped,
    
    /// <summary>
    /// Recording failed or encountered an error
    /// </summary>
    Failed,
    
    /// <summary>
    /// Test has been generated from this recording
    /// </summary>
    Generated
}

/// <summary>
/// Represents a complete recording session of user interactions
/// </summary>
public sealed class RecordingSession
{
    /// <summary>
    /// Unique identifier for this recording session
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Name or title of the recording session
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Description of what is being tested
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Current status of the recording
    /// </summary>
    public RecordingStatus Status { get; set; } = RecordingStatus.Recording;
    
    /// <summary>
    /// When the recording started
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// When the recording was stopped or paused
    /// </summary>
    public DateTimeOffset? EndedAt { get; set; }
    
    /// <summary>
    /// Total duration of the recording in milliseconds
    /// </summary>
    public long DurationMs => EndedAt.HasValue 
        ? (long)(EndedAt.Value - StartedAt).TotalMilliseconds 
        : (long)(DateTimeOffset.UtcNow - StartedAt).TotalMilliseconds;
    
    /// <summary>
    /// Starting URL of the recording
    /// </summary>
    public required string StartUrl { get; init; }
    
    /// <summary>
    /// Browser used for recording
    /// </summary>
    public string Browser { get; init; } = "chromium";
    
    /// <summary>
    /// Viewport size used during recording
    /// </summary>
    public (int Width, int Height) ViewportSize { get; init; } = (1920, 1080);
    
    /// <summary>
    /// List of all recorded interactions
    /// </summary>
    public List<UserInteraction> Interactions { get; set; } = [];
    
    /// <summary>
    /// Generated test code
    /// </summary>
    public string? GeneratedTestCode { get; set; }
    
    /// <summary>
    /// Test framework to generate code for
    /// </summary>
    public string TestFramework { get; set; } = "xUnit";
    
    /// <summary>
    /// Programming language for generated tests
    /// </summary>
    public string Language { get; set; } = "C#";
    
    /// <summary>
    /// Configuration options used for this recording
    /// </summary>
    public RecordingConfiguration Configuration { get; init; } = new();
    
    /// <summary>
    /// Metrics and statistics about the recording
    /// </summary>
    public RecordingMetrics Metrics { get; set; } = new();
    
    /// <summary>
    /// Tags for categorizing recordings
    /// </summary>
    public List<string> Tags { get; set; } = [];
    
    /// <summary>
    /// User who created this recording
    /// </summary>
    public string? CreatedBy { get; init; }
}

/// <summary>
/// Configuration options for a recording session
/// </summary>
public sealed class RecordingConfiguration
{
    /// <summary>
    /// Whether to capture screenshots for each action
    /// </summary>
    public bool CaptureScreenshots { get; init; } = true;
    
    /// <summary>
    /// Whether to record network traffic
    /// </summary>
    public bool RecordNetwork { get; init; } = false;
    
    /// <summary>
    /// Whether to record console logs
    /// </summary>
    public bool RecordConsoleLogs { get; init; } = false;
    
    /// <summary>
    /// Minimum time between actions to be considered separate (ms)
    /// </summary>
    public int ActionDebounceMs { get; init; } = 100;
    
    /// <summary>
    /// Whether to ignore hover actions
    /// </summary>
    public bool IgnoreHover { get; init; } = true;
    
    /// <summary>
    /// Whether to automatically generate assertions
    /// </summary>
    public bool AutoGenerateAssertions { get; init; } = true;
    
    /// <summary>
    /// Maximum number of actions to record (0 = unlimited)
    /// </summary>
    public int MaxActions { get; init; } = 0;
    
    /// <summary>
    /// Whether to use AI for intent detection
    /// </summary>
    public bool UseAiIntentDetection { get; init; } = true;
}

/// <summary>
/// Metrics and statistics about a recording session
/// </summary>
public sealed class RecordingMetrics
{
    /// <summary>
    /// Total number of actions recorded
    /// </summary>
    public int TotalActions { get; set; }
    
    /// <summary>
    /// Number of unique pages visited
    /// </summary>
    public int PagesVisited { get; set; }
    
    /// <summary>
    /// Number of form submissions
    /// </summary>
    public int FormSubmissions { get; set; }
    
    /// <summary>
    /// Number of assertions generated
    /// </summary>
    public int AssertionsGenerated { get; set; }
    
    /// <summary>
    /// Average confidence score of intent detection
    /// </summary>
    public double AverageIntentConfidence { get; set; }
    
    /// <summary>
    /// Action recognition accuracy (0.0 - 1.0)
    /// </summary>
    public double ActionRecognitionAccuracy { get; set; }
    
    /// <summary>
    /// Test generation success rate
    /// </summary>
    public double GenerationSuccessRate { get; set; }
}
