namespace EvoAITest.Agents.Models;

/// <summary>
/// Represents the chain-of-thought reasoning process used to create an execution plan.
/// Documents the step-by-step thinking that led to the final plan structure.
/// </summary>
/// <remarks>
/// <para>
/// Chain-of-thought reasoning captures the LLM's intermediate reasoning steps,
/// making the planning process transparent and debuggable. This is crucial for:
/// </para>
/// <list type="bullet">
/// <item><description>Debugging why certain steps were chosen</description></item>
/// <item><description>Understanding dependencies and ordering decisions</description></item>
/// <item><description>Auditing and compliance requirements</description></item>
/// <item><description>Improving plan quality through rationale review</description></item>
/// </list>
/// </remarks>
public sealed class ChainOfThought
{
    /// <summary>
    /// Gets or sets the unique identifier for this reasoning chain.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the task description that was analyzed.
    /// </summary>
    public required string TaskDescription { get; init; }

    /// <summary>
    /// Gets or sets the overall goal identified from the task.
    /// </summary>
    /// <remarks>
    /// This is the high-level objective the plan aims to achieve.
    /// Example: "Authenticate user and navigate to dashboard"
    /// </remarks>
    public required string IdentifiedGoal { get; init; }

    /// <summary>
    /// Gets or sets the list of key requirements extracted from the task.
    /// </summary>
    /// <remarks>
    /// Requirements are the constraints and expectations that must be satisfied.
    /// Examples: "Must handle 2FA", "Should verify successful login", "Must capture screenshots"
    /// </remarks>
    public List<string> KeyRequirements { get; init; } = new();

    /// <summary>
    /// Gets or sets the reasoning steps that led to the final plan.
    /// </summary>
    /// <remarks>
    /// Each reasoning step documents a thought or decision in the planning process.
    /// Steps should be ordered chronologically to show the flow of reasoning.
    /// </remarks>
    public List<ReasoningStep> ReasoningSteps { get; init; } = new();

    /// <summary>
    /// Gets or sets potential risks or challenges identified during planning.
    /// </summary>
    /// <remarks>
    /// Risks help anticipate failures and inform error handling strategies.
    /// Examples: "Element selectors may change", "Network delays possible"
    /// </remarks>
    public List<string> IdentifiedRisks { get; init; } = new();

    /// <summary>
    /// Gets or sets mitigation strategies for identified risks.
    /// </summary>
    /// <remarks>
    /// Mitigations explain how the plan addresses potential risks.
    /// Examples: "Use multiple selector strategies", "Add retry logic"
    /// </remarks>
    public List<string> MitigationStrategies { get; init; } = new();

    /// <summary>
    /// Gets or sets dependencies between steps identified during planning.
    /// </summary>
    /// <remarks>
    /// Dependencies explain why certain steps must come before others.
    /// Format: "Step X must complete before Step Y because..."
    /// </remarks>
    public List<StepDependency> StepDependencies { get; init; } = new();

    /// <summary>
    /// Gets or sets alternative approaches considered but not chosen.
    /// </summary>
    /// <remarks>
    /// Documenting alternatives shows the decision-making process and
    /// provides fallback options if the primary approach fails.
    /// </remarks>
    public List<Alternative> AlternativesConsidered { get; init; } = new();

    /// <summary>
    /// Gets or sets the confidence level in this reasoning (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// Lower confidence may indicate ambiguous requirements or novel scenarios.
    /// Should align with the overall plan confidence score.
    /// </remarks>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this reasoning was generated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata about the reasoning process.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Represents a single step in the chain-of-thought reasoning process.
/// </summary>
public sealed class ReasoningStep
{
    /// <summary>
    /// Gets or sets the sequence number of this reasoning step.
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Gets or sets the thought or consideration at this step.
    /// </summary>
    /// <remarks>
    /// This should be a natural language explanation of what the planner
    /// is thinking or deciding at this point in the reasoning process.
    /// Example: "First, we need to navigate to the login page to access the form."
    /// </remarks>
    public required string Thought { get; init; }

    /// <summary>
    /// Gets or sets the conclusion or decision made at this step.
    /// </summary>
    /// <remarks>
    /// The conclusion should clearly state what action or step was decided upon.
    /// Example: "Therefore, add a 'navigate' action to the login URL."
    /// </remarks>
    public required string Conclusion { get; init; }

    /// <summary>
    /// Gets or sets the reasoning category for this step.
    /// </summary>
    public ReasoningCategory Category { get; init; } = ReasoningCategory.General;

    /// <summary>
    /// Gets or sets the execution plan step number this reasoning led to (if applicable).
    /// </summary>
    public int? ResultingPlanStepNumber { get; set; }
}

/// <summary>
/// Defines categories of reasoning to classify thought processes.
/// </summary>
public enum ReasoningCategory
{
    /// <summary>General reasoning or observation.</summary>
    General,

    /// <summary>Identifying requirements from the task description.</summary>
    RequirementAnalysis,

    /// <summary>Determining step ordering and sequencing.</summary>
    StepOrdering,

    /// <summary>Analyzing dependencies between steps.</summary>
    DependencyAnalysis,

    /// <summary>Considering error handling and edge cases.</summary>
    ErrorHandling,

    /// <summary>Evaluating alternative approaches.</summary>
    AlternativeEvaluation,

    /// <summary>Validating the plan against requirements.</summary>
    Validation,

    /// <summary>Risk identification and mitigation.</summary>
    RiskAnalysis
}

/// <summary>
/// Represents a dependency relationship between steps.
/// </summary>
public sealed class StepDependency
{
    /// <summary>
    /// Gets or sets the step number that depends on another step.
    /// </summary>
    public required int DependentStepNumber { get; init; }

