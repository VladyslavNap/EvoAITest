namespace EvoAITest.Core.Data.Models;

/// <summary>
/// Database entity for recording sessions
/// </summary>
public sealed class RecordingSessionEntity
{
    /// <summary>
    /// Unique identifier for the recording session
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Name or title of the recording session
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Description of what is being tested
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Current status of the recording (Recording, Paused, Stopped, Failed, Generated)
    /// </summary>
    public required string Status { get; set; }
    
    /// <summary>
    /// When the recording started
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }
    
    /// <summary>
    /// When the recording was stopped or paused
    /// </summary>
    public DateTimeOffset? EndedAt { get; set; }
    
    /// <summary>
    /// Starting URL of the recording
    /// </summary>
    public required string StartUrl { get; set; }
    
    /// <summary>
    /// Browser used for recording
    /// </summary>
    public string Browser { get; set; } = "chromium";
    
    /// <summary>
    /// Viewport width
    /// </summary>
    public int ViewportWidth { get; set; }
    
    /// <summary>
    /// Viewport height
    /// </summary>
    public int ViewportHeight { get; set; }
    
    /// <summary>
    /// Generated test code (JSON serialized)
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
    /// Configuration options (JSON serialized)
    /// </summary>
    public required string ConfigurationJson { get; set; }
    
    /// <summary>
    /// Metrics and statistics (JSON serialized)
    /// </summary>
    public required string MetricsJson { get; set; }
    
    /// <summary>
    /// Tags for categorizing recordings (JSON array)
    /// </summary>
    public string? TagsJson { get; set; }
    
    /// <summary>
    /// User who created this recording
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Navigation collection to recorded interactions
    /// </summary>
    public ICollection<RecordedInteractionEntity> Interactions { get; set; } = new List<RecordedInteractionEntity>();
}
