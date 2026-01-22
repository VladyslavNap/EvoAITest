namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Represents an alert rule for monitoring analytics metrics
/// </summary>
public sealed class AlertRule
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// User-friendly name for the alert
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Metric being monitored (passRate, flakinessScore, avgDuration, etc.)
    /// </summary>
    public required string Metric { get; set; }

    /// <summary>
    /// Comparison operator
    /// </summary>
    public AlertOperator Operator { get; set; }

    /// <summary>
    /// Threshold value for comparison
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Alert severity level
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Whether this alert is currently active
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional recording session ID to scope the alert
    /// </summary>
    public Guid? RecordingSessionId { get; set; }

    /// <summary>
    /// Minimum time between alerts (throttling) in minutes
    /// </summary>
    public int ThrottleMinutes { get; set; } = 60;

    /// <summary>
    /// Notification channels (JSON array: ["email", "slack", "webhook"])
    /// </summary>
    public required string Channels { get; set; }

    /// <summary>
    /// Alert recipients (JSON object with channel-specific data)
    /// </summary>
    public required string Recipients { get; set; }

    /// <summary>
    /// When this alert was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this alert was last modified
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who created this alert
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When this alert was last triggered
    /// </summary>
    public DateTimeOffset? LastTriggeredAt { get; set; }

    /// <summary>
    /// Number of times this alert has been triggered
    /// </summary>
    public int TriggerCount { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
    
    /// <summary>
    /// Whether this record is flaky
    /// </summary>
    public bool IsFlaky => Severity >= AlertSeverity.Warning;
}

/// <summary>
/// Alert comparison operators
/// </summary>
public enum AlertOperator
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Equals,
    NotEquals,
    Between,       // Requires two threshold values
    Outside        // Requires two threshold values
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
