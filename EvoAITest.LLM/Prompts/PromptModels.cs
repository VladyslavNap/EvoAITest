namespace EvoAITest.LLM.Prompts;

/// <summary>
/// Represents a complete prompt with all its components.
/// </summary>
public sealed class Prompt
{
    /// <summary>
    /// Gets or sets the unique identifier for this prompt.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the prompt version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the system instruction component.
    /// </summary>
    public PromptComponent SystemInstruction { get; set; } = new();

    /// <summary>
    /// Gets or sets the context component.
    /// </summary>
    public PromptComponent Context { get; set; } = new();

    /// <summary>
    /// Gets or sets the user instruction component.
    /// </summary>
    public PromptComponent UserInstruction { get; set; } = new();

    /// <summary>
    /// Gets or sets the tool definitions component.
    /// </summary>
    public PromptComponent? ToolDefinitions { get; set; }

    /// <summary>
    /// Gets or sets the examples component.
    /// </summary>
    public PromptComponent? Examples { get; set; }

    /// <summary>
    /// Gets or sets the output format instructions.
    /// </summary>
    public PromptComponent? OutputFormat { get; set; }

    /// <summary>
    /// Gets or sets the template variables.
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Gets or sets custom metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets whether injection protection is enabled.
    /// </summary>
    public bool InjectionProtectionEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when the prompt was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents a component of a prompt (instruction, context, tools, etc.).
/// </summary>
public sealed class PromptComponent
{
    /// <summary>
    /// Gets or sets the raw template content with placeholders.
    /// </summary>
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this component requires sanitization.
    /// </summary>
    public bool RequiresSanitization { get; set; } = true;

    /// <summary>
    /// Gets or sets the component priority for ordering.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets component-specific metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a template for building prompts.
/// </summary>
public sealed class PromptTemplate
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the template content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the required variables for this template.
    /// </summary>
    public List<string> RequiredVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets optional variables with default values.
    /// </summary>
    public Dictionary<string, string> DefaultValues { get; set; } = new();

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets template tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Represents system instructions for different scenarios.
/// </summary>
public sealed class SystemInstruction
{
    /// <summary>
    /// Gets or sets the instruction key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the instruction version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the instruction content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scenario this instruction applies to.
    /// </summary>
    public string Scenario { get; set; } = "default";

    /// <summary>
    /// Gets or sets the model compatibility (e.g., "gpt-4", "ollama").
    /// </summary>
    public List<string> ModelCompatibility { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this is the default instruction.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the priority (higher = preferred).
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Represents the result of building a prompt.
/// </summary>
public sealed class PromptBuildResult
{
    /// <summary>
    /// Gets or sets whether the build was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the final prompt text.
    /// </summary>
    public string PromptText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of tokens (estimated).
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// Gets or sets validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the sanitization log.
    /// </summary>
    public List<string> SanitizationLog { get; set; } = new();

    /// <summary>
    /// Gets or sets the original prompt before sanitization.
    /// </summary>
    public Prompt? OriginalPrompt { get; set; }

    /// <summary>
    /// Gets or sets build metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Options for prompt injection protection.
/// </summary>
public sealed class InjectionProtectionOptions
{
    /// <summary>
    /// Gets or sets whether to enable injection detection.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to block detected injections.
    /// </summary>
    public bool BlockOnDetection { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to log suspected injections.
    /// </summary>
    public bool LogSuspectedInjections { get; set; } = true;

    /// <summary>
    /// Gets or sets patterns to detect as potential injections.
    /// </summary>
    public List<string> DetectionPatterns { get; set; } = new()
    {
        @"ignore (all )?previous (instructions|prompts|rules)",
        @"forget (everything|all instructions|your role)",
        @"you are now",
        @"disregard (all )?previous",
        @"new instructions:",
        @"system:",
        @"<\|im_start\|>",
        @"<\|im_end\|>",
        @"\[INST\]",
        @"\[/INST\]"
    };

    /// <summary>
    /// Gets or sets the maximum allowed prompt length.
    /// </summary>
    public int MaxPromptLength { get; set; } = 50000;

    /// <summary>
    /// Gets or sets whether to escape special characters.
    /// </summary>
    public bool EscapeSpecialCharacters { get; set; } = true;

    /// <summary>
    /// Gets or sets characters to escape.
    /// </summary>
    public Dictionary<string, string> EscapeMap { get; set; } = new()
    {
        { "<|", "&lt;|" },
        { "|>", "|&gt;" },
        { "```", "\\`\\`\\`" }
    };
}
