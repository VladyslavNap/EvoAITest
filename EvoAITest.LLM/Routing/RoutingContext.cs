namespace EvoAITest.LLM.Routing;

/// <summary>
/// Defines the context for routing LLM requests to appropriate providers.
/// Contains information about the request type, complexity, and requirements.
/// </summary>
public sealed class RoutingContext
{
    /// <summary>
    /// Gets or sets the type of task being performed.
    /// </summary>
    /// <remarks>
    /// This helps the router select the most appropriate model for the task.
    /// For example, planning tasks may use GPT-5, while code generation uses Qwen.
    /// </remarks>
    public TaskType TaskType { get; set; } = TaskType.General;

    /// <summary>
    /// Gets or sets the estimated complexity of the request.
    /// </summary>
    /// <remarks>
    /// Complex tasks may be routed to more capable (but expensive) models,
    /// while simple tasks can use faster, cheaper models.
    /// </remarks>
    public ComplexityLevel Complexity { get; set; } = ComplexityLevel.Medium;

    /// <summary>
    /// Gets or sets whether streaming is required for this request.
    /// </summary>
    /// <remarks>
    /// Some providers or models may not support streaming.
    /// The router will consider this when selecting a provider.
    /// </remarks>
    public bool RequiresStreaming { get; set; }

    /// <summary>
    /// Gets or sets whether function calling/tool use is required.
    /// </summary>
    /// <remarks>
    /// Not all models support function calling. This flag ensures
    /// the router selects a compatible provider.
    /// </remarks>
    public bool RequiresFunctionCalling { get; set; }

    /// <summary>
    /// Gets or sets the maximum acceptable latency in milliseconds.
    /// </summary>
    /// <remarks>
    /// For real-time scenarios, the router may prefer faster local models
    /// over remote API calls, even if quality is slightly lower.
    /// </remarks>
    public int? MaxLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the preferred model identifier.
    /// </summary>
    /// <remarks>
    /// If specified, the router will attempt to use this model.
    /// Falls back to routing rules if the preferred model is unavailable.
    /// </remarks>
    public string? PreferredModel { get; set; }

    /// <summary>
    /// Gets or sets whether to allow fallback to alternative providers.
    /// </summary>
    /// <remarks>
    /// When true, the router will automatically fall back to secondary providers
    /// if the primary provider fails or is unavailable.
    /// </remarks>
    public bool AllowFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata for routing decisions.
    /// </summary>
    /// <remarks>
    /// Custom metadata can be used by routing strategies to make
    /// context-aware routing decisions.
    /// </remarks>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the priority of this request.
    /// </summary>
    /// <remarks>
    /// High-priority requests may be routed to premium providers
    /// with guaranteed availability and lower latency.
    /// </remarks>
    public RequestPriority Priority { get; set; } = RequestPriority.Normal;
}

/// <summary>
/// Defines task types for routing decisions.
/// </summary>
public enum TaskType
{
    /// <summary>General-purpose task with no specific requirements.</summary>
    General,
    
    /// <summary>Browser automation planning task.</summary>
    Planning,
    
    /// <summary>Code generation or analysis task.</summary>
    CodeGeneration,
    
    /// <summary>Natural language understanding or question answering.</summary>
    Understanding,
    
    /// <summary>Data extraction or summarization task.</summary>
    Extraction,
    
    /// <summary>Error analysis and healing suggestions.</summary>
    Healing,
    
    /// <summary>Complex reasoning or multi-step problem solving.</summary>
    Reasoning
}

/// <summary>
/// Defines complexity levels for routing decisions.
/// </summary>
public enum ComplexityLevel
{
    /// <summary>Simple task requiring minimal reasoning.</summary>
    Low,
    
    /// <summary>Moderate complexity requiring standard model capabilities.</summary>
    Medium,
    
    /// <summary>High complexity requiring advanced reasoning.</summary>
    High,
    
    /// <summary>Expert-level task requiring most capable model.</summary>
    Expert
}

/// <summary>
/// Defines request priority levels for routing decisions.
/// </summary>
public enum RequestPriority
{
    /// <summary>Low priority, can tolerate delays and use budget models.</summary>
    Low,
    
    /// <summary>Normal priority for standard operations.</summary>
    Normal,
    
    /// <summary>High priority requiring fast, reliable providers.</summary>
    High,
    
    /// <summary>Critical priority requiring guaranteed availability.</summary>
    Critical
}
