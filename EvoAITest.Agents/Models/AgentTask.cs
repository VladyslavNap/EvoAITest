namespace EvoAITest.Agents.Models;

/// <summary>
/// Represents a task to be executed by an AI agent.
/// </summary>
public sealed class AgentTask
{
    /// <summary>
    /// Gets or sets the unique task identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the task description in natural language.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the starting URL for the task.
    /// </summary>
    public string? StartUrl { get; set; }

    /// <summary>
    /// Gets or sets the task type.
    /// </summary>
    public TaskType Type { get; set; }

    /// <summary>
    /// Gets or sets input parameters for the task.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets constraints on task execution.
    /// </summary>
    public TaskConstraints? Constraints { get; set; }

    /// <summary>
    /// Gets or sets the expected outputs or success criteria.
    /// </summary>
    public TaskExpectations? Expectations { get; set; }

    /// <summary>
    /// Gets or sets the task priority.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds.
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 300000; // 5 minutes

    /// <summary>
    /// Gets or sets the task status.
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// Gets or sets metadata about the task.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when execution started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Defines task types.
/// </summary>
public enum TaskType
{
    /// <summary>Navigate to a page.</summary>
    Navigation,
    
    /// <summary>Extract data from a page.</summary>
    DataExtraction,
    
    /// <summary>Fill and submit a form.</summary>
    FormSubmission,
    
    /// <summary>Perform authentication.</summary>
    Authentication,
    
    /// <summary>Search for something.</summary>
    Search,
    
    /// <summary>Click through a workflow.</summary>
    Workflow,
    
    /// <summary>Monitor for changes.</summary>
    Monitoring,
    
    /// <summary>Custom task type.</summary>
    Custom
}

/// <summary>
/// Defines task priorities.
/// </summary>
public enum TaskPriority
{
    /// <summary>Low priority.</summary>
    Low,
    
    /// <summary>Normal priority.</summary>
    Normal,
    
    /// <summary>High priority.</summary>
    High,
    
    /// <summary>Critical priority.</summary>
    Critical
}

/// <summary>
/// Defines task execution status.
/// </summary>
public enum TaskStatus
{
    /// <summary>Task is pending execution.</summary>
    Pending,
    
    /// <summary>Task is currently being planned.</summary>
    Planning,
    
    /// <summary>Task is currently executing.</summary>
    Executing,
    
    /// <summary>Task execution is paused.</summary>
    Paused,
    
    /// <summary>Task completed successfully.</summary>
    Completed,
    
    /// <summary>Task failed.</summary>
    Failed,
    
    /// <summary>Task was cancelled.</summary>
    Cancelled
}

/// <summary>
/// Represents constraints on task execution.
/// </summary>
public sealed class TaskConstraints
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum number of steps allowed.
    /// </summary>
    public int? MaxSteps { get; set; }

    /// <summary>
    /// Gets or sets allowed domains for navigation.
    /// </summary>
    public List<string> AllowedDomains { get; set; } = new();

    /// <summary>
    /// Gets or sets blocked domains.
    /// </summary>
    public List<string> BlockedDomains { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to allow external links.
    /// </summary>
    public bool AllowExternalLinks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable healing.
    /// </summary>
    public bool EnableHealing { get; set; } = true;
}

/// <summary>
/// Represents expected outcomes for a task.
/// </summary>
public sealed class TaskExpectations
{
    /// <summary>
    /// Gets or sets the expected final URL pattern.
    /// </summary>
    public string? ExpectedUrl { get; set; }

    /// <summary>
    /// Gets or sets expected data fields to extract.
    /// </summary>
    public List<string> ExpectedDataFields { get; set; } = new();

    /// <summary>
    /// Gets or sets elements that should be present on success.
    /// </summary>
    public List<string> ExpectedElements { get; set; } = new();

    /// <summary>
    /// Gets or sets success indicators (text, elements, etc.).
    /// </summary>
    public Dictionary<string, object> SuccessIndicators { get; set; } = new();
}
