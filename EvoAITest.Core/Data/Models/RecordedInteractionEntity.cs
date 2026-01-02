namespace EvoAITest.Core.Data.Models;

/// <summary>
/// Database entity for recorded user interactions
/// </summary>
public sealed class RecordedInteractionEntity
{
    /// <summary>
    /// Unique identifier for this interaction
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID of the recording session this interaction belongs to
    /// </summary>
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// Sequence number within the recording session
    /// </summary>
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// Type of action performed (Click, Input, Navigation, etc.)
    /// </summary>
    public required string ActionType { get; set; }
    
    /// <summary>
    /// Detected intent of the action (AI-generated)
    /// </summary>
    public string Intent { get; set; } = "Unknown";
    
    /// <summary>
    /// Natural language description of the action (AI-generated)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Duration of the action in milliseconds (if applicable)
    /// </summary>
    public int? DurationMs { get; set; }
    
    /// <summary>
    /// Input value (for text input actions)
    /// </summary>
    public string? InputValue { get; set; }
    
    /// <summary>
    /// Key pressed (for keyboard actions)
    /// </summary>
    public string? Key { get; set; }
    
    /// <summary>
    /// Mouse X coordinate
    /// </summary>
    public int? CoordinateX { get; set; }
    
    /// <summary>
    /// Mouse Y coordinate
    /// </summary>
    public int? CoordinateY { get; set; }
    
    /// <summary>
    /// Context information (JSON serialized ActionContext)
    /// </summary>
    public required string ContextJson { get; set; }
    
    /// <summary>
    /// Whether this action should be included in generated tests
    /// </summary>
    public bool IncludeInTest { get; set; } = true;
    
    /// <summary>
    /// Confidence score of the intent detection (0.0 - 1.0)
    /// </summary>
    public double IntentConfidence { get; set; }
    
    /// <summary>
    /// Generated test code for this action
    /// </summary>
    public string? GeneratedCode { get; set; }
    
    /// <summary>
    /// Assertions associated with this action (JSON serialized)
    /// </summary>
    public string? AssertionsJson { get; set; }
    
    /// <summary>
    /// Navigation property to recording session
    /// </summary>
    public RecordingSessionEntity? Session { get; set; }
}