    /// <summary>
    /// Gets or sets the step number that must complete first.
    /// </summary>
    public required int RequiredStepNumber { get; init; }

    /// <summary>
    /// Gets or sets the reason for this dependency.
    /// </summary>
    /// <remarks>
    /// Example: "Step 2 depends on Step 1 because we need the authentication token from login."
    /// </remarks>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets or sets the dependency type.
    /// </summary>
    public DependencyType Type { get; init; } = DependencyType.Sequential;
}

/// <summary>
/// Defines types of dependencies between steps.
/// </summary>
public enum DependencyType
{
    /// <summary>Steps must execute in strict order (A then B).</summary>
    Sequential,

    /// <summary>Step B requires data/output from Step A.</summary>
    DataFlow,

    /// <summary>Step B requires Step A's state change (e.g., page load).</summary>
    StateChange,

    /// <summary>Step B should only execute if Step A succeeds.</summary>
    Conditional
}

/// <summary>
/// Represents an alternative approach that was considered but not chosen.
/// </summary>
public sealed class Alternative
{
    /// <summary>
    /// Gets or sets a description of the alternative approach.
    /// </summary>
    /// <remarks>
    /// Example: "Use XPath selectors instead of CSS selectors"
    /// </remarks>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or sets the reason this alternative was not chosen.
    /// </summary>
    /// <remarks>
    /// Example: "CSS selectors are more maintainable and readable"
    /// </remarks>
    public required string ReasonNotChosen { get; init; }

    /// <summary>
    /// Gets or sets the pros of this alternative.
    /// </summary>
    public List<string> Pros { get; init; } = new();

    /// <summary>
    /// Gets or sets the cons of this alternative.
    /// </summary>
    public List<string> Cons { get; init; } = new();
}

/// <summary>
/// Represents a visual graph of the execution plan for analysis and debugging.
/// </summary>
/// <remarks>
/// <para>
/// Plan graphs provide a visual representation of step relationships and dependencies.
/// Useful for:
/// </para>
/// <list type="bullet">
/// <item><description>Understanding complex execution flows</description></item>
/// <item><description>Identifying bottlenecks and parallelization opportunities</description></item>
/// <item><description>Debugging dependency issues</description></item>
/// <item><description>Documentation and communication</description></item>
/// </list>
/// </remarks>
public sealed class PlanGraph
{
    /// <summary>
    /// Gets or sets the unique identifier for this graph.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the plan ID this graph represents.
    /// </summary>
    public required string PlanId { get; init; }

    /// <summary>
    /// Gets or sets the graph nodes (steps).
    /// </summary>
    public List<GraphNode> Nodes { get; init; } = new();

    /// <summary>
    /// Gets or sets the graph edges (relationships between steps).
    /// </summary>
    public List<GraphEdge> Edges { get; init; } = new();

    /// <summary>
    /// Gets or sets the graph format (JSON, DOT, Mermaid, etc.).
    /// </summary>
    public GraphFormat Format { get; set; } = GraphFormat.Json;

    /// <summary>
    /// Gets or sets the serialized graph content.
    /// </summary>
    /// <remarks>
    /// Format depends on the <see cref="Format"/> property.
    /// Can be JSON, Graphviz DOT, Mermaid, or PlantUML syntax.
    /// </remarks>
    public required string Content { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this graph was generated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a node in the plan graph.
/// </summary>
public sealed class GraphNode
{
    /// <summary>
    /// Gets or sets the unique identifier for this node.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the step number this node represents.
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Gets or sets the action type (navigate, click, type, etc.).
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Gets or sets a short label for this node.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets or sets the node style (for visualization).
    /// </summary>
    public NodeStyle Style { get; init; } = new();
}

/// <summary>
/// Represents an edge connecting two nodes in the plan graph.
/// </summary>
public sealed class GraphEdge
{
    /// <summary>
    /// Gets or sets the source node ID.
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// Gets or sets the target node ID.
    /// </summary>
    public required string TargetId { get; init; }

    /// <summary>
    /// Gets or sets the relationship type.
    /// </summary>
    public required string RelationType { get; init; }

    /// <summary>
    /// Gets or sets an optional label for the edge.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets or sets the edge style (for visualization).
    /// </summary>
    public EdgeStyle Style { get; init; } = new();
}

/// <summary>
/// Styling information for graph nodes.
/// </summary>
public sealed class NodeStyle
{
    /// <summary>Gets or sets the node shape.</summary>
    public string Shape { get; set; } = "box";

    /// <summary>Gets or sets the node color.</summary>
    public string Color { get; set; } = "#4A90E2";

    /// <summary>Gets or sets the border color.</summary>
    public string BorderColor { get; set; } = "#2E5C8A";

    /// <summary>Gets or sets the text color.</summary>
    public string TextColor { get; set; } = "#FFFFFF";
}

/// <summary>
/// Styling information for graph edges.
/// </summary>
public sealed class EdgeStyle
{
    /// <summary>Gets or sets the edge color.</summary>
    public string Color { get; set; } = "#666666";

    /// <summary>Gets or sets the edge style (solid, dashed, dotted).</summary>
    public string LineStyle { get; set; } = "solid";

    /// <summary>Gets or sets the arrow type.</summary>
    public string ArrowType { get; set; } = "normal";
}

/// <summary>
/// Defines supported graph formats for visualization.
/// </summary>
public enum GraphFormat
{
    /// <summary>JSON format with nodes and edges.</summary>
    Json,

    /// <summary>Graphviz DOT language.</summary>
    GraphvizDot,

    /// <summary>Mermaid diagram syntax.</summary>
    Mermaid,

    /// <summary>PlantUML syntax.</summary>
    PlantUml,

    /// <summary>D3.js compatible JSON.</summary>
    D3Json
}
