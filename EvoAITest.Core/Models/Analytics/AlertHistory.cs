namespace EvoAITest.Core.Models.Analytics;

/// <summary>
/// Represents an alert trigger event
/// </summary>
public sealed class AlertHistory
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Associated alert rule ID
    /// </summary>
    public Guid AlertRuleId { get; set; }

    /// <summary>
    /// Alert rule navigation property
    /// </summary>
    public AlertRule? AlertRule { get; set; }

    /// <summary>
    /// When this alert was triggered
    /// </summary>
    public DateTimeOffset TriggeredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Current value of the monitored metric
    /// </summary>
    public double ActualValue { get; set; }

    /// <summary>
    /// Expected threshold value
    /// </summary>
    public double ThresholdValue { get; set; }

    /// <summary>
    /// Alert severity at time of trigger
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Alert message sent
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Channels notified (JSON array: ["email", "slack"])
    /// </summary>
    public required string ChannelsNotified { get; set; }

    /// <summary>
    /// Whether this alert has been acknowledged
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// When this alert was acknowledged
    /// </summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }

    /// <summary>
    /// User who acknowledged this alert
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Notes added during acknowledgment
    /// </summary>
    public string? AcknowledgmentNotes { get; set; }

    /// <summary>
    /// Optional recording session ID if alert is scoped
    /// </summary>
    public Guid? RecordingSessionId { get; set; }

    /// <summary>
    /// Additional context data (JSON)
    /// </summary>
    public Dictionary<string, string> Context { get; set; } = [];

    /// <summary>
    /// Whether notification was successfully delivered
    /// </summary>
    public bool NotificationSuccess { get; set; }

    /// <summary>
    /// Error message if notification failed
    /// </summary>
    public string? NotificationError { get; set; }
}
