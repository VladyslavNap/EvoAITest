namespace EvoAITest.LLM.Models;

/// <summary>
/// Defines the type of task for LLM routing.
/// Used to classify requests and route them to appropriate models.
/// </summary>
/// <remarks>
/// Task types help optimize costs and performance by routing different types of work
/// to models that are best suited for each task. For example:
/// - Planning tasks ? High-quality models (GPT-5, Claude)
/// - Code generation ? Code-optimized models (Qwen, CodeLlama)
/// - Simple tasks ? Fast, local models
/// </remarks>
public enum TaskType
{
    /// <summary>
    /// General purpose task with no specific optimization.
    /// Used when task type cannot be determined or doesn't match other categories.
    /// </summary>
    /// <remarks>
    /// Routes to default provider, typically a balanced general-purpose model.
    /// Examples: General questions, multi-purpose requests
    /// </remarks>
    General = 0,

    /// <summary>
    /// Planning and strategy tasks.
    /// Requires complex reasoning, step-by-step thinking, and high-quality output.
    /// </summary>
    /// <remarks>
    /// Best suited for: GPT-5, GPT-4, Claude Sonnet
    /// Characteristics: Complex, requires accuracy, can tolerate higher latency
    /// Examples: 
    /// - Creating automation plans
    /// - Breaking down complex workflows
    /// - Strategic decision making
    /// </remarks>
    Planning = 1,

    /// <summary>
    /// Code generation tasks.
    /// Requires understanding of programming languages and code structure.
    /// </summary>
    /// <remarks>
    /// Best suited for: Qwen 2.5, CodeLlama, StarCoder
    /// Characteristics: Code-focused, requires syntax accuracy, benefits from specialized models
    /// Examples:
    /// - Generating test code
    /// - Writing automation scripts
    /// - Creating API client code
    /// </remarks>
    CodeGeneration = 2,

    /// <summary>
    /// Analysis and interpretation tasks.
    /// Requires understanding context, patterns, and extracting insights.
    /// </summary>
    /// <remarks>
    /// Best suited for: GPT-4, Claude, Qwen
    /// Characteristics: Analytical, pattern recognition, contextual understanding
    /// Examples:
    /// - Analyzing recorded browser interactions
    /// - Extracting patterns from data
    /// - Understanding user intent
    /// </remarks>
    Analysis = 3,

    /// <summary>
    /// Intent detection tasks.
    /// Determines what the user is trying to accomplish.
    /// </summary>
    /// <remarks>
    /// Best suited for: GPT-4, Claude
    /// Characteristics: Requires contextual understanding, relatively fast
    /// Examples:
    /// - Detecting user intent from actions
    /// - Classifying interaction types
    /// - Understanding goals
    /// </remarks>
    IntentDetection = 4,

    /// <summary>
    /// Validation and verification tasks.
    /// Checks if something is correct or meets requirements.
    /// </summary>
    /// <remarks>
    /// Best suited for: GPT-3.5, Llama, fast local models
    /// Characteristics: Fast, straightforward, less complex
    /// Examples:
    /// - Validating test outputs
    /// - Checking assertions
    /// - Verifying data formats
    /// </remarks>
    Validation = 5,

    /// <summary>
    /// Summarization tasks.
    /// Condensing longer content into shorter form.
    /// </summary>
    /// <remarks>
    /// Best suited for: GPT-3.5, Qwen, fast models
    /// Characteristics: Extractive/abstractive, relatively fast
    /// Examples:
    /// - Summarizing test results
    /// - Creating execution reports
    /// - Condensing logs
    /// </remarks>
    Summarization = 6,

    /// <summary>
    /// Translation tasks.
    /// Converting content between languages or formats.
    /// </summary>
    /// <remarks>
    /// Best suited for: Specialized translation models, GPT-4
    /// Characteristics: Language-specific, accuracy important
    /// Examples:
    /// - Translating UI text
    /// - Converting between data formats
    /// - Localizing content
    /// </remarks>
    Translation = 7,

    /// <summary>
    /// Classification tasks.
    /// Categorizing content into predefined classes.
    /// </summary>
    /// <remarks>
    /// Best suited for: Fast models, GPT-3.5, Llama
    /// Characteristics: Fast, deterministic, low latency critical
    /// Examples:
    /// - Classifying error types
    /// - Categorizing user actions
    /// - Tagging content
    /// </remarks>
    Classification = 8,

    /// <summary>
    /// Long-form content generation.
    /// Creating extensive, detailed output.
    /// </summary>
    /// <remarks>
    /// Best suited for: GPT-4, Claude (with large context)
    /// Characteristics: High token count, requires consistency, can be slow
    /// Examples:
    /// - Generating comprehensive test suites
    /// - Creating detailed documentation
    /// - Writing extensive reports
    /// </remarks>
    LongFormGeneration = 9
}

/// <summary>
/// Extension methods for TaskType enum.
/// </summary>
public static class TaskTypeExtensions
{
    /// <summary>
    /// Gets a human-readable description of the task type.
    /// </summary>
    /// <param name="taskType">The task type.</param>
    /// <returns>A descriptive string.</returns>
    public static string GetDescription(this TaskType taskType) => taskType switch
    {
        TaskType.General => "General purpose task",
        TaskType.Planning => "Planning and strategy",
        TaskType.CodeGeneration => "Code generation",
        TaskType.Analysis => "Analysis and interpretation",
        TaskType.IntentDetection => "Intent detection",
        TaskType.Validation => "Validation and verification",
        TaskType.Summarization => "Summarization",
        TaskType.Translation => "Translation",
        TaskType.Classification => "Classification",
        TaskType.LongFormGeneration => "Long-form content generation",
        _ => "Unknown task type"
    };

    /// <summary>
    /// Determines if a task type typically requires high-quality models.
    /// </summary>
    /// <param name="taskType">The task type.</param>
    /// <returns>True if high quality is typically needed.</returns>
    public static bool RequiresHighQuality(this TaskType taskType) => taskType switch
    {
        TaskType.Planning => true,
        TaskType.Analysis => true,
        TaskType.IntentDetection => true,
        TaskType.LongFormGeneration => true,
        _ => false
    };

    /// <summary>
    /// Determines if a task type can benefit from specialized code models.
    /// </summary>
    /// <param name="taskType">The task type.</param>
    /// <returns>True if code models are beneficial.</returns>
    public static bool BenefitsFromCodeModels(this TaskType taskType) => taskType switch
    {
        TaskType.CodeGeneration => true,
        _ => false
    };

    /// <summary>
    /// Determines if a task type prioritizes speed over quality.
    /// </summary>
    /// <param name="taskType">The task type.</param>
    /// <returns>True if speed is prioritized.</returns>
    public static bool PrioritizesSpeed(this TaskType taskType) => taskType switch
    {
        TaskType.Validation => true,
        TaskType.Classification => true,
        _ => false
    };

    /// <summary>
    /// Gets typical expected token count for this task type.
    /// </summary>
    /// <param name="taskType">The task type.</param>
    /// <returns>Typical token count estimate.</returns>
    public static int GetTypicalTokenCount(this TaskType taskType) => taskType switch
    {
        TaskType.Validation => 500,
        TaskType.Classification => 500,
        TaskType.Summarization => 1000,
        TaskType.IntentDetection => 1000,
        TaskType.CodeGeneration => 2000,
        TaskType.Analysis => 2000,
        TaskType.Planning => 3000,
        TaskType.Translation => 2000,
        TaskType.LongFormGeneration => 5000,
        TaskType.General => 1500,
        _ => 1500
    };
}
