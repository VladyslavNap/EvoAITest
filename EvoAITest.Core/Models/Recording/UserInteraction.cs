namespace EvoAITest.Core.Models.Recording;

/// <summary>
/// Represents a single user interaction captured during a recording session
/// </summary>
public sealed class UserInteraction
{
    /// <summary>
    /// Unique identifier for this interaction
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// ID of the recording session this interaction belongs to
    /// </summary>
    public required Guid SessionId { get; init; }
    
    /// <summary>
    /// Sequence number within the recording session
    /// </summary>
    public required int SequenceNumber { get; init; }
    
    /// <summary>
    /// Type of action performed
    /// </summary>
    public required ActionType ActionType { get; init; }
    
    /// <summary>
    /// Detected intent of the action (AI-generated)
    /// </summary>
    public ActionIntent Intent { get; set; } = ActionIntent.Unknown;
    
    /// <summary>
    /// Natural language description of the action (AI-generated)
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Duration of the action in milliseconds (if applicable)
    /// </summary>
    public int? DurationMs { get; init; }
    
    /// <summary>
    /// Input value (for text input actions)
    /// </summary>
    public string? InputValue { get; init; }
    
    /// <summary>
    /// Key pressed (for keyboard actions)
    /// </summary>
    public string? Key { get; init; }
    
    /// <summary>
    /// Mouse coordinates (X, Y) relative to element
    /// </summary>
    public (int X, int Y)? Coordinates { get; init; }
    
    /// <summary>
    /// Context information about the page and element
    /// </summary>
    public required ActionContext Context { get; init; }
    
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
    /// Assertions associated with this action
    /// </summary>
    public List<ActionAssertion> Assertions { get; set; } = [];
}

/// <summary>
/// Represents an assertion to be generated for an action
/// </summary>
public sealed class ActionAssertion
{
    /// <summary>
    /// Type of assertion
    /// </summary>
    public required AssertionType Type { get; init; }
    
    /// <summary>
    /// Expected value or condition
    /// </summary>
    public required string ExpectedValue { get; init; }
    
    /// <summary>
    /// Selector or property being asserted
    /// </summary>
    public string? Target { get; init; }
    
    /// <summary>
    /// Natural language description of the assertion
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Generated assertion code
    /// </summary>
    public string? Code { get; init; }
}

/// <summary>
/// Types of assertions that can be generated
/// </summary>
public enum AssertionType
{
    ElementExists,
    ElementNotExists,
    ElementVisible,
    ElementHidden,
    ElementEnabled,
    ElementDisabled,
    TextEquals,
    TextContains,
    ValueEquals,
    AttributeEquals,
    UrlEquals,
    UrlContains,
    TitleEquals,
    CountEquals,
    VisualMatch,
    CustomCondition
}
